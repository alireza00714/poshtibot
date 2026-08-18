using Namadno.AI.Support.Domain.Common;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class SupportTicketEvent : Entity
{
    private SupportTicketEvent()
    {
        EventType = string.Empty;
        ActorId = string.Empty;
    }

    public Guid SupportTicketId { get; private set; }

    public string EventType { get; private set; }

    public string ActorId { get; private set; }

    public string? Metadata { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SupportTicketEvent Create(
        Guid supportTicketId,
        string eventType,
        string actorId,
        string? metadata,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        return new SupportTicketEvent
        {
            Id = Guid.NewGuid(),
            SupportTicketId = supportTicketId,
            EventType = eventType,
            ActorId = actorId,
            Metadata = metadata,
            CreatedAt = now
        };
    }
}
