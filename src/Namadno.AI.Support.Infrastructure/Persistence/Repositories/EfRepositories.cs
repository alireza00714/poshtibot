using Microsoft.EntityFrameworkCore;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Infrastructure.Persistence.Repositories;

internal sealed class EfSupportUnitOfWork(SupportDbContext db) : ISupportUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}

internal sealed class ConversationRepository(SupportDbContext db) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Conversations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Conversation>> ListByUserAsync(string userId, CancellationToken cancellationToken) =>
        await db.Conversations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken) =>
        await db.Conversations.AddAsync(conversation, cancellationToken);
}

internal sealed class MessageRepository(SupportDbContext db) : IMessageRepository
{
    public async Task<IReadOnlyList<Message>> ListByConversationAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken)
    {
        var list = await db.Messages
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return list.OrderBy(x => x.CreatedAt).ToList();
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken) =>
        await db.Messages.AddAsync(message, cancellationToken);
}

internal sealed class FaqRepository(SupportDbContext db) : IFaqRepository
{
    public Task<FaqArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.FaqArticles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FaqArticle>> ListAsync(
        FaqStatus? status,
        FaqCategory? category,
        CancellationToken cancellationToken)
    {
        IQueryable<FaqArticle> query = db.FaqArticles;
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (category is not null)
        {
            query = query.Where(x => x.Category == category);
        }

        return await query.OrderByDescending(x => x.Priority).ThenBy(x => x.Question).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FaqArticle>> ListActiveAsync(CancellationToken cancellationToken) =>
        await db.FaqArticles
            .Where(x => x.Status == FaqStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        db.FaqArticles.CountAsync(cancellationToken);

    public async Task AddAsync(FaqArticle article, CancellationToken cancellationToken) =>
        await db.FaqArticles.AddAsync(article, cancellationToken);
}

internal sealed class SupportTicketRepository(SupportDbContext db) : ISupportTicketRepository
{
    public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.SupportTickets.Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SupportTicket?> GetOpenByConversationAsync(Guid conversationId, CancellationToken cancellationToken) =>
        db.SupportTickets
            .Include(x => x.Events)
            .Where(x => x.ConversationId == conversationId
                        && x.Status != SupportTicketStatus.Closed
                        && x.Status != SupportTicketStatus.Resolved)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<SupportTicket>> ListAsync(
        SupportTicketStatus? status,
        string? assignedAgentId,
        CancellationToken cancellationToken)
    {
        IQueryable<SupportTicket> query = db.SupportTickets.Include(x => x.Events);
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(assignedAgentId))
        {
            query = query.Where(x => x.AssignedAgentId == assignedAgentId);
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken) =>
        await db.SupportTickets.AddAsync(ticket, cancellationToken);
}

internal sealed class UnansweredQuestionRepository(SupportDbContext db) : IUnansweredQuestionRepository
{
    public Task<UnansweredQuestion?> GetByNormalizedQuestionAsync(
        string normalizedQuestion,
        CancellationToken cancellationToken) =>
        db.UnansweredQuestions.FirstOrDefaultAsync(x => x.NormalizedQuestion == normalizedQuestion, cancellationToken);

    public Task<UnansweredQuestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.UnansweredQuestions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UnansweredQuestion>> ListAsync(CancellationToken cancellationToken) =>
        await db.UnansweredQuestions.OrderByDescending(x => x.OccurrenceCount).ToListAsync(cancellationToken);

    public async Task AddAsync(UnansweredQuestion question, CancellationToken cancellationToken) =>
        await db.UnansweredQuestions.AddAsync(question, cancellationToken);
}

internal sealed class AuditEventRepository(SupportDbContext db) : IAuditEventRepository
{
    public async Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        await db.AuditEvents.AddAsync(auditEvent, cancellationToken);
}

internal sealed class IdempotencyStore(SupportDbContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> GetAsync(
        string key,
        string userId,
        string operation,
        CancellationToken cancellationToken) =>
        db.IdempotencyRecords.FirstOrDefaultAsync(
            x => x.Key == key && x.UserId == userId && x.Operation == operation,
            cancellationToken);

    public async Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken) =>
        await db.IdempotencyRecords.AddAsync(record, cancellationToken);
}
