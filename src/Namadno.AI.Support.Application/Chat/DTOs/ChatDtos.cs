using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Chat.DTOs;

public sealed record ChatActionDto(string Type, string Label, string? Route = null);

public sealed record ChatSupportDto(bool Available, string? Reason);

public sealed record ChatMessageDto(
    Guid Id,
    string SenderType,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record ChatResponseDto(
    Guid ConversationId,
    ChatMessageDto Message,
    IReadOnlyList<ChatActionDto> Actions,
    ChatSupportDto Support,
    string ResponseMode,
    string? Intent);

public sealed record ConversationSummaryDto(
    Guid Id,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt);

public sealed record ConversationDetailDto(
    Guid Id,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<ChatActionDto>? WelcomeActions,
    string? WelcomeMessage);

public sealed record SendChatMessageRequest(string Content);

public sealed record CreateConversationRequest(string? Channel);

public sealed record IntentResult(ChatIntent Intent, double Confidence, string Category);

public sealed record KnowledgeHit(
    Guid FaqId,
    string Question,
    string Answer,
    string? NavigationKey,
    ChatIntent Intent,
    double Score,
    bool ExactMatch);
