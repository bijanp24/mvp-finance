using System.Text.Json.Serialization;

namespace FinanceEngine.Api.Models;

/// <summary>
/// How to interpret the sign of a single amount column.
/// Bank statements and credit-card statements use opposite conventions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AmountConvention
{
    /// <summary>
    /// Standard bank/checking convention: positive = money in (income/deposit),
    /// negative = money out (expense). This is the default.
    /// </summary>
    Standard,

    /// <summary>
    /// Credit-card statement convention: positive = a charge you made (expense),
    /// negative = a payment/credit/refund. The amount sign is flipped on import.
    /// </summary>
    CreditCard
}

public class ImportPreviewRequest
{
    public required string FileName { get; set; }
    public required string FileContent { get; set; } // Base64 encoded
    public int? AccountId { get; set; } // Default account for imported transactions
    public ColumnMapping? Mapping { get; set; } // Optional custom mapping
}

public class ColumnMapping
{
    public int DateColumn { get; set; }
    public int DescriptionColumn { get; set; }
    public int AmountColumn { get; set; }
    public int? DebitColumn { get; set; } // For banks with separate debit/credit columns
    public int? CreditColumn { get; set; }
    public int? CategoryColumn { get; set; }
    public string DateFormat { get; set; } = "MM/dd/yyyy";
    public bool HasHeaderRow { get; set; } = true;

    /// <summary>
    /// Sign convention for the single amount column. Ignored when separate
    /// debit/credit columns are used (those are already unambiguous).
    /// Defaults to <see cref="AmountConvention.CreditCard"/> when importing into a
    /// Debt account, otherwise <see cref="AmountConvention.Standard"/>.
    /// </summary>
    public AmountConvention AmountConvention { get; set; } = AmountConvention.Standard;
}

public class ImportPreviewResponse
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public List<string> Headers { get; set; } = new();
    public List<List<string>> SampleRows { get; set; } = new(); // First 5 rows
    public int TotalRows { get; set; }
    public ColumnMapping? DetectedMapping { get; set; }
    public List<ImportPreviewRow> PreviewTransactions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class ImportPreviewRow
{
    public int RowNumber { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Category { get; set; }
    public bool IsDuplicate { get; set; }
    public int? ExistingTransactionId { get; set; } // If duplicate, the ID of existing
    public bool IsValid { get; set; } = true;
    public string? ValidationError { get; set; }
    public bool Selected { get; set; } = true; // Whether to import this row
}

public class ImportCommitRequest
{
    public required string SessionId { get; set; }
    public required int AccountId { get; set; }
    public required ColumnMapping Mapping { get; set; }
    public List<int>? SelectedRows { get; set; } // If null, import all valid non-duplicate rows
    public bool IncludeDuplicates { get; set; } = false;
}

public class ImportCommitResponse
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ParsedRow
{
    public int RowNumber { get; set; }
    public List<string> Values { get; set; } = new();
}

public class ParsedFile
{
    public List<string> Headers { get; set; } = new();
    public List<ParsedRow> Rows { get; set; } = new();
    public bool HasHeaderRow { get; set; }
}
