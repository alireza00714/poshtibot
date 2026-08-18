using Microsoft.EntityFrameworkCore;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Infrastructure.Persistence.Embeddings;

namespace Namadno.AI.Support.Infrastructure.Persistence;

public sealed class SupportDbContext : DbContext
{
    public SupportDbContext(DbContextOptions<SupportDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<FaqArticle> FaqArticles => Set<FaqArticle>();

    public DbSet<FaqEmbeddingRecord> FaqEmbeddings => Set<FaqEmbeddingRecord>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<SupportTicketEvent> SupportTicketEvents => Set<SupportTicketEvent>();

    public DbSet<UnansweredQuestion> UnansweredQuestions => Set<UnansweredQuestion>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupportDbContext).Assembly);
    }
}
