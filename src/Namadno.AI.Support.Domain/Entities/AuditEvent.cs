using Namadno.AI.Support.Domain.Common;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class AuditEvent : Entity
{
    private AuditEvent()
    {
        EventType = string.Empty;
        ActorId = string.Empty;
    }

    public string EventType { get; private set; }

    public string ActorId { get; private set; }

    public string? EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public string? Metadata { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditEvent Create(
        string eventType,
        string actorId,
        string? entityType,
        Guid? entityId,
        string? metadata,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);

        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            ActorId = actorId,
            EntityType = entityType,
            EntityId = entityId,
            Metadata = metadata,
            CreatedAt = now
        };
    }
}
