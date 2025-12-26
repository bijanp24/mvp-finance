using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetEvents);
        group.MapGet("/{id}", GetEventById);
        group.MapPost("/", CreateEvent);
        group.MapPut("/{id}", UpdateEvent);
        group.MapPatch("/{id}/status", UpdateEventStatus);
        group.MapDelete("/{id}", DeleteEvent);
        group.MapGet("/recent", GetRecentEvents);

        return group;
    }

    private static async Task<IResult> GetEvents(
        FinanceDbContext db,
        int? accountId = null,
        string? type = null,
        string? status = null,
        int? categoryId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100)
    {
        var query = db.Events.Include(e => e.Category).AsQueryable();

        if (accountId.HasValue)
            query = query.Where(e => e.AccountId == accountId || e.TargetAccountId == accountId);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<EventType>(type, true, out var eventType))
            query = query.Where(e => e.Type == eventType);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<EventStatus>(status, true, out var eventStatus))
            query = query.Where(e => e.Status == eventStatus);

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId);

        if (startDate.HasValue)
            query = query.Where(e => e.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Date <= endDate.Value);

        var events = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new EventDto(
                e.Id,
                e.Date,
                e.Type.ToString(),
                e.Amount,
                e.Description,
                e.AccountId,
                e.TargetAccountId,
                e.CategoryId,
                e.Category != null ? e.Category.Name : null,
                e.Status.ToString()
            ))
            .ToListAsync();

        return Results.Ok(events);
    }

    private static async Task<IResult> GetEventById(int id, FinanceDbContext db)
    {
        var evt = await db.Events
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null)
            return Results.NotFound();

        return Results.Ok(new EventDto(
            evt.Id,
            evt.Date,
            evt.Type.ToString(),
            evt.Amount,
            evt.Description,
            evt.AccountId,
            evt.TargetAccountId,
            evt.CategoryId,
            evt.Category?.Name,
            evt.Status.ToString()
        ));
    }

    private static async Task<IResult> CreateEvent(CreateEventRequest request, FinanceDbContext db)
    {
        if (!Enum.TryParse<EventType>(request.Type, true, out var eventType))
            return Results.BadRequest("Invalid event type");

        var description = request.Description ?? string.Empty;

        // Validate category if provided
        string? categoryName = null;
        if (request.CategoryId.HasValue)
        {
            var category = await db.Categories.FindAsync(request.CategoryId.Value);
            if (category is null)
                return Results.BadRequest("Category not found");
            if (!category.IsActive)
                return Results.BadRequest("Cannot assign inactive category");
            categoryName = category.Name;
        }

        bool IsTransferType(EventType type) =>
            type == EventType.DebtPayment ||
            type == EventType.SavingsContribution ||
            type == EventType.InvestmentContribution;

        if (IsTransferType(eventType))
        {
            if (!request.AccountId.HasValue || !request.TargetAccountId.HasValue)
                return Results.BadRequest("Both accountId and targetAccountId are required for transfers");

            var debitEvent = new FinancialEventEntity
            {
                Date = request.Date,
                Type = eventType,
                Amount = request.Amount,
                Description = description,
                AccountId = request.AccountId,
                TargetAccountId = request.TargetAccountId,
                CategoryId = request.CategoryId
            };

            var creditEvent = new FinancialEventEntity
            {
                Date = request.Date,
                Type = eventType,
                Amount = request.Amount,
                Description = description,
                AccountId = request.TargetAccountId,
                TargetAccountId = request.AccountId,
                CategoryId = request.CategoryId
            };

            db.Events.AddRange(debitEvent, creditEvent);
            await db.SaveChangesAsync();

            return Results.Created($"/api/events/{debitEvent.Id}", new EventDto(
                debitEvent.Id,
                debitEvent.Date,
                debitEvent.Type.ToString(),
                debitEvent.Amount,
                debitEvent.Description,
                debitEvent.AccountId,
                debitEvent.TargetAccountId,
                debitEvent.CategoryId,
                categoryName,
                debitEvent.Status.ToString()
            ));
        }

        if (eventType == EventType.DebtCharge)
        {
            if (!request.TargetAccountId.HasValue)
                return Results.BadRequest("targetAccountId is required for debt charges");
        }
        else if (eventType == EventType.Income || eventType == EventType.Expense || eventType == EventType.InterestFee)
        {
            if (!request.AccountId.HasValue)
                return Results.BadRequest("accountId is required for this event type");
        }

        var accountId = eventType == EventType.DebtCharge ? request.TargetAccountId : request.AccountId;
        var evt = new FinancialEventEntity
        {
            Date = request.Date,
            Type = eventType,
            Amount = request.Amount,
            Description = description,
            AccountId = accountId,
            TargetAccountId = eventType == EventType.DebtCharge ? null : request.TargetAccountId,
            CategoryId = request.CategoryId
        };

        db.Events.Add(evt);
        await db.SaveChangesAsync();

        return Results.Created($"/api/events/{evt.Id}", new EventDto(
            evt.Id,
            evt.Date,
            evt.Type.ToString(),
            evt.Amount,
            evt.Description,
            evt.AccountId,
            evt.TargetAccountId,
            evt.CategoryId,
            categoryName,
            evt.Status.ToString()
        ));
    }

    private static async Task<IResult> UpdateEvent(int id, UpdateEventRequest request, FinanceDbContext db)
    {
        var evt = await db.Events
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null)
            return Results.NotFound();

        // Validate event type if it's being changed
        EventType eventType = evt.Type;
        if (!string.IsNullOrEmpty(request.Type))
        {
            if (!Enum.TryParse<EventType>(request.Type, true, out eventType))
                return Results.BadRequest("Invalid event type");
        }

        // Update fields
        if (request.Date.HasValue)
            evt.Date = request.Date.Value;

        if (!string.IsNullOrEmpty(request.Type))
            evt.Type = eventType;

        if (request.Amount.HasValue)
            evt.Amount = request.Amount.Value;

        if (request.Description != null)
            evt.Description = request.Description;

        if (request.AccountId.HasValue)
            evt.AccountId = request.AccountId;

        if (request.TargetAccountId.HasValue)
            evt.TargetAccountId = request.TargetAccountId;

        // Handle category update
        if (request.CategoryId.HasValue)
        {
            var category = await db.Categories.FindAsync(request.CategoryId.Value);
            if (category is null)
                return Results.BadRequest("Category not found");
            if (!category.IsActive)
                return Results.BadRequest("Cannot assign inactive category");
            evt.CategoryId = request.CategoryId.Value;
            evt.Category = category;
        }
        else if (request.ClearCategory == true)
        {
            evt.CategoryId = null;
            evt.Category = null;
        }

        // Validate based on event type
        bool IsTransferType(EventType type) =>
            type == EventType.DebtPayment ||
            type == EventType.SavingsContribution ||
            type == EventType.InvestmentContribution;

        if (IsTransferType(evt.Type))
        {
            if (!evt.AccountId.HasValue || !evt.TargetAccountId.HasValue)
                return Results.BadRequest("Both accountId and targetAccountId are required for transfers");
        }
        else if (evt.Type == EventType.DebtCharge)
        {
            if (!evt.TargetAccountId.HasValue)
                return Results.BadRequest("targetAccountId is required for debt charges");
        }
        else if (evt.Type == EventType.Income || evt.Type == EventType.Expense || evt.Type == EventType.InterestFee)
        {
            if (!evt.AccountId.HasValue)
                return Results.BadRequest("accountId is required for this event type");
        }

        await db.SaveChangesAsync();

        return Results.Ok(new EventDto(
            evt.Id,
            evt.Date,
            evt.Type.ToString(),
            evt.Amount,
            evt.Description,
            evt.AccountId,
            evt.TargetAccountId,
            evt.CategoryId,
            evt.Category?.Name,
            evt.Status.ToString()
        ));
    }

    private static async Task<IResult> DeleteEvent(int id, FinanceDbContext db)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null)
            return Results.NotFound();

        db.Events.Remove(evt);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> UpdateEventStatus(int id, UpdateStatusRequest request, FinanceDbContext db)
    {
        var evt = await db.Events
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null)
            return Results.NotFound();

        if (!Enum.TryParse<EventStatus>(request.Status, true, out var status))
            return Results.BadRequest("Invalid status. Must be 'Pending' or 'Cleared'.");

        evt.Status = status;
        await db.SaveChangesAsync();

        return Results.Ok(new EventDto(
            evt.Id,
            evt.Date,
            evt.Type.ToString(),
            evt.Amount,
            evt.Description,
            evt.AccountId,
            evt.TargetAccountId,
            evt.CategoryId,
            evt.Category?.Name,
            evt.Status.ToString()
        ));
    }

    private static async Task<IResult> GetRecentEvents(FinanceDbContext db, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var events = await db.Events
            .Include(e => e.Category)
            .Where(e => e.Date >= startDate)
            .OrderByDescending(e => e.Date)
            .Select(e => new EventDto(
                e.Id,
                e.Date,
                e.Type.ToString(),
                e.Amount,
                e.Description,
                e.AccountId,
                e.TargetAccountId,
                e.CategoryId,
                e.Category != null ? e.Category.Name : null,
                e.Status.ToString()
            ))
            .ToListAsync();

        return Results.Ok(events);
    }
}

// DTOs
public record EventDto(
    int Id,
    DateTime Date,
    string Type,
    decimal Amount,
    string Description,
    int? AccountId,
    int? TargetAccountId,
    int? CategoryId,
    string? CategoryName,
    string Status
);

public record UpdateStatusRequest(string Status);

public record CreateEventRequest(
    DateTime Date,
    string Type,
    decimal Amount,
    string? Description = null,
    int? AccountId = null,
    int? TargetAccountId = null,
    int? CategoryId = null
);

public record UpdateEventRequest(
    DateTime? Date = null,
    string? Type = null,
    decimal? Amount = null,
    string? Description = null,
    int? AccountId = null,
    int? TargetAccountId = null,
    int? CategoryId = null,
    bool? ClearCategory = null
);
