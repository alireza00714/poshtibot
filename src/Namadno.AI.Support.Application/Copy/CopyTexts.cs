using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Namadno.AI.Support.Application.Copy;

public sealed class CopyTexts
{
    public const string SectionName = "Copy";

    [Required]
    public CopyErrors Errors { get; set; } = new();

    [Required]
    public CopyChat Chat { get; set; } = new();

    [Required]
    public CopyActions Actions { get; set; } = new();

    [Required]
    public CopyTools Tools { get; set; } = new();

    [Required]
    public CopyPrompts Prompts { get; set; } = new();

    public bool HasRequiredText() =>
        StringsFilled(Errors) && StringsFilled(Chat) && StringsFilled(Actions) && StringsFilled(Tools) && StringsFilled(Prompts);

    private static bool StringsFilled(object target) =>
        target.GetType()
            .GetProperties()
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.GetValue(target) as string)
            .All(value => !string.IsNullOrWhiteSpace(value));
}

public sealed class CopyErrors
{
    [Required] public string Unauthorized { get; set; } = string.Empty;
    [Required] public string InvalidMessage { get; set; } = string.Empty;
    [Required] public string InvalidInput { get; set; } = string.Empty;
    [Required] public string InvalidStatus { get; set; } = string.Empty;
    [Required] public string InvalidCategory { get; set; } = string.Empty;
    [Required] public string InvalidIntent { get; set; } = string.Empty;
    [Required] public string Forbidden { get; set; } = string.Empty;
    [Required] public string ConversationNotFound { get; set; } = string.Empty;
    [Required] public string ConversationAccessDenied { get; set; } = string.Empty;
    [Required] public string ConversationClosed { get; set; } = string.Empty;
    [Required] public string ArticleNotFound { get; set; } = string.Empty;
    [Required] public string ItemNotFound { get; set; } = string.Empty;
    [Required] public string TicketNotFound { get; set; } = string.Empty;
    [Required] public string SupportMisconfigured { get; set; } = string.Empty;
    [Required] public string SupportAlreadyRegistered { get; set; } = string.Empty;
    [Required] public string SupportAlreadyOpen { get; set; } = string.Empty;
    [Required] public string Internal { get; set; } = string.Empty;
    [Required] public string RateLimited { get; set; } = string.Empty;
}

public sealed class CopyChat
{
    [Required] public string Welcome { get; set; } = string.Empty;
    [Required] public string Handoff { get; set; } = string.Empty;
    [Required] public string Confirmation { get; set; } = string.Empty;
    [Required] public string PromptInjectionRefuse { get; set; } = string.Empty;
    [Required] public string UnauthorizedActionRefuse { get; set; } = string.Empty;
    [Required] public string FinancialAdviceRefuse { get; set; } = string.Empty;
    [Required] public string NavigationHint { get; set; } = string.Empty;
    [Required] public string ToolNotAllowed { get; set; } = string.Empty;
    [Required] public string ExternalUnavailable { get; set; } = string.Empty;
    [Required] public string Greeting { get; set; } = string.Empty;
    [Required] public string Thanks { get; set; } = string.Empty;
    [Required] public string HowAreYou { get; set; } = string.Empty;
    [Required] public string Identity { get; set; } = string.Empty;
    [Required] public string Goodbye { get; set; } = string.Empty;
    [Required] public string SmallTalk { get; set; } = string.Empty;
}

public sealed class CopyActions
{
    [Required] public string ConnectSupport { get; set; } = string.Empty;
    [Required] public string CreateSupportRequest { get; set; } = string.Empty;
    [Required] public string SupportTicketTitle { get; set; } = string.Empty;
    [Required] public string GoInvestment { get; set; } = string.Empty;
    [Required] public string GoLeasing { get; set; } = string.Empty;
    [Required] public string GoNeobank { get; set; } = string.Empty;
    [Required] public string GoInsurance { get; set; } = string.Empty;
    [Required] public string GoPublicServices { get; set; } = string.Empty;

    public string LabelForRoute(string key) => key switch
    {
        "investment" => GoInvestment,
        "leasing" => GoLeasing,
        "neobank" => GoNeobank,
        "insurance" => GoInsurance,
        "public-services" => GoPublicServices,
        _ => string.Empty
    };
}

public sealed class CopyTools
{
    [Required] public string NamadnoNotFound { get; set; } = string.Empty;
    [Required] public string NamadnoStatusFallback { get; set; } = string.Empty;
    [Required] public string MockTransfer { get; set; } = string.Empty;
    [Required] public string MockCard { get; set; } = string.Empty;
    [Required] public string MockInsurance { get; set; } = string.Empty;
    [Required] public string MockPayment { get; set; } = string.Empty;
    [Required] public string MockProfile { get; set; } = string.Empty;
    [Required] public string MockFundOrderStatus { get; set; } = string.Empty;

    public string FormatMockFundOrderStatus(int trackingNumber) =>
        string.Format(CultureInfo.InvariantCulture, MockFundOrderStatus, trackingNumber);
}

public sealed class CopyPrompts
{
    [Required] public string HistoryHeading { get; set; } = string.Empty;
    [Required] public string UserMessageHeading { get; set; } = string.Empty;
    [Required] public string UserQuestionHeading { get; set; } = string.Empty;
    [Required] public string FaqHeading { get; set; } = string.Empty;
    [Required] public string ApprovedFallbackHeading { get; set; } = string.Empty;
    [Required] public string RagOnlyInstruction { get; set; } = string.Empty;
    [Required] public string FaqExcerpt { get; set; } = string.Empty;

    public string FormatFaqExcerpt(int index, string question, string answer) =>
        string.Format(CultureInfo.InvariantCulture, FaqExcerpt, index, question, answer);
}
