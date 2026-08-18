namespace Namadno.AI.Support.Application.Abstractions;

/// <summary>
/// Trusted caller identity. Never populated from client-supplied UserId fields.
/// </summary>
public interface IUserContext
{
    string UserId { get; }

    IReadOnlyList<string> Roles { get; }

    IReadOnlyList<string> Permissions { get; }

    string CorrelationId { get; }

    string? TenantId { get; }

    bool IsAuthenticated { get; }

    bool HasPermission(string permission);
}
