using FinanceEngine.Api.Services;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace FinanceEngine.Tests.Services;

public class PdfImportTests
{
    private readonly ImportService _service = new();

    #region Line extraction (deterministic)

    [Fact]
    public void ExtractTransactions_ParsesDateDescriptionAmount()
    {
        var lines = new[]
        {
            "ACME BANK PLATINUM CARD",          // header noise - ignored
            "Statement Period 03/01/2025 - 03/31/2025",
            "Date Description Amount",           // column header noise
            "03/15/2025 STARBUCKS STORE 1234 12.50",
            "03/18/2025 AMAZON.COM 89.99",
            "Previous Balance 1,200.00",         // no leading date - ignored
        };

        var file = _service.ExtractTransactionsFromLines(lines);

        Assert.Equal(new[] { "Date", "Description", "Amount" }, file.Headers);
        Assert.Equal(2, file.Rows.Count);

        Assert.Equal("03/15/2025", file.Rows[0].Values[0]);
        Assert.Equal("STARBUCKS STORE 1234", file.Rows[0].Values[1]);
        Assert.Equal("12.50", file.Rows[0].Values[2]);

        Assert.Equal("AMAZON.COM", file.Rows[1].Values[1]);
        Assert.Equal("89.99", file.Rows[1].Values[2]);
    }

    [Fact]
    public void ExtractTransactions_HandlesCreditsAndNegatives()
    {
        var lines = new[]
        {
            "03/20/2025 PAYMENT THANK YOU 200.00 CR",
            "03/21/2025 REFUND - WALMART 45.00-",
            "03/22/2025 ANNUAL FEE ADJUSTMENT (25.00)",
        };

        var file = _service.ExtractTransactionsFromLines(lines);

        Assert.Equal(3, file.Rows.Count);
        Assert.Equal("-200.00", file.Rows[0].Values[2]); // CR suffix => credit
        Assert.Equal("-45.00", file.Rows[1].Values[2]);  // trailing minus
        Assert.Equal("-25.00", file.Rows[2].Values[2]);  // parentheses
    }

    [Fact]
    public void ExtractTransactions_StripsPostingDateAndInfersYear()
    {
        var lines = new[]
        {
            "03/15 03/16 WHOLE FOODS MARKET 64.20", // trans date + posting date, no year
        };

        var file = _service.ExtractTransactionsFromLines(lines);

        Assert.Single(file.Rows);
        Assert.Equal($"03/15/{DateTime.Today.Year}", file.Rows[0].Values[0]);
        Assert.Equal("WHOLE FOODS MARKET", file.Rows[0].Values[1]);
        Assert.Equal("64.20", file.Rows[0].Values[2]);
    }

    [Fact]
    public void ExtractTransactions_NoTransactions_ReturnsEmptyTable()
    {
        var lines = new[] { "Thank you for being a customer", "Page 1 of 3" };

        var file = _service.ExtractTransactionsFromLines(lines);

        Assert.Empty(file.Rows);
        Assert.Equal(3, file.Headers.Count);
    }

    #endregion

    #region Real PDF round-trip (exercises PdfPig reading path)

    [Fact]
    public void ParseFile_RealPdf_ExtractsTransactions()
    {
        var pdfBytes = BuildStatementPdf(
            "03/15/2025 STARBUCKS 12.50",
            "03/18/2025 AMAZON MARKETPLACE 89.99",
            "03/20/2025 PAYMENT THANK YOU 200.00 CR");

        var base64 = Convert.ToBase64String(pdfBytes);

        var file = _service.ParseFile("statement.pdf", base64);

        Assert.Equal(3, file.Rows.Count);
        Assert.Equal("STARBUCKS", file.Rows[0].Values[1]);
        Assert.Equal("12.50", file.Rows[0].Values[2]);
        Assert.Equal("-200.00", file.Rows[2].Values[2]); // credit preserved through the full path
    }

    private static byte[] BuildStatementPdf(params string[] transactionLines)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);

        // Lay lines top-to-bottom with generous spacing so each is its own visual line.
        var y = 750;
        foreach (var line in transactionLines)
        {
            page.AddText(line, 11, new PdfPoint(40, y), font);
            y -= 24;
        }

        return builder.Build();
    }

    #endregion
}
