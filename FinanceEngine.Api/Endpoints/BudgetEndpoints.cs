using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class BudgetEndpoints
{
    public static RouteGroupBuilder MapBudgetEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllBudgets);
        group.MapGet("/{id}", GetBudgetById);
        group.MapPost("/", CreateBudget);
        group.MapPut("/{id}", UpdateBudget);
        group.MapDelete("/{id}", DeleteBudget);

        return group;
    }

    private static async Task<IResult> GetAllBudgets(FinanceDbContext db, bool? activeOnly = true, int? categoryId = null)
    {
        var query = db.Budgets
            .Include(b => b.Category)
            .Include(b => b.LinkedAccount)
            .AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(b => b.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        var budgets = await query
            .OrderBy(b => b.Category.SortOrder)
            .ThenBy(b => b.EffectiveDate)
            .Select(b => new BudgetDto(
                b.Id,
                b.CategoryId,
                b.Category.Name,
                b.Amount,
                b.Frequency.ToString(),
                b.EffectiveDate,
                b.EndDate,
                b.LinkedAccountId,
                b.LinkedAccount != null ? b.LinkedAccount.Name : null,
                b.Notes,
                b.IsActive
            ))
            .ToListAsync();

        return Results.Ok(budgets);
    }

    private static async Task<IResult> GetBudgetById(int id, FinanceDbContext db)
    {
        var budget = await db.Budgets
            .Include(b => b.Category)
            .Include(b => b.LinkedAccount)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (budget is null)
            return Results.NotFound();

        return Results.Ok(new BudgetDto(
            budget.Id,
            budget.CategoryId,
            budget.Category.Name,
            budget.Amount,
            budget.Frequency.ToString(),
            budget.EffectiveDate,
            budget.EndDate,
            budget.LinkedAccountId,
            budget.LinkedAccount?.Name,
            budget.Notes,
            budget.IsActive
        ));
    }

    private static async Task<IResult> CreateBudget(CreateBudgetRequest request, FinanceDbContext db)
    {
        // Validate category exists
        var category = await db.Categories.FindAsync(request.CategoryId);
        if (category is null)
            return Results.BadRequest("Category not found");

        if (!category.IsActive)
            return Results.BadRequest("Cannot create budget for inactive category");

        // Validate amount
        if (request.Amount <= 0)
            return Results.BadRequest("Budget amount must be greater than 0");

        // Validate frequency
        if (!Enum.TryParse<BudgetFrequency>(request.Frequency, true, out var frequency))
            return Results.BadRequest("Invalid frequency. Must be 'Monthly', 'BiWeekly', or 'Weekly'");

        // Validate linked account if provided
        if (request.LinkedAccountId.HasValue)
        {
            var account = await db.Accounts.FindAsync(request.LinkedAccountId.Value);
            if (account is null)
                return Results.BadRequest("Linked account not found");
        }

        // Validate dates
        if (request.EndDate.HasValue && request.EndDate.Value <= request.EffectiveDate)
            return Results.BadRequest("End date must be after effective date");

        var budget = new BudgetEntity
        {
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Frequency = frequency,
            EffectiveDate = request.EffectiveDate,
            EndDate = request.EndDate,
            LinkedAccountId = request.LinkedAccountId,
            Notes = request.Notes,
            IsActive = true
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        // Reload with navigation properties
        await db.Entry(budget).Reference(b => b.Category).LoadAsync();
        if (budget.LinkedAccountId.HasValue)
            await db.Entry(budget).Reference(b => b.LinkedAccount).LoadAsync();

        return Results.Created($"/api/budgets/{budget.Id}", new BudgetDto(
            budget.Id,
            budget.CategoryId,
            budget.Category.Name,
            budget.Amount,
            budget.Frequency.ToString(),
            budget.EffectiveDate,
            budget.EndDate,
            budget.LinkedAccountId,
            budget.LinkedAccount?.Name,
            budget.Notes,
            budget.IsActive
        ));
    }

    private static async Task<IResult> UpdateBudget(int id, UpdateBudgetRequest request, FinanceDbContext db)
    {
        var budget = await db.Budgets
            .Include(b => b.Category)
            .Include(b => b.LinkedAccount)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (budget is null)
            return Results.NotFound();

        // Validate and update category if provided
        if (request.CategoryId.HasValue)
        {
            var category = await db.Categories.FindAsync(request.CategoryId.Value);
            if (category is null)
                return Results.BadRequest("Category not found");

            if (!category.IsActive)
                return Results.BadRequest("Cannot assign budget to inactive category");

            budget.CategoryId = request.CategoryId.Value;
            budget.Category = category;
        }

        // Validate and update amount if provided
        if (request.Amount.HasValue)
        {
            if (request.Amount.Value <= 0)
                return Results.BadRequest("Budget amount must be greater than 0");

            budget.Amount = request.Amount.Value;
        }

        // Validate and update frequency if provided
        if (request.Frequency is not null)
        {
            if (!Enum.TryParse<BudgetFrequency>(request.Frequency, true, out var frequency))
                return Results.BadRequest("Invalid frequency. Must be 'Monthly', 'BiWeekly', or 'Weekly'");

            budget.Frequency = frequency;
        }

        // Update effective date if provided
        if (request.EffectiveDate.HasValue)
        {
            budget.EffectiveDate = request.EffectiveDate.Value;
        }

        // Update end date if provided (can be set to null)
        if (request.EndDate.HasValue)
        {
            if (request.EndDate.Value <= budget.EffectiveDate)
                return Results.BadRequest("End date must be after effective date");

            budget.EndDate = request.EndDate.Value;
        }
        else if (request.ClearEndDate == true)
        {
            budget.EndDate = null;
        }

        // Validate and update linked account if provided
        if (request.LinkedAccountId.HasValue)
        {
            var account = await db.Accounts.FindAsync(request.LinkedAccountId.Value);
            if (account is null)
                return Results.BadRequest("Linked account not found");

            budget.LinkedAccountId = request.LinkedAccountId.Value;
            budget.LinkedAccount = account;
        }
        else if (request.ClearLinkedAccount == true)
        {
            budget.LinkedAccountId = null;
            budget.LinkedAccount = null;
        }

        // Update notes if provided
        if (request.Notes is not null)
        {
            budget.Notes = request.Notes;
        }

        // Update active status if provided
        if (request.IsActive.HasValue)
        {
            budget.IsActive = request.IsActive.Value;
        }

        await db.SaveChangesAsync();

        return Results.Ok(new BudgetDto(
            budget.Id,
            budget.CategoryId,
            budget.Category.Name,
            budget.Amount,
            budget.Frequency.ToString(),
            budget.EffectiveDate,
            budget.EndDate,
            budget.LinkedAccountId,
            budget.LinkedAccount?.Name,
            budget.Notes,
            budget.IsActive
        ));
    }

    private static async Task<IResult> DeleteBudget(int id, FinanceDbContext db)
    {
        var budget = await db.Budgets.FindAsync(id);

        if (budget is null)
            return Results.NotFound();

        db.Budgets.Remove(budget);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}

// DTOs
public record BudgetDto(
    int Id,
    int CategoryId,
    string CategoryName,
    decimal Amount,
    string Frequency,
    DateTime EffectiveDate,
    DateTime? EndDate,
    int? LinkedAccountId,
    string? LinkedAccountName,
    string? Notes,
    bool IsActive
);

public record CreateBudgetRequest(
    int CategoryId,
    decimal Amount,
    string Frequency,
    DateTime EffectiveDate,
    DateTime? EndDate,
    int? LinkedAccountId,
    string? Notes
);

public record UpdateBudgetRequest(
    int? CategoryId,
    decimal? Amount,
    string? Frequency,
    DateTime? EffectiveDate,
    DateTime? EndDate,
    bool? ClearEndDate,
    int? LinkedAccountId,
    bool? ClearLinkedAccount,
    string? Notes,
    bool? IsActive
);
