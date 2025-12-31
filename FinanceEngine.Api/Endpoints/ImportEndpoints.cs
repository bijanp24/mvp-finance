using FinanceEngine.Api.Models;
using FinanceEngine.Api.Services;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class ImportEndpoints
{
    private static readonly ImportService _importService = new();

    public static RouteGroupBuilder MapImportEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/preview", PreviewImport);
        group.MapPost("/commit", CommitImport);

        return group;
    }

    /// <summary>
    /// Preview an import file, detect columns, and identify duplicates.
    /// </summary>
    private static async Task<IResult> PreviewImport(
        ImportPreviewRequest request,
        FinanceDbContext db)
    {
        var response = new ImportPreviewResponse();

        try
        {
            // Parse the file
            var parsedFile = _importService.ParseFile(request.FileName, request.FileContent);

            if (parsedFile.Rows.Count == 0)
            {
                response.Errors.Add("File contains no data rows");
                return Results.BadRequest(response);
            }

            response.Headers = parsedFile.Headers;
            response.SampleRows = parsedFile.Rows
                .Take(5)
                .Select(r => r.Values)
                .ToList();
            response.TotalRows = parsedFile.Rows.Count;

            // Detect or use provided mapping
            var mapping = request.Mapping ?? _importService.DetectColumnMapping(parsedFile);

            if (mapping == null)
            {
                response.Warnings.Add("Could not auto-detect column mapping. Please configure manually.");
                _importService.CacheSession(response.SessionId, parsedFile);
                return Results.Ok(response);
            }

            response.DetectedMapping = mapping;

            // Map to transactions
            var transactions = _importService.MapToTransactions(parsedFile, mapping);

            // Detect duplicates if account specified
            if (request.AccountId.HasValue)
            {
                transactions = await _importService.DetectDuplicates(transactions, request.AccountId.Value, db);
            }

            response.PreviewTransactions = transactions;

            // Add warnings
            var invalidCount = transactions.Count(t => !t.IsValid);
            var duplicateCount = transactions.Count(t => t.IsDuplicate);

            if (invalidCount > 0)
                response.Warnings.Add($"{invalidCount} row(s) have validation errors and will be skipped");
            if (duplicateCount > 0)
                response.Warnings.Add($"{duplicateCount} potential duplicate(s) detected");

            // Cache the parsed file for commit
            _importService.CacheSession(response.SessionId, parsedFile);

            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            response.Errors.Add(ex.Message);
            return Results.BadRequest(response);
        }
        catch (Exception ex)
        {
            response.Errors.Add($"Failed to parse file: {ex.Message}");
            return Results.BadRequest(response);
        }
    }

    /// <summary>
    /// Commit the previewed import, creating transactions.
    /// </summary>
    private static async Task<IResult> CommitImport(
        ImportCommitRequest request,
        FinanceDbContext db)
    {
        var response = new ImportCommitResponse();

        // Get cached file
        var parsedFile = _importService.GetCachedSession(request.SessionId);
        if (parsedFile == null)
        {
            response.Errors.Add("Import session expired or not found. Please upload the file again.");
            return Results.BadRequest(response);
        }

        // Verify account exists
        var account = await db.Accounts.FindAsync(request.AccountId);
        if (account == null)
        {
            response.Errors.Add($"Account not found: {request.AccountId}");
            return Results.BadRequest(response);
        }

        try
        {
            // Map transactions
            var transactions = _importService.MapToTransactions(parsedFile, request.Mapping);

            // Detect duplicates
            transactions = await _importService.DetectDuplicates(transactions, request.AccountId, db);

            // Filter to selected rows
            var toImport = transactions.Where(t => t.IsValid);

            if (request.SelectedRows != null && request.SelectedRows.Count > 0)
            {
                toImport = toImport.Where(t => request.SelectedRows.Contains(t.RowNumber));
            }

            if (!request.IncludeDuplicates)
            {
                response.DuplicateCount = toImport.Count(t => t.IsDuplicate);
                toImport = toImport.Where(t => !t.IsDuplicate);
            }

            var transactionList = toImport.ToList();

            // Create events
            foreach (var t in transactionList)
            {
                var eventType = t.Amount >= 0 ? EventType.Income : EventType.Expense;
                var eventEntity = new FinancialEventEntity
                {
                    AccountId = request.AccountId,
                    Date = t.Date,
                    Amount = Math.Abs(t.Amount),
                    Type = eventType,
                    Description = t.Description,
                    Status = EventStatus.Cleared, // Imported transactions are already cleared
                    CreatedAt = DateTime.UtcNow
                };

                // Try to match category
                if (!string.IsNullOrEmpty(t.Category))
                {
                    var category = await db.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == t.Category.ToLower());
                    if (category != null)
                    {
                        eventEntity.CategoryId = category.Id;
                    }
                }

                db.Events.Add(eventEntity);
                response.ImportedCount++;
            }

            response.SkippedCount = transactions.Count(t => !t.IsValid);
            response.ErrorCount = transactions.Count(t => !t.IsValid);

            await db.SaveChangesAsync();

            // Clean up session
            _importService.RemoveSession(request.SessionId);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            response.Errors.Add($"Import failed: {ex.Message}");
            return Results.BadRequest(response);
        }
    }
}
