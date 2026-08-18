using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.Knowledge;
using Namadno.AI.Support.Application.Abstractions.LLM;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Intent;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Knowledge;

public sealed class HybridKnowledgeSearcher(
    IFaqRepository faqs,
    IVectorSearchRepository vectors,
    IEmbeddingModel embeddings,
    IPersianTextNormalizer normalizer,
    IOptions<RagOptions> ragOptions) : IKnowledgeSearchService
{
    public async Task<IReadOnlyList<KnowledgeHit>> SearchAsync(
        string normalizedQuestion,
        ChatIntent intent,
        CancellationToken cancellationToken)
    {
        var options = ragOptions.Value;
        var articles = await faqs.ListActiveAsync(cancellationToken);
        if (articles.Count == 0)
        {
            return [];
        }

        var vectorScores = new Dictionary<Guid, double>();
        var embedding = await embeddings.EmbedAsync(normalizedQuestion, cancellationToken);
        if (embedding is { Length: > 0 })
        {
            var hits = await vectors.SearchAsync(embedding, options.TopK, cancellationToken);
            foreach (var hit in hits)
            {
                vectorScores[hit.FaqArticleId] = hit.Similarity;
            }
        }

        var ranked = articles
            .Select(article =>
            {
                var keyword = KeywordScore(normalizedQuestion, article.NormalizedQuestion, article.Keywords);
                var intentScore = article.Intent == intent && intent != ChatIntent.Unknown ? 1.0 : 0.0;
                var categoryScore = HeuristicIntentClassifier.CategoryOf(article.Intent)
                    .Equals(HeuristicIntentClassifier.CategoryOf(intent), StringComparison.Ordinal)
                    ? 1.0
                    : 0.0;
                var priority = Math.Clamp(article.Priority / 100d, 0, 1);
                vectorScores.TryGetValue(article.Id, out var vec);
                var score =
                    (options.VectorWeight * vec) +
                    (options.KeywordWeight * keyword) +
                    (options.IntentWeight * intentScore) +
                    (options.CategoryWeight * categoryScore) +
                    (options.PriorityWeight * priority);
                var exact = keyword >= 0.99;
                return new KnowledgeHit(
                    article.Id,
                    article.Question,
                    article.Answer,
                    article.NavigationAction,
                    article.Intent,
                    score,
                    exact);
            })
            .Where(hit =>
                hit.ExactMatch
                || hit.Score >= options.SimilarityThreshold * 0.5
                || (hit.Intent == intent && intent != ChatIntent.Unknown))
            .OrderByDescending(hit => hit.ExactMatch)
            .ThenByDescending(hit => hit.Score)
            .Take(options.TopK)
            .ToList();

        return ranked;
    }

    private double KeywordScore(string question, string faqNormalized, string keywords)
    {
        if (question.Length == 0)
        {
            return 0;
        }

        if (question.Equals(faqNormalized, StringComparison.Ordinal) ||
            faqNormalized.Contains(question, StringComparison.Ordinal) ||
            question.Contains(faqNormalized, StringComparison.Ordinal))
        {
            return 1.0;
        }

        var qTokens = Tokens(question);
        var fTokens = Tokens(faqNormalized + " " + normalizer.Normalize(keywords));
        if (qTokens.Count == 0 || fTokens.Count == 0)
        {
            return 0;
        }

        var overlap = qTokens.Intersect(fTokens, StringComparer.Ordinal).Count();
        return (double)overlap / Math.Max(qTokens.Count, 1);
    }

    private static HashSet<string> Tokens(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 1)
            .ToHashSet(StringComparer.Ordinal);
}
