using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    [Range(1, 16_000)]
    public int MaxMessageLength { get; set; } = 4000;

    [Range(1, 200)]
    public int MaxConversationContextMessages { get; set; } = 20;

    [Range(16, 8192)]
    public int MaxTokens { get; set; } = 1024;

    [Range(0d, 1d)]
    public double IntentThreshold { get; set; } = 0.75;

    public string WelcomeMessage { get; set; } = string.Empty;
}
