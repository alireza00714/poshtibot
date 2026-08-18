using Namadno.AI.Support.Application.Abstractions.Text;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Intent;

public interface IIntentClassifier
{
    IntentResult Classify(string original, string normalized);
}

public sealed class HeuristicIntentClassifier(IPersianTextNormalizer normalizer, CopyLexicon lexicon) : IIntentClassifier
{
    public IntentResult Classify(string original, string normalized)
    {
        var text = string.IsNullOrWhiteSpace(normalized) ? normalizer.Normalize(original) : normalized;
        if (string.IsNullOrWhiteSpace(text))
        {
            return new IntentResult(ChatIntent.Unknown, 0.0, "General");
        }

        foreach (var rule in lexicon.IntentRules)
        {
            foreach (var needle in rule.Needles)
            {
                var n = normalizer.Normalize(needle);
                if (n.Length > 0 && text.Contains(n, StringComparison.Ordinal))
                {
                    var confidence = text.Equals(n, StringComparison.Ordinal) ? 0.96 : 0.88;
                    return new IntentResult(rule.Intent, confidence, CategoryOf(rule.Intent));
                }
            }
        }

        if (IsPrimarilyGreeting(text))
        {
            return new IntentResult(ChatIntent.Greeting, 0.95, "General");
        }

        if (ContainsPhrase(text, lexicon.SmallTalkPhrases))
        {
            return new IntentResult(ChatIntent.SmallTalk, 0.9, "General");
        }

        return new IntentResult(ChatIntent.Unknown, 0.2, "General");
    }

    public static string CategoryOf(ChatIntent intent) => intent switch
    {
        ChatIntent.InvestmentStart or ChatIntent.InvestmentMethods or ChatIntent.FundSelection
            or ChatIntent.FundIssuance or ChatIntent.FundIssuanceStatus or ChatIntent.FundRedemption
            or ChatIntent.FundRedemptionStatus or ChatIntent.EtfPurchase or ChatIntent.EtfSell
            or ChatIntent.BrokerageRegistration or ChatIntent.NavigateInvestment => "Investment",
        ChatIntent.NeobankInformation or ChatIntent.AccountOpening or ChatIntent.MoneyTransfer
            or ChatIntent.TransferMethods or ChatIntent.CardToCard or ChatIntent.CardPasswordChange
            or ChatIntent.TransferStatus or ChatIntent.CardBlock or ChatIntent.CardPasswordRecovery
            or ChatIntent.TransactionHistory or ChatIntent.NavigateNeobank => "Neobank",
        ChatIntent.NavigateInsurance => "Insurance",
        ChatIntent.NavigateLeasing => "Leasing",
        _ => "General"
    };

    public static bool IsConversational(ChatIntent intent) =>
        intent is ChatIntent.Greeting or ChatIntent.SmallTalk;

    private bool IsPrimarilyGreeting(string text)
    {
        if (!ContainsPhrase(text, lexicon.GreetingPhrases))
        {
            return false;
        }

        var remaining = StripPhrases(text, lexicon.GreetingPhrases);
        remaining = StripPhrases(remaining, lexicon.SmallTalkPhrases);
        var tokens = remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length <= 1;
    }

    private bool ContainsPhrase(string text, IReadOnlyList<string> phrases) =>
        phrases.Any(phrase =>
        {
            var n = normalizer.Normalize(phrase);
            return n.Length > 0 && text.Contains(n, StringComparison.Ordinal);
        });

    private string StripPhrases(string text, IReadOnlyList<string> phrases)
    {
        var remaining = text;
        foreach (var phrase in phrases.OrderByDescending(p => p.Length))
        {
            var n = normalizer.Normalize(phrase);
            if (n.Length == 0)
            {
                continue;
            }

            while (remaining.Contains(n, StringComparison.Ordinal))
            {
                remaining = remaining.Replace(n, " ", StringComparison.Ordinal);
            }
        }

        return normalizer.Normalize(remaining);
    }
}
