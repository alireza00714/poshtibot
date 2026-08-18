using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Chat.Policy;

public enum PolicyDecisionKind
{
    Continue,
    RefuseFinancialAdvice,
    RefusePromptInjection,
    RefuseUnauthorizedAction,
    RequireTool,
    OfferSupport
}

public sealed record PolicyDecision(PolicyDecisionKind Kind, string? Reason);

public interface IPolicyEvaluator
{
    PolicyDecision Evaluate(string original, string normalized, IntentResult intent);
}

public sealed class PolicyEvaluator(IPersianTextNormalizer normalizer, CopyLexicon lexicon) : IPolicyEvaluator
{
    public PolicyDecision Evaluate(string original, string normalized, IntentResult intent)
    {
        var blob = $"{normalizer.Normalize(original)} {normalized}".Trim();

        if (ContainsAny(blob, lexicon.InjectionNeedles) || ContainsAny(original.ToLowerInvariant(), lexicon.InjectionNeedles))
        {
            return new PolicyDecision(PolicyDecisionKind.RefusePromptInjection, "PROMPT_INJECTION");
        }

        if (ContainsAny(blob, lexicon.AdviceNeedles))
        {
            return new PolicyDecision(PolicyDecisionKind.RefuseFinancialAdvice, "FINANCIAL_ADVICE");
        }

        if (ContainsAny(blob, lexicon.UnauthorizedActionNeedles))
        {
            return new PolicyDecision(PolicyDecisionKind.RefuseUnauthorizedAction, "UNAUTHORIZED_ACTION");
        }

        if (IsUserSpecificStatus(intent.Intent))
        {
            return new PolicyDecision(PolicyDecisionKind.RequireTool, "USER_SPECIFIC");
        }

        if (intent.Intent is ChatIntent.HumanSupport or ChatIntent.CreateSupportTicket)
        {
            return new PolicyDecision(PolicyDecisionKind.OfferSupport, "USER_REQUEST");
        }

        return new PolicyDecision(PolicyDecisionKind.Continue, null);
    }

    public static bool IsUserSpecificStatus(ChatIntent intent) => intent is
        ChatIntent.FundIssuanceStatus or
        ChatIntent.FundRedemptionStatus or
        ChatIntent.TransferStatus or
        ChatIntent.TransactionHistory;

    private static bool ContainsAny(string text, IEnumerable<string> needles) =>
        needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));
}
