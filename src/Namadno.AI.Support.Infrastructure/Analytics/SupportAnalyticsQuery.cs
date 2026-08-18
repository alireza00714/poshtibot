using Microsoft.EntityFrameworkCore;
using Namadno.AI.Support.Application.Abstractions.Analytics;
using Namadno.AI.Support.Domain.Enums;
using Namadno.AI.Support.Infrastructure.Persistence;

namespace Namadno.AI.Support.Infrastructure.Analytics;

internal sealed class SupportAnalyticsQuery(SupportDbContext db) : ISupportAnalyticsQuery
{
    public async Task<SupportAnalyticsDto> GetAsync(CancellationToken cancellationToken)
    {
        var conversations = await db.Conversations.CountAsync(cancellationToken);
        var openTickets = await db.SupportTickets.CountAsync(
            x => x.Status != SupportTicketStatus.Closed && x.Status != SupportTicketStatus.Resolved,
            cancellationToken);
        var unanswered = await db.UnansweredQuestions.CountAsync(
            x => x.Status == UnansweredQuestionStatus.Open,
            cancellationToken);
        var activeFaqs = await db.FaqArticles.CountAsync(x => x.Status == FaqStatus.Active, cancellationToken);
        var disabledFaqs = await db.FaqArticles.CountAsync(x => x.Status == FaqStatus.Disabled, cancellationToken);
        return new SupportAnalyticsDto(conversations, openTickets, unanswered, activeFaqs, disabledFaqs);
    }
}
