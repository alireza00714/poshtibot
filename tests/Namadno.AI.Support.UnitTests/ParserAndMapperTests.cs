using FluentAssertions;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Intent;
using Namadno.AI.Support.Application.Text;
using Namadno.AI.Support.Domain.Enums;
using Namadno.AI.Support.Infrastructure.Integrations.Namadno;
using Namadno.AI.Support.Infrastructure.LLM;

namespace Namadno.AI.Support.UnitTests;

public sealed class ParserAndMapperTests
{
    [Fact]
    public void Chat_completion_parser_reads_message_content()
    {
        const string json = """{"model":"test-model","choices":[{"message":{"role":"assistant","content":"hello"}}]}""";
        OpenAiCompatibleParsers.TryReadChatContent(json, out var content, out var model).Should().BeTrue();
        content.Should().Be("hello");
        model.Should().Be("test-model");
    }

    [Fact]
    public void Chat_completion_parser_strips_think_blocks()
    {
        const string json = """{"model":"qwen","choices":[{"message":{"role":"assistant","content":"<think>plan</think>\nسلام"}}]}""";
        OpenAiCompatibleParsers.TryReadChatContent(json, out var content, out _).Should().BeTrue();
        content.Should().Be("سلام");
    }

    [Fact]
    public void Embedding_parser_rejects_dimension_mismatch()
    {
        const string json = """{"data":[{"embedding":[0.1,0.2]}]}""";
        OpenAiCompatibleParsers.TryReadEmbedding(json, 768, out _).Should().BeFalse();
        OpenAiCompatibleParsers.TryReadEmbedding(json, 2, out var vector).Should().BeTrue();
        vector.Should().Equal(0.1f, 0.2f);
    }

    [Fact]
    public void Namadno_mapper_uses_safe_message_and_ignores_urls()
    {
        NamadnoResponseMapper.Map("""{"message":"وضعیت در حال بررسی است"}""", "fallback")
            .Should().Be("وضعیت در حال بررسی است");
        NamadnoResponseMapper.Map("""{"message":"see https://evil.example"}""", "fallback")
            .Should().Be("fallback");
        NamadnoResponseMapper.Map("not-json", "fallback").Should().Be("fallback");
    }

    [Fact]
    public void Better_fund_question_maps_to_fund_selection()
    {
        var normalizer = new PersianTextNormalizer();
        var lexicon = SupportCopyFiles.LoadSection<CopyLexicon>(CopyLexicon.SectionName, AppContext.BaseDirectory);
        var classifier = new HeuristicIntentClassifier(normalizer, lexicon);
        var text = "کدوم صندوق بهتره؟";
        classifier.Classify(text, normalizer.Normalize(text)).Intent.Should().Be(ChatIntent.FundSelection);
    }
}
