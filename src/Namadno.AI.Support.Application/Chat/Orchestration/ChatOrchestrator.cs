using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Abstractions.Knowledge;
using Namadno.AI.Support.Application.Abstractions.Persistence;
using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Abstractions.Time;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Chat.Generation;
using Namadno.AI.Support.Application.Chat.Policy;
using Namadno.AI.Support.Application.Chat.Validation;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Intent;
using Namadno.AI.Support.Application.Navigation;
using Namadno.AI.Support.Application.Tools;
using Namadno.AI.Support.Domain.Entities;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Chat.Orchestration;

public interface IChatOrchestrator
{
    Task<ChatResponseDto> ExecuteAsync(Conversation conversation, string content, CancellationToken cancellationToken);
}

public sealed class ChatOrchestrator(
    IUserContext user,
    IClock clock,
    IPersianTextNormalizer normalizer,
    IIntentClassifier intents,
    IPolicyEvaluator policy,
    IKnowledgeSearchService knowledge,
    IChatToolRegistry tools,
    IToolAuthorizationService toolAuth,
    INavigationRegistry navigation,
    IResponseValidator validator,
    GroundedRagGenerator rag,
    ConversationalGenerator conversational,
    IUnansweredQuestionRepository unanswered,
    IMessageRepository messages,
    ISupportUnitOfWork unitOfWork,
    IOptions<ChatOptions> chatOptions,
    IOptions<RagOptions> ragOptions,
    IOptions<SupportOptions> supportOptions,
    CopyTexts copy,
    ILogger<ChatOrchestrator> logger) : IChatOrchestrator
{
    public async Task<ChatResponseDto> ExecuteAsync(
        Conversation conversation,
        string content,
        CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated)
        {
            throw new AppException(ErrorCodes.Unauthorized, 401, copy.Errors.Unauthorized);
        }

        conversation.EnsureOwnedBy(user.UserId);
        if (string.IsNullOrWhiteSpace(content) || content.Length > chatOptions.Value.MaxMessageLength)
        {
            throw new AppException(ErrorCodes.ValidationError, 400, copy.Errors.InvalidMessage);
        }

        var now = clock.UtcNow;
        var normalized = normalizer.Normalize(content);
        var userMessage = conversation.AddMessage(SenderType.User, content.Trim(), normalized, now);
        await messages.AddAsync(userMessage, cancellationToken);

        var intent = intents.Classify(content, normalized);
        var decision = policy.Evaluate(content, normalized, intent);
        var draft = await RouteAsync(conversation, content, normalized, intent, decision, cancellationToken);
        if (!validator.IsSafe(draft.Text, draft.Mode))
        {
            draft = SafeFallback(supportOptions.Value.HandoffMessage, "UNSAFE_RESPONSE");
        }

        var assistant = conversation.AddMessage(SenderType.Assistant, draft.Text, normalizer.Normalize(draft.Text), now);
        assistant.AttachAiMetadata(
            draft.Mode.ToString(),
            intent.Intent.ToCode(),
            intent.Confidence,
            draft.PromptVersion ?? GroundedRagGenerator.PromptVersion,
            draft.ModelName,
            null);
        await messages.AddAsync(assistant, cancellationToken);

        if (draft.Mode is ResponseMode.SafeFallback or ResponseMode.SupportHandoff)
        {
            await RecordUnansweredAsync(conversation, content, normalized, intent, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        ChatTelemetry.Record(intent.Intent.ToCode(), draft.Mode.ToString());
        logger.LogInformation(
            "Chat response conversation={ConversationId} intent={Intent} mode={Mode}",
            conversation.Id,
            intent.Intent.ToCode(),
            draft.Mode);

        return new ChatResponseDto(
            conversation.Id,
            new ChatMessageDto(assistant.Id, "assistant", assistant.Content, assistant.CreatedAt),
            draft.Actions,
            draft.Support,
            draft.Mode.ToString(),
            intent.Intent.ToCode());
    }

    private async Task<Draft> RouteAsync(
        Conversation conversation,
        string content,
        string normalized,
        IntentResult intent,
        PolicyDecision decision,
        CancellationToken cancellationToken)
    {
        return decision.Kind switch
        {
            PolicyDecisionKind.RefusePromptInjection => new Draft(
                copy.Chat.PromptInjectionRefuse,
                ResponseMode.SafeFallback,
                [],
                new ChatSupportDto(false, null)),
            PolicyDecisionKind.RefuseFinancialAdvice => AdviceDraft(),
            PolicyDecisionKind.RefuseUnauthorizedAction => new Draft(
                copy.Chat.UnauthorizedActionRefuse,
                ResponseMode.SafeFallback,
                NavigationFor(intent.Intent),
                new ChatSupportDto(true, "POLICY")),
            PolicyDecisionKind.OfferSupport => SupportDraft(),
            PolicyDecisionKind.RequireTool => await ToolDraftAsync(intent, content, normalized, cancellationToken),
            _ when HeuristicIntentClassifier.IsConversational(intent.Intent) =>
                await ConversationalDraftAsync(conversation, content, intent, cancellationToken),
            _ => await KnowledgeDraftAsync(conversation, content, normalized, intent, cancellationToken)
        };
    }

    private async Task<Draft> KnowledgeDraftAsync(
        Conversation conversation,
        string content,
        string normalized,
        IntentResult intent,
        CancellationToken cancellationToken)
    {
        var hits = await knowledge.SearchAsync(normalized, intent.Intent, cancellationToken);
        var top = hits.FirstOrDefault();
        var nav = NavigationFor(intent.Intent);
        if (top is not null)
        {
            var fromFaq = navigation.ForIntentKey(top.NavigationKey);
            if (fromFaq is not null && nav.All(a => a.Route != fromFaq.Route))
            {
                nav = [fromFaq, .. nav];
            }
        }

        if (top is not null && (top.ExactMatch || (ragOptions.Value.UseDirectFaqFallback && top.Score >= ragOptions.Value.DirectFaqExactMatchThreshold)))
        {
            return new Draft(top.Answer, ResponseMode.DirectFaq, nav, new ChatSupportDto(false, null));
        }

        if (top is not null && (top.Score >= ragOptions.Value.SimilarityThreshold || (top.Intent == intent.Intent && intent.Intent != ChatIntent.Unknown)))
        {
            var history = await messages.ListByConversationAsync(
                conversation.Id,
                chatOptions.Value.MaxConversationContextMessages,
                cancellationToken);
            var generated = await rag.TryGenerateAsync(
                content,
                hits,
                history.Select(m => new ChatMessageDto(m.Id, m.SenderType.ToString(), m.Content, m.CreatedAt)).ToList(),
                cancellationToken);
            if (generated is not null && validator.IsSafe(generated.Content, ResponseMode.RagGenerated))
            {
                return new Draft(
                    generated.Content.Trim(),
                    ResponseMode.RagGenerated,
                    nav,
                    new ChatSupportDto(false, null),
                    generated.Model);
            }

            var mode = top.Intent == intent.Intent ? ResponseMode.DirectFaq : ResponseMode.RagGenerated;
            return new Draft(top.Answer, mode, nav, new ChatSupportDto(false, null));
        }

        if (IsPureNavigation(intent.Intent) && nav.Count > 0)
        {
            return new Draft(copy.Chat.NavigationHint, ResponseMode.Navigation, nav, new ChatSupportDto(false, null));
        }

        return SafeFallback(supportOptions.Value.HandoffMessage, "LOW_CONFIDENCE");
    }

    private async Task<Draft> ConversationalDraftAsync(
        Conversation conversation,
        string content,
        IntentResult intent,
        CancellationToken cancellationToken)
    {
        var history = await messages.ListByConversationAsync(
            conversation.Id,
            chatOptions.Value.MaxConversationContextMessages,
            cancellationToken);
        var generated = await conversational.GenerateAsync(
            intent.Intent,
            content,
            history.Select(m => new ChatMessageDto(m.Id, m.SenderType.ToString(), m.Content, m.CreatedAt)).ToList(),
            cancellationToken);
        var text = generated.Text.Trim();
        if (!validator.IsSafe(text, ResponseMode.Conversational))
        {
            text = conversational.CannedReply(intent.Intent, content);
        }

        return new Draft(
            text,
            ResponseMode.Conversational,
            intent.Intent == ChatIntent.Greeting ? navigation.WelcomeActions() : [],
            new ChatSupportDto(false, null),
            generated.Model,
            ConversationalGenerator.PromptVersion);
    }

    private async Task<Draft> ToolDraftAsync(
        IntentResult intent,
        string content,
        string normalized,
        CancellationToken cancellationToken)
    {
        var tool = tools.Resolve(intent.Intent);
        if (tool is null || !toolAuth.IsAllowed(tool, user.UserId))
        {
            return SafeFallback(
                copy.Chat.ToolNotAllowed,
                "TOOL_NOT_ALLOWED");
        }

        var result = await tool.ExecuteAsync(
            new ToolExecutionContext(user.UserId, intent.Intent, content, normalized),
            cancellationToken);
        if (!result.Succeeded)
        {
            return SafeFallback(
                copy.Chat.ExternalUnavailable,
                "EXTERNAL_SERVICE_UNAVAILABLE");
        }

        return new Draft(result.UserMessage, ResponseMode.ToolResult, [], new ChatSupportDto(false, null));
    }

    private Draft AdviceDraft() => new(
        copy.Chat.FinancialAdviceRefuse,
        ResponseMode.SafeFallback,
        navigation.TryGet("investment", out var action) ? [action] : [],
        new ChatSupportDto(false, null));

    private Draft SupportDraft() => new(
        supportOptions.Value.HandoffMessage,
        ResponseMode.SupportHandoff,
        SupportActions(),
        new ChatSupportDto(true, "USER_REQUEST"));

    private Draft SafeFallback(string text, string reason) => new(
        text,
        reason == "USER_REQUEST" ? ResponseMode.SupportHandoff : ResponseMode.SafeFallback,
        SupportActions(),
        new ChatSupportDto(true, reason));

    private IReadOnlyList<ChatActionDto> SupportActions() =>
    [
        new("support", copy.Actions.ConnectSupport),
        new("support", copy.Actions.CreateSupportRequest)
    ];

    private IReadOnlyList<ChatActionDto> NavigationFor(ChatIntent intent)
    {
        var key = intent switch
        {
            ChatIntent.InvestmentStart or ChatIntent.InvestmentMethods or ChatIntent.FundSelection
                or ChatIntent.FundIssuance or ChatIntent.NavigateInvestment => "investment",
            ChatIntent.NeobankInformation or ChatIntent.AccountOpening or ChatIntent.MoneyTransfer
                or ChatIntent.CardToCard or ChatIntent.NavigateNeobank => "neobank",
            ChatIntent.NavigateInsurance => "insurance",
            ChatIntent.NavigateLeasing => "leasing",
            ChatIntent.NavigatePublicServices => "public-services",
            _ => null
        };
        return navigation.ForIntentKey(key) is { } action ? [action] : [];
    }

    private static bool IsPureNavigation(ChatIntent intent) => intent is
        ChatIntent.NavigateInvestment or ChatIntent.NavigateNeobank or ChatIntent.NavigateInsurance
        or ChatIntent.NavigateLeasing or ChatIntent.NavigatePublicServices;

    private async Task RecordUnansweredAsync(
        Conversation conversation,
        string content,
        string normalized,
        IntentResult intent,
        CancellationToken cancellationToken)
    {
        var existing = await unanswered.GetByNormalizedQuestionAsync(normalized, cancellationToken);
        if (existing is null)
        {
            var item = UnansweredQuestion.Record(
                user.UserId,
                conversation.Id,
                content,
                normalized,
                intent.Intent,
                Enum.TryParse<FaqCategory>(intent.Category, out var cat) ? cat : FaqCategory.General,
                clock.UtcNow);
            await unanswered.AddAsync(item, cancellationToken);
        }
        else
        {
            existing.RecordOccurrence(clock.UtcNow);
        }
    }

    private sealed record Draft(
        string Text,
        ResponseMode Mode,
        IReadOnlyList<ChatActionDto> Actions,
        ChatSupportDto Support,
        string? ModelName = null,
        string? PromptVersion = null);
}
