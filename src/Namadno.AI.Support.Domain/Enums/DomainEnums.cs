namespace Namadno.AI.Support.Domain.Enums;

public enum ConversationStatus
{
    Active = 0,
    HandedOff = 1,
    Resolved = 2,
    Closed = 3
}

public enum ConversationChannel
{
    App = 0,
    Unknown = 1
}

public enum SenderType
{
    User = 0,
    Assistant = 1,
    SupportAgent = 2,
    System = 3
}

public enum FaqStatus
{
    Draft = 0,
    Active = 1,
    Disabled = 2
}

public enum FaqCategory
{
    General = 0,
    Investment = 1,
    Leasing = 2,
    Neobank = 3,
    Insurance = 4
}

public enum SupportTicketStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    WaitingForUser = 3,
    Resolved = 4,
    Closed = 5
}

public enum SupportTicketPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

public enum UnansweredQuestionStatus
{
    Open = 0,
    Reviewed = 1,
    ConvertedToFaq = 2,
    Dismissed = 3
}

public enum KnowledgeSourceType
{
    FaqArticle = 0
}

public enum ChatIntent
{
    Unknown = 0,
    GeneralInformation = 1,
    HumanSupport = 2,
    Greeting = 3,
    SmallTalk = 4,
    InvestmentStart = 10,
    InvestmentMethods = 11,
    FundSelection = 12,
    FundIssuance = 13,
    FundIssuanceStatus = 14,
    FundRedemption = 15,
    FundRedemptionStatus = 16,
    EtfPurchase = 17,
    EtfSell = 18,
    BrokerageRegistration = 19,
    NeobankInformation = 30,
    AccountOpening = 31,
    MoneyTransfer = 32,
    TransferMethods = 33,
    CardToCard = 34,
    CardPasswordChange = 35,
    TransferStatus = 36,
    CardBlock = 37,
    CardPasswordRecovery = 38,
    TransactionHistory = 39,
    NavigateInvestment = 50,
    NavigateNeobank = 51,
    NavigateInsurance = 52,
    NavigateLeasing = 53,
    NavigatePublicServices = 54,
    CreateSupportTicket = 60
}

public enum ResponseMode
{
    DirectFaq = 0,
    RagGenerated = 1,
    ToolResult = 2,
    Navigation = 3,
    SupportHandoff = 4,
    SafeFallback = 5,
    Conversational = 6
}
