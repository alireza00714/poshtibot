namespace Namadno.AI.Support.Application.Abstractions.Analytics;

public sealed record SupportAnalyticsDto(
    int ConversationCount,
    int OpenTicketCount,
    int UnansweredOpenCount,
    int ActiveFaqCount,
    int DisabledFaqCount);

public interface ISupportAnalyticsQuery
{
    Task<SupportAnalyticsDto> GetAsync(CancellationToken cancellationToken);
}
