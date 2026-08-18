using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namadno.AI.Support.Infrastructure.Persistence.Embeddings;

namespace Namadno.AI.Support.Infrastructure.Persistence.Configurations;

internal sealed class FaqEmbeddingRecordConfiguration : IEntityTypeConfiguration<FaqEmbeddingRecord>
{
    /// <summary>
    /// Must match Embedding:Dimensions in configuration. Changing this requires a new migration.
    /// </summary>
    public const int EmbeddingDimensions = 1024;

    public void Configure(EntityTypeBuilder<FaqEmbeddingRecord> builder)
    {
        builder.ToTable("faq_embeddings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Embedding)
            .HasColumnType($"vector({EmbeddingDimensions})")
            .IsRequired();
        builder.HasIndex(x => x.FaqArticleId).IsUnique();
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
