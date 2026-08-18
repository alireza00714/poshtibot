using System.ComponentModel.DataAnnotations;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Copy;

public sealed class CopyLexicon
{
    public const string SectionName = "Lexicon";

    [MinLength(1)]
    public List<CopyIntentRule> IntentRules { get; set; } = [];

    [MinLength(1)]
    public List<string> GreetingPhrases { get; set; } = [];

    [MinLength(1)]
    public List<string> SmallTalkPhrases { get; set; } = [];

    [MinLength(1)]
    public List<string> ThanksNeedles { get; set; } = [];

    [MinLength(1)]
    public List<string> GoodbyeNeedles { get; set; } = [];

    [MinLength(1)]
    public List<string> IdentityNeedles { get; set; } = [];

    [MinLength(1)]
    public List<string> HowAreYouNeedles { get; set; } = [];

    [MinLength(1)]
    public List<string> AdviceNeedles { get; set; } = [];

    [MinLength(1)]
    public List<string> InjectionNeedles { get; set; } = [];

    [MinLength(1)]
    public List<string> UnauthorizedActionNeedles { get; set; } = [];

    [MinLength(1)]
    public List<string> ForbiddenResponseNeedles { get; set; } = [];
}

public sealed class CopyIntentRule
{
    public ChatIntent Intent { get; set; }

    [MinLength(1)]
    public List<string> Needles { get; set; } = [];
}
