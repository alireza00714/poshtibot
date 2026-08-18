using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Conversations;

namespace Namadno.AI.Support.Api.Controllers;

[ApiController]
[Route("api/v1/chat/conversations")]
public sealed class ChatConversationsController(
    CreateConversationHandler create,
    ListConversationsHandler list,
    GetConversationHandler get,
    GetMessagesHandler messages,
    SendChatMessageHandler send,
    IValidator<SendChatMessageRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConversationDetailDto>> Create(CancellationToken cancellationToken) =>
        Ok(await create.HandleAsync(cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await list.HandleAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetailDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await get.HandleAsync(id, cancellationToken));

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await messages.HandleAsync(id, cancellationToken));

    [HttpPost("{id:guid}/messages")]
    [EnableRateLimiting("chat")]
    public async Task<ActionResult<ChatResponseDto>> Send(
        Guid id,
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await send.HandleAsync(id, request.Content, cancellationToken));
    }

    [HttpPost("{id:guid}/messages/stream")]
    [EnableRateLimiting("chat")]
    public async Task SendStream(
        Guid id,
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var response = await send.HandleAsync(id, request.Content, cancellationToken);
        Response.Headers.ContentType = "text/event-stream";
        await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(response)}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
