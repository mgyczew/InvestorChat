namespace InvestorChat.Server.Models;

/// <summary>Aktualny kurs spółki notowanej na GPW.</summary>
public sealed record MarketQuoteResponse(
    string Symbol,
    decimal PricePln,
    decimal Change,
    DateTimeOffset UpdatedAt);

/// <summary>Dywidenda na akcję wybrana dla bieżącego lub poprzedniego roku.</summary>
public sealed record DividendResponse(
    string Symbol,
    decimal? AmountPlnPerShare,
    int CurrentYear,
    int? DividendYear,
    bool IsPreviousYear,
    DateTimeOffset? ReferenceDate,
    string? ReferenceDateLabel,
    string Source);

public sealed record NetIncomeHistoryResponse(
    string Symbol,
    IReadOnlyList<NetIncomeYearResponse> Years);

public sealed record NetIncomeYearResponse(
    int FiscalYear,
    decimal Amount,
    string Currency);
