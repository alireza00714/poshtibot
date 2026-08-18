using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.Analytics;
using Namadno.AI.Support.Application.Abstractions.Integrations;
using Namadno.AI.Support.Application.Abstractions.Jobs;
using Namadno.AI.Support.Application.Abstractions.Knowledge;
using Namadno.AI.Support.Application.Abstractions.LLM;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Time;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Knowledge;
using Namadno.AI.Support.Infrastructure.Analytics;
using Namadno.AI.Support.Infrastructure.BackgroundJobs;
using Namadno.AI.Support.Infrastructure.Embeddings;
using Namadno.AI.Support.Infrastructure.Integrations.Namadno;
using Namadno.AI.Support.Infrastructure.LLM;
using Namadno.AI.Support.Infrastructure.Persistence;
using Namadno.AI.Support.Infrastructure.Persistence.Repositories;
using Namadno.AI.Support.Infrastructure.Retention;
using Namadno.AI.Support.Infrastructure.Seeding;
using Namadno.AI.Support.Infrastructure.Time;
using Namadno.AI.Support.Infrastructure.VectorSearch;
using Npgsql;

namespace Namadno.AI.Support.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IBackgroundJobDispatcher, InProcessBackgroundJobDispatcher>();
        services.AddSingleton<IConversationEventPublisher, NoOpConversationEventPublisher>();
        services.AddScoped<IFaqEmbeddingProcessor, FaqEmbeddingProcessor>();
        services.AddScoped<FaqSeeder>();
        services.AddScoped<ISupportAnalyticsQuery, SupportAnalyticsQuery>();
        services.AddScoped<IDataRetentionProcessor, DataRetentionProcessor>();
        services.AddHostedService<DataRetentionHostedService>();
        services.AddLlmClients(configuration);
        services.AddNamadnoClients(configuration);
        services.AddPersistence(configuration);
        return services;
    }

    public static async Task InitializeInfrastructureAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<FaqSeeder>();
        await seeder.SeedAsync(cancellationToken);

        var embedding = scope.ServiceProvider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
        if (!embedding.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobDispatcher>();
            jobs.Enqueue(async token =>
            {
                await using var jobScope = services.CreateAsyncScope();
                var processor = jobScope.ServiceProvider.GetRequiredService<IFaqEmbeddingProcessor>();
                await processor.RebuildAllActiveAsync(token);
            });
        }
    }

    private static IServiceCollection AddLlmClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("llm", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<LLMOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient("embedding", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        var llmProvider = configuration["LLM:Provider"] ?? "OpenAICompatible";
        if (llmProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IChatModel, MockChatModel>();
        }
        else
        {
            services.AddSingleton<IChatModel, OpenAICompatibleChatModel>();
        }

        var embeddingProvider = configuration["Embedding:Provider"] ?? "OpenAICompatible";
        if (embeddingProvider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmbeddingModel, HashEmbeddingModel>();
        }
        else
        {
            services.AddSingleton<IEmbeddingModel, OpenAICompatibleEmbeddingModel>();
        }

        services.AddSingleton<IChatModelRouter, ChatModelRouter>();
        return services;
    }

    private static IServiceCollection AddNamadnoClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("namadno", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<NamadnoIntegrationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        }).AddStandardResilienceHandler();

        var mode = configuration["ExternalServices:Mode"] ?? ExternalServiceModes.Mock;
        if (mode.Equals(ExternalServiceModes.Http, StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<NamadnoHttpGateway>();
            services.AddScoped<IInvestmentService, HttpInvestmentService>();
            services.AddScoped<INeobankService, HttpNeobankService>();
            services.AddScoped<IInsuranceService, HttpInsuranceService>();
            services.AddScoped<IPaymentService, HttpPaymentService>();
            services.AddScoped<IUserService, HttpUserService>();
            return services;
        }

        services.AddScoped<IInvestmentService, MockInvestmentService>();
        services.AddScoped<INeobankService, MockNeobankService>();
        services.AddScoped<IInsuranceService, MockInsuranceService>();
        services.AddScoped<IPaymentService, MockPaymentService>();
        services.AddScoped<IUserService, MockUserService>();
        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        services.AddSingleton(_ =>
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.UseVector();
            return builder.Build();
        });

        services.AddDbContext<SupportDbContext>((provider, options) =>
        {
            var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(dataSource, npgsql => npgsql.UseVector());
        });

        services.AddScoped<ISupportUnitOfWork, EfSupportUnitOfWork>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IFaqRepository, FaqRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<IUnansweredQuestionRepository, UnansweredQuestionRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IVectorSearchRepository, PgVectorSearchRepository>();
        return services;
    }
}
