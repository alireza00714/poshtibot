using Namadno.AI.Support.Application.Abstractions.Jobs;

namespace Namadno.AI.Support.Infrastructure.BackgroundJobs;

internal sealed class InProcessBackgroundJobDispatcher : IBackgroundJobDispatcher
{
    public void Enqueue(Func<CancellationToken, Task> work)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await work(CancellationToken.None);
            }
            catch
            {
                // Embedding rebuild must not crash the API process.
            }
        });
    }
}

internal sealed class NoOpConversationEventPublisher : IConversationEventPublisher
{
    public Task PublishAsync(string eventType, Guid conversationId, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
