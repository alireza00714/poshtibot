namespace Namadno.AI.Support.Application.Common;

/// <summary>
/// Stable API error codes. Do not rename without a versioned contract change.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string ConversationNotFound = "CONVERSATION_NOT_FOUND";
    public const string ConversationAccessDenied = "CONVERSATION_ACCESS_DENIED";
    public const string LlmUnavailable = "LLM_UNAVAILABLE";
    public const string KnowledgeUnavailable = "KNOWLEDGE_UNAVAILABLE";
    public const string ExternalServiceUnavailable = "EXTERNAL_SERVICE_UNAVAILABLE";
    public const string ToolNotAllowed = "TOOL_NOT_ALLOWED";
    public const string ToolExecutionFailed = "TOOL_EXECUTION_FAILED";
    public const string SupportConfirmationRequired = "SUPPORT_CONFIRMATION_REQUIRED";
    public const string SupportTicketAlreadyExists = "SUPPORT_TICKET_ALREADY_EXISTS";
    public const string RateLimited = "RATE_LIMITED";
    public const string ConversationClosed = "CONVERSATION_CLOSED";
    public const string InternalError = "INTERNAL_ERROR";
}
