using InvestorChat.Server.Models;
using InvestorChat.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace InvestorChat.Server.Endpoints;

public static class MarketEndpoints
{
    public static IEndpointRouteBuilder MapMarketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/market/{symbol}/quote", GetQuoteAsync)
            .WithName("GetMarketQuote")
            .WithSummary("Pobiera aktualny kurs spółki GPW.")
            .WithDescription("Symbol jest weryfikowany w źródle notowań Bankier.pl.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        endpoints.MapGet("/api/market/{symbol}/dividend", GetDividendAsync)
            .WithName("GetMarketDividend")
            .WithSummary("Pobiera dywidendę na akcję za bieżący lub poprzedni rok.")
            .WithDescription("Pobiera historię z BiznesRadar; przy braku danych korzysta z Yahoo Finance. Jeśli brak dywidendy z bieżącego roku, zwraca dane z poprzedniego roku.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        endpoints.MapGet("/api/market/{symbol}/net-income", GetNetIncomeHistoryAsync)
            .WithName("GetMarketNetIncomeHistory")
            .WithSummary("Pobiera do pięciu rocznych wartości zysku netto spółki.")
            .WithDescription("Źródłem rocznych raportów finansowych jest Yahoo Finance; zwracana jest także waluta raportu.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        return endpoints;
    }

    private static async Task<Results<Ok<MarketQuoteResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, ProblemHttpResult>> GetQuoteAsync(
        string symbol,
        IMarketDataService marketData,
        CancellationToken cancellationToken)
    {
        if (!BankierMarketDataService.IsValidSymbol(symbol))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Nieprawidłowy symbol spółki.",
                Detail = "Ticker może zawierać tylko litery, cyfry, kropki i myślniki."
            });
        }

        try
        {
            var quote = await marketData.GetQuoteAsync(symbol, cancellationToken);
            return quote is null
                ? TypedResults.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Nie znaleziono spółki.",
                    Detail = "Źródło notowań nie zwróciło kursu dla podanego symbolu."
                })
                : TypedResults.Ok(quote);
        }
        catch (HttpRequestException)
        {
            return TypedResults.Problem(
                title: "Źródło notowań jest chwilowo niedostępne.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<Results<Ok<DividendResponse>, BadRequest<ProblemDetails>, ProblemHttpResult>> GetDividendAsync(
        string symbol,
        IMarketDataService marketData,
        CancellationToken cancellationToken)
    {
        if (!BankierMarketDataService.IsValidSymbol(symbol))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Nieprawidłowy symbol spółki.",
                Detail = "Ticker może zawierać tylko litery, cyfry, kropki i myślniki."
            });
        }

        try
        {
            var dividend = await marketData.GetDividendAsync(symbol, DateTimeOffset.UtcNow.Year, cancellationToken);
            return TypedResults.Ok(dividend);
        }
        catch (HttpRequestException)
        {
            return TypedResults.Problem(
                title: "Źródło danych o dywidendach jest chwilowo niedostępne.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<Results<Ok<NetIncomeHistoryResponse>, BadRequest<ProblemDetails>, ProblemHttpResult>> GetNetIncomeHistoryAsync(
        string symbol,
        IMarketDataService marketData,
        CancellationToken cancellationToken)
    {
        if (!BankierMarketDataService.IsValidSymbol(symbol))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Nieprawidłowy symbol spółki.",
                Detail = "Ticker może zawierać tylko litery, cyfry, kropki i myślniki."
            });
        }

        try
        {
            return TypedResults.Ok(await marketData.GetNetIncomeHistoryAsync(symbol, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return TypedResults.Problem(
                title: "Źródło danych o zysku netto jest chwilowo niedostępne.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
