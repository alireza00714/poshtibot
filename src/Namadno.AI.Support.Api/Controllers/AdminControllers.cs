using Microsoft.AspNetCore.Mvc;
using Namadno.AI.Support.Application.Analytics;
using Namadno.AI.Support.Application.Knowledge;
using Namadno.AI.Support.Application.Support;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Api.Controllers;

[ApiController]
[Route("api/v1/admin/faqs")]
public sealed class AdminFaqsController(FaqAdminService faqs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FaqDto>>> List(CancellationToken cancellationToken) =>
        Ok(await faqs.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FaqDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await faqs.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<FaqDto>> Create([FromBody] UpsertFaqRequest request, CancellationToken cancellationToken) =>
        Ok(await faqs.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FaqDto>> Update(Guid id, [FromBody] UpsertFaqRequest request, CancellationToken cancellationToken) =>
        Ok(await faqs.UpdateAsync(id, request, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, [FromBody] StatusPatch request, CancellationToken cancellationToken)
    {
        await faqs.ChangeStatusAsync(id, request.Status, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await faqs.DisableAsync(id, cancellationToken);
        return NoContent();
    }
}

public sealed record StatusPatch(string Status);

[ApiController]
[Route("api/v1/admin/support/tickets")]
public sealed class AdminSupportController(AdminSupportService support) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SupportTicketStatus? status, CancellationToken cancellationToken) =>
        Ok(await support.ListAsync(status, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await support.GetAsync(id, cancellationToken));

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] AgentReplyRequest request, CancellationToken cancellationToken)
    {
        await support.ReplyAsync(id, request.Content, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, [FromBody] TicketStatusPatch request, CancellationToken cancellationToken)
    {
        await support.ChangeStatusAsync(id, request.Status, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/assignment")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignmentPatch request, CancellationToken cancellationToken)
    {
        await support.AssignAsync(id, request.AgentId, cancellationToken);
        return NoContent();
    }
}

public sealed record AgentReplyRequest(string Content);

public sealed record TicketStatusPatch(SupportTicketStatus Status);

public sealed record AssignmentPatch(string AgentId);

[ApiController]
[Route("api/v1/admin/unanswered")]
public sealed class AdminUnansweredController(UnansweredAdminService unanswered) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await unanswered.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await unanswered.GetAsync(id, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, [FromBody] UnansweredStatusPatch request, CancellationToken cancellationToken)
    {
        await unanswered.ChangeStatusAsync(id, request.Status, cancellationToken);
        return NoContent();
    }
}

public sealed record UnansweredStatusPatch(UnansweredQuestionStatus Status);

[ApiController]
[Route("api/v1/admin/analytics")]
public sealed class AdminAnalyticsController(SupportAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await analytics.GetAsync(cancellationToken));
}
