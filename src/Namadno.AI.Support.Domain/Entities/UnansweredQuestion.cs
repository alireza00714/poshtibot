using Namadno.AI.Support.Domain.Common;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class UnansweredQuestion : AggregateRoot
{
    private UnansweredQuestion()
    {
        UserId = string.Empty;
        Question = string.Empty;
        NormalizedQuestion = string.Empty;
    }

    public string UserId { get; private set; }

    public Guid ConversationId { get; private set; }

    public string Question { get; private set; }

    public string NormalizedQuestion { get; private set; }

    public ChatIntent DetectedIntent { get; private set; }

    public FaqCategory Category { get; private set; }

    public int OccurrenceCount { get; private set; }

    public UnansweredQuestionStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public static UnansweredQuestion Record(
        string userId,
        Guid conversationId,
        string question,
        string normalizedQuestion,
        ChatIntent detectedIntent,
        FaqCategory category,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        return new UnansweredQuestion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConversationId = conversationId,
            Question = question,
            NormalizedQuestion = string.IsNullOrWhiteSpace(normalizedQuestion) ? question : normalizedQuestion,
            DetectedIntent = detectedIntent,
            Category = category,
            OccurrenceCount = 1,
            Status = UnansweredQuestionStatus.Open,
            CreatedAt = now,
            LastSeenAt = now
        };
    }

    public void RecordOccurrence(DateTimeOffset now)
    {
        OccurrenceCount++;
        LastSeenAt = now;
    }

    public void ChangeStatus(UnansweredQuestionStatus status)
    {
        Status = status;
    }
}
