using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Tools;

public enum ToolRiskLevel
{
    ReadOnly = 0,
    SensitiveRead = 1,
    Write = 2,
    Financial = 3
}

public sealed record ToolExecutionContext(
    string UserId,
    ChatIntent Intent,
    string Question,
    string NormalizedQuestion);

public sealed record ToolExecutionResult(bool Succeeded, string UserMessage, string? ErrorCode);

public interface IChatTool
{
    string Name { get; }

    string Description { get; }

    ToolRiskLevel RiskLevel { get; }

    bool CanHandle(ChatIntent intent);

    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken);
}

public interface IChatToolRegistry
{
    IChatTool? Resolve(ChatIntent intent);
}

public interface IToolAuthorizationService
{
    bool IsAllowed(IChatTool tool, string userId);
}

public sealed class ChatToolRegistry(IEnumerable<IChatTool> tools) : IChatToolRegistry
{
    public IChatTool? Resolve(ChatIntent intent) =>
        tools.FirstOrDefault(t => t.CanHandle(intent));
}

public sealed class ToolAuthorizationService : IToolAuthorizationService
{
    public bool IsAllowed(IChatTool tool, string userId) =>
        !string.IsNullOrWhiteSpace(userId)
        && tool.RiskLevel is ToolRiskLevel.ReadOnly or ToolRiskLevel.SensitiveRead;
}
