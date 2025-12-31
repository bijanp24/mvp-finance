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
        group.MapGet("/recurring", ExportRecurringContributions);
        group.MapGet("/full", ExportFullData);
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

    /// <summary>
    /// Export recurring contributions to CSV or Excel.
    /// </summary>
    private static async Task<IResult> ExportRecurringContributions(
        FinanceDbContext db,
        string format = "csv")
    {
        var contributions = await db.RecurringContributions
            .Include(r => r.SourceAccount)
            .Include(r => r.TargetAccount)
            .ToListAsync();

        var rows = contributions.Select(r => new RecurringContributionExportRow
        {
            Name = r.Name,
            FromAccount = r.SourceAccount?.Name ?? "N/A",
            ToAccount = r.TargetAccount?.Name ?? "Unknown",
            Amount = r.Amount,
            Frequency = r.Frequency.ToString(),
            NextDate = r.NextContributionDate.ToString("yyyy-MM-dd"),
            IsActive = r.IsActive ? "Yes" : "No"
        }).ToList();

        return format.ToLowerInvariant() switch
        {
            "xlsx" or "excel" => GenerateExcelFile(rows, "RecurringContributions", "recurring-contributions.xlsx"),
            _ => GenerateCsvFile(rows, "recurring-contributions.csv")
        };
    }

    /// <summary>
    /// Export all financial data to a multi-sheet Excel workbook.
    /// </summary>
    private static async Task<IResult> ExportFullData(FinanceDbContext db)
    {
        // Gather all data
        var accounts = await db.Accounts.ToListAsync();
        var events = await db.Events.Include(e => e.Account).Include(e => e.Category).ToListAsync();
        var contributions = await db.RecurringContributions
            .Include(r => r.SourceAccount)
            .Include(r => r.TargetAccount)
            .ToListAsync();
        var categories = await db.Categories.ToListAsync();
        var budgets = await db.Budgets.Include(b => b.Category).ToListAsync();
        var goals = await db.Goals.ToListAsync();

        // Build account rows with balances
        var accountRows = accounts.Select(a =>
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

        // Build transaction rows
        var transactionRows = events
            .OrderByDescending(e => e.Date)
            .Select(t => new TransactionExportRow
            {
                Date = t.Date.ToString("yyyy-MM-dd"),
                Type = t.Type.ToString(),
                Description = t.Description ?? "",
                Amount = t.Amount,
                Account = t.Account?.Name ?? "Unknown",
                Category = t.Category?.Name ?? "",
                Status = t.Status.ToString()
            }).ToList();

        // Build recurring contribution rows
        var recurringRows = contributions.Select(r => new RecurringContributionExportRow
        {
            Name = r.Name,
            FromAccount = r.SourceAccount?.Name ?? "N/A",
            ToAccount = r.TargetAccount?.Name ?? "Unknown",
            Amount = r.Amount,
            Frequency = r.Frequency.ToString(),
            NextDate = r.NextContributionDate.ToString("yyyy-MM-dd"),
            IsActive = r.IsActive ? "Yes" : "No"
        }).ToList();

        // Build category rows
        var categoryRows = categories.Select(c => new CategoryExportRow
        {
            Name = c.Name,
            Color = c.Color ?? "",
            IsActive = c.IsActive ? "Yes" : "No"
        }).ToList();

        // Build budget rows
        var budgetRows = budgets.Select(b => new BudgetExportRow
        {
            Category = b.Category?.Name ?? "Unknown",
            Amount = b.Amount,
            Frequency = b.Frequency.ToString(),
            EffectiveDate = b.EffectiveDate.ToString("yyyy-MM-dd"),
            EndDate = b.EndDate?.ToString("yyyy-MM-dd") ?? "Ongoing",
            IsActive = b.IsActive ? "Yes" : "No"
        }).ToList();

        // Build goal rows
        var goalRows = goals.Select(g => new GoalExportRow
        {
            Name = g.Name,
            Type = g.Type.ToString(),
            TargetAmount = g.TargetAmount ?? 0m,
            TargetDate = g.TargetDate.ToString("yyyy-MM-dd"),
            Priority = g.Priority,
            IsActive = g.IsActive ? "Yes" : "No"
        }).ToList();

        // Build summary
        var totalAssets = accountRows.Where(a => a.Type != "Debt").Sum(a => a.CurrentBalance);
        var totalDebt = accountRows.Where(a => a.Type == "Debt").Sum(a => Math.Abs(a.CurrentBalance));
        var netWorth = totalAssets - totalDebt;

        // Generate multi-sheet Excel
        return GenerateMultiSheetExcel(
            accountRows,
            transactionRows,
            recurringRows,
            categoryRows,
            budgetRows,
            goalRows,
            totalAssets,
            totalDebt,
            netWorth
        );
    }

    private static IResult GenerateMultiSheetExcel(
        List<AccountExportRow> accounts,
        List<TransactionExportRow> transactions,
        List<RecurringContributionExportRow> recurring,
        List<CategoryExportRow> categories,
        List<BudgetExportRow> budgets,
        List<GoalExportRow> goals,
        decimal totalAssets,
        decimal totalDebt,
        decimal netWorth)
    {
        using var workbook = new XLWorkbook();

        // Summary sheet
        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Finance Dashboard - Full Data Export";
        summary.Cell(1, 1).Style.Font.Bold = true;
        summary.Cell(1, 1).Style.Font.FontSize = 16;

        summary.Cell(3, 1).Value = "Export Date:";
        summary.Cell(3, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        summary.Cell(5, 1).Value = "Financial Summary";
        summary.Cell(5, 1).Style.Font.Bold = true;

        summary.Cell(6, 1).Value = "Total Assets:";
        summary.Cell(6, 2).Value = totalAssets;
        summary.Cell(6, 2).Style.NumberFormat.Format = "$#,##0.00";

        summary.Cell(7, 1).Value = "Total Debt:";
        summary.Cell(7, 2).Value = totalDebt;
        summary.Cell(7, 2).Style.NumberFormat.Format = "$#,##0.00";

        summary.Cell(8, 1).Value = "Net Worth:";
        summary.Cell(8, 2).Value = netWorth;
        summary.Cell(8, 2).Style.NumberFormat.Format = "$#,##0.00";
        summary.Cell(8, 2).Style.Font.Bold = true;

        summary.Cell(10, 1).Value = "Data Counts";
        summary.Cell(10, 1).Style.Font.Bold = true;

        summary.Cell(11, 1).Value = "Accounts:";
        summary.Cell(11, 2).Value = accounts.Count;

        summary.Cell(12, 1).Value = "Transactions:";
        summary.Cell(12, 2).Value = transactions.Count;

        summary.Cell(13, 1).Value = "Recurring Contributions:";
        summary.Cell(13, 2).Value = recurring.Count;

        summary.Cell(14, 1).Value = "Categories:";
        summary.Cell(14, 2).Value = categories.Count;

        summary.Cell(15, 1).Value = "Budgets:";
        summary.Cell(15, 2).Value = budgets.Count;

        summary.Cell(16, 1).Value = "Goals:";
        summary.Cell(16, 2).Value = goals.Count;

        summary.Columns().AdjustToContents();

        // Add data sheets
        AddDataSheet(workbook, "Accounts", accounts);
        AddDataSheet(workbook, "Transactions", transactions);
        AddDataSheet(workbook, "Recurring", recurring);
        AddDataSheet(workbook, "Categories", categories);
        AddDataSheet(workbook, "Budgets", budgets);
        AddDataSheet(workbook, "Goals", goals);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        var bytes = memoryStream.ToArray();

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
        return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"finance-export-{timestamp}.xlsx");
    }

    private static void AddDataSheet<T>(XLWorkbook workbook, string sheetName, List<T> data)
    {
        var worksheet = workbook.Worksheets.Add(sheetName);
        var properties = typeof(T).GetProperties();

        // Write headers
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        // Write data
        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(data[row]);
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

        worksheet.Columns().AdjustToContents();
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

public class RecurringContributionExportRow
{
    public string Name { get; set; } = "";
    public string FromAccount { get; set; } = "";
    public string ToAccount { get; set; } = "";
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "";
    public string NextDate { get; set; } = "";
    public string IsActive { get; set; } = "";
}

public class CategoryExportRow
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public string IsActive { get; set; } = "";
}

public class BudgetExportRow
{
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "";
    public string EffectiveDate { get; set; } = "";
    public string EndDate { get; set; } = "";
    public string IsActive { get; set; } = "";
}

public class GoalExportRow
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal TargetAmount { get; set; }
    public string TargetDate { get; set; } = "";
    public int Priority { get; set; }
    public string IsActive { get; set; } = "";
}

#endregion
