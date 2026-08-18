using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.Knowledge;
using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Analytics;
using Namadno.AI.Support.Application.Chat.Generation;
using Namadno.AI.Support.Application.Chat.Orchestration;
using Namadno.AI.Support.Application.Chat.Policy;
using Namadno.AI.Support.Application.Chat.Validation;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Conversations;
using Namadno.AI.Support.Application.Intent;
using Namadno.AI.Support.Application.Knowledge;
using Namadno.AI.Support.Application.Navigation;
using Namadno.AI.Support.Application.Support;
using Namadno.AI.Support.Application.Text;
using Namadno.AI.Support.Application.Tools;

namespace Namadno.AI.Support.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSupportOptions(configuration);
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
        services.AddSingleton<IPersianTextNormalizer, PersianTextNormalizer>();
        services.AddSingleton<IIntentClassifier, HeuristicIntentClassifier>();
        services.AddSingleton<IPolicyEvaluator, PolicyEvaluator>();
        services.AddSingleton<INavigationRegistry, NavigationRegistry>();
        services.AddSingleton<IResponseValidator, ResponseValidator>();
        services.AddSingleton<IToolAuthorizationService, ToolAuthorizationService>();
        services.AddScoped<IChatToolRegistry, ChatToolRegistry>();
        services.AddScoped<IChatTool, GetFundOrderStatusTool>();
        services.AddScoped<IChatTool, GetTransferStatusTool>();
        services.AddScoped<IChatTool, GetPaymentStatusTool>();
        services.AddScoped<IChatTool, GetCardStatusTool>();
        services.AddScoped<IChatTool, GetUserProfileTool>();
        services.AddScoped<IKnowledgeSearchService, HybridKnowledgeSearcher>();
        services.AddScoped<GroundedRagGenerator>();
        services.AddScoped<ConversationalGenerator>();
        services.AddScoped<IChatOrchestrator, ChatOrchestrator>();
        services.AddScoped<CreateConversationHandler>();
        services.AddScoped<GetConversationHandler>();
        services.AddScoped<ListConversationsHandler>();
        services.AddScoped<GetMessagesHandler>();
        services.AddScoped<SendChatMessageHandler>();
        services.AddScoped<ConfirmSupportHandoffHandler>();
        services.AddScoped<AdminSupportService>();
        services.AddScoped<FaqAdminService>();
        services.AddScoped<UnansweredAdminService>();
        services.AddScoped<SupportAnalyticsService>();
        return services;
    }

    public static IServiceCollection AddSupportOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        BindAndValidate<CopyTexts>(services, configuration, CopyTexts.SectionName)
            .Validate(copy => copy.HasRequiredText(), "Copy JSON is missing one or more required strings.");
        BindAndValidate<CopyLexicon>(services, configuration, CopyLexicon.SectionName);
        BindAndValidate<FaqSeedOptions>(services, configuration, FaqSeedOptions.SectionName);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<CopyTexts>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<CopyLexicon>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<FaqSeedOptions>>().Value);

        BindAndValidate<LLMOptions>(services, configuration, LLMOptions.SectionName);
        BindAndValidate<EmbeddingOptions>(services, configuration, EmbeddingOptions.SectionName);
        BindAndValidate<RagOptions>(services, configuration, RagOptions.SectionName);
        BindAndValidate<ChatOptions>(services, configuration, ChatOptions.SectionName)
            .PostConfigure<CopyTexts>((chat, copy) =>
            {
                if (string.IsNullOrWhiteSpace(chat.WelcomeMessage))
                {
                    chat.WelcomeMessage = copy.Chat.Welcome;
                }
            });
        BindAndValidate<SupportOptions>(services, configuration, SupportOptions.SectionName)
            .PostConfigure<CopyTexts>((support, copy) =>
            {
                if (string.IsNullOrWhiteSpace(support.HandoffMessage))
                {
                    support.HandoffMessage = copy.Chat.Handoff;
                }

                if (string.IsNullOrWhiteSpace(support.ConfirmationMessage))
                {
                    support.ConfirmationMessage = copy.Chat.Confirmation;
                }
            });
        BindAndValidate<NamadnoIntegrationOptions>(services, configuration, NamadnoIntegrationOptions.SectionName);
        BindAndValidate<SecurityOptions>(services, configuration, SecurityOptions.SectionName);
        BindAndValidate<RateLimitOptions>(services, configuration, RateLimitOptions.SectionName);
        BindAndValidate<DataRetentionOptions>(services, configuration, DataRetentionOptions.SectionName);
        BindAndValidate<NavigationOptions>(services, configuration, NavigationOptions.SectionName)
            .PostConfigure<CopyTexts>((navigation, copy) =>
            {
                foreach (var route in navigation.Routes)
                {
                    if (string.IsNullOrWhiteSpace(route.Label))
                    {
                        route.Label = copy.Actions.LabelForRoute(route.Key);
                    }
                }
            });
        BindAndValidate<ExternalServicesOptions>(services, configuration, ExternalServicesOptions.SectionName);
        BindAndValidate<AuthOptions>(services, configuration, AuthOptions.SectionName);
        return services;
    }

    private static OptionsBuilder<TOptions> BindAndValidate<TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        return services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
