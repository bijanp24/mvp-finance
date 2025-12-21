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
        group.MapDelete("/{id}", DeleteEvent);
        group.MapGet("/recent", GetRecentEvents);

        return group;
    }

    private static async Task<IResult> GetEvents(
        FinanceDbContext db,
        int? accountId = null,
        string? type = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100)
    {
        var query = db.Events.AsQueryable();

        if (accountId.HasValue)
            query = query.Where(e => e.AccountId == accountId || e.TargetAccountId == accountId);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<EventType>(type, true, out var eventType))
            query = query.Where(e => e.Type == eventType);

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
                e.TargetAccountId
            ))
            .ToListAsync();

        return Results.Ok(events);
    }

    private static async Task<IResult> GetEventById(int id, FinanceDbContext db)
    {
        var evt = await db.Events.FindAsync(id);
        if (evt is null)
            return Results.NotFound();

        return Results.Ok(new EventDto(
            evt.Id,
            evt.Date,
            evt.Type.ToString(),
            evt.Amount,
            evt.Description,
            evt.AccountId,
            evt.TargetAccountId
        ));
    }

    private static async Task<IResult> CreateEvent(CreateEventRequest request, FinanceDbContext db)
    {
        if (!Enum.TryParse<EventType>(request.Type, true, out var eventType))
            return Results.BadRequest("Invalid event type");

        var description = request.Description ?? string.Empty;

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
                TargetAccountId = request.TargetAccountId
            };

            var creditEvent = new FinancialEventEntity
            {
                Date = request.Date,
                Type = eventType,
                Amount = request.Amount,
                Description = description,
                AccountId = request.TargetAccountId,
                TargetAccountId = request.AccountId
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
                debitEvent.TargetAccountId
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
            TargetAccountId = eventType == EventType.DebtCharge ? null : request.TargetAccountId
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
            evt.TargetAccountId
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

    private static async Task<IResult> GetRecentEvents(FinanceDbContext db, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var events = await db.Events
            .Where(e => e.Date >= startDate)
            .OrderByDescending(e => e.Date)
            .Select(e => new EventDto(
                e.Id,
                e.Date,
                e.Type.ToString(),
                e.Amount,
                e.Description,
                e.AccountId,
                e.TargetAccountId
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
    int? TargetAccountId
);

public record CreateEventRequest(
    DateTime Date,
    string Type,
    decimal Amount,
    string? Description = null,
    int? AccountId = null,
    int? TargetAccountId = null
);
