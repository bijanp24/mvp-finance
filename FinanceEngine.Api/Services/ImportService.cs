using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FinanceEngine.Api.Models;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FinanceEngine.Api.Services;

public class ImportService
{
    private static readonly Dictionary<string, ParsedFile> _sessionCache = new();
    private static readonly string[] DateColumnNames = { "date", "transaction date", "posted date", "trans date", "posting date" };
    private static readonly string[] DescriptionColumnNames = { "description", "memo", "narrative", "details", "transaction", "name" };
    private static readonly string[] AmountColumnNames = { "amount", "sum", "total", "value" };
    private static readonly string[] DebitColumnNames = { "debit", "withdrawal", "expense", "payment", "out" };
    private static readonly string[] CreditColumnNames = { "credit", "deposit", "income", "in" };
    private static readonly string[] CategoryColumnNames = { "category", "type", "tag" };

    public ParsedFile ParseFile(string fileName, string base64Content)
    {
        var bytes = Convert.FromBase64String(base64Content);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".csv" => ParseCsv(bytes),
            ".xlsx" or ".xls" => ParseExcel(bytes),
            ".pdf" => ParsePdf(bytes),
            _ => throw new ArgumentException($"Unsupported file type: {extension}")
        };
    }

    private ParsedFile ParseCsv(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);

        var result = new ParsedFile { HasHeaderRow = true };

        // Read header
        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord;
        if (headers != null)
        {
            result.Headers.AddRange(headers.Select(h => h?.Trim() ?? ""));
        }

        var headerCount = result.Headers.Count;
        var rowNumber = 0;

        // Read data rows
        while (csv.Read())
        {
            rowNumber++;
            var row = new ParsedRow { RowNumber = rowNumber };

            for (int i = 0; i < headerCount; i++)
            {
                var value = csv.GetField(i) ?? "";
                row.Values.Add(value);
            }

            result.Rows.Add(row);
        }

        return result;
    }

    private ParsedFile ParseExcel(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();
        var result = new ParsedFile { HasHeaderRow = true };

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null) return result;

        var firstRow = usedRange.FirstRow();
        foreach (var cell in firstRow.Cells())
        {
            result.Headers.Add(cell.GetString() ?? $"Column {cell.Address.ColumnNumber}");
        }

        var rowNumber = 0;
        foreach (var row in usedRange.Rows().Skip(1)) // Skip header
        {
            rowNumber++;
            var parsedRow = new ParsedRow { RowNumber = rowNumber };
            foreach (var cell in row.Cells())
            {
                parsedRow.Values.Add(cell.GetString() ?? "");
            }
            // Pad with empty values if row has fewer columns
            while (parsedRow.Values.Count < result.Headers.Count)
            {
                parsedRow.Values.Add("");
            }
            result.Rows.Add(parsedRow);
        }

        return result;
    }

    // Matches a statement transaction line: a leading date, a description, and a
    // trailing money amount (optionally negative via parentheses, a leading/trailing
    // minus, or a "CR" credit suffix). Issuer layouts vary, so this is deliberately
    // forgiving; the preview lets the user deselect anything misread.
    private static readonly Regex TransactionLine = new(
        @"^\s*(?<date>\d{1,2}[/-]\d{1,2}(?:[/-]\d{2,4})?)\s+(?<desc>.+?)\s+(?<amt>\(?-?\$?[\d,]+\.\d{2}\)?)\s*(?<suffix>CR|-)?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LeadingSecondDate = new(
        @"^\d{1,2}[/-]\d{1,2}(?:[/-]\d{2,4})?\s+", RegexOptions.Compiled);

    private ParsedFile ParsePdf(byte[] bytes)
    {
        var lines = new List<string>();
        var totalWords = 0;

        using (var document = PdfDocument.Open(bytes))
        {
            foreach (var page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                totalWords += words.Count;
                lines.AddRange(ReconstructLines(words));
            }
        }

        if (totalWords == 0)
        {
            throw new ArgumentException(
                "This PDF has no readable text layer - it may be a scan or image. " +
                "Try downloading a CSV/Excel statement from your card's website instead.");
        }

        return ExtractTransactionsFromLines(lines);
    }

    /// <summary>
    /// Reconstructs visual text lines from positioned words by clustering words
    /// with a near-identical vertical position, then ordering each line left to right.
    /// </summary>
    private static IEnumerable<string> ReconstructLines(IReadOnlyList<Word> words)
    {
        const double yTolerance = 3.0;

        var ordered = words
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .OrderByDescending(w => w.BoundingBox.Top)
            .ToList();

        var current = new List<Word>();
        double? currentY = null;

        foreach (var word in ordered)
        {
            var y = word.BoundingBox.Top;
            if (currentY is null || Math.Abs(y - currentY.Value) <= yTolerance)
            {
                current.Add(word);
                currentY ??= y;
            }
            else
            {
                yield return JoinLine(current);
                current = new List<Word> { word };
                currentY = y;
            }
        }

        if (current.Count > 0)
            yield return JoinLine(current);

        static string JoinLine(List<Word> ws) =>
            string.Join(" ", ws.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
    }

    /// <summary>
    /// Turns reconstructed text lines into a normalized [Date, Description, Amount]
    /// table so the rest of the import pipeline (column detection, sign convention,
    /// duplicate detection) works exactly as it does for CSV/Excel.
    /// </summary>
    public ParsedFile ExtractTransactionsFromLines(IEnumerable<string> lines)
    {
        var file = new ParsedFile { HasHeaderRow = true };
        file.Headers.AddRange(new[] { "Date", "Description", "Amount" });

        var rowNumber = 0;
        foreach (var rawLine in lines)
        {
            var match = TransactionLine.Match(rawLine);
            if (!match.Success) continue;

            var date = NormalizeDate(match.Groups["date"].Value);
            if (date is null) continue; // not a plausible date - skip

            var description = LeadingSecondDate.Replace(match.Groups["desc"].Value, "").Trim();
            if (string.IsNullOrWhiteSpace(description)) continue;

            var negative = match.Groups["suffix"].Value.Length > 0   // trailing "-" or "CR"
                || match.Groups["amt"].Value.Contains('(');           // accounting parentheses
            var amount = NormalizeAmount(match.Groups["amt"].Value, negative);
            if (amount is null) continue;

            rowNumber++;
            file.Rows.Add(new ParsedRow
            {
                RowNumber = rowNumber,
                Values = { date, description, amount }
            });
        }

        return file;
    }

    private static string? NormalizeDate(string token)
    {
        var parts = token.Split('/', '-');
        if (parts.Length < 2) return null;
        if (!int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var day))
            return null;
        if (month is < 1 or > 12 || day is < 1 or > 31) return null;

        int year;
        if (parts.Length >= 3 && int.TryParse(parts[2], out var parsedYear))
        {
            year = parsedYear < 100 ? 2000 + parsedYear : parsedYear;
        }
        else
        {
            // Bare MM/DD with no year - assume the current year.
            year = DateTime.Today.Year;
        }

        return $"{month:D2}/{day:D2}/{year:D4}";
    }

    private static string? NormalizeAmount(string token, bool forceNegative)
    {
        var cleaned = token.Replace("$", "").Replace(",", "").Replace("(", "").Replace(")", "").Trim();
        if (!decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return null;

        value = Math.Abs(value);
        if (forceNegative) value = -value;
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public ColumnMapping? DetectColumnMapping(ParsedFile file, AccountType? accountType = null)
    {
        if (file.Headers.Count == 0) return null;

        var mapping = new ColumnMapping
        {
            DateColumn = -1,
            DescriptionColumn = -1,
            AmountColumn = -1,
            HasHeaderRow = file.HasHeaderRow,
            // Debt accounts (credit cards) almost always report charges as positive
            // amounts; default to the credit-card convention so they import as expenses.
            AmountConvention = accountType == AccountType.Debt
                ? AmountConvention.CreditCard
                : AmountConvention.Standard
        };

        for (int i = 0; i < file.Headers.Count; i++)
        {
            var header = file.Headers[i].ToLowerInvariant().Trim();

            if (mapping.DateColumn < 0 && DateColumnNames.Any(n => header.Contains(n)))
                mapping.DateColumn = i;

            if (mapping.DescriptionColumn < 0 && DescriptionColumnNames.Any(n => header.Contains(n)))
                mapping.DescriptionColumn = i;

            if (mapping.AmountColumn < 0 && AmountColumnNames.Any(n => header.Contains(n)))
                mapping.AmountColumn = i;

            if (mapping.DebitColumn == null && DebitColumnNames.Any(n => header.Contains(n)))
                mapping.DebitColumn = i;

            if (mapping.CreditColumn == null && CreditColumnNames.Any(n => header.Contains(n)))
                mapping.CreditColumn = i;

            if (mapping.CategoryColumn == null && CategoryColumnNames.Any(n => header.Contains(n)))
                mapping.CategoryColumn = i;
        }

        // Try to detect date format from sample data
        if (mapping.DateColumn >= 0 && file.Rows.Count > 0)
        {
            mapping.DateFormat = DetectDateFormat(file.Rows.Take(5)
                .Select(r => r.Values.ElementAtOrDefault(mapping.DateColumn) ?? "")
                .ToList());
        }

        // Validate minimum required columns found
        if (mapping.DateColumn < 0 || mapping.DescriptionColumn < 0 ||
            (mapping.AmountColumn < 0 && mapping.DebitColumn == null))
        {
            return null;
        }

        return mapping;
    }

    private string DetectDateFormat(List<string> samples)
    {
        var formats = new[]
        {
            "MM/dd/yyyy", "M/d/yyyy", "MM-dd-yyyy", "M-d-yyyy",
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd",
            "MMM dd, yyyy", "MMMM dd, yyyy"
        };

        foreach (var format in formats)
        {
            var matches = samples.Count(s =>
                DateTime.TryParseExact(s.Trim(), format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _));

            if (matches >= samples.Count * 0.8) // 80% match threshold
                return format;
        }

        return "MM/dd/yyyy"; // Default
    }

    public List<ImportPreviewRow> MapToTransactions(ParsedFile file, ColumnMapping mapping)
    {
        var result = new List<ImportPreviewRow>();

        foreach (var row in file.Rows)
        {
            var preview = new ImportPreviewRow { RowNumber = row.RowNumber };

            // Parse date
            var dateStr = row.Values.ElementAtOrDefault(mapping.DateColumn) ?? "";
            if (!DateTime.TryParseExact(dateStr.Trim(), mapping.DateFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                // Try common formats
                if (!DateTime.TryParse(dateStr.Trim(), out date))
                {
                    preview.IsValid = false;
                    preview.ValidationError = $"Invalid date: {dateStr}";
                    result.Add(preview);
                    continue;
                }
            }
            preview.Date = date;

            // Parse description
            preview.Description = row.Values.ElementAtOrDefault(mapping.DescriptionColumn)?.Trim() ?? "";
            if (string.IsNullOrEmpty(preview.Description))
            {
                preview.IsValid = false;
                preview.ValidationError = "Missing description";
                result.Add(preview);
                continue;
            }

            // Parse amount
            decimal amount = 0;
            if (mapping.DebitColumn.HasValue || mapping.CreditColumn.HasValue)
            {
                // Separate debit/credit columns
                var debitStr = mapping.DebitColumn.HasValue
                    ? row.Values.ElementAtOrDefault(mapping.DebitColumn.Value) ?? ""
                    : "";
                var creditStr = mapping.CreditColumn.HasValue
                    ? row.Values.ElementAtOrDefault(mapping.CreditColumn.Value) ?? ""
                    : "";

                if (TryParseAmount(debitStr, out var debit))
                    amount -= Math.Abs(debit);
                if (TryParseAmount(creditStr, out var credit))
                    amount += Math.Abs(credit);
            }
            else
            {
                // Single amount column
                var amountStr = row.Values.ElementAtOrDefault(mapping.AmountColumn) ?? "";
                if (!TryParseAmount(amountStr, out amount))
                {
                    preview.IsValid = false;
                    preview.ValidationError = $"Invalid amount: {amountStr}";
                    result.Add(preview);
                    continue;
                }

                // Credit-card statements report charges as positive and payments as
                // negative - the opposite of a bank account. Flip the sign so a charge
                // becomes a negative amount (Expense) and a payment becomes positive.
                if (mapping.AmountConvention == AmountConvention.CreditCard)
                {
                    amount = -amount;
                }
            }
            preview.Amount = amount;

            // Parse category if available
            if (mapping.CategoryColumn.HasValue)
            {
                preview.Category = row.Values.ElementAtOrDefault(mapping.CategoryColumn.Value)?.Trim();
            }

            result.Add(preview);
        }

        return result;
    }

    private bool TryParseAmount(string value, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        // Remove currency symbols, spaces, and handle parentheses for negative
        var cleaned = value.Trim()
            .Replace("$", "")
            .Replace("£", "")
            .Replace("€", "")
            .Replace(",", "")
            .Replace(" ", "");

        // Handle accounting format (1,234.56) = -1234.56
        if (cleaned.StartsWith("(") && cleaned.EndsWith(")"))
        {
            cleaned = "-" + cleaned.Trim('(', ')');
        }

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
    }

    public async Task<List<ImportPreviewRow>> DetectDuplicates(
        List<ImportPreviewRow> transactions,
        int accountId,
        FinanceDbContext db)
    {
        var validTransactions = transactions.Where(t => t.IsValid).ToList();
        if (validTransactions.Count == 0) return transactions;

        var minDate = validTransactions.Min(t => t.Date).AddDays(-1);
        var maxDate = validTransactions.Max(t => t.Date).AddDays(1);

        var existingTransactions = await db.Events
            .Where(e => e.AccountId == accountId)
            .Where(e => e.Date >= minDate && e.Date <= maxDate)
            .ToListAsync();

        foreach (var transaction in transactions.Where(t => t.IsValid))
        {
            var duplicate = existingTransactions.FirstOrDefault(e =>
                e.Date.Date == transaction.Date.Date &&
                Math.Abs(e.Amount - Math.Abs(transaction.Amount)) < 0.01m &&
                (e.Description?.Contains(transaction.Description, StringComparison.OrdinalIgnoreCase) == true ||
                 transaction.Description.Contains(e.Description ?? "", StringComparison.OrdinalIgnoreCase)));

            if (duplicate != null)
            {
                transaction.IsDuplicate = true;
                transaction.ExistingTransactionId = duplicate.Id;
                transaction.Selected = false; // Don't select duplicates by default
            }
        }

        return transactions;
    }

    public void CacheSession(string sessionId, ParsedFile file)
    {
        _sessionCache[sessionId] = file;

        // Clean up old sessions (older than 1 hour)
        // In production, use a proper cache with TTL
    }

    public ParsedFile? GetCachedSession(string sessionId)
    {
        return _sessionCache.TryGetValue(sessionId, out var file) ? file : null;
    }

    public void RemoveSession(string sessionId)
    {
        _sessionCache.Remove(sessionId);
    }
}
