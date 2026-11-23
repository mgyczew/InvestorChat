using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using InvestorChat.Application.Portfolios;

namespace InvestorChat.Server.Services;

public sealed class PortfolioCsvParser
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex QuantityPattern = new(
        @"^\s*(?<available>[^()]+?)(?:\s*\(\s*(?<blocked>[^()]*)\s*\))?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly CultureInfo PolishCulture = CultureInfo.GetCultureInfo("pl-PL");
    private static readonly char[] Delimiters = [';', ',', '\t'];
    private static readonly Encoding Windows1250;

    static PortfolioCsvParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Windows1250 = Encoding.GetEncoding(1250, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    public IReadOnlyList<PortfolioPositionData> Parse(Stream stream)
    {
        string content;
        try
        {
            content = ReadContent(stream);
        }
        catch (PortfolioImportException)
        {
            throw;
        }
        catch (IOException exception)
        {
            throw new PortfolioImportException("Nie udało się odczytać pliku CSV.", exception);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new PortfolioImportException("Plik CSV jest pusty.");
        }

        PortfolioImportException? parsingError = null;
        foreach (var delimiter in Delimiters)
        {
            List<string[]> records;
            try
            {
                records = ParseRecords(content, delimiter);
            }
            catch (PortfolioImportException exception)
            {
                parsingError = exception;
                continue;
            }

            if (records.Count == 0 || !TryGetColumns(records[0], out var columns))
            {
                continue;
            }

            return ParsePositions(records, columns);
        }

        throw parsingError ?? new PortfolioImportException("Nie znaleziono wymaganych nagłówków CSV. Sprawdź separator i kolumny portfela.");
    }

    private static string ReadContent(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();

        try
        {
            using var reader = new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        catch (DecoderFallbackException)
        {
            try
            {
                return Windows1250.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw new PortfolioImportException("Nie można rozpoznać kodowania CSV. Zapisz plik jako UTF-8 lub Windows-1250.", exception);
            }
        }
    }

    private static IReadOnlyList<PortfolioPositionData> ParsePositions(IReadOnlyList<string[]> records, CsvColumns columns)
    {
        var positions = new List<PortfolioPositionData>();
        for (var index = 1; index < records.Count; index++)
        {
            var record = records[index];
            if (record.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var rowNumber = index + 1;
            var paper = GetRequiredText(record, columns.Paper, "Papier", rowNumber);
            var exchange = GetRequiredText(record, columns.Exchange, "Giełda", rowNumber);
            var (available, blocked) = ParseQuantity(GetField(record, columns.Quantity), rowNumber);
            var price = ParseDecimal(GetField(record, columns.Price), "Kurs", rowNumber);
            var priceCurrency = GetRequiredText(record, columns.PriceCurrency, "Waluta kursu", rowNumber);
            var value = ParseDecimal(GetField(record, columns.Value), "Wartość", rowNumber);
            var valueCurrency = GetRequiredText(record, columns.ValueCurrency, "Waluta wartości", rowNumber);

            positions.Add(new PortfolioPositionData(
                paper,
                exchange,
                available,
                blocked,
                price,
                priceCurrency,
                value,
                valueCurrency));
        }

        if (positions.Count == 0)
        {
            throw new PortfolioImportException("Plik CSV nie zawiera żadnych pozycji portfela.");
        }

        return positions;
    }

    private static bool TryGetColumns(string[] header, out CsvColumns columns)
    {
        var headers = header
            .Select((value, index) => (Name: NormalizeHeader(value), Index: index))
            .GroupBy(item => item.Name)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Index).ToArray());

        if (!TryGetSingleColumn(headers, "Papier", out var paper) ||
            !TryGetSingleColumn(headers, "Giełda", out var exchange) ||
            !TryGetSingleColumn(headers, "Liczba dostępna (Blokady)", out var quantity) ||
            !TryGetSingleColumn(headers, "Kurs", out var price) ||
            !TryGetSingleColumn(headers, "Wartość", out var value) ||
            !headers.TryGetValue(NormalizeHeader("Waluta"), out var currencies) ||
            currencies.Length != 2 || !(price < currencies[0] && currencies[0] < value && value < currencies[1]))
        {
            columns = default;
            return false;
        }

        columns = new CsvColumns(paper, exchange, quantity, price, currencies[0], value, currencies[1]);
        return true;
    }

    private static bool TryGetSingleColumn(IReadOnlyDictionary<string, int[]> headers, string name, out int index)
    {
        if (headers.TryGetValue(NormalizeHeader(name), out var indexes) && indexes.Length == 1)
        {
            index = indexes[0];
            return true;
        }

        index = -1;
        return false;
    }

    private static List<string[]> ParseRecords(string content, char delimiter)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (insideQuotes)
            {
                if (character == '"' && index + 1 < content.Length && content[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    insideQuotes = false;
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == '"' && field.Length == 0)
            {
                insideQuotes = true;
            }
            else if (character == delimiter)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else if (character is '\r' or '\n')
            {
                fields.Add(field.ToString());
                AddRecord(records, fields);
                fields.Clear();
                field.Clear();
                if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n')
                {
                    index++;
                }
            }
            else
            {
                field.Append(character);
            }
        }

        if (insideQuotes)
        {
            throw new PortfolioImportException("Plik CSV zawiera niedomknięte pole cytowane.");
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            AddRecord(records, fields);
        }

        return records;
    }

    private static void AddRecord(ICollection<string[]> records, IReadOnlyCollection<string> fields)
    {
        if (fields.Any(field => !string.IsNullOrWhiteSpace(field)))
        {
            records.Add(fields.ToArray());
        }
    }

    private static string NormalizeHeader(string value) =>
        Whitespace.Replace(value.Trim().TrimStart('\uFEFF'), " ").ToUpperInvariant();

    private static string GetField(string[] record, int column) =>
        column < record.Length ? record[column].Trim() : string.Empty;

    private static string GetRequiredText(string[] record, int column, string field, int rowNumber)
    {
        var value = GetField(record, column);
        if (value.Length == 0)
        {
            throw new PortfolioImportException($"Wiersz {rowNumber}: pole „{field}” jest puste.");
        }

        return value;
    }

    private static (decimal Available, decimal Blocked) ParseQuantity(string value, int rowNumber)
    {
        var match = QuantityPattern.Match(value);
        if (!match.Success)
        {
            throw new PortfolioImportException($"Wiersz {rowNumber}: nieprawidłowa liczba w kolumnie „Liczba dostępna (Blokady)”. Użyj np. „10 (2)”.");
        }

        var available = ParseDecimalText(match.Groups["available"].Value, "Liczba dostępna (Blokady)", rowNumber);
        var blocked = match.Groups["blocked"].Success
            ? ParseDecimalText(match.Groups["blocked"].Value, "Liczba dostępna (Blokady)", rowNumber)
            : 0m;
        return (available, blocked);
    }

    private static decimal ParseDecimal(string value, string field, int rowNumber) =>
        ParseDecimalText(value, field, rowNumber);

    private static decimal ParseDecimalText(string value, string field, int rowNumber)
    {
        var normalized = value.Trim().Replace("\u00A0", " ").Replace(" ", string.Empty);
        if (decimal.TryParse(normalized, NumberStyles.Number, PolishCulture, out var result) ||
            decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        throw new PortfolioImportException($"Wiersz {rowNumber}: nieprawidłowa wartość w kolumnie „{field}”.");
    }

    private readonly record struct CsvColumns(
        int Paper,
        int Exchange,
        int Quantity,
        int Price,
        int PriceCurrency,
        int Value,
        int ValueCurrency);
}

public sealed class PortfolioImportException(string message, Exception? innerException = null) : Exception(message, innerException);
