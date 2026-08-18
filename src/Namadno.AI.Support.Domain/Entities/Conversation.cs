using Namadno.AI.Support.Domain.Common;
using Namadno.AI.Support.Domain.Enums;
using Namadno.AI.Support.Domain.Exceptions;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class Conversation : AggregateRoot
{
    private Conversation()
    {
        UserId = string.Empty;
    }

    public string UserId { get; private set; }

    public ConversationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset LastMessageAt { get; private set; }

    public ConversationChannel Channel { get; private set; }

    public string? MetadataJson { get; private set; }

    public static Conversation Start(string userId, ConversationChannel channel, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId.Trim(),
            Status = ConversationStatus.Active,
            Channel = channel,
            CreatedAt = now,
            UpdatedAt = now,
            LastMessageAt = now
        };
    }

    public void EnsureOwnedBy(string userId)
    {
        if (!string.Equals(UserId, userId, StringComparison.Ordinal))
        {
            throw new ConversationAccessDeniedException();
        }
    }

    public bool CanReceiveAiMessages =>
        Status is ConversationStatus.Active or ConversationStatus.HandedOff;

    public Message AddMessage(SenderType senderType, string content, string normalizedContent, DateTimeOffset now)
    {
        if (Status == ConversationStatus.Closed && senderType != SenderType.System)
        {
            throw new ConversationClosedException();
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var message = Message.Create(Id, senderType, content, normalizedContent, now);
        Touch(now);
        return message;
    }

    public void HandOff(DateTimeOffset now)
    {
        EnsureNotClosed();
        Status = ConversationStatus.HandedOff;
        Touch(now);
    }

    public void Resolve(DateTimeOffset now)
    {
        EnsureNotClosed();
        Status = ConversationStatus.Resolved;
        Touch(now);
    }

    public void Close(DateTimeOffset now)
    {
        Status = ConversationStatus.Closed;
        Touch(now);
    }

    private void EnsureNotClosed()
    {
        if (Status == ConversationStatus.Closed)
        {
            throw new ConversationClosedException();
        }
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        LastMessageAt = now;
    }
}
