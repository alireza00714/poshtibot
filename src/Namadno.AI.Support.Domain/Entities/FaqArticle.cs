using Namadno.AI.Support.Domain.Common;
using Namadno.AI.Support.Domain.Enums;
using Namadno.AI.Support.Domain.Exceptions;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class FaqArticle : AggregateRoot
{
    private FaqArticle()
    {
        Question = string.Empty;
        Answer = string.Empty;
        NormalizedQuestion = string.Empty;
        CreatedBy = string.Empty;
        UpdatedBy = string.Empty;
        Keywords = string.Empty;
    }

    public string Question { get; private set; }

    public string Answer { get; private set; }

    public string NormalizedQuestion { get; private set; }

    public FaqCategory Category { get; private set; }

    public ChatIntent Intent { get; private set; }

    public string Keywords { get; private set; }

    public string? NavigationAction { get; private set; }

    public FaqStatus Status { get; private set; }

    public int Priority { get; private set; }

    public int Version { get; private set; }

    public KnowledgeSourceType SourceType { get; private set; }

    public string CreatedBy { get; private set; }

    public string UpdatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsRetrievable => Status == FaqStatus.Active;

    public static FaqArticle Create(
        string question,
        string answer,
        string normalizedQuestion,
        FaqCategory category,
        ChatIntent intent,
        string keywords,
        string? navigationAction,
        int priority,
        string createdBy,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        return new FaqArticle
        {
            Id = Guid.NewGuid(),
            Question = question.Trim(),
            Answer = answer.Trim(),
            NormalizedQuestion = string.IsNullOrWhiteSpace(normalizedQuestion) ? question.Trim() : normalizedQuestion.Trim(),
            Category = category,
            Intent = intent,
            Keywords = keywords ?? string.Empty,
            NavigationAction = navigationAction,
            Status = FaqStatus.Draft,
            Priority = priority,
            Version = 1,
            SourceType = KnowledgeSourceType.FaqArticle,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateContent(
        string question,
        string answer,
        string normalizedQuestion,
        FaqCategory category,
        ChatIntent intent,
        string keywords,
        string? navigationAction,
        int priority,
        string updatedBy,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Question = question.Trim();
        Answer = answer.Trim();
        NormalizedQuestion = string.IsNullOrWhiteSpace(normalizedQuestion) ? question.Trim() : normalizedQuestion.Trim();
        Category = category;
        Intent = intent;
        Keywords = keywords ?? string.Empty;
        NavigationAction = navigationAction;
        Priority = priority;
        Version++;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }

    public void Activate(string updatedBy, DateTimeOffset now)
    {
        Status = FaqStatus.Active;
        Touch(updatedBy, now);
    }

    public void Disable(string updatedBy, DateTimeOffset now)
    {
        Status = FaqStatus.Disabled;
        Touch(updatedBy, now);
    }

    public void EnsureRetrievable()
    {
        if (!IsRetrievable)
        {
            throw new FaqNotRetrievableException();
        }
    }

    private void Touch(string updatedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        UpdatedBy = updatedBy;
        UpdatedAt = now;
        Version++;
    }
}
