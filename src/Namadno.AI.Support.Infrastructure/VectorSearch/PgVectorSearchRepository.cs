using Microsoft.EntityFrameworkCore;
using Namadno.AI.Support.Application.Abstractions.Knowledge;
using Namadno.AI.Support.Infrastructure.Persistence;
using Namadno.AI.Support.Infrastructure.Persistence.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Namadno.AI.Support.Infrastructure.VectorSearch;

internal sealed class PgVectorSearchRepository(SupportDbContext db) : IVectorSearchRepository
{
    public async Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        float[] embedding,
        int topK,
        CancellationToken cancellationToken)
    {
        if (embedding.Length == 0 || !await db.FaqEmbeddings.AnyAsync(cancellationToken))
        {
            return [];
        }

        var vector = new Vector(embedding);
        var rows = await db.FaqEmbeddings
            .OrderBy(x => x.Embedding.CosineDistance(vector))
            .Take(topK)
            .ToListAsync(cancellationToken);

        return rows.Select(row =>
        {
            var values = row.Embedding.ToArray();
            var similarity = Cosine(values, embedding);
            return new VectorSearchHit(row.FaqArticleId, similarity);
        }).ToList();
    }

    public async Task UpsertAsync(
        Guid faqArticleId,
        string contentHash,
        float[] embedding,
        string model,
        int dimensions,
        CancellationToken cancellationToken)
    {
        var existing = await db.FaqEmbeddings.FirstOrDefaultAsync(x => x.FaqArticleId == faqArticleId, cancellationToken);
        if (existing is null)
        {
            db.FaqEmbeddings.Add(new FaqEmbeddingRecord
            {
                Id = Guid.NewGuid(),
                FaqArticleId = faqArticleId,
                ContentHash = contentHash,
                Embedding = new Vector(embedding),
                Model = model,
                Dimensions = dimensions,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existing.ContentHash = contentHash;
            existing.Embedding = new Vector(embedding);
            existing.Model = model;
            existing.Dimensions = dimensions;
            existing.CreatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static double Cosine(float[] a, float[] b)
    {
        var n = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom <= 0 ? 0 : dot / denom;
    }
}
