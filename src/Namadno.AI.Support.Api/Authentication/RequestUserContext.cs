using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Api.Middleware;
using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Configuration;
using PermissionNames = Namadno.AI.Support.Application.Common.Permissions;

namespace Namadno.AI.Support.Api.Authentication;

public sealed class RequestUserContext : IUserContext
{
    private static readonly string[] DevelopmentPermissions =
    [
        PermissionNames.FaqRead,
        PermissionNames.FaqWrite,
        PermissionNames.FaqActivate,
        PermissionNames.FaqDisable,
        PermissionNames.SupportRead,
        PermissionNames.SupportReply,
        PermissionNames.SupportAssign,
        PermissionNames.SupportResolve,
        PermissionNames.AnalyticsRead
    ];

    private readonly IHttpContextAccessor _accessor;
    private readonly AuthOptions _auth;

    public RequestUserContext(IHttpContextAccessor accessor, IOptions<AuthOptions> auth)
    {
        _accessor = accessor;
        _auth = auth.Value;
    }

    public string UserId => ResolveUserId();

    public IReadOnlyList<string> Roles => ResolveRoles();

    public IReadOnlyList<string> Permissions => ResolvePermissions();

    public string CorrelationId =>
        _accessor.HttpContext?.Items[CorrelationIdMiddleware.HeaderName] as string
        ?? Guid.NewGuid().ToString("N");

    public string? TenantId =>
        _accessor.HttpContext?.User.FindFirst("tenant")?.Value
        ?? _accessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.Ordinal);

    public static bool SharedSecretMatches(AuthOptions auth, HttpRequest request)
    {
        if (string.IsNullOrEmpty(auth.TrustedHeadersSharedSecret))
        {
            return false;
        }

        var provided = request.Headers[auth.TrustedHeadersSharedSecretHeader].FirstOrDefault() ?? string.Empty;
        var expected = Encoding.UTF8.GetBytes(auth.TrustedHeadersSharedSecret);
        var actual = Encoding.UTF8.GetBytes(provided);
        return expected.Length == actual.Length
               && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string ResolveUserId()
    {
        var http = _accessor.HttpContext;
        if (_auth.Strategy.Equals(AuthenticationStrategies.Development, StringComparison.OrdinalIgnoreCase))
        {
            var debug = http?.Request.Headers["X-Debug-User-Id"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(debug) ? _auth.DevelopmentUserId : debug.Trim();
        }

        if (_auth.Strategy.Equals(AuthenticationStrategies.Gateway, StringComparison.OrdinalIgnoreCase)
            || _auth.Strategy.Equals(AuthenticationStrategies.TrustedHeaders, StringComparison.OrdinalIgnoreCase))
        {
            return http?.Request.Headers[_auth.GatewayUserIdHeader].FirstOrDefault()?.Trim() ?? string.Empty;
        }

        return http?.User.FindFirst("sub")?.Value
            ?? http?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;
    }

    private IReadOnlyList<string> ResolveRoles()
    {
        if (_auth.Strategy.Equals(AuthenticationStrategies.Development, StringComparison.OrdinalIgnoreCase))
        {
            return ["developer"];
        }

        var http = _accessor.HttpContext;
        if (http is null)
        {
            return [];
        }

        if (_auth.Strategy.Equals(AuthenticationStrategies.Jwt, StringComparison.OrdinalIgnoreCase))
        {
            return http.User.FindAll(ClaimTypes.Role)
                .Concat(http.User.FindAll("role"))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();
        }

        return SplitHeader(http.Request.Headers[_auth.GatewayRolesHeader].FirstOrDefault());
    }

    private IReadOnlyList<string> ResolvePermissions()
    {
        if (_auth.Strategy.Equals(AuthenticationStrategies.Development, StringComparison.OrdinalIgnoreCase))
        {
            return DevelopmentPermissions;
        }

        var http = _accessor.HttpContext;
        if (http is null)
        {
            return [];
        }

        if (_auth.Strategy.Equals(AuthenticationStrategies.Jwt, StringComparison.OrdinalIgnoreCase))
        {
            return http.User.FindAll("permission")
                .Concat(http.User.FindAll("permissions"))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();
        }

        return SplitHeader(http.Request.Headers[_auth.GatewayPermissionsHeader].FirstOrDefault());
    }

    private static string[] SplitHeader(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
