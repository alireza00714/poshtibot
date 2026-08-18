using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    [Range(1024, 10_485_760)]
    public int MaxRequestBodyBytes { get; set; } = 32_768;

    public bool RedactLogs { get; set; } = true;
}
