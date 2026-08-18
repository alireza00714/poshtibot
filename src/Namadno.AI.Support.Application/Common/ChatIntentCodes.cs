using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Common;

public static class ChatIntentCodes
{
    public const string Unknown = "UNKNOWN";
    public const string GeneralInformation = "GENERAL_INFORMATION";
    public const string HumanSupport = "HUMAN_SUPPORT";
    public const string Greeting = "GREETING";
    public const string SmallTalk = "SMALL_TALK";
    public const string InvestmentStart = "INVESTMENT_START";
    public const string InvestmentMethods = "INVESTMENT_METHODS";
    public const string FundSelection = "FUND_SELECTION";
    public const string FundIssuance = "FUND_ISSUANCE";
    public const string FundIssuanceStatus = "FUND_ISSUANCE_STATUS";
    public const string FundRedemption = "FUND_REDEMPTION";
    public const string FundRedemptionStatus = "FUND_REDEMPTION_STATUS";
    public const string EtfPurchase = "ETF_PURCHASE";
    public const string EtfSell = "ETF_SELL";
    public const string BrokerageRegistration = "BROKERAGE_REGISTRATION";
    public const string NeobankInformation = "NEOBANK_INFORMATION";
    public const string AccountOpening = "ACCOUNT_OPENING";
    public const string MoneyTransfer = "MONEY_TRANSFER";
    public const string TransferMethods = "TRANSFER_METHODS";
    public const string CardToCard = "CARD_TO_CARD";
    public const string CardPasswordChange = "CARD_PASSWORD_CHANGE";
    public const string TransferStatus = "TRANSFER_STATUS";
    public const string CardBlock = "CARD_BLOCK";
    public const string CardPasswordRecovery = "CARD_PASSWORD_RECOVERY";
    public const string TransactionHistory = "TRANSACTION_HISTORY";
    public const string NavigateInvestment = "NAVIGATE_INVESTMENT";
    public const string NavigateNeobank = "NAVIGATE_NEOBANK";
    public const string NavigateInsurance = "NAVIGATE_INSURANCE";
    public const string NavigateLeasing = "NAVIGATE_LEASING";
    public const string NavigatePublicServices = "NAVIGATE_PUBLIC_SERVICES";
    public const string CreateSupportTicket = "CREATE_SUPPORT_TICKET";

    public static string ToCode(this ChatIntent intent) => intent switch
    {
        ChatIntent.Unknown => Unknown,
        ChatIntent.GeneralInformation => GeneralInformation,
        ChatIntent.HumanSupport => HumanSupport,
        ChatIntent.Greeting => Greeting,
        ChatIntent.SmallTalk => SmallTalk,
        ChatIntent.InvestmentStart => InvestmentStart,
        ChatIntent.InvestmentMethods => InvestmentMethods,
        ChatIntent.FundSelection => FundSelection,
        ChatIntent.FundIssuance => FundIssuance,
        ChatIntent.FundIssuanceStatus => FundIssuanceStatus,
        ChatIntent.FundRedemption => FundRedemption,
        ChatIntent.FundRedemptionStatus => FundRedemptionStatus,
        ChatIntent.EtfPurchase => EtfPurchase,
        ChatIntent.EtfSell => EtfSell,
        ChatIntent.BrokerageRegistration => BrokerageRegistration,
        ChatIntent.NeobankInformation => NeobankInformation,
        ChatIntent.AccountOpening => AccountOpening,
        ChatIntent.MoneyTransfer => MoneyTransfer,
        ChatIntent.TransferMethods => TransferMethods,
        ChatIntent.CardToCard => CardToCard,
        ChatIntent.CardPasswordChange => CardPasswordChange,
        ChatIntent.TransferStatus => TransferStatus,
        ChatIntent.CardBlock => CardBlock,
        ChatIntent.CardPasswordRecovery => CardPasswordRecovery,
        ChatIntent.TransactionHistory => TransactionHistory,
        ChatIntent.NavigateInvestment => NavigateInvestment,
        ChatIntent.NavigateNeobank => NavigateNeobank,
        ChatIntent.NavigateInsurance => NavigateInsurance,
        ChatIntent.NavigateLeasing => NavigateLeasing,
        ChatIntent.NavigatePublicServices => NavigatePublicServices,
        ChatIntent.CreateSupportTicket => CreateSupportTicket,
        _ => Unknown
    };

    public static bool TryParse(string? value, out ChatIntent intent)
    {
        intent = value?.Trim().ToUpperInvariant() switch
        {
            Unknown => ChatIntent.Unknown,
            GeneralInformation => ChatIntent.GeneralInformation,
            HumanSupport => ChatIntent.HumanSupport,
            Greeting => ChatIntent.Greeting,
            SmallTalk => ChatIntent.SmallTalk,
            InvestmentStart => ChatIntent.InvestmentStart,
            InvestmentMethods => ChatIntent.InvestmentMethods,
            FundSelection => ChatIntent.FundSelection,
            FundIssuance => ChatIntent.FundIssuance,
            FundIssuanceStatus => ChatIntent.FundIssuanceStatus,
            FundRedemption => ChatIntent.FundRedemption,
            FundRedemptionStatus => ChatIntent.FundRedemptionStatus,
            EtfPurchase => ChatIntent.EtfPurchase,
            EtfSell => ChatIntent.EtfSell,
            BrokerageRegistration => ChatIntent.BrokerageRegistration,
            NeobankInformation => ChatIntent.NeobankInformation,
            AccountOpening => ChatIntent.AccountOpening,
            MoneyTransfer => ChatIntent.MoneyTransfer,
            TransferMethods => ChatIntent.TransferMethods,
            CardToCard => ChatIntent.CardToCard,
            CardPasswordChange => ChatIntent.CardPasswordChange,
            TransferStatus => ChatIntent.TransferStatus,
            CardBlock => ChatIntent.CardBlock,
            CardPasswordRecovery => ChatIntent.CardPasswordRecovery,
            TransactionHistory => ChatIntent.TransactionHistory,
            NavigateInvestment => ChatIntent.NavigateInvestment,
            NavigateNeobank => ChatIntent.NavigateNeobank,
            NavigateInsurance => ChatIntent.NavigateInsurance,
            NavigateLeasing => ChatIntent.NavigateLeasing,
            NavigatePublicServices => ChatIntent.NavigatePublicServices,
            CreateSupportTicket => ChatIntent.CreateSupportTicket,
            _ => (ChatIntent)(-1)
        };

        if ((int)intent == -1)
        {
            intent = ChatIntent.Unknown;
            return false;
        }

        return true;
    }
}
