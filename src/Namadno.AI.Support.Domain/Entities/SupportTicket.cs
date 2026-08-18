using Namadno.AI.Support.Domain.Common;
using Namadno.AI.Support.Domain.Enums;
using Namadno.AI.Support.Domain.Exceptions;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class SupportTicket : AggregateRoot
{
    private readonly List<SupportTicketEvent> _events = [];

    private SupportTicket()
    {
        UserId = string.Empty;
        Service = string.Empty;
        Subject = string.Empty;
        LatestQuestion = string.Empty;
    }

    public string UserId { get; private set; }

    public Guid ConversationId { get; private set; }

    public string Service { get; private set; }

    public string Subject { get; private set; }

    public string LatestQuestion { get; private set; }

    public SupportTicketStatus Status { get; private set; }

    public SupportTicketPriority Priority { get; private set; }

    public string? AssignedAgentId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyCollection<SupportTicketEvent> Events => _events;

    public static SupportTicket CreateAfterUserConfirmation(
        string userId,
        Guid conversationId,
        string service,
        string subject,
        string latestQuestion,
        SupportTicketPriority priority,
        string actorId,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(latestQuestion);

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId.Trim(),
            ConversationId = conversationId,
            Service = string.IsNullOrWhiteSpace(service) ? "general" : service.Trim(),
            Subject = string.IsNullOrWhiteSpace(subject) ? latestQuestion.Trim() : subject.Trim(),
            LatestQuestion = latestQuestion.Trim(),
            Status = SupportTicketStatus.New,
            Priority = priority,
            CreatedAt = now,
            UpdatedAt = now
        };

        ticket._events.Add(SupportTicketEvent.Create(ticket.Id, "Created", actorId, null, now));
        return ticket;
    }

    public void Assign(string agentId, string actorId, DateTimeOffset now)
    {
        EnsureOpen();
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        AssignedAgentId = agentId;
        Status = SupportTicketStatus.Assigned;
        Touch(now);
        _events.Add(SupportTicketEvent.Create(Id, "Assigned", actorId, agentId, now));
    }

    public void MarkInProgress(string actorId, DateTimeOffset now)
    {
        EnsureOpen();
        Status = SupportTicketStatus.InProgress;
        Touch(now);
        _events.Add(SupportTicketEvent.Create(Id, "InProgress", actorId, null, now));
    }

    public void WaitForUser(string actorId, DateTimeOffset now)
    {
        EnsureOpen();
        Status = SupportTicketStatus.WaitingForUser;
        Touch(now);
        _events.Add(SupportTicketEvent.Create(Id, "WaitingForUser", actorId, null, now));
    }

    public void RegisterAgentReply(string actorId, DateTimeOffset now)
    {
        EnsureOpen();
        Status = SupportTicketStatus.WaitingForUser;
        Touch(now);
        _events.Add(SupportTicketEvent.Create(Id, "AgentReplied", actorId, null, now));
    }

    public void Resolve(string actorId, DateTimeOffset now)
    {
        EnsureOpen();
        Status = SupportTicketStatus.Resolved;
        ResolvedAt = now;
        Touch(now);
        _events.Add(SupportTicketEvent.Create(Id, "Resolved", actorId, null, now));
    }

    public void Close(string actorId, DateTimeOffset now)
    {
        if (Status == SupportTicketStatus.Closed)
        {
            return;
        }

        Status = SupportTicketStatus.Closed;
        ClosedAt = now;
        Touch(now);
        _events.Add(SupportTicketEvent.Create(Id, "Closed", actorId, null, now));
    }

    public void EnsureOwnedBy(string userId)
    {
        if (!string.Equals(UserId, userId, StringComparison.Ordinal))
        {
            throw new ConversationAccessDeniedException();
        }
    }

    private void EnsureOpen()
    {
        if (Status is SupportTicketStatus.Closed or SupportTicketStatus.Resolved)
        {
            throw new SupportTicketClosedException();
        }
    }

    private void Touch(DateTimeOffset now) => UpdatedAt = now;
}
