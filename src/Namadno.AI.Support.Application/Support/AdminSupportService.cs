using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Abstractions.Time;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Support;

public sealed record SupportTicketListItemDto(
    Guid Id,
    string UserId,
    Guid ConversationId,
    string Status,
    string Subject,
    DateTimeOffset CreatedAt);

public sealed record SupportTicketDetailDto(
    Guid Id,
    string UserId,
    Guid ConversationId,
    string Status,
    string Subject,
    string LatestQuestion,
    IReadOnlyList<ChatMessageDto> ConversationMessages);

public sealed class AdminSupportService(
    IUserContext user,
    IClock clock,
    IPersianTextNormalizer normalizer,
    ISupportTicketRepository tickets,
    IConversationRepository conversations,
    IMessageRepository messages,
    IAuditEventRepository audit,
    ISupportUnitOfWork unitOfWork,
    CopyTexts copy)
{
    public async Task<IReadOnlyList<SupportTicketListItemDto>> ListAsync(
        SupportTicketStatus? status,
        CancellationToken cancellationToken)
    {
        Ensure(Permissions.SupportRead);
        var items = await tickets.ListAsync(status, null, cancellationToken);
        return items.Select(t => new SupportTicketListItemDto(
            t.Id, t.UserId, t.ConversationId, t.Status.ToString(), t.Subject, t.CreatedAt)).ToList();
    }

    public async Task<SupportTicketDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Ensure(Permissions.SupportRead);
        var ticket = await tickets.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.TicketNotFound);
        var history = await messages.ListByConversationAsync(ticket.ConversationId, 200, cancellationToken);
        return new SupportTicketDetailDto(
            ticket.Id,
            ticket.UserId,
            ticket.ConversationId,
            ticket.Status.ToString(),
            ticket.Subject,
            ticket.LatestQuestion,
            history.Select(m => new ChatMessageDto(
                m.Id,
                m.SenderType.ToString(),
                m.Content,
                m.CreatedAt)).ToList());
    }

    public async Task ReplyAsync(Guid id, string content, CancellationToken cancellationToken)
    {
        Ensure(Permissions.SupportReply);
        var ticket = await tickets.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.TicketNotFound);
        var conversation = await conversations.GetByIdAsync(ticket.ConversationId, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.ConversationNotFound);
        var now = clock.UtcNow;
        ticket.RegisterAgentReply(user.UserId, now);
        var message = conversation.AddMessage(
            SenderType.SupportAgent,
            content.Trim(),
            normalizer.Normalize(content),
            now);
        await messages.AddAsync(message, cancellationToken);
        await audit.AddAsync(
            AuditEvent.Create("support.agent.replied", user.UserId, "SupportTicket", ticket.Id, null, now),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignAsync(Guid id, string agentId, CancellationToken cancellationToken)
    {
        Ensure(Permissions.SupportAssign);
        var ticket = await tickets.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.TicketNotFound);
        ticket.Assign(agentId, user.UserId, clock.UtcNow);
        await audit.AddAsync(
            AuditEvent.Create("support.ticket.assigned", user.UserId, "SupportTicket", ticket.Id, agentId, clock.UtcNow),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeStatusAsync(Guid id, SupportTicketStatus status, CancellationToken cancellationToken)
    {
        Ensure(Permissions.SupportResolve);
        var ticket = await tickets.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.ConversationNotFound, 404, copy.Errors.TicketNotFound);
        var now = clock.UtcNow;
        switch (status)
        {
            case SupportTicketStatus.InProgress:
                ticket.MarkInProgress(user.UserId, now);
                break;
            case SupportTicketStatus.WaitingForUser:
                ticket.WaitForUser(user.UserId, now);
                break;
            case SupportTicketStatus.Resolved:
                ticket.Resolve(user.UserId, now);
                break;
            case SupportTicketStatus.Closed:
                ticket.Close(user.UserId, now);
                break;
            default:
                throw new AppException(ErrorCodes.ValidationError, 400, copy.Errors.InvalidStatus);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void Ensure(string permission)
    {
        if (!user.HasPermission(permission))
        {
            throw new AppException(ErrorCodes.Forbidden, 403, copy.Errors.Forbidden);
        }
    }
}
