using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class ExternalServicesOptions
{
    public const string SectionName = "ExternalServices";

    /// <summary>
    /// Mock keeps the service fully local. Http uses configured Namadno endpoints.
    /// </summary>
    [Required]
    public string Mode { get; set; } = ExternalServiceModes.Mock;
}

public static class ExternalServiceModes
{
    public const string Mock = "Mock";
    public const string Http = "Http";
}
