using FluentAssertions;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;
using Namadno.AI.Support.Domain.Exceptions;

namespace Namadno.AI.Support.UnitTests.Domain;

public sealed class ConversationRulesTests
{
    [Fact]
    public void Closed_conversation_rejects_user_and_assistant_messages()
    {
        var now = DateTimeOffset.UtcNow;
        var conversation = Conversation.Start("user-1", ConversationChannel.App, now);
        conversation.Close(now);

        var act = () => conversation.AddMessage(SenderType.User, "سلام", "سلام", now);

        act.Should().Throw<ConversationClosedException>();
    }

    [Fact]
    public void Other_user_cannot_access_conversation()
    {
        var conversation = Conversation.Start("user-1", ConversationChannel.App, DateTimeOffset.UtcNow);

        var act = () => conversation.EnsureOwnedBy("user-2");

        act.Should().Throw<ConversationAccessDeniedException>();
    }

    [Fact]
    public void Active_conversation_accepts_user_message()
    {
        var now = DateTimeOffset.UtcNow;
        var conversation = Conversation.Start("user-1", ConversationChannel.App, now);

        var message = conversation.AddMessage(SenderType.User, "چطور صندوق بخرم؟", "چطور صندوق بخرم", now);

        message.SenderType.Should().Be(SenderType.User);
        message.ConversationId.Should().Be(conversation.Id);
        conversation.Status.Should().Be(ConversationStatus.Active);
    }
}

public sealed class SupportTicketRulesTests
{
    [Fact]
    public void Closed_ticket_rejects_agent_reply()
    {
        var now = DateTimeOffset.UtcNow;
        var ticket = SupportTicket.CreateAfterUserConfirmation(
            "user-1",
            Guid.NewGuid(),
            "investment",
            "سوال",
            "وضعیت صدور",
            SupportTicketPriority.Normal,
            "user-1",
            now);
        ticket.Close("agent-1", now);

        var act = () => ticket.RegisterAgentReply("agent-1", now);

        act.Should().Throw<SupportTicketClosedException>();
    }
}

public sealed class FaqRulesTests
{
    [Fact]
    public void Disabled_faq_is_not_retrievable()
    {
        var now = DateTimeOffset.UtcNow;
        var faq = FaqArticle.Create(
            "Namadno چیست",
            "سوپراپلیکیشن نمادنو",
            "namadno چیست",
            FaqCategory.General,
            ChatIntent.GeneralInformation,
            "namadno",
            null,
            10,
            "seed",
            now);
        faq.Activate("seed", now);
        faq.Disable("ops", now);

        faq.IsRetrievable.Should().BeFalse();
        var act = () => faq.EnsureRetrievable();
        act.Should().Throw<FaqNotRetrievableException>();
    }
}
