using Namadno.AI.Support.Application.Tools;

namespace Namadno.AI.Support.Application.Abstractions.Integrations;

public interface IInvestmentService
{
    Task<ToolExecutionResult> GetFundOrderStatusAsync(string userId, CancellationToken cancellationToken);
}

public interface INeobankService
{
    Task<ToolExecutionResult> GetTransferStatusAsync(string userId, CancellationToken cancellationToken);

    Task<ToolExecutionResult> GetCardStatusAsync(string userId, CancellationToken cancellationToken);
}

public interface IInsuranceService
{
    Task<ToolExecutionResult> GetPolicySummaryAsync(string userId, CancellationToken cancellationToken);
}

public interface IPaymentService
{
    Task<ToolExecutionResult> GetLatestTransactionAsync(string userId, CancellationToken cancellationToken);
}

public interface IUserService
{
    Task<ToolExecutionResult> GetProfileAsync(string userId, CancellationToken cancellationToken);
}
