using Namadno.AI.Support.Application.Abstractions.Integrations;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Tools;

namespace Namadno.AI.Support.Infrastructure.Integrations.Namadno;

internal sealed class MockInvestmentService(CopyTexts copy) : IInvestmentService
{
    public Task<ToolExecutionResult> GetFundOrderStatusAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(new ToolExecutionResult(
            true,
            copy.Tools.FormatMockFundOrderStatus(Math.Abs(userId.GetHashCode()) % 10000),
            null));
}

internal sealed class MockNeobankService(CopyTexts copy) : INeobankService
{
    public Task<ToolExecutionResult> GetTransferStatusAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(new ToolExecutionResult(
            true,
            copy.Tools.MockTransfer,
            null));

    public Task<ToolExecutionResult> GetCardStatusAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(new ToolExecutionResult(
            true,
            copy.Tools.MockCard,
            null));
}

internal sealed class MockInsuranceService(CopyTexts copy) : IInsuranceService
{
    public Task<ToolExecutionResult> GetPolicySummaryAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(new ToolExecutionResult(true, copy.Tools.MockInsurance, null));
}

internal sealed class MockPaymentService(CopyTexts copy) : IPaymentService
{
    public Task<ToolExecutionResult> GetLatestTransactionAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(new ToolExecutionResult(
            true,
            copy.Tools.MockPayment,
            null));
}

internal sealed class MockUserService(CopyTexts copy) : IUserService
{
    public Task<ToolExecutionResult> GetProfileAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(new ToolExecutionResult(true, copy.Tools.MockProfile, null));
}
