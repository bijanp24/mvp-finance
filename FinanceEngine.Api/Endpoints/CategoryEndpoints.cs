using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllCategories);
        group.MapGet("/{id}", GetCategoryById);
        group.MapPost("/", CreateCategory);
        group.MapPut("/{id}", UpdateCategory);
        group.MapDelete("/{id}", DeleteCategory);

        return group;
    }

    private static async Task<IResult> GetAllCategories(FinanceDbContext db, bool? activeOnly = true)
    {
        var query = db.Categories.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Type.ToString(),
                c.Icon,
                c.Color,
                c.SortOrder,
                c.IsActive
            ))
            .ToListAsync();

        return Results.Ok(categories);
    }

    private static async Task<IResult> GetCategoryById(int id, FinanceDbContext db)
    {
        var category = await db.Categories.FindAsync(id);

        if (category is null)
            return Results.NotFound();

        return Results.Ok(new CategoryDto(
            category.Id,
            category.Name,
            category.Type.ToString(),
            category.Icon,
            category.Color,
            category.SortOrder,
            category.IsActive
        ));
    }

    private static async Task<IResult> CreateCategory(CreateCategoryRequest request, FinanceDbContext db)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Category name is required");

        if (request.Name.Length > 100)
            return Results.BadRequest("Category name cannot exceed 100 characters");

        // Validate type
        if (!Enum.TryParse<CategoryType>(request.Type, true, out var categoryType))
            return Results.BadRequest("Invalid category type. Must be 'Recurring' or 'OneTime'");

        // Check for duplicate name
        var exists = await db.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.ToLower());
        if (exists)
            return Results.BadRequest("A category with this name already exists");

        // Get next sort order if not specified
        var sortOrder = request.SortOrder ?? await db.Categories.MaxAsync(c => (int?)c.SortOrder) + 1 ?? 1;

        var category = new CategoryEntity
        {
            Name = request.Name,
            Type = categoryType,
            Icon = request.Icon,
            Color = request.Color,
            SortOrder = sortOrder,
            IsActive = true
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Results.Created($"/api/categories/{category.Id}", new CategoryDto(
            category.Id,
            category.Name,
            category.Type.ToString(),
            category.Icon,
            category.Color,
            category.SortOrder,
            category.IsActive
        ));
    }

    private static async Task<IResult> UpdateCategory(int id, UpdateCategoryRequest request, FinanceDbContext db)
    {
        var category = await db.Categories.FindAsync(id);

        if (category is null)
            return Results.NotFound();

        // Validate name if provided
        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Category name cannot be empty");

            if (request.Name.Length > 100)
                return Results.BadRequest("Category name cannot exceed 100 characters");

            // Check for duplicate name (excluding self)
            var exists = await db.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == request.Name.ToLower());
            if (exists)
                return Results.BadRequest("A category with this name already exists");

            category.Name = request.Name;
        }

        // Validate and update type if provided
        if (request.Type is not null)
        {
            if (!Enum.TryParse<CategoryType>(request.Type, true, out var categoryType))
                return Results.BadRequest("Invalid category type. Must be 'Recurring' or 'OneTime'");

            category.Type = categoryType;
        }

        // Update optional fields
        if (request.Icon is not null)
            category.Icon = request.Icon;

        if (request.Color is not null)
            category.Color = request.Color;

        if (request.SortOrder.HasValue)
            category.SortOrder = request.SortOrder.Value;

        if (request.IsActive.HasValue)
            category.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync();

        return Results.Ok(new CategoryDto(
            category.Id,
            category.Name,
            category.Type.ToString(),
            category.Icon,
            category.Color,
            category.SortOrder,
            category.IsActive
        ));
    }

    private static async Task<IResult> DeleteCategory(int id, FinanceDbContext db)
    {
        var category = await db.Categories.FindAsync(id);

        if (category is null)
            return Results.NotFound();

        // Check if category is in use by any budgets
        var hasRelatedBudgets = await db.Budgets.AnyAsync(b => b.CategoryId == id);
        if (hasRelatedBudgets)
            return Results.BadRequest("Cannot delete category that has associated budgets. Deactivate it instead.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}

// DTOs
public record CategoryDto(
    int Id,
    string Name,
    string Type,
    string? Icon,
    string? Color,
    int SortOrder,
    bool IsActive
);

public record CreateCategoryRequest(
    string Name,
    string Type,
    string? Icon,
    string? Color,
    int? SortOrder
);

public record UpdateCategoryRequest(
    string? Name,
    string? Type,
    string? Icon,
    string? Color,
    int? SortOrder,
    bool? IsActive
);
