using Namadno.AI.Support.Application.Abstractions.Integrations;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Tools;

public sealed class GetFundOrderStatusTool(IInvestmentService investment) : IChatTool
{
    public string Name => "GetFundOrderStatus";

    public string Description => "Read-only fund issuance/redemption status for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.SensitiveRead;

    public bool CanHandle(ChatIntent intent) =>
        intent is ChatIntent.FundIssuanceStatus or ChatIntent.FundRedemptionStatus;

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken) =>
        investment.GetFundOrderStatusAsync(context.UserId, cancellationToken);
}

public sealed class GetTransferStatusTool(INeobankService neobank) : IChatTool
{
    public string Name => "GetTransferStatus";

    public string Description => "Read-only transfer status for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.SensitiveRead;

    public bool CanHandle(ChatIntent intent) => intent == ChatIntent.TransferStatus;

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken) =>
        neobank.GetTransferStatusAsync(context.UserId, cancellationToken);
}

public sealed class GetPaymentStatusTool(IPaymentService payments) : IChatTool
{
    public string Name => "GetPaymentStatus";

    public string Description => "Read-only payment status for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.SensitiveRead;

    public bool CanHandle(ChatIntent intent) => intent == ChatIntent.TransactionHistory;

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken) =>
        payments.GetLatestTransactionAsync(context.UserId, cancellationToken);
}

public sealed class GetCardStatusTool(INeobankService neobank) : IChatTool
{
    public string Name => "GetCardStatus";

    public string Description => "Read-only card status for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.SensitiveRead;

    public bool CanHandle(ChatIntent intent) =>
        intent is ChatIntent.CardBlock or ChatIntent.CardPasswordChange or ChatIntent.CardPasswordRecovery;

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken) =>
        neobank.GetCardStatusAsync(context.UserId, cancellationToken);
}

public sealed class GetUserProfileTool(IUserService users) : IChatTool
{
    public string Name => "GetUserProfile";

    public string Description => "Read-only profile for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.SensitiveRead;

    public bool CanHandle(ChatIntent intent) => false;

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken) =>
        users.GetProfileAsync(context.UserId, cancellationToken);
}
