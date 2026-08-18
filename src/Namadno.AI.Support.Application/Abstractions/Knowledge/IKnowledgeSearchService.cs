using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Abstractions.Knowledge;

public interface IKnowledgeSearchService
{
    Task<IReadOnlyList<KnowledgeHit>> SearchAsync(
        string normalizedQuestion,
        ChatIntent intent,
        CancellationToken cancellationToken);
}

public interface IVectorSearchRepository
{
    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        float[] embedding,
        int topK,
        CancellationToken cancellationToken);

    Task UpsertAsync(
        Guid faqArticleId,
        string contentHash,
        float[] embedding,
        string model,
        int dimensions,
        CancellationToken cancellationToken);
}

public sealed record VectorSearchHit(Guid FaqArticleId, double Similarity);
