using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Abstractions.Persistence;

public interface ISupportUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conversation>> ListByUserAsync(string userId, CancellationToken cancellationToken);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);
}

public interface IMessageRepository
{
    Task<IReadOnlyList<Message>> ListByConversationAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken);

    Task AddAsync(Message message, CancellationToken cancellationToken);
}

public interface IFaqRepository
{
    Task<FaqArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<FaqArticle>> ListAsync(
        FaqStatus? status,
        FaqCategory? category,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FaqArticle>> ListActiveAsync(CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task AddAsync(FaqArticle article, CancellationToken cancellationToken);
}

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SupportTicket?> GetOpenByConversationAsync(Guid conversationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SupportTicket>> ListAsync(
        SupportTicketStatus? status,
        string? assignedAgentId,
        CancellationToken cancellationToken);

    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken);
}

public interface IUnansweredQuestionRepository
{
    Task<UnansweredQuestion?> GetByNormalizedQuestionAsync(string normalizedQuestion, CancellationToken cancellationToken);

    Task<UnansweredQuestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UnansweredQuestion>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(UnansweredQuestion question, CancellationToken cancellationToken);
}

public interface IAuditEventRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key, string userId, string operation, CancellationToken cancellationToken);

    Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken);
}
