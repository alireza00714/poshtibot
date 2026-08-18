using FluentValidation;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Time;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Chat.Orchestration;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Navigation;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Conversations;

public sealed class CreateConversationHandler(
    IUserContext user,
    IClock clock,
    IConversationRepository conversations,
    IMessageRepository messages,
    ISupportUnitOfWork unitOfWork,
    INavigationRegistry navigation,
    IOptions<ChatOptions> chatOptions,
    CopyTexts copy)
{
    public async Task<ConversationDetailDto> HandleAsync(CancellationToken cancellationToken)
    {
        EnsureUser();
        var now = clock.UtcNow;
        var conversation = Conversation.Start(user.UserId, ConversationChannel.App, now);
        var welcome = conversation.AddMessage(
            SenderType.Assistant,
            chatOptions.Value.WelcomeMessage,
            chatOptions.Value.WelcomeMessage,
            now);
        await conversations.AddAsync(conversation, cancellationToken);
        await messages.AddAsync(welcome, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConversationDetailDto(
            conversation.Id,
            conversation.Status.ToString(),
            conversation.CreatedAt,
            conversation.LastMessageAt,
            navigation.WelcomeActions(),
            chatOptions.Value.WelcomeMessage);
    }

    private void EnsureUser()
    {
        if (!user.IsAuthenticated)
        {
            throw new AppException(ErrorCodes.Unauthorized, 401, copy.Errors.Unauthorized);
        }
    }
}

public sealed class GetConversationHandler(
    IUserContext user,
    IConversationRepository conversations,
    CopyTexts copy)
{
    public async Task<ConversationDetailDto> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.ConversationNotFound);
        conversation.EnsureOwnedBy(user.UserId);
        return new ConversationDetailDto(
            conversation.Id,
            conversation.Status.ToString(),
            conversation.CreatedAt,
            conversation.LastMessageAt,
            null,
            null);
    }
}

public sealed class ListConversationsHandler(
    IUserContext user,
    IConversationRepository conversations)
{
    public async Task<IReadOnlyList<ConversationSummaryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var items = await conversations.ListByUserAsync(user.UserId, cancellationToken);
        return items
            .Select(c => new ConversationSummaryDto(c.Id, c.Status.ToString(), c.CreatedAt, c.LastMessageAt))
            .ToList();
    }
}

public sealed class GetMessagesHandler(
    IUserContext user,
    IConversationRepository conversations,
    IMessageRepository messages,
    CopyTexts copy)
{
    public async Task<IReadOnlyList<ChatMessageDto>> HandleAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.ConversationNotFound);
        conversation.EnsureOwnedBy(user.UserId);
        var items = await messages.ListByConversationAsync(conversationId, 200, cancellationToken);
        return items
            .Select(m => new ChatMessageDto(m.Id, ToSender(m.SenderType), m.Content, m.CreatedAt))
            .ToList();
    }

    private static string ToSender(SenderType type) => type switch
    {
        SenderType.User => "user",
        SenderType.Assistant => "assistant",
        SenderType.SupportAgent => "supportAgent",
        _ => "system"
    };
}

public sealed class SendChatMessageHandler(
    IUserContext user,
    IConversationRepository conversations,
    IChatOrchestrator orchestrator,
    CopyTexts copy)
{
    public async Task<ChatResponseDto> HandleAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.ConversationNotFound);
        conversation.EnsureOwnedBy(user.UserId);
        return await orchestrator.ExecuteAsync(conversation, content, cancellationToken);
    }
}

public sealed class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator(IOptions<ChatOptions> options)
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(options.Value.MaxMessageLength);
    }
}
