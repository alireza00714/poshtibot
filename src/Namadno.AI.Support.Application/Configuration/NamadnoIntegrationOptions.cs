using System.ComponentModel.DataAnnotations;

namespace Namadno.AI.Support.Application.Configuration;

public sealed class NamadnoIntegrationOptions
{
    public const string SectionName = "Namadno";

    [Required]
    public string BaseUrl { get; set; } = "https://namadno.example.invalid";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;

    public NamadnoApiEndpoints Apis { get; set; } = new();
}

public sealed class NamadnoApiEndpoints
{
    public string UserProfile { get; set; } = "/placeholder/user-profile";

    public string InvestmentStatus { get; set; } = "/placeholder/investment-status";

    public string FundOrderStatus { get; set; } = "/placeholder/fund-order-status";

    public string PaymentStatus { get; set; } = "/placeholder/payment-status";

    public string TransferStatus { get; set; } = "/placeholder/transfer-status";

    public string CardStatus { get; set; } = "/placeholder/card-status";

    public string InsurancePolicy { get; set; } = "/placeholder/insurance-policy";
}
