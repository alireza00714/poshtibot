using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    [Required]
    public string Strategy { get; set; } = AuthenticationStrategies.Development;

    public string DevelopmentUserId { get; set; } = "dev-user";

    public string JwtAuthority { get; set; } = string.Empty;

    public string JwtAudience { get; set; } = string.Empty;

    public string GatewayUserIdHeader { get; set; } = "X-User-Id";

    public string TrustedHeadersSharedSecretHeader { get; set; } = "X-Internal-Auth";

    public string TrustedHeadersSharedSecret { get; set; } = string.Empty;

    public string GatewayPermissionsHeader { get; set; } = "X-Permissions";

    public string GatewayRolesHeader { get; set; } = "X-Roles";
}

public static class AuthenticationStrategies
{
    public const string Development = "Development";
    public const string Jwt = "Jwt";
    public const string Gateway = "Gateway";
    public const string TrustedHeaders = "TrustedHeaders";
}
