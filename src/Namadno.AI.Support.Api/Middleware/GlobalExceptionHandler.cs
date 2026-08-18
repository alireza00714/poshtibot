using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Domain.Exceptions;

namespace Namadno.AI.Support.Api.Middleware;

public sealed class GlobalExceptionHandler(CopyTexts copy) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, message) = exception switch
        {
            AppException app => (app.StatusCode, app.Code, app.Message),
            ConversationAccessDeniedException => (StatusCodes.Status403Forbidden, ErrorCodes.ConversationAccessDenied, copy.Errors.ConversationAccessDenied),
            ConversationClosedException => (StatusCodes.Status409Conflict, ErrorCodes.ConversationClosed, copy.Errors.ConversationClosed),
            DomainException domain => (StatusCodes.Status400BadRequest, domain.Code, domain.Message),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, copy.Errors.InvalidInput),
            _ => (StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, copy.Errors.Internal)
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                error = new { code, message },
                traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier
            },
            cancellationToken);
        return true;
    }
}

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        await next(context);
    }
}
