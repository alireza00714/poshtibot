using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.Jobs;
using Namadno.AI.Support.Application.Abstractions.Time;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Infrastructure.Persistence;

namespace Namadno.AI.Support.Infrastructure.Retention;

internal sealed class DataRetentionProcessor(
    SupportDbContext db,
    IClock clock,
    IOptions<DataRetentionOptions> options,
    ILogger<DataRetentionProcessor> logger) : IDataRetentionProcessor
{
    public async Task<DataRetentionResult> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var settings = options.Value;
        var conversationCutoff = now.AddDays(-settings.ConversationDays);
        var messageCutoff = now.AddDays(-settings.MessageDays);
        var unansweredCutoff = now.AddDays(-settings.UnansweredQuestionDays);

        var messagesDeleted = await db.Messages
            .Where(x => x.CreatedAt < messageCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var conversationsDeleted = await db.Conversations
            .Where(x => x.LastMessageAt < conversationCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var unansweredDeleted = await db.UnansweredQuestions
            .Where(x => x.LastSeenAt < unansweredCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var result = new DataRetentionResult(
            conversationsDeleted,
            messagesDeleted,
            unansweredDeleted,
            conversationCutoff,
            messageCutoff,
            unansweredCutoff);

        logger.LogInformation(
            "Data retention purge conversations={Conversations} messages={Messages} unanswered={Unanswered} conversationCutoff={ConversationCutoff:o} messageCutoff={MessageCutoff:o} unansweredCutoff={UnansweredCutoff:o}",
            result.ConversationsDeleted,
            result.MessagesDeleted,
            result.UnansweredDeleted,
            result.ConversationCutoff,
            result.MessageCutoff,
            result.UnansweredCutoff);

        return result;
    }
}

internal sealed class DataRetentionHostedService(
    IServiceScopeFactory scopes,
    ILogger<DataRetentionHostedService> logger) : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _loop = RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();
        if (_loop is not null)
        {
            await _loop.WaitAsync(cancellationToken);
        }
    }

    public void Dispose() => _cts.Dispose();

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        try
        {
            await PurgeOnceAsync(cancellationToken);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await PurgeOnceAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutdown
        }
    }

    private async Task PurgeOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IDataRetentionProcessor>();
            await processor.PurgeExpiredAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Data retention purge failed");
        }
    }
}
