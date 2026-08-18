namespace Namadno.AI.Support.Infrastructure.Persistence.Embeddings;

/// <summary>
/// Persistence-only vector row. Stays out of Domain because it depends on pgvector.
/// </summary>
public sealed class FaqEmbeddingRecord
{
    public Guid Id { get; set; }

    public Guid FaqArticleId { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public Pgvector.Vector Embedding { get; set; } = null!;

    public string Model { get; set; } = string.Empty;

    public int Dimensions { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
