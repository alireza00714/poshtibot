namespace Namadno.AI.Support.Application.Common;

/// <summary>
/// Permission names. Bind roles to these in the host, not in domain rules.
/// </summary>
public static class Permissions
{
    public const string FaqRead = "FAQ_READ";
    public const string FaqWrite = "FAQ_WRITE";
    public const string FaqActivate = "FAQ_ACTIVATE";
    public const string FaqDisable = "FAQ_DISABLE";
    public const string SupportRead = "SUPPORT_READ";
    public const string SupportReply = "SUPPORT_REPLY";
    public const string SupportAssign = "SUPPORT_ASSIGN";
    public const string SupportResolve = "SUPPORT_RESOLVE";
    public const string AnalyticsRead = "ANALYTICS_READ";
}
