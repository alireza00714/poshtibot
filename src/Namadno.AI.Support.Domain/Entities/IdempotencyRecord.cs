using Namadno.AI.Support.Domain.Common;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class IdempotencyRecord : Entity
{
    private IdempotencyRecord()
    {
        Key = string.Empty;
        UserId = string.Empty;
        Operation = string.Empty;
    }

    public string Key { get; private set; }

    public string UserId { get; private set; }

    public string Operation { get; private set; }

    public Guid ResourceId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static IdempotencyRecord Create(
        string key,
        string userId,
        string operation,
        Guid resourceId,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            UserId = userId,
            Operation = operation,
            ResourceId = resourceId,
            CreatedAt = now
        };
    }
}
