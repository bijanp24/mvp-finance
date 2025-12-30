using FinanceEngine.Calculators;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;
using FinanceEngine.Services;
using Microsoft.EntityFrameworkCore;
using ServiceGoalType = FinanceEngine.Services.GoalType;
using ServiceGoalStatus = FinanceEngine.Services.GoalStatus;
using ServiceAccountType = FinanceEngine.Services.AccountType;
using ServiceEventType = FinanceEngine.Services.EventType;
using EntityAccountType = FinanceEngine.Data.Entities.AccountType;
using EntityEventType = FinanceEngine.Data.Entities.EventType;
using EntityGoalType = FinanceEngine.Data.Entities.GoalType;
using EntityTimeHorizon = FinanceEngine.Data.Entities.TimeHorizon;
using CalcTimeHorizon = FinanceEngine.Models.Inputs.TimeHorizon;
using CalcBudgetFrequency = FinanceEngine.Models.Inputs.BudgetFrequency;
using EntityBudgetFrequency = FinanceEngine.Data.Entities.BudgetFrequency;
using CalcGoalStatus = FinanceEngine.Services.GoalStatus;
using IncomeEvent = FinanceEngine.Models.IncomeEvent;

namespace FinanceEngine.Api.Endpoints;

public static class SafeToSpendEndpoints
{
    public static RouteGroupBuilder MapSafeToSpendEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetSafeToSpend);
        group.MapGet("/analysis", GetBudgetAnalysis);
        group.MapGet("/suggestions", GetSuggestions);
        group.MapGet("/full", GetFullSafeToSpendReport);

        return group;
    }

    /// <summary>
    /// Gets the safe-to-spend calculation for the current user.
    /// </summary>
    private static async Task<IResult> GetSafeToSpend(
        FinanceDbContext db,
        string? timeHorizon = null,
        DateTime? nextPaycheckDate = null,
        decimal? minimumBuffer = null)
    {
        var settings = await GetOrCreateSettings(db);
        var calculationDate = DateTime.UtcNow;

        // Determine time horizon from parameter or user settings
        var horizon = ParseTimeHorizon(timeHorizon) ?? MapTimeHorizon(settings.PreferredTimeHorizon);

        // Calculate available cash
        var availableCash = await CalculateAvailableCash(db);

        // Get budget data
        var budgets = await GetBudgetInfos(db, calculationDate);

        // Get goal data
        var goals = await GetGoalInfos(db);

        // Get upcoming income
        var upcomingIncome = await GetUpcomingIncome(db, calculationDate, calculationDate.AddDays(60));

        // Build input
        var input = new SafeToSpendInput(
            AvailableCash: availableCash,
            CalculationDate: calculationDate,
            TimeHorizon: horizon,
            Budgets: budgets,
            Goals: goals,
            UpcomingIncome: upcomingIncome,
            MinimumBuffer: minimumBuffer ?? settings.SafetyBuffer,
            NextPaycheckDate: nextPaycheckDate ?? settings.NextPaycheckDate
        );

        // Calculate
        var result = SafeToSpendCalculator.Calculate(input);

        return Results.Ok(MapToDto(result));
    }

    /// <summary>
    /// Gets budget overspending analysis.
    /// </summary>
    private static async Task<IResult> GetBudgetAnalysis(FinanceDbContext db, int? periodDays = null)
    {
        var calculationDate = DateTime.UtcNow;
        var days = periodDays ?? 30;

        // Get budgets with spending
        var budgets = await GetBudgetInfos(db, calculationDate);

        // Get goals for impact calculation
        var goals = await GetGoalInfos(db);

        var input = new BudgetAnalysisInput(
            Budgets: budgets,
            Goals: goals,
            PeriodDays: days
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        return Results.Ok(new BudgetAnalysisDto(
            result.OverspentCategories.Select(o => new BudgetOverspendDto(
                o.CategoryId,
                o.CategoryName,
                o.BudgetAmount,
                o.SpentAmount,
                o.OverspendAmount,
                o.GoalImpacts.Select(MapGoalImpactDto).ToList()
            )).ToList(),
            result.TotalOverspend,
            result.OverallGoalImpacts.Select(MapGoalImpactDto).ToList(),
            result.HasOverspending
        ));
    }

    /// <summary>
    /// Gets adjustment suggestions.
    /// </summary>
    private static async Task<IResult> GetSuggestions(
        FinanceDbContext db,
        int? maxSuggestions = null,
        string? timeHorizon = null)
    {
        var settings = await GetOrCreateSettings(db);
        var calculationDate = DateTime.UtcNow;

        // First calculate safe-to-spend
        var horizon = ParseTimeHorizon(timeHorizon) ?? MapTimeHorizon(settings.PreferredTimeHorizon);
        var availableCash = await CalculateAvailableCash(db);
        var budgets = await GetBudgetInfos(db, calculationDate);
        var goals = await GetGoalInfos(db);
        var upcomingIncome = await GetUpcomingIncome(db, calculationDate, calculationDate.AddDays(60));

        var safeToSpendInput = new SafeToSpendInput(
            AvailableCash: availableCash,
            CalculationDate: calculationDate,
            TimeHorizon: horizon,
            Budgets: budgets,
            Goals: goals,
            UpcomingIncome: upcomingIncome,
            MinimumBuffer: settings.SafetyBuffer,
            NextPaycheckDate: settings.NextPaycheckDate
        );

        var safeToSpendResult = SafeToSpendCalculator.Calculate(safeToSpendInput);

        // Then calculate budget analysis
        var budgetAnalysisInput = new BudgetAnalysisInput(
            Budgets: budgets,
            Goals: goals,
            PeriodDays: 30
        );

        var budgetAnalysisResult = BudgetAnalysisCalculator.Analyze(budgetAnalysisInput);

        // Finally calculate suggestions
        var suggestionInput = new AdjustmentSuggestionInput(
            SafeToSpendResult: safeToSpendResult,
            BudgetAnalysis: budgetAnalysisResult,
            MaxSuggestions: maxSuggestions ?? 5
        );

        var result = AdjustmentSuggestionCalculator.Calculate(suggestionInput);

        return Results.Ok(new SuggestionsDto(
            result.Suggestions.Select(s => new SuggestionDto(
                s.Id,
                s.Category.ToString(),
                s.Title,
                s.Description,
                s.Priority.ToString(),
                s.PotentialSavings,
                s.ActionType.ToString(),
                s.ActionTarget,
                s.ImpactOnGoals.ToList()
            )).ToList(),
            result.HasUrgentSuggestions,
            result.TotalPotentialSavings
        ));
    }

    /// <summary>
    /// Gets the full safe-to-spend report including analysis and suggestions.
    /// </summary>
    private static async Task<IResult> GetFullSafeToSpendReport(
        FinanceDbContext db,
        string? timeHorizon = null,
        int? maxSuggestions = null)
    {
        var settings = await GetOrCreateSettings(db);
        var calculationDate = DateTime.UtcNow;

        // Gather all data
        var horizon = ParseTimeHorizon(timeHorizon) ?? MapTimeHorizon(settings.PreferredTimeHorizon);
        var availableCash = await CalculateAvailableCash(db);
        var budgets = await GetBudgetInfos(db, calculationDate);
        var goals = await GetGoalInfos(db);
        var upcomingIncome = await GetUpcomingIncome(db, calculationDate, calculationDate.AddDays(60));

        // 1. Safe-to-spend calculation
        var safeToSpendInput = new SafeToSpendInput(
            AvailableCash: availableCash,
            CalculationDate: calculationDate,
            TimeHorizon: horizon,
            Budgets: budgets,
            Goals: goals,
            UpcomingIncome: upcomingIncome,
            MinimumBuffer: settings.SafetyBuffer,
            NextPaycheckDate: settings.NextPaycheckDate
        );

        var safeToSpendResult = SafeToSpendCalculator.Calculate(safeToSpendInput);

        // 2. Budget analysis
        var budgetAnalysisInput = new BudgetAnalysisInput(
            Budgets: budgets,
            Goals: goals,
            PeriodDays: 30
        );

        var budgetAnalysisResult = BudgetAnalysisCalculator.Analyze(budgetAnalysisInput);

        // 3. Suggestions
        var suggestionInput = new AdjustmentSuggestionInput(
            SafeToSpendResult: safeToSpendResult,
            BudgetAnalysis: budgetAnalysisResult,
            MaxSuggestions: maxSuggestions ?? 5
        );

        var suggestionsResult = AdjustmentSuggestionCalculator.Calculate(suggestionInput);

        return Results.Ok(new FullSafeToSpendReportDto(
            MapToDto(safeToSpendResult),
            new BudgetAnalysisDto(
                budgetAnalysisResult.OverspentCategories.Select(o => new BudgetOverspendDto(
                    o.CategoryId,
                    o.CategoryName,
                    o.BudgetAmount,
                    o.SpentAmount,
                    o.OverspendAmount,
                    o.GoalImpacts.Select(MapGoalImpactDto).ToList()
                )).ToList(),
                budgetAnalysisResult.TotalOverspend,
                budgetAnalysisResult.OverallGoalImpacts.Select(MapGoalImpactDto).ToList(),
                budgetAnalysisResult.HasOverspending
            ),
            new SuggestionsDto(
                suggestionsResult.Suggestions.Select(s => new SuggestionDto(
                    s.Id,
                    s.Category.ToString(),
                    s.Title,
                    s.Description,
                    s.Priority.ToString(),
                    s.PotentialSavings,
                    s.ActionType.ToString(),
                    s.ActionTarget,
                    s.ImpactOnGoals.ToList()
                )).ToList(),
                suggestionsResult.HasUrgentSuggestions,
                suggestionsResult.TotalPotentialSavings
            ),
            calculationDate
        ));
    }

    #region Helper Methods

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

    private static async Task<decimal> CalculateAvailableCash(FinanceDbContext db)
    {
        var cashAccounts = await db.Accounts
            .Where(a => a.IsActive && a.Type == EntityAccountType.Cash)
            .ToListAsync();

        var events = await db.Events.ToListAsync();
        decimal totalCash = 0m;

        foreach (var account in cashAccounts)
        {
            var accountEvents = events
                .Where(e => e.AccountId == account.Id)
                .Select(e => new FinancialEvent(MapEventType(e.Type), e.Amount));

            var balance = BalanceCalculator.Calculate(
                ServiceAccountType.Cash,
                account.InitialBalance,
                accountEvents
            );

            totalCash += balance;
        }

        return totalCash;
    }

    private static async Task<List<BudgetInfo>> GetBudgetInfos(FinanceDbContext db, DateTime calculationDate)
    {
        var activeBudgets = await db.Budgets
            .Include(b => b.Category)
            .Where(b => b.IsActive && b.EffectiveDate <= calculationDate)
            .Where(b => !b.EndDate.HasValue || b.EndDate.Value >= calculationDate)
            .ToListAsync();

        // Get spending for each category this month
        var startOfMonth = new DateTime(calculationDate.Year, calculationDate.Month, 1);
        var categorySpending = await db.Events
            .Where(e => e.CategoryId.HasValue && e.Date >= startOfMonth && e.Date <= calculationDate)
            .Where(e => e.Type == EntityEventType.Expense)
            .GroupBy(e => e.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total);

        return activeBudgets.Select(b => new BudgetInfo(
            CategoryId: b.CategoryId,
            CategoryName: b.Category.Name,
            MonthlyAmount: b.Amount,
            Frequency: MapBudgetFrequency(b.Frequency),
            SpentThisPeriod: categorySpending.GetValueOrDefault(b.CategoryId, 0m)
        )).ToList();
    }

    private static async Task<List<GoalInfo>> GetGoalInfos(FinanceDbContext db)
    {
        var activeGoals = await db.Goals
            .Where(g => g.IsActive)
            .ToListAsync();

        var accounts = await db.Accounts
            .Where(a => a.IsActive)
            .ToListAsync();

        var events = await db.Events.ToListAsync();

        var goalInfos = new List<GoalInfo>();

        foreach (var goal in activeGoals)
        {
            // Calculate progress
            var progress = CalculateGoalProgress(goal, accounts, events);

            goalInfos.Add(new GoalInfo(
                GoalId: goal.Id,
                GoalName: goal.Name,
                GoalType: MapGoalType(goal.Type),
                RequiredMonthlyContribution: progress.RequiredMonthlyAmount,
                CurrentMonthlyContribution: progress.RequiredMonthlyAmount, // TODO: Get from recurring contributions
                Status: MapGoalStatus(progress.Status)
            ));
        }

        return goalInfos;
    }

    private static async Task<List<IncomeEvent>> GetUpcomingIncome(FinanceDbContext db, DateTime start, DateTime end)
    {
        // Get recurring contributions that represent income
        var recurring = await db.RecurringContributions
            .Where(r => r.IsActive)
            .ToListAsync();

        var incomeEvents = new List<IncomeEvent>();

        // Get events marked as income in the future
        var futureIncome = await db.Events
            .Where(e => e.Type == EntityEventType.Income && e.Date >= start && e.Date <= end)
            .OrderBy(e => e.Date)
            .ToListAsync();

        foreach (var income in futureIncome)
        {
            incomeEvents.Add(new IncomeEvent(income.Date, income.Amount, income.Description ?? "Income"));
        }

        return incomeEvents;
    }

    private static GoalProgressResult CalculateGoalProgress(
        GoalEntity goal,
        List<AccountEntity> accounts,
        List<FinancialEventEntity> events)
    {
        var linkedAccountIds = ParseLinkedAccountIds(goal.LinkedAccountIds);
        var goalType = MapGoalType(goal.Type);
        decimal currentBalance = 0m;

        if (linkedAccountIds.Count > 0)
        {
            foreach (var account in accounts.Where(a => linkedAccountIds.Contains(a.Id)))
            {
                var accountEvents = events
                    .Where(e => e.AccountId == account.Id)
                    .Select(e => new FinancialEvent(MapEventType(e.Type), e.Amount));

                var balance = BalanceCalculator.Calculate(
                    MapAccountType(account.Type),
                    account.InitialBalance,
                    accountEvents
                );

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
            MonthlyContributionRate: null,
            CalculationDate: DateTime.UtcNow
        );

        return GoalProgressCalculator.Calculate(input);
    }

    private static List<int> ParseLinkedAccountIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<int>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    private static CalcTimeHorizon? ParseTimeHorizon(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.ToLowerInvariant() switch
        {
            "nextpaycheck" => CalcTimeHorizon.NextPaycheck,
            "currentmonth" => CalcTimeHorizon.CurrentMonth,
            "rollingtwoweeks" => CalcTimeHorizon.RollingTwoWeeks,
            _ => null
        };
    }

    private static CalcTimeHorizon MapTimeHorizon(EntityTimeHorizon horizon)
    {
        return horizon switch
        {
            EntityTimeHorizon.NextPaycheck => CalcTimeHorizon.NextPaycheck,
            EntityTimeHorizon.CurrentMonth => CalcTimeHorizon.CurrentMonth,
            EntityTimeHorizon.RollingTwoWeeks => CalcTimeHorizon.RollingTwoWeeks,
            _ => CalcTimeHorizon.RollingTwoWeeks
        };
    }

    private static CalcBudgetFrequency MapBudgetFrequency(EntityBudgetFrequency frequency)
    {
        return frequency switch
        {
            EntityBudgetFrequency.Monthly => CalcBudgetFrequency.Monthly,
            EntityBudgetFrequency.BiWeekly => CalcBudgetFrequency.BiWeekly,
            EntityBudgetFrequency.Weekly => CalcBudgetFrequency.Weekly,
            _ => CalcBudgetFrequency.Monthly
        };
    }

    private static ServiceGoalType MapGoalType(EntityGoalType type)
    {
        return type switch
        {
            EntityGoalType.DebtFree => ServiceGoalType.DebtFree,
            EntityGoalType.InvestmentTarget => ServiceGoalType.InvestmentTarget,
            EntityGoalType.SavingsGoal => ServiceGoalType.SavingsGoal,
            EntityGoalType.NetWorthMilestone => ServiceGoalType.NetWorthMilestone,
            _ => ServiceGoalType.SavingsGoal
        };
    }

    private static ServiceGoalStatus MapGoalStatus(CalcGoalStatus status)
    {
        return status switch
        {
            CalcGoalStatus.OnTrack => ServiceGoalStatus.OnTrack,
            CalcGoalStatus.Ahead => ServiceGoalStatus.Ahead,
            CalcGoalStatus.Behind => ServiceGoalStatus.Behind,
            CalcGoalStatus.AtRisk => ServiceGoalStatus.AtRisk,
            _ => ServiceGoalStatus.OnTrack
        };
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

    private static SafeToSpendDto MapToDto(SafeToSpendResult result)
    {
        return new SafeToSpendDto(
            result.SafeToSpend,
            result.Status.ToString(),
            new SafeToSpendBreakdownDto(
                result.Breakdown.AvailableCash,
                result.Breakdown.UpcomingBills,
                result.Breakdown.RequiredGoalContributions,
                result.Breakdown.MinimumBuffer,
                result.Breakdown.DaysInHorizon
            ),
            result.GoalImpacts.Select(MapGoalImpactDto).ToList(),
            result.StatusMessage,
            result.HorizonEndDate
        );
    }

    private static GoalImpactDto MapGoalImpactDto(GoalImpact impact)
    {
        return new GoalImpactDto(
            impact.GoalId,
            impact.GoalName,
            impact.GoalType,
            impact.CurrentStatus,
            impact.RequiredMonthlyContribution,
            impact.CurrentMonthlyContribution,
            impact.ContributionGap,
            impact.DelayedMonths,
            impact.ImpactMessage
        );
    }

    #endregion
}

#region DTOs

public record SafeToSpendDto(
    decimal SafeToSpend,
    string Status,
    SafeToSpendBreakdownDto Breakdown,
    List<GoalImpactDto> GoalImpacts,
    string StatusMessage,
    DateTime HorizonEndDate
);

public record SafeToSpendBreakdownDto(
    decimal AvailableCash,
    decimal UpcomingBills,
    decimal RequiredGoalContributions,
    decimal MinimumBuffer,
    int DaysInHorizon
);

public record GoalImpactDto(
    int GoalId,
    string GoalName,
    string GoalType,
    string CurrentStatus,
    decimal RequiredMonthlyContribution,
    decimal CurrentMonthlyContribution,
    decimal ContributionGap,
    int? DelayedMonths,
    string ImpactMessage
);

public record BudgetAnalysisDto(
    List<BudgetOverspendDto> OverspentCategories,
    decimal TotalOverspend,
    List<GoalImpactDto> OverallGoalImpacts,
    bool HasOverspending
);

public record BudgetOverspendDto(
    int CategoryId,
    string CategoryName,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal OverspendAmount,
    List<GoalImpactDto> GoalImpacts
);

public record SuggestionsDto(
    List<SuggestionDto> Suggestions,
    bool HasUrgentSuggestions,
    decimal TotalPotentialSavings
);

public record SuggestionDto(
    string Id,
    string Category,
    string Title,
    string Description,
    string Priority,
    decimal? PotentialSavings,
    string ActionType,
    string? ActionTarget,
    List<string> ImpactOnGoals
);

public record FullSafeToSpendReportDto(
    SafeToSpendDto SafeToSpend,
    BudgetAnalysisDto BudgetAnalysis,
    SuggestionsDto Suggestions,
    DateTime CalculatedAt
);

#endregion
