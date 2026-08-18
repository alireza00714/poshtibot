using System.Text.Json;
using FluentAssertions;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Intent;
using Namadno.AI.Support.Application.Text;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.EvaluationTests;

public sealed class PersianEvaluationTests
{
    [Fact]
    public void Dataset_has_at_least_150_cases_and_core_examples_classify()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "persian-evaluation.json");
        File.Exists(path).Should().BeTrue();
        var cases = JsonSerializer.Deserialize<List<EvalCase>>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        cases.Should().NotBeNull();
        cases!.Count.Should().BeGreaterThanOrEqualTo(150);

        var normalizer = new PersianTextNormalizer();
        var lexicon = SupportCopyFiles.LoadSection<CopyLexicon>(CopyLexicon.SectionName, AppContext.BaseDirectory);
        var classifier = new HeuristicIntentClassifier(normalizer, lexicon);

        Classify(classifier, normalizer, "چجوری صندوق بخرم؟").Should().Be(ChatIntent.FundSelection);
        Classify(classifier, normalizer, "چطور میتونم سرمایه گذاری کنم؟").Should().Be(ChatIntent.InvestmentStart);
        Classify(classifier, normalizer, "وضعیت درخواست صدور من چیه؟").Should().Be(ChatIntent.FundIssuanceStatus);
        Classify(classifier, normalizer, "میخوام با پشتیبانی صحبت کنم.").Should().Be(ChatIntent.HumanSupport);
        Classify(classifier, normalizer, "سلام").Should().Be(ChatIntent.Greeting);
        Classify(classifier, normalizer, "تو کی هستی؟").Should().Be(ChatIntent.SmallTalk);
    }

    private static ChatIntent Classify(
        HeuristicIntentClassifier classifier,
        PersianTextNormalizer normalizer,
        string input) =>
        classifier.Classify(input, normalizer.Normalize(input)).Intent;

    private sealed class EvalCase
    {
        public string Input { get; set; } = string.Empty;

        public string? ExpectedIntent { get; set; }

        public string? ExpectedBehavior { get; set; }
    }
}
