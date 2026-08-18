namespace Namadno.AI.Support.Application.Abstractions.Jobs;

public interface IBackgroundJobDispatcher
{
    void Enqueue(Func<CancellationToken, Task> work);
}

public interface IConversationEventPublisher
{
    Task PublishAsync(string eventType, Guid conversationId, CancellationToken cancellationToken);
}
