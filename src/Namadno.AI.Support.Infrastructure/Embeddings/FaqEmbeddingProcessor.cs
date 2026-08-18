using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.Knowledge;
using Namadno.AI.Support.Application.Abstractions.LLM;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Knowledge;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Infrastructure.Embeddings;

internal sealed class FaqEmbeddingProcessor(
    IFaqRepository faqs,
    IEmbeddingModel embeddings,
    IVectorSearchRepository vectors,
    IOptions<EmbeddingOptions> options,
    ILogger<FaqEmbeddingProcessor> logger) : IFaqEmbeddingProcessor
{
    public async Task RebuildAsync(Guid faqId, CancellationToken cancellationToken)
    {
        var faq = await faqs.GetByIdAsync(faqId, cancellationToken);
        if (faq is null || faq.Status != FaqStatus.Active)
        {
            return;
        }

        var payload = $"{faq.NormalizedQuestion}\n{faq.Answer}";
        var vector = await embeddings.EmbedAsync(payload, cancellationToken);
        if (vector is null)
        {
            return;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        await vectors.UpsertAsync(faq.Id, hash, vector, options.Value.Model, options.Value.Dimensions, cancellationToken);
    }

    public async Task RebuildAllActiveAsync(CancellationToken cancellationToken)
    {
        var articles = await faqs.ListActiveAsync(cancellationToken);
        logger.LogInformation("Rebuilding embeddings for {Count} active FAQ articles with model {Model}", articles.Count, options.Value.Model);
        foreach (var article in articles)
        {
            await RebuildAsync(article.Id, cancellationToken);
        }
    }
}
