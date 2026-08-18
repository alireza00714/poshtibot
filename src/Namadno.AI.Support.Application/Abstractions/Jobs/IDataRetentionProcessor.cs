namespace Namadno.AI.Support.Application.Abstractions.Jobs;

public sealed record DataRetentionResult(
    int ConversationsDeleted,
    int MessagesDeleted,
    int UnansweredDeleted,
    DateTimeOffset ConversationCutoff,
    DateTimeOffset MessageCutoff,
    DateTimeOffset UnansweredCutoff);

public interface IDataRetentionProcessor
{
    Task<DataRetentionResult> PurgeExpiredAsync(CancellationToken cancellationToken);
}
