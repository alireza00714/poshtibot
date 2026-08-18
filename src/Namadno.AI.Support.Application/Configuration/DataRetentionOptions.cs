using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    [Range(1, 3650)]
    public int ConversationDays { get; set; } = 365;

    [Range(1, 3650)]
    public int MessageDays { get; set; } = 365;

    [Range(1, 3650)]
    public int UnansweredQuestionDays { get; set; } = 730;
}
