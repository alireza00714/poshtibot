namespace Namadno.AI.Support.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}

public sealed class ConversationClosedException : DomainException
{
    public ConversationClosedException()
        : base("CONVERSATION_CLOSED", "Closed conversation cannot receive messages.")
    {
    }
}

public sealed class ConversationAccessDeniedException : DomainException
{
    public ConversationAccessDeniedException()
        : base("CONVERSATION_ACCESS_DENIED", "User cannot access this conversation.")
    {
    }
}

public sealed class SupportTicketClosedException : DomainException
{
    public SupportTicketClosedException()
        : base("SUPPORT_TICKET_CLOSED", "Closed support ticket cannot receive a reply.")
    {
    }
}

public sealed class FaqNotRetrievableException : DomainException
{
    public FaqNotRetrievableException()
        : base("FAQ_NOT_RETRIEVABLE", "Disabled or draft FAQ cannot be used in production search.")
    {
    }
}
