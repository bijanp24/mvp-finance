using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ServiceGoalType = FinanceEngine.Services.GoalType;
using ServiceAccountType = FinanceEngine.Services.AccountType;
using ServiceEventType = FinanceEngine.Services.EventType;
using EntityGoalType = FinanceEngine.Data.Entities.GoalType;
using EntityAccountType = FinanceEngine.Data.Entities.AccountType;
using EntityEventType = FinanceEngine.Data.Entities.EventType;
using FinanceEngine.Services;

namespace FinanceEngine.Api.Endpoints;

public static class GoalEndpoints
{
    public static RouteGroupBuilder MapGoalEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllGoals);
        group.MapGet("/{id}", GetGoalById);
        group.MapGet("/{id}/progress", GetGoalProgress);
        group.MapPost("/", CreateGoal);
        group.MapPut("/{id}", UpdateGoal);
        group.MapDelete("/{id}", DeleteGoal);

        return group;
    }

    private static async Task<IResult> GetAllGoals(FinanceDbContext db, bool? activeOnly = true)
    {
        var query = db.Goals.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(g => g.IsActive);
        }

        var goals = await query
            .OrderBy(g => g.Priority)
            .ThenBy(g => g.TargetDate)
            .ToListAsync();

        // Get all accounts for balance calculation
        var accounts = await db.Accounts
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.Type, a.InitialBalance })
            .ToListAsync();

        var events = await db.Events.ToListAsync();

        var goalDtos = new List<GoalDto>();
        foreach (var goal in goals)
        {
            var linkedAccountIds = ParseLinkedAccountIds(goal.LinkedAccountIds);
            var progress = CalculateGoalProgress(goal, accounts, events, linkedAccountIds);
            goalDtos.Add(ToDto(goal, progress));
        }

        return Results.Ok(goalDtos);
    }

    private static async Task<IResult> GetGoalById(int id, FinanceDbContext db)
    {
        var goal = await db.Goals.FindAsync(id);

        if (goal is null)
            return Results.NotFound();

        var accounts = await db.Accounts
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.Type, a.InitialBalance })
            .ToListAsync();

        var events = await db.Events.ToListAsync();
        var linkedAccountIds = ParseLinkedAccountIds(goal.LinkedAccountIds);
        var progress = CalculateGoalProgress(goal, accounts, events, linkedAccountIds);

        return Results.Ok(ToDto(goal, progress));
    }

    private static async Task<IResult> GetGoalProgress(int id, FinanceDbContext db)
    {
        var goal = await db.Goals.FindAsync(id);

        if (goal is null)
            return Results.NotFound();

        var accounts = await db.Accounts
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.Type, a.InitialBalance })
            .ToListAsync();

        var events = await db.Events.ToListAsync();
        var linkedAccountIds = ParseLinkedAccountIds(goal.LinkedAccountIds);
        var progress = CalculateGoalProgress(goal, accounts, events, linkedAccountIds);

        return Results.Ok(new GoalProgressDto(
            goal.Id,
            goal.Name,
            progress.CurrentValue,
            progress.TargetValue,
            progress.ProgressPercentage,
            progress.RequiredMonthlyAmount,
            progress.ProjectedCompletionDate,
            progress.Status.ToString(),
            progress.MonthsRemaining,
            progress.AmountRemaining,
            progress.StatusMessage
        ));
    }

    private static async Task<IResult> CreateGoal(CreateGoalRequest request, FinanceDbContext db)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Goal name is required");

        if (request.Name.Length > 100)
            return Results.BadRequest("Goal name cannot exceed 100 characters");

        // Validate type
        if (!Enum.TryParse<ServiceGoalType>(request.Type, true, out var goalType))
            return Results.BadRequest("Invalid goal type. Must be 'DebtFree', 'InvestmentTarget', 'SavingsGoal', or 'NetWorthMilestone'");

        // Validate target amount for non-DebtFree goals
        if (goalType != ServiceGoalType.DebtFree && (!request.TargetAmount.HasValue || request.TargetAmount <= 0))
            return Results.BadRequest("Target amount is required for this goal type");

        // Validate target date
        if (request.TargetDate <= DateTime.UtcNow.Date)
            return Results.BadRequest("Target date must be in the future");

        // Validate linked accounts if provided
        List<int>? linkedAccountIds = null;
        if (request.LinkedAccountIds is not null && request.LinkedAccountIds.Count > 0)
        {
            var validAccountIds = await db.Accounts
                .Where(a => request.LinkedAccountIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            if (validAccountIds.Count != request.LinkedAccountIds.Count)
                return Results.BadRequest("One or more linked account IDs are invalid");

            linkedAccountIds = request.LinkedAccountIds;
        }

        // Get next priority if not specified
        var priority = request.Priority ?? await db.Goals.MaxAsync(g => (int?)g.Priority) + 1 ?? 1;

        var goal = new GoalEntity
        {
            Name = request.Name,
            Type = MapToEntityGoalType(goalType),
            TargetAmount = request.TargetAmount,
            TargetDate = request.TargetDate,
            LinkedAccountIds = linkedAccountIds is not null ? JsonSerializer.Serialize(linkedAccountIds) : null,
            Priority = priority,
            Notes = request.Notes,
            IsActive = true
        };

        db.Goals.Add(goal);
        await db.SaveChangesAsync();

        // Calculate initial progress
        var accounts = await db.Accounts
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.Type, a.InitialBalance })
            .ToListAsync();

        var events = await db.Events.ToListAsync();
        var progress = CalculateGoalProgress(goal, accounts, events, linkedAccountIds ?? new List<int>());

        return Results.Created($"/api/goals/{goal.Id}", ToDto(goal, progress));
    }

    private static async Task<IResult> UpdateGoal(int id, UpdateGoalRequest request, FinanceDbContext db)
    {
        var goal = await db.Goals.FindAsync(id);

        if (goal is null)
            return Results.NotFound();

        // Validate and update name if provided
        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Goal name cannot be empty");

            if (request.Name.Length > 100)
                return Results.BadRequest("Goal name cannot exceed 100 characters");

            goal.Name = request.Name;
        }

        // Validate and update type if provided
        if (request.Type is not null)
        {
            if (!Enum.TryParse<ServiceGoalType>(request.Type, true, out var goalType))
                return Results.BadRequest("Invalid goal type");

            goal.Type = MapToEntityGoalType(goalType);
        }

        // Update target amount
        if (request.TargetAmount.HasValue)
            goal.TargetAmount = request.TargetAmount.Value;

        // Update target date
        if (request.TargetDate.HasValue)
        {
            if (request.TargetDate.Value <= DateTime.UtcNow.Date)
                return Results.BadRequest("Target date must be in the future");

            goal.TargetDate = request.TargetDate.Value;
        }

        // Update linked accounts
        if (request.LinkedAccountIds is not null)
        {
            if (request.LinkedAccountIds.Count > 0)
            {
                var validAccountIds = await db.Accounts
                    .Where(a => request.LinkedAccountIds.Contains(a.Id))
                    .Select(a => a.Id)
                    .ToListAsync();

                if (validAccountIds.Count != request.LinkedAccountIds.Count)
                    return Results.BadRequest("One or more linked account IDs are invalid");
            }

            goal.LinkedAccountIds = request.LinkedAccountIds.Count > 0
                ? JsonSerializer.Serialize(request.LinkedAccountIds)
                : null;
        }

        // Update priority
        if (request.Priority.HasValue)
            goal.Priority = request.Priority.Value;

        // Update notes
        if (request.Notes is not null)
            goal.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes;

        // Update active status
        if (request.IsActive.HasValue)
            goal.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync();

        // Calculate updated progress
        var accounts = await db.Accounts
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.Type, a.InitialBalance })
            .ToListAsync();

        var events = await db.Events.ToListAsync();
        var linkedAccountIds = ParseLinkedAccountIds(goal.LinkedAccountIds);
        var progress = CalculateGoalProgress(goal, accounts, events, linkedAccountIds);

        return Results.Ok(ToDto(goal, progress));
    }

    private static async Task<IResult> DeleteGoal(int id, FinanceDbContext db)
    {
        var goal = await db.Goals.FindAsync(id);

        if (goal is null)
            return Results.NotFound();

        db.Goals.Remove(goal);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // Helper methods
    private static List<int> ParseLinkedAccountIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<int>();

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    private static GoalProgressResult CalculateGoalProgress(
        GoalEntity goal,
        IEnumerable<dynamic> accounts,
        IEnumerable<FinancialEventEntity> events,
        List<int> linkedAccountIds)
    {
        var goalType = MapToServiceGoalType(goal.Type);
        decimal currentBalance = 0m;

        if (linkedAccountIds.Count > 0)
        {
            // Calculate balance for linked accounts
            foreach (var account in accounts.Where(a => linkedAccountIds.Contains((int)a.Id)))
            {
                var accountEvents = events
                    .Where(e => e.AccountId == (int)account.Id)
                    .Select(e => new FinancialEvent(
                        MapEventType(e.Type),
                        e.Amount
                    ));

                var balance = BalanceCalculator.Calculate(
                    MapAccountType((EntityAccountType)account.Type),
                    (decimal)account.InitialBalance,
                    accountEvents
                );

                // For debt-free goals, we want the debt amount (positive)
                // For investment/savings goals, we want the balance
                if (goalType == ServiceGoalType.DebtFree)
                {
                    currentBalance += Math.Abs(balance);
                }
                else
                {
                    currentBalance += balance;
                }
            }
        }

        var input = new GoalProgressInput(
            Type: goalType,
            TargetAmount: goal.TargetAmount,
            TargetDate: goal.TargetDate,
            CurrentLinkedBalance: currentBalance,
            MonthlyContributionRate: null, // TODO: Calculate from recurring contributions
            CalculationDate: DateTime.UtcNow
        );

        return GoalProgressCalculator.Calculate(input);
    }

    private static ServiceGoalType MapToServiceGoalType(EntityGoalType entityType)
    {
        return entityType switch
        {
            EntityGoalType.DebtFree => ServiceGoalType.DebtFree,
            EntityGoalType.InvestmentTarget => ServiceGoalType.InvestmentTarget,
            EntityGoalType.SavingsGoal => ServiceGoalType.SavingsGoal,
            EntityGoalType.NetWorthMilestone => ServiceGoalType.NetWorthMilestone,
            _ => ServiceGoalType.SavingsGoal
        };
    }

    private static EntityGoalType MapToEntityGoalType(ServiceGoalType serviceType)
    {
        return serviceType switch
        {
            ServiceGoalType.DebtFree => EntityGoalType.DebtFree,
            ServiceGoalType.InvestmentTarget => EntityGoalType.InvestmentTarget,
            ServiceGoalType.SavingsGoal => EntityGoalType.SavingsGoal,
            ServiceGoalType.NetWorthMilestone => EntityGoalType.NetWorthMilestone,
            _ => EntityGoalType.SavingsGoal
        };
    }

    private static ServiceAccountType MapAccountType(EntityAccountType entityType)
    {
        return entityType switch
        {
            EntityAccountType.Cash => ServiceAccountType.Cash,
            EntityAccountType.Debt => ServiceAccountType.Debt,
            EntityAccountType.Investment => ServiceAccountType.Investment,
            _ => ServiceAccountType.Cash
        };
    }

    private static ServiceEventType MapEventType(EntityEventType entityType)
    {
        return entityType switch
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

    private static GoalDto ToDto(GoalEntity goal, GoalProgressResult progress)
    {
        return new GoalDto(
            goal.Id,
            goal.Name,
            goal.Type.ToString(),
            goal.TargetAmount,
            goal.TargetDate,
            ParseLinkedAccountIds(goal.LinkedAccountIds),
            goal.Priority,
            goal.Notes,
            goal.IsActive,
            new GoalProgressSummary(
                progress.CurrentValue,
                progress.TargetValue,
                progress.ProgressPercentage,
                progress.Status.ToString(),
                progress.MonthsRemaining,
                progress.StatusMessage
            )
        );
    }
}

// DTOs
public record GoalDto(
    int Id,
    string Name,
    string Type,
    decimal? TargetAmount,
    DateTime TargetDate,
    List<int> LinkedAccountIds,
    int Priority,
    string? Notes,
    bool IsActive,
    GoalProgressSummary Progress
);

public record GoalProgressSummary(
    decimal CurrentValue,
    decimal TargetValue,
    decimal ProgressPercentage,
    string Status,
    int MonthsRemaining,
    string StatusMessage
);

public record GoalProgressDto(
    int GoalId,
    string GoalName,
    decimal CurrentValue,
    decimal TargetValue,
    decimal ProgressPercentage,
    decimal RequiredMonthlyAmount,
    DateTime? ProjectedCompletionDate,
    string Status,
    int MonthsRemaining,
    decimal AmountRemaining,
    string StatusMessage
);

public record CreateGoalRequest(
    string Name,
    string Type,
    decimal? TargetAmount,
    DateTime TargetDate,
    List<int>? LinkedAccountIds,
    int? Priority,
    string? Notes
);

public record UpdateGoalRequest(
    string? Name,
    string? Type,
    decimal? TargetAmount,
    DateTime? TargetDate,
    List<int>? LinkedAccountIds,
    int? Priority,
    string? Notes,
    bool? IsActive
);
