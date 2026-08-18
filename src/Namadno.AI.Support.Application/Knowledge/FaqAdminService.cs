using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Abstractions.Jobs;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Abstractions.Time;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Knowledge;

public sealed record FaqDto(
    Guid Id,
    string Question,
    string Answer,
    string Category,
    string Intent,
    string Status,
    int Priority,
    int Version,
    string? NavigationAction);

public sealed record UpsertFaqRequest(
    string Question,
    string Answer,
    string Category,
    string Intent,
    string? Keywords,
    string? NavigationAction,
    int Priority);

public sealed class FaqAdminService(
    IUserContext user,
    IClock clock,
    IPersianTextNormalizer normalizer,
    IFaqRepository faqs,
    IAuditEventRepository audit,
    ISupportUnitOfWork unitOfWork,
    IBackgroundJobDispatcher jobs,
    IFaqEmbeddingProcessor embeddings,
    CopyTexts copy)
{
    public async Task<IReadOnlyList<FaqDto>> ListAsync(CancellationToken cancellationToken)
    {
        Ensure(Permissions.FaqRead);
        var items = await faqs.ListAsync(null, null, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<FaqDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Ensure(Permissions.FaqRead);
        var faq = await faqs.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.KnowledgeUnavailable, 404, copy.Errors.ArticleNotFound);
        return ToDto(faq);
    }

    public async Task<FaqDto> CreateAsync(UpsertFaqRequest request, CancellationToken cancellationToken)
    {
        Ensure(Permissions.FaqWrite);
        var now = clock.UtcNow;
        var faq = FaqArticle.Create(
            request.Question,
            request.Answer,
            normalizer.Normalize(request.Question),
            ParseCategory(request.Category),
            ParseIntent(request.Intent),
            request.Keywords ?? string.Empty,
            request.NavigationAction,
            request.Priority,
            user.UserId,
            now);
        await faqs.AddAsync(faq, cancellationToken);
        await audit.AddAsync(AuditEvent.Create("faq.created", user.UserId, "FaqArticle", faq.Id, null, now), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        jobs.Enqueue(ct => embeddings.RebuildAsync(faq.Id, ct));
        return ToDto(faq);
    }

    public async Task<FaqDto> UpdateAsync(Guid id, UpsertFaqRequest request, CancellationToken cancellationToken)
    {
        Ensure(Permissions.FaqWrite);
        var faq = await faqs.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.KnowledgeUnavailable, 404, copy.Errors.ArticleNotFound);
        faq.UpdateContent(
            request.Question,
            request.Answer,
            normalizer.Normalize(request.Question),
            ParseCategory(request.Category),
            ParseIntent(request.Intent),
            request.Keywords ?? string.Empty,
            request.NavigationAction,
            request.Priority,
            user.UserId,
            clock.UtcNow);
        await audit.AddAsync(AuditEvent.Create("faq.updated", user.UserId, "FaqArticle", faq.Id, null, clock.UtcNow), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        jobs.Enqueue(ct => embeddings.RebuildAsync(faq.Id, ct));
        return ToDto(faq);
    }

    public async Task ChangeStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        var faq = await faqs.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.KnowledgeUnavailable, 404, copy.Errors.ArticleNotFound);
        if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            Ensure(Permissions.FaqActivate);
            faq.Activate(user.UserId, clock.UtcNow);
            await audit.AddAsync(AuditEvent.Create("faq.activated", user.UserId, "FaqArticle", faq.Id, null, clock.UtcNow), cancellationToken);
        }
        else if (status.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            Ensure(Permissions.FaqDisable);
            faq.Disable(user.UserId, clock.UtcNow);
            await audit.AddAsync(AuditEvent.Create("faq.disabled", user.UserId, "FaqArticle", faq.Id, null, clock.UtcNow), cancellationToken);
        }
        else
        {
            throw new AppException(ErrorCodes.ValidationError, 400, copy.Errors.InvalidStatus);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        jobs.Enqueue(ct => embeddings.RebuildAsync(faq.Id, ct));
    }

    public async Task DisableAsync(Guid id, CancellationToken cancellationToken) =>
        await ChangeStatusAsync(id, "Disabled", cancellationToken);

    private void Ensure(string permission)
    {
        if (!user.HasPermission(permission))
        {
            throw new AppException(ErrorCodes.Forbidden, 403, copy.Errors.Forbidden);
        }
    }

    private static FaqDto ToDto(FaqArticle faq) => new(
        faq.Id,
        faq.Question,
        faq.Answer,
        faq.Category.ToString(),
        faq.Intent.ToCode(),
        faq.Status.ToString(),
        faq.Priority,
        faq.Version,
        faq.NavigationAction);

    private FaqCategory ParseCategory(string value) =>
        Enum.TryParse<FaqCategory>(value, true, out var category)
            ? category
            : throw new AppException(ErrorCodes.ValidationError, 400, copy.Errors.InvalidCategory);

    private ChatIntent ParseIntent(string value) =>
        ChatIntentCodes.TryParse(value, out var intent)
            ? intent
            : throw new AppException(ErrorCodes.ValidationError, 400, copy.Errors.InvalidIntent);
}

public interface IFaqEmbeddingProcessor
{
    Task RebuildAsync(Guid faqId, CancellationToken cancellationToken);

    Task RebuildAllActiveAsync(CancellationToken cancellationToken);
}

public sealed record UnansweredQuestionDto(
    Guid Id,
    string UserId,
    Guid ConversationId,
    string Question,
    string DetectedIntent,
    string Category,
    int OccurrenceCount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt);

public sealed class UnansweredAdminService(
    IUserContext user,
    IUnansweredQuestionRepository unanswered,
    ISupportUnitOfWork unitOfWork,
    CopyTexts copy)
{
    public async Task<IReadOnlyList<UnansweredQuestionDto>> ListAsync(CancellationToken cancellationToken)
    {
        if (!user.HasPermission(Permissions.FaqRead))
        {
            throw new AppException(ErrorCodes.Forbidden, 403, copy.Errors.Forbidden);
        }

        var items = await unanswered.ListAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<UnansweredQuestionDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!user.HasPermission(Permissions.FaqRead))
        {
            throw new AppException(ErrorCodes.Forbidden, 403, copy.Errors.Forbidden);
        }

        var item = await unanswered.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.KnowledgeUnavailable, 404, copy.Errors.ItemNotFound);
        return ToDto(item);
    }

    public async Task ChangeStatusAsync(Guid id, UnansweredQuestionStatus status, CancellationToken cancellationToken)
    {
        if (!user.HasPermission(Permissions.FaqWrite))
        {
            throw new AppException(ErrorCodes.Forbidden, 403, copy.Errors.Forbidden);
        }

        var item = await unanswered.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException(ErrorCodes.KnowledgeUnavailable, 404, copy.Errors.ItemNotFound);
        item.ChangeStatus(status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static UnansweredQuestionDto ToDto(UnansweredQuestion item) =>
        new(
            item.Id,
            item.UserId,
            item.ConversationId,
            item.Question,
            item.DetectedIntent.ToCode(),
            item.Category.ToString(),
            item.OccurrenceCount,
            item.Status.ToString(),
            item.CreatedAt,
            item.LastSeenAt);
}
