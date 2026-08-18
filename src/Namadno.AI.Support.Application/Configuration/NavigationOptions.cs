using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class NavigationOptions
{
    public const string SectionName = "Navigation";

    [MinLength(1)]
    public List<NavigationRouteOptions> Routes { get; set; } =
    [
        new() { Key = "investment", Route = "app://investment" },
        new() { Key = "leasing", Route = "app://leasing" },
        new() { Key = "neobank", Route = "app://neobank" },
        new() { Key = "insurance", Route = "app://insurance" },
        new() { Key = "public-services", Route = "app://public-services" }
    ];
}

public sealed class NavigationRouteOptions
{
    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Label { get; set; } = string.Empty;

    [Required]
    public string Route { get; set; } = string.Empty;
}
