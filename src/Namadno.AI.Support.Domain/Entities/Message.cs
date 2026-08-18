using Namadno.AI.Support.Domain.Common;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Domain.Entities;

public sealed class Message : Entity
{
    private Message()
    {
        Content = string.Empty;
        NormalizedContent = string.Empty;
    }

    public Guid ConversationId { get; private set; }

    public SenderType SenderType { get; private set; }

    public string Content { get; private set; }

    public string NormalizedContent { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? MetadataJson { get; private set; }

    public string? ResponseMode { get; private set; }

    public string? Intent { get; private set; }

    public double? IntentConfidence { get; private set; }

    public string? PromptVersion { get; private set; }

    public string? ModelName { get; private set; }

    public static Message Create(
        Guid conversationId,
        SenderType senderType,
        string content,
        string normalizedContent,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderType = senderType,
            Content = content,
            NormalizedContent = string.IsNullOrWhiteSpace(normalizedContent) ? content : normalizedContent,
            CreatedAt = now
        };
    }

    public void AttachAiMetadata(
        string? responseMode,
        string? intent,
        double? intentConfidence,
        string? promptVersion,
        string? modelName,
        string? metadataJson)
    {
        ResponseMode = responseMode;
        Intent = intent;
        IntentConfidence = intentConfidence;
        PromptVersion = promptVersion;
        ModelName = modelName;
        MetadataJson = metadataJson;
    }
}
