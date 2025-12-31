using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinanceEngine.Api.Services;

public class PdfExportService
{
    static PdfExportService()
    {
        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateChartPdf(ChartPdfRequest request)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(c => ComposeHeader(c, request.Title));
                page.Content().Element(c => ComposeContent(c, request));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, string title)
    {
        container.Column(column =>
        {
            column.Item().Text(title)
                .FontSize(24)
                .SemiBold()
                .FontColor(Colors.Blue.Darken3);

            column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, ChartPdfRequest request)
    {
        container.Column(column =>
        {
            // Description if provided
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                column.Item()
                    .PaddingTop(15)
                    .Text(request.Description)
                    .FontSize(12)
                    .FontColor(Colors.Grey.Darken2);
            }

            // Chart image
            column.Item()
                .PaddingTop(20)
                .AlignCenter()
                .Image(request.ChartImageBytes)
                .FitArea();

            // Date range if provided
            if (!string.IsNullOrWhiteSpace(request.DateRange))
            {
                column.Item()
                    .PaddingTop(15)
                    .AlignCenter()
                    .Text($"Date Range: {request.DateRange}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generated on ")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Medium);
                text.Span(DateTime.Now.ToString("MMMM dd, yyyy 'at' h:mm tt"))
                    .FontSize(9)
                    .FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Finance Dashboard")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Medium);
            });
        });
    }
}

public class ChartPdfRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? DateRange { get; set; }
    public required byte[] ChartImageBytes { get; set; }
}
