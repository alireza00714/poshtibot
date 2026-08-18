namespace Namadno.AI.Support.Application.Configuration;

public sealed class SupportOptions
{
    public const string SectionName = "Support";

    public bool RequireExplicitConfirmation { get; set; } = true;

    public string HandoffMessage { get; set; } = string.Empty;

    public string ConfirmationMessage { get; set; } = string.Empty;
}
