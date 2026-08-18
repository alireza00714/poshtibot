using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namadno.AI.Support.Domain.Entities;

namespace Namadno.AI.Support.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.LastMessageAt);
        builder.HasMany<Message>().WithOne().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.NormalizedContent).IsRequired();
        builder.Property(x => x.SenderType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.ResponseMode).HasMaxLength(64);
        builder.Property(x => x.Intent).HasMaxLength(64);
        builder.Property(x => x.PromptVersion).HasMaxLength(32);
        builder.Property(x => x.ModelName).HasMaxLength(128);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
    }
}

internal sealed class FaqArticleConfiguration : IEntityTypeConfiguration<FaqArticle>
{
    public void Configure(EntityTypeBuilder<FaqArticle> builder)
    {
        builder.ToTable("faq_articles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Question).IsRequired();
        builder.Property(x => x.Answer).IsRequired();
        builder.Property(x => x.NormalizedQuestion).IsRequired();
        builder.Property(x => x.Keywords).HasMaxLength(2000);
        builder.Property(x => x.NavigationAction).HasMaxLength(128);
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Intent).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(32);
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Intent);
    }
}

internal sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("support_tickets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Service).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.LatestQuestion).IsRequired();
        builder.Property(x => x.AssignedAgentId).HasMaxLength(128);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AssignedAgentId);
        builder.HasIndex(x => x.ConversationId);
        builder.HasMany(x => x.Events)
            .WithOne()
            .HasForeignKey(x => x.SupportTicketId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SupportTicketEventConfiguration : IEntityTypeConfiguration<SupportTicketEvent>
{
    public void Configure(EntityTypeBuilder<SupportTicketEvent> builder)
    {
        builder.ToTable("support_ticket_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Metadata).HasMaxLength(2000);
        builder.HasIndex(x => x.SupportTicketId);
    }
}

internal sealed class UnansweredQuestionConfiguration : IEntityTypeConfiguration<UnansweredQuestion>
{
    public void Configure(EntityTypeBuilder<UnansweredQuestion> builder)
    {
        builder.ToTable("unanswered_questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Question).IsRequired();
        builder.Property(x => x.NormalizedQuestion).IsRequired();
        builder.Property(x => x.DetectedIntent).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.NormalizedQuestion);
    }
}

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(64);
        builder.Property(x => x.Metadata).HasColumnType("jsonb");
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.EntityId);
    }
}

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Operation).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.Key, x.UserId, x.Operation }).IsUnique();
    }
}
