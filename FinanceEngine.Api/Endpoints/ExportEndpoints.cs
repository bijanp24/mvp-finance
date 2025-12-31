using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FinanceEngine.Api.Services;
using FinanceEngine.Calculators;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Services;
using Microsoft.EntityFrameworkCore;
using ServiceAccountType = FinanceEngine.Services.AccountType;
using EntityAccountType = FinanceEngine.Data.Entities.AccountType;
using ServiceEventType = FinanceEngine.Services.EventType;
using EntityEventType = FinanceEngine.Data.Entities.EventType;
using EntityTimeHorizon = FinanceEngine.Data.Entities.TimeHorizon;

namespace FinanceEngine.Api.Endpoints;

public static class ExportEndpoints
{
    public static RouteGroupBuilder MapExportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/projections", ExportProjections);
        group.MapGet("/transactions", ExportTransactions);
        group.MapGet("/accounts", ExportAccounts);
        group.MapPost("/chart-pdf", ExportChartPdf);

        return group;
    }

    /// <summary>
    /// Export a chart as a PDF document.
    /// </summary>
    private static IResult ExportChartPdf(ChartPdfExportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest("Title is required");

        if (string.IsNullOrWhiteSpace(request.ChartImage))
            return Results.BadRequest("Chart image is required");

        try
        {
            // Parse base64 image (may include data URI prefix)
            var base64Data = request.ChartImage;
            if (base64Data.Contains(','))
            {
                base64Data = base64Data.Split(',')[1];
            }

            var imageBytes = Convert.FromBase64String(base64Data);

            var pdfService = new PdfExportService();
            var pdfBytes = pdfService.GenerateChartPdf(new ChartPdfRequest
            {
                Title = request.Title,
                Description = request.Description,
                DateRange = request.DateRange,
                ChartImageBytes = imageBytes
            });

            return Results.File(pdfBytes, "application/pdf", $"{SanitizeFilename(request.Title)}.pdf");
        }
        catch (FormatException)
        {
            return Results.BadRequest("Invalid base64 image data");
        }
    }

    private static string SanitizeFilename(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    /// <summary>
    /// Export projection data to CSV or Excel.
    /// </summary>
    private static async Task<IResult> ExportProjections(
        FinanceDbContext db,
        string format = "csv",
        string? startDate = null,
        string? endDate = null,
        int months = 12)
    {
        // Parse dates
        var start = ParseDate(startDate) ?? DateTime.UtcNow.Date;
        var end = ParseDate(endDate) ?? start.AddMonths(months);

        // Get accounts and events
        var accounts = await db.Accounts.Where(a => a.IsActive).ToListAsync();
        var events = await db.Events.ToListAsync();
        var settings = await GetOrCreateSettings(db);
        var recurringContributions = await db.RecurringContributions.Where(r => r.IsActive).ToListAsync();

        // Build projection data using ForwardSimulationEngine
        var projectionRows = GenerateProjectionData(accounts, events, recurringContributions, settings, start, end);

        return format.ToLowerInvariant() switch
        {
            "xlsx" or "excel" => GenerateExcelFile(projectionRows, "Projections", "projections.xlsx"),
            _ => GenerateCsvFile(projectionRows, "projections.csv")
        };
    }

    /// <summary>
    /// Export transaction history to CSV or Excel.
    /// </summary>
    private static async Task<IResult> ExportTransactions(
        FinanceDbContext db,
        string format = "csv",
        string? startDate = null,
        string? endDate = null)
    {
        // Parse dates
        var start = ParseDate(startDate);
        var end = ParseDate(endDate);

        // Query transactions
        var query = db.Events.Include(e => e.Account).Include(e => e.Category).AsQueryable();

        if (start.HasValue)
            query = query.Where(e => e.Date >= start.Value);
        if (end.HasValue)
            query = query.Where(e => e.Date <= end.Value);

        var transactions = await query.OrderByDescending(e => e.Date).ToListAsync();

        var rows = transactions.Select(t => new TransactionExportRow
        {
            Date = t.Date.ToString("yyyy-MM-dd"),
            Type = t.Type.ToString(),
            Description = t.Description ?? "",
            Amount = t.Amount,
            Account = t.Account?.Name ?? "Unknown",
            Category = t.Category?.Name ?? "",
            Status = t.Status.ToString()
        }).ToList();

        return format.ToLowerInvariant() switch
        {
            "xlsx" or "excel" => GenerateExcelFile(rows, "Transactions", "transactions.xlsx"),
            _ => GenerateCsvFile(rows, "transactions.csv")
        };
    }

    /// <summary>
    /// Export account summary to CSV or Excel.
    /// </summary>
    private static async Task<IResult> ExportAccounts(
        FinanceDbContext db,
        string format = "csv")
    {
        var accounts = await db.Accounts.ToListAsync();
        var events = await db.Events.ToListAsync();

        var rows = accounts.Select(a =>
        {
            var accountEvents = events
                .Where(e => e.AccountId == a.Id)
                .Select(e => new FinancialEvent(MapEventType(e.Type), e.Amount));

            var balance = BalanceCalculator.Calculate(
                MapAccountType(a.Type),
                a.InitialBalance,
                accountEvents
            );

            return new AccountExportRow
            {
                Name = a.Name,
                Type = a.Type.ToString(),
                CurrentBalance = balance,
                InitialBalance = a.InitialBalance,
                IsActive = a.IsActive ? "Yes" : "No",
                CreatedAt = a.CreatedAt.ToString("yyyy-MM-dd")
            };
        }).ToList();

        return format.ToLowerInvariant() switch
        {
            "xlsx" or "excel" => GenerateExcelFile(rows, "Accounts", "accounts.xlsx"),
            _ => GenerateCsvFile(rows, "accounts.csv")
        };
    }

    #region Projection Data Generation

    private static List<ProjectionExportRow> GenerateProjectionData(
        List<AccountEntity> accounts,
        List<FinancialEventEntity> events,
        List<RecurringContributionEntity> recurringContributions,
        UserSettingsEntity settings,
        DateTime start,
        DateTime end)
    {
        var rows = new List<ProjectionExportRow>();
        var currentDate = start;

        while (currentDate <= end)
        {
            decimal totalNetWorth = 0m;

            foreach (var account in accounts)
            {
                // Get events up to this date
                var accountEvents = events
                    .Where(e => e.AccountId == account.Id && e.Date <= currentDate)
                    .Select(e => new FinancialEvent(MapEventType(e.Type), e.Amount));

                var balance = BalanceCalculator.Calculate(
                    MapAccountType(account.Type),
                    account.InitialBalance,
                    accountEvents
                );

                // Adjust for recurring contributions between original date and projection date
                var contributionAdjustment = CalculateContributionAdjustment(
                    recurringContributions, account.Id, DateTime.UtcNow, currentDate);

                if (account.Type == EntityAccountType.Investment)
                    balance += contributionAdjustment;
                else if (account.Type == EntityAccountType.Debt)
                    balance -= contributionAdjustment;

                // Calculate net worth contribution
                if (account.Type == EntityAccountType.Debt)
                    totalNetWorth -= Math.Abs(balance);
                else
                    totalNetWorth += balance;

                rows.Add(new ProjectionExportRow
                {
                    Date = currentDate.ToString("yyyy-MM-dd"),
                    AccountName = account.Name,
                    AccountType = account.Type.ToString(),
                    Balance = balance,
                    NetWorth = 0 // Will set after loop
                });
            }

            // Update net worth for all rows on this date
            var dateRows = rows.Where(r => r.Date == currentDate.ToString("yyyy-MM-dd")).ToList();
            foreach (var row in dateRows)
            {
                row.NetWorth = totalNetWorth;
            }

            currentDate = currentDate.AddMonths(1);
        }

        return rows;
    }

    private static decimal CalculateContributionAdjustment(
        List<RecurringContributionEntity> contributions,
        int accountId,
        DateTime from,
        DateTime to)
    {
        decimal total = 0m;

        foreach (var contrib in contributions.Where(c => c.TargetAccountId == accountId))
        {
            var current = contrib.NextContributionDate;
            while (current < to)
            {
                if (current >= from && current <= to)
                {
                    total += contrib.Amount;
                }

                current = contrib.Frequency switch
                {
                    ContributionFrequency.Weekly => current.AddDays(7),
                    ContributionFrequency.BiWeekly => current.AddDays(14),
                    ContributionFrequency.Monthly => current.AddMonths(1),
                    _ => current.AddMonths(1)
                };
            }
        }

        return total;
    }

    #endregion

    #region File Generation

    private static IResult GenerateCsvFile<T>(List<T> rows, string fileName)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        csv.WriteRecords(rows);
        writer.Flush();

        var bytes = memoryStream.ToArray();
        return Results.File(bytes, "text/csv", fileName);
    }

    private static IResult GenerateExcelFile<T>(List<T> rows, string sheetName, string fileName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Get properties for headers
        var properties = typeof(T).GetProperties();

        // Write headers
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        // Write data
        for (int row = 0; row < rows.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(rows[row]);
                var cell = worksheet.Cell(row + 2, col + 1);

                if (value is decimal decimalValue)
                {
                    cell.Value = decimalValue;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                }
                else if (value is int intValue)
                {
                    cell.Value = intValue;
                }
                else
                {
                    cell.Value = value?.ToString() ?? "";
                }
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        var bytes = memoryStream.ToArray();

        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    #endregion

    #region Helper Methods

    private static DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return null;

        if (DateTime.TryParse(dateStr, out var date))
            return date;

        return null;
    }

    private static async Task<UserSettingsEntity> GetOrCreateSettings(FinanceDbContext db)
    {
        var settings = await db.UserSettings.FirstOrDefaultAsync(s => s.IsActive);

        if (settings is null)
        {
            settings = new UserSettingsEntity
            {
                PayFrequency = PayFrequency.BiWeekly,
                PaycheckAmount = 2500m,
                SafetyBuffer = 100m,
                PreferredTimeHorizon = EntityTimeHorizon.NextPaycheck,
                IsActive = true
            };
            db.UserSettings.Add(settings);
            await db.SaveChangesAsync();
        }

        return settings;
    }

    private static ServiceAccountType MapAccountType(EntityAccountType type)
    {
        return type switch
        {
            EntityAccountType.Cash => ServiceAccountType.Cash,
            EntityAccountType.Debt => ServiceAccountType.Debt,
            EntityAccountType.Investment => ServiceAccountType.Investment,
            _ => ServiceAccountType.Cash
        };
    }

    private static ServiceEventType MapEventType(EntityEventType type)
    {
        return type switch
        {
            EntityEventType.Income => ServiceEventType.Income,
            EntityEventType.Expense => ServiceEventType.Expense,
            EntityEventType.DebtCharge => ServiceEventType.DebtCharge,
            EntityEventType.DebtPayment => ServiceEventType.DebtPayment,
            EntityEventType.InterestFee => ServiceEventType.InterestFee,
            EntityEventType.SavingsContribution => ServiceEventType.SavingsContribution,
            EntityEventType.InvestmentContribution => ServiceEventType.InvestmentContribution,
            _ => ServiceEventType.Expense
        };
    }

    #endregion
}

#region Export DTOs

public class ProjectionExportRow
{
    public string Date { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string AccountType { get; set; } = "";
    public decimal Balance { get; set; }
    public decimal NetWorth { get; set; }
}

public class TransactionExportRow
{
    public string Date { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Account { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
}

public class AccountExportRow
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public decimal InitialBalance { get; set; }
    public string IsActive { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class ChartPdfExportRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? DateRange { get; set; }
    public required string ChartImage { get; set; }
}

#endregion
