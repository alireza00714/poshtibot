using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Abstractions.Time;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Support;

public sealed class ConfirmSupportHandoffHandler(
    IUserContext user,
    IClock clock,
    IPersianTextNormalizer normalizer,
    IConversationRepository conversations,
    IMessageRepository messages,
    ISupportTicketRepository tickets,
    IIdempotencyStore idempotency,
    IAuditEventRepository audit,
    ISupportUnitOfWork unitOfWork,
    IOptions<SupportOptions> supportOptions,
    CopyTexts copy)
{
    public async Task<ChatResponseDto> HandleAsync(
        Guid conversationId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!supportOptions.Value.RequireExplicitConfirmation)
        {
            throw new AppException(ErrorCodes.InternalError, 500, copy.Errors.SupportMisconfigured);
        }

        var conversation = await conversations.GetByIdAsync(conversationId, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.ConversationNotFound);
        conversation.EnsureOwnedBy(user.UserId);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingKey = await idempotency.GetAsync(idempotencyKey, user.UserId, "support-confirm", cancellationToken);
            if (existingKey is not null)
            {
                throw new AppException(ErrorCodes.SupportTicketAlreadyExists, 409, copy.Errors.SupportAlreadyRegistered);
            }
        }

        var open = await tickets.GetOpenByConversationAsync(conversationId, cancellationToken);
        if (open is not null)
        {
            throw new AppException(ErrorCodes.SupportTicketAlreadyExists, 409, copy.Errors.SupportAlreadyOpen);
        }

        var history = await messages.ListByConversationAsync(conversationId, 50, cancellationToken);
        var latest = history.LastOrDefault(m => m.SenderType == SenderType.User)?.Content ?? copy.Actions.SupportTicketTitle;
        var now = clock.UtcNow;
        var ticket = SupportTicket.CreateAfterUserConfirmation(
            user.UserId,
            conversation.Id,
            "general",
            copy.Actions.SupportTicketTitle,
            latest,
            SupportTicketPriority.Normal,
            user.UserId,
            now);
        conversation.HandOff(now);
        var confirmation = conversation.AddMessage(
            SenderType.System,
            supportOptions.Value.ConfirmationMessage,
            normalizer.Normalize(supportOptions.Value.ConfirmationMessage),
            now);

        await tickets.AddAsync(ticket, cancellationToken);
        await messages.AddAsync(confirmation, cancellationToken);
        await audit.AddAsync(
            AuditEvent.Create("support.ticket.created", user.UserId, "SupportTicket", ticket.Id, null, now),
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await idempotency.AddAsync(
                IdempotencyRecord.Create(idempotencyKey, user.UserId, "support-confirm", ticket.Id, now),
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatResponseDto(
            conversation.Id,
            new ChatMessageDto(confirmation.Id, "system", confirmation.Content, confirmation.CreatedAt),
            [],
            new ChatSupportDto(false, null),
            ResponseMode.SupportHandoff.ToString(),
            ChatIntent.HumanSupport.ToCode());
    }
}
