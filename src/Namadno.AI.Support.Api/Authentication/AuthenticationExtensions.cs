using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;

namespace Namadno.AI.Support.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddSupportAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        if (!auth.Strategy.Equals(AuthenticationStrategies.Jwt, StringComparison.OrdinalIgnoreCase))
        {
            return services;
        }

        if (string.IsNullOrWhiteSpace(auth.JwtAuthority))
        {
            throw new InvalidOperationException("Auth:JwtAuthority is required when Auth:Strategy is Jwt.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = auth.JwtAuthority;
                options.Audience = auth.JwtAudience;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(auth.JwtAudience);
            });
        services.AddAuthorization();
        return services;
    }
}

public sealed class RequireIdentityMiddleware(RequestDelegate next, IOptions<AuthOptions> auth, CopyTexts copy)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsApiPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        var options = auth.Value;
        if (options.Strategy.Equals(AuthenticationStrategies.Development, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (options.Strategy.Equals(AuthenticationStrategies.Jwt, StringComparison.OrdinalIgnoreCase))
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await WriteUnauthorizedAsync(context, copy);
                return;
            }

            await next(context);
            return;
        }

        if (options.Strategy.Equals(AuthenticationStrategies.TrustedHeaders, StringComparison.OrdinalIgnoreCase)
            && !RequestUserContext.SharedSecretMatches(options, context.Request))
        {
            await WriteUnauthorizedAsync(context, copy);
            return;
        }

        if (options.Strategy.Equals(AuthenticationStrategies.Gateway, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(options.TrustedHeadersSharedSecret)
            && !RequestUserContext.SharedSecretMatches(options, context.Request))
        {
            await WriteUnauthorizedAsync(context, copy);
            return;
        }

        var userId = context.Request.Headers[options.GatewayUserIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await WriteUnauthorizedAsync(context, copy);
            return;
        }

        await next(context);
    }

    private static bool IsApiPath(PathString path) =>
        path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    private static Task WriteUnauthorizedAsync(HttpContext context, CopyTexts copy)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new
        {
            error = new { code = ErrorCodes.Unauthorized, message = copy.Errors.Unauthorized },
            traceId = Activity.Current?.Id ?? context.TraceIdentifier
        });
    }
}
