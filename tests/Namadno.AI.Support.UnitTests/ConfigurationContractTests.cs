using FluentAssertions;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;

namespace Namadno.AI.Support.UnitTests;

public sealed class ConfigurationContractTests
{
    [Fact]
    public void Error_codes_must_be_unique_and_stable()
    {
        var codes = typeof(ErrorCodes)
            .GetFields()
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().BeEquivalentTo(
            "VALIDATION_ERROR",
            "UNAUTHORIZED",
            "FORBIDDEN",
            "CONVERSATION_NOT_FOUND",
            "CONVERSATION_ACCESS_DENIED",
            "LLM_UNAVAILABLE",
            "KNOWLEDGE_UNAVAILABLE",
            "EXTERNAL_SERVICE_UNAVAILABLE",
            "TOOL_NOT_ALLOWED",
            "TOOL_EXECUTION_FAILED",
            "SUPPORT_CONFIRMATION_REQUIRED",
            "SUPPORT_TICKET_ALREADY_EXISTS",
            "RATE_LIMITED",
            "CONVERSATION_CLOSED",
            "INTERNAL_ERROR");
    }

    [Fact]
    public void Option_section_names_must_match_configuration_contract()
    {
        LLMOptions.SectionName.Should().Be("LLM");
        EmbeddingOptions.SectionName.Should().Be("Embedding");
        RagOptions.SectionName.Should().Be("Rag");
        ChatOptions.SectionName.Should().Be("Chat");
        SupportOptions.SectionName.Should().Be("Support");
        NamadnoIntegrationOptions.SectionName.Should().Be("Namadno");
        SecurityOptions.SectionName.Should().Be("Security");
        RateLimitOptions.SectionName.Should().Be("RateLimit");
        DataRetentionOptions.SectionName.Should().Be("DataRetention");
        NavigationOptions.SectionName.Should().Be("Navigation");
        ExternalServicesOptions.SectionName.Should().Be("ExternalServices");
        AuthOptions.SectionName.Should().Be("Auth");
        CopyTexts.SectionName.Should().Be("Copy");
        CopyLexicon.SectionName.Should().Be("Lexicon");
        FaqSeedOptions.SectionName.Should().Be("FaqSeed");
    }
}
