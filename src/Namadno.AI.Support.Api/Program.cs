using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Namadno.AI.Support.Api.Authentication;
using Namadno.AI.Support.Api.Middleware;
using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.DependencyInjection;
using Namadno.AI.Support.Infrastructure.DependencyInjection;
using Namadno.AI.Support.Infrastructure.Persistence;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddSupportCopyFiles(builder.Environment.ContentRootPath, AppContext.BaseDirectory);

builder.Host.UseSerilog((context, _, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Namadno.AI.Support")
        .WriteTo.Console();
});

var maxRequestBodyBytes = builder.Configuration.GetValue("Security:MaxRequestBodyBytes", 32_768);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, RequestUserContext>();
builder.Services.AddSupportAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SupportDbContext>("postgres", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    var rate = builder.Configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>() ?? new RateLimitOptions();
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                error = new { code = ErrorCodes.RateLimited, message = context.HttpContext.RequestServices.GetRequiredService<CopyTexts>().Errors.RateLimited },
                traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
            },
            token);
    };
    options.AddFixedWindowLimiter("chat", limiter =>
    {
        limiter.PermitLimit = rate.PermitLimit;
        limiter.Window = TimeSpan.FromSeconds(rate.WindowSeconds);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();
await app.Services.InitializeInfrastructureAsync(CancellationToken.None);

app.UseExceptionHandler();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnostic, http) =>
    {
        diagnostic.Set(
            "CorrelationId",
            http.Items[CorrelationIdMiddleware.HeaderName] as string ?? http.TraceIdentifier);
        diagnostic.Set("HasUser", http.User.Identity?.IsAuthenticated == true);
    };
    options.GetLevel = (http, _, exception) =>
        exception is not null || http.Response.StatusCode >= 500
            ? Serilog.Events.LogEventLevel.Error
            : Serilog.Events.LogEventLevel.Information;
});
app.UseMiddleware<CorrelationIdMiddleware>();
if (builder.Configuration["Auth:Strategy"]?.Equals(AuthenticationStrategies.Jwt, StringComparison.OrdinalIgnoreCase) == true)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseMiddleware<RequireIdentityMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        });
    }
});

app.Run();

internal static class DatabaseStartup
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is NpgsqlException or TimeoutException)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}

public partial class Program;
