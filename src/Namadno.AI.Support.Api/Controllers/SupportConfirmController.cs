using Microsoft.AspNetCore.Mvc;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Support;

namespace Namadno.AI.Support.Api.Controllers;

[ApiController]
[Route("api/v1/chat/conversations/{conversationId:guid}/support")]
public sealed class SupportConfirmController(ConfirmSupportHandoffHandler handler) : ControllerBase
{
    [HttpPost("confirm")]
    public async Task<ActionResult<ChatResponseDto>> Confirm(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var key = Request.Headers["Idempotency-Key"].FirstOrDefault();
        return Ok(await handler.HandleAsync(conversationId, key, cancellationToken));
    }
}
