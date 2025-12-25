using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using FinanceEngine.Services;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class RecurringContributionEndpoints
{
    public static RouteGroupBuilder MapRecurringContributionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        group.MapPatch("/{id:int}/toggle", Toggle);
        group.MapGet("/{id:int}/preview", Preview);

        return group;
    }

    private static async Task<IResult> GetAll(FinanceDbContext db)
    {
        var contributions = await db.RecurringContributions
            .Include(c => c.SourceAccount)
            .Include(c => c.TargetAccount)
            .OrderBy(c => c.Name)
            .Select(c => new RecurringContributionDto(
                c.Id,
                c.Name,
                c.Amount,
                c.Frequency.ToString(),
                c.NextContributionDate,
                c.SourceAccountId,
                c.TargetAccountId,
                c.SourceAccount != null ? c.SourceAccount.Name : null,
                c.TargetAccount != null ? c.TargetAccount.Name : null,
                c.IsActive,
                c.CreatedAt
            ))
            .ToListAsync();

        return Results.Ok(contributions);
    }

    private static async Task<IResult> GetById(int id, FinanceDbContext db)
    {
        var contribution = await db.RecurringContributions
            .Include(c => c.SourceAccount)
            .Include(c => c.TargetAccount)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contribution is null)
            return Results.NotFound();

        return Results.Ok(new RecurringContributionDto(
            contribution.Id,
            contribution.Name,
            contribution.Amount,
            contribution.Frequency.ToString(),
            contribution.NextContributionDate,
            contribution.SourceAccountId,
            contribution.TargetAccountId,
            contribution.SourceAccount?.Name,
            contribution.TargetAccount?.Name,
            contribution.IsActive,
            contribution.CreatedAt
        ));
    }

    private static async Task<IResult> Create(CreateRecurringContributionRequest request, FinanceDbContext db)
    {
        // Validation
        var validationError = await ValidateRequest(request.Name, request.Amount, request.Frequency,
            request.SourceAccountId, request.TargetAccountId, db);
        if (validationError is not null)
            return validationError;

        // Parse frequency
        if (!Enum.TryParse<ContributionFrequency>(request.Frequency, true, out var frequency))
            return Results.BadRequest("Invalid frequency. Must be: Weekly, BiWeekly, SemiMonthly, Monthly, Quarterly, or Annually.");

        var contribution = new RecurringContributionEntity
        {
            Name = request.Name,
            Amount = request.Amount,
            Frequency = frequency,
            NextContributionDate = request.NextContributionDate,
            SourceAccountId = request.SourceAccountId,
            TargetAccountId = request.TargetAccountId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.RecurringContributions.Add(contribution);
        await db.SaveChangesAsync();

        // Reload with navigation properties
        await db.Entry(contribution).Reference(c => c.SourceAccount).LoadAsync();
        await db.Entry(contribution).Reference(c => c.TargetAccount).LoadAsync();

        var dto = new RecurringContributionDto(
            contribution.Id,
            contribution.Name,
            contribution.Amount,
            contribution.Frequency.ToString(),
            contribution.NextContributionDate,
            contribution.SourceAccountId,
            contribution.TargetAccountId,
            contribution.SourceAccount?.Name,
            contribution.TargetAccount?.Name,
            contribution.IsActive,
            contribution.CreatedAt
        );

        return Results.Created($"/api/recurring-contributions/{contribution.Id}", dto);
    }

    private static async Task<IResult> Update(int id, UpdateRecurringContributionRequest request, FinanceDbContext db)
    {
        var contribution = await db.RecurringContributions.FindAsync(id);
        if (contribution is null)
            return Results.NotFound();

        // Validation
        var validationError = await ValidateRequest(request.Name, request.Amount, request.Frequency,
            request.SourceAccountId, request.TargetAccountId, db);
        if (validationError is not null)
            return validationError;

        // Parse frequency
        if (!Enum.TryParse<ContributionFrequency>(request.Frequency, true, out var frequency))
            return Results.BadRequest("Invalid frequency. Must be: Weekly, BiWeekly, SemiMonthly, Monthly, Quarterly, or Annually.");

        // Update fields
        contribution.Name = request.Name;
        contribution.Amount = request.Amount;
        contribution.Frequency = frequency;
        contribution.NextContributionDate = request.NextContributionDate;
        contribution.SourceAccountId = request.SourceAccountId;
        contribution.TargetAccountId = request.TargetAccountId;
        contribution.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        // Reload with navigation properties
        await db.Entry(contribution).Reference(c => c.SourceAccount).LoadAsync();
        await db.Entry(contribution).Reference(c => c.TargetAccount).LoadAsync();

        return Results.Ok(new RecurringContributionDto(
            contribution.Id,
            contribution.Name,
            contribution.Amount,
            contribution.Frequency.ToString(),
            contribution.NextContributionDate,
            contribution.SourceAccountId,
            contribution.TargetAccountId,
            contribution.SourceAccount?.Name,
            contribution.TargetAccount?.Name,
            contribution.IsActive,
            contribution.CreatedAt
        ));
    }

    private static async Task<IResult> Delete(int id, FinanceDbContext db)
    {
        var contribution = await db.RecurringContributions.FindAsync(id);
        if (contribution is null)
            return Results.NotFound();

        // Soft delete
        contribution.IsActive = false;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> Toggle(int id, FinanceDbContext db)
    {
        var contribution = await db.RecurringContributions.FindAsync(id);
        if (contribution is null)
            return Results.NotFound();

        contribution.IsActive = !contribution.IsActive;
        await db.SaveChangesAsync();

        // Reload with navigation properties
        await db.Entry(contribution).Reference(c => c.SourceAccount).LoadAsync();
        await db.Entry(contribution).Reference(c => c.TargetAccount).LoadAsync();

        return Results.Ok(new RecurringContributionDto(
            contribution.Id,
            contribution.Name,
            contribution.Amount,
            contribution.Frequency.ToString(),
            contribution.NextContributionDate,
            contribution.SourceAccountId,
            contribution.TargetAccountId,
            contribution.SourceAccount?.Name,
            contribution.TargetAccount?.Name,
            contribution.IsActive,
            contribution.CreatedAt
        ));
    }

    private static async Task<IResult> Preview(int id, FinanceDbContext db)
    {
        var contribution = await db.RecurringContributions.FindAsync(id);
        if (contribution is null)
            return Results.NotFound();

        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var endDate = startDate.AddYears(1);

        // Convert ContributionFrequency to RecurringFrequency
        var recurringFrequency = (RecurringFrequency)(int)contribution.Frequency;

        var occurrences = RecurringEventExpansionService.ExpandContributions(
            contribution.Amount,
            recurringFrequency,
            contribution.NextContributionDate,
            startDate,
            endDate
        ).Take(12);

        return Results.Ok(new ContributionPreviewResponse(
            contribution.Id,
            contribution.Name,
            occurrences.Select(o => new ContributionOccurrence(o.Date, o.Amount))
        ));
    }

    private static async Task<IResult?> ValidateRequest(
        string name,
        decimal amount,
        string frequency,
        int sourceAccountId,
        int targetAccountId,
        FinanceDbContext db)
    {
        // Name validation
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest("Name is required.");

        if (name.Length > 100)
            return Results.BadRequest("Name must not exceed 100 characters.");

        // Amount validation
        if (amount <= 0)
            return Results.BadRequest("Amount must be greater than zero.");

        // Frequency validation
        if (!Enum.TryParse<ContributionFrequency>(frequency, true, out _))
            return Results.BadRequest("Invalid frequency. Must be: Weekly, BiWeekly, SemiMonthly, Monthly, Quarterly, or Annually.");

        // Source and target cannot be the same
        if (sourceAccountId == targetAccountId)
            return Results.BadRequest("Source and target accounts must be different.");

        // Source account validation
        var sourceAccount = await db.Accounts.FindAsync(sourceAccountId);
        if (sourceAccount is null)
            return Results.BadRequest("Source account does not exist.");

        if (sourceAccount.Type != Data.Entities.AccountType.Cash)
            return Results.BadRequest("Source account must be of type Cash.");

        // Target account validation
        var targetAccount = await db.Accounts.FindAsync(targetAccountId);
        if (targetAccount is null)
            return Results.BadRequest("Target account does not exist.");

        if (targetAccount.Type != Data.Entities.AccountType.Investment && targetAccount.Type != Data.Entities.AccountType.Cash)
            return Results.BadRequest("Target account must be of type Investment or Cash.");

        return null;
    }
}

// DTOs
public record RecurringContributionDto(
    int Id,
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId,
    string? SourceAccountName,
    string? TargetAccountName,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateRecurringContributionRequest(
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId
);

public record UpdateRecurringContributionRequest(
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId,
    bool IsActive
);

public record ContributionPreviewResponse(
    int Id,
    string Name,
    IEnumerable<ContributionOccurrence> Occurrences
);

public record ContributionOccurrence(
    DateTime Date,
    decimal Amount
);

