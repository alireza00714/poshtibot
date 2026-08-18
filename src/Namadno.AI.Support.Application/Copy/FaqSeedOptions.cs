using System.ComponentModel.DataAnnotations;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Copy;

public sealed class FaqSeedOptions
{
    public const string SectionName = "FaqSeed";

    [MinLength(1)]
    public List<FaqSeedItem> Items { get; set; } = [];
}

public sealed class FaqSeedItem
{
    [Required]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string Answer { get; set; } = string.Empty;

    public FaqCategory Category { get; set; }

    public ChatIntent Intent { get; set; }

    public string Keywords { get; set; } = string.Empty;

    public string? Navigation { get; set; }

    public int Priority { get; set; }
}
