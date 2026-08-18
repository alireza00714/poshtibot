using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";

    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    public string Provider { get; set; } = "OpenAICompatible";

    [Range(1, 4096)]
    public int Dimensions { get; set; } = 1024;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 60;
}
