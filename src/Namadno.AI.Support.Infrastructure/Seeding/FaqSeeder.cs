using Namadno.AI.Support.Application.Abstractions.Jobs;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Knowledge;
using Namadno.AI.Support.Domain.Entities;

namespace Namadno.AI.Support.Infrastructure.Seeding;

internal sealed class FaqSeeder(
    IFaqRepository faqs,
    ISupportUnitOfWork unitOfWork,
    IPersianTextNormalizer normalizer,
    IBackgroundJobDispatcher jobs,
    IFaqEmbeddingProcessor embeddings,
    FaqSeedOptions seed)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await faqs.CountAsync(cancellationToken) > 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in seed.Items)
        {
            var faq = FaqArticle.Create(
                item.Question,
                item.Answer,
                normalizer.Normalize(item.Question),
                item.Category,
                item.Intent,
                item.Keywords,
                item.Navigation,
                item.Priority,
                "seed",
                now);
            faq.Activate("seed", now);
            await faqs.AddAsync(faq, cancellationToken);
            jobs.Enqueue(ct => embeddings.RebuildAsync(faq.Id, ct));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
