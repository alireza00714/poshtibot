using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class LLMOptions
{
    public const string SectionName = "LLM";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";

    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    public string Provider { get; set; } = "OpenAICompatible";

    /// <summary>
    /// Qwen3.5 thinking/reasoning. Keep false for faster, direct Persian replies.
    /// </summary>
    public bool EnableThinking { get; set; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 60;
}
