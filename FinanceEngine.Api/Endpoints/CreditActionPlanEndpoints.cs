using FinanceEngine.Calculators;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using FinanceEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

/// <summary>
/// Turns the user's current credit-card debt plus a lump-sum windfall into a
/// personalized payoff action plan (emergency reserve first, then avalanche).
/// </summary>
public static class CreditActionPlanEndpoints
{
    public static RouteGroupBuilder MapCreditActionPlanEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/calculate", CalculatePlan)
            .WithName("CalculateCreditActionPlan")
            .WithDescription("Build a debt-payoff action plan from a windfall");

        group.MapGet("/defaults", GetDefaults)
            .WithName("GetCreditActionPlanDefaults")
            .WithDescription("Suggested inputs (windfall, essential expenses, income, debts) for the plan");

        return group;
    }

    private static async Task<IResult> CalculatePlan(
        CreditActionPlanRequest request,
        FinanceDbContext db,
        CancellationToken ct)
    {
        if (request.Windfall < 0)
            return Results.BadRequest(new { error = "Windfall cannot be negative." });

        var context = await GatherContext(db, ct);

        var input = new CreditActionPlanInput(
            Debts: context.Debts,
            Windfall: request.Windfall,
            MonthlyEssentialExpenses: request.MonthlyEssentialExpenses ?? context.MonthlyEssentialExpenses,
            EmergencyFundMonths: request.EmergencyFundMonths ?? 6,
            MonthlyIncome: context.MonthlyIncome,
            Strategy: ParseStrategy(request.Strategy));

        try
        {
            var result = CreditActionPlanCalculator.Calculate(input);
            return Results.Ok(MapToResponse(result));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetDefaults(FinanceDbContext db, CancellationToken ct)
    {
        var context = await GatherContext(db, ct);

        return Results.Ok(new CreditActionPlanDefaultsResponse(
            SuggestedWindfall: context.TotalCash,
            MonthlyEssentialExpenses: context.MonthlyEssentialExpenses,
            MonthlyIncome: context.MonthlyIncome,
            DefaultEmergencyFundMonths: 6,
            Debts: context.Debts
                .Select(d => new PlanDebtSummary(d.Name, d.Balance, d.EffectiveAPR, d.MinimumPayment))
                .ToList()));
    }

    // -- Shared data gathering ------------------------------------------------

    private sealed record PlanContext(
        List<Debt> Debts,
        decimal TotalCash,
        decimal MonthlyEssentialExpenses,
        decimal MonthlyIncome);

    private static async Task<PlanContext> GatherContext(FinanceDbContext db, CancellationToken ct)
    {
        var accounts = await db.Accounts.Where(a => a.IsActive).ToListAsync(ct);
        var events = await db.Events.Where(e => e.Status == EventStatus.Cleared).ToListAsync(ct);

        var totalCash = accounts
            .Where(a => a.Type == AccountType.Cash)
            .Sum(a => CalculateAccountBalance(a, events));

        var debts = accounts
            .Where(a => a.Type == AccountType.Debt)
            .Select(a => new
            {
                Account = a,
                Balance = CalculateAccountBalance(a, events)
            })
            .Where(x => x.Balance > 0)
            .Select(x => new Debt(
                Name: x.Account.Name,
                Balance: x.Balance,
                AnnualPercentageRate: x.Account.AnnualPercentageRate ?? 0m,
                MinimumPayment: x.Account.MinimumPayment ?? 0m,
                PromotionalAnnualPercentageRate: x.Account.PromotionalAnnualPercentageRate,
                PromotionalPeriodEndDate: x.Account.PromotionalPeriodEndDate))
            .ToList();

        // Essential monthly expenses: recurring-category budgets. Fall back to all
        // active budgets if none are tagged recurring.
        var budgets = await db.Budgets.Where(b => b.IsActive).ToListAsync(ct);
        var recurringCategoryIds = await db.Categories
            .Where(c => c.Type == CategoryType.Recurring)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var recurringBudgets = budgets.Where(b => recurringCategoryIds.Contains(b.CategoryId)).ToList();
        var essentialSource = recurringBudgets.Count > 0 ? recurringBudgets : budgets;
        var monthlyEssential = essentialSource.Sum(b => ToMonthly(b.Amount, b.Frequency));

        var settings = await db.UserSettings.FirstOrDefaultAsync(ct);
        var monthlyIncome = settings != null
            ? ToMonthlyIncome(settings.PaycheckAmount, settings.PayFrequency)
            : 0m;

        return new PlanContext(debts, totalCash, monthlyEssential, monthlyIncome);
    }

    private static decimal CalculateAccountBalance(AccountEntity account, List<FinancialEventEntity> events)
    {
        var balance = account.InitialBalance;
        foreach (var evt in events.Where(e => e.AccountId == account.Id || e.TargetAccountId == account.Id))
        {
            var isSource = evt.AccountId == account.Id;
            var isTarget = evt.TargetAccountId == account.Id;

            balance += (evt.Type, account.Type, isSource, isTarget) switch
            {
                (EventType.Income, AccountType.Cash, true, _) => evt.Amount,
                (EventType.Expense, AccountType.Cash, true, _) => -evt.Amount,
                (EventType.DebtPayment, AccountType.Cash, true, _) => -evt.Amount,
                (EventType.SavingsContribution, AccountType.Cash, true, _) => -evt.Amount,
                (EventType.InvestmentContribution, AccountType.Cash, true, _) => -evt.Amount,
                (EventType.DebtCharge, AccountType.Debt, true, _) => evt.Amount,
                (EventType.DebtPayment, AccountType.Debt, _, true) => -evt.Amount,
                (EventType.InterestFee, AccountType.Debt, true, _) => evt.Amount,
                (EventType.InvestmentContribution, AccountType.Investment, _, true) => evt.Amount,
                (EventType.SavingsContribution, AccountType.Investment, _, true) => evt.Amount,
                _ => 0
            };
        }
        return balance;
    }

    private static decimal ToMonthly(decimal amount, BudgetFrequency frequency) => frequency switch
    {
        BudgetFrequency.Weekly => amount * 4.33m,
        BudgetFrequency.BiWeekly => amount * 2.17m,
        BudgetFrequency.Monthly => amount,
        _ => amount
    };

    private static decimal ToMonthlyIncome(decimal paycheck, PayFrequency frequency) => frequency switch
    {
        PayFrequency.Weekly => paycheck * 4.33m,
        PayFrequency.BiWeekly => paycheck * 2.17m,
        PayFrequency.SemiMonthly => paycheck * 2,
        PayFrequency.Monthly => paycheck,
        _ => paycheck
    };

    private static AllocationStrategy ParseStrategy(string? strategy) =>
        Enum.TryParse<AllocationStrategy>(strategy, ignoreCase: true, out var parsed)
            ? parsed
            : AllocationStrategy.Avalanche;

    private static CreditActionPlanResponse MapToResponse(CreditActionPlanResult r) => new(
        Strategy: r.Strategy.ToString(),
        EmergencyFundTarget: r.EmergencyFundTarget,
        EmergencyFundReserved: r.EmergencyFundReserved,
        IsEmergencyFundFunded: r.IsEmergencyFundFunded,
        MonthsOfExpensesCovered: r.MonthsOfExpensesCovered,
        WindfallTotal: r.WindfallTotal,
        WindfallToDebt: r.WindfallToDebt,
        WindfallRemaining: r.WindfallRemaining,
        TotalDebtBefore: r.TotalDebtBefore,
        TotalDebtAfter: r.TotalDebtAfter,
        TotalInterestSaved: r.TotalInterestSaved,
        MonthsToDebtFreeBefore: r.MonthsToDebtFreeBefore,
        MonthsToDebtFreeAfter: r.MonthsToDebtFreeAfter,
        Steps: r.Steps,
        Recommendations: r.Recommendations);
}

public record CreditActionPlanRequest(
    decimal Windfall,
    int? EmergencyFundMonths,
    decimal? MonthlyEssentialExpenses,
    string? Strategy);

public record CreditActionPlanDefaultsResponse(
    decimal SuggestedWindfall,
    decimal MonthlyEssentialExpenses,
    decimal MonthlyIncome,
    int DefaultEmergencyFundMonths,
    IReadOnlyList<PlanDebtSummary> Debts);

public record PlanDebtSummary(
    string Name,
    decimal Balance,
    decimal EffectiveAPR,
    decimal MinimumPayment);

public record CreditActionPlanResponse(
    string Strategy,
    decimal EmergencyFundTarget,
    decimal EmergencyFundReserved,
    bool IsEmergencyFundFunded,
    decimal MonthsOfExpensesCovered,
    decimal WindfallTotal,
    decimal WindfallToDebt,
    decimal WindfallRemaining,
    decimal TotalDebtBefore,
    decimal TotalDebtAfter,
    decimal TotalInterestSaved,
    int MonthsToDebtFreeBefore,
    int MonthsToDebtFreeAfter,
    IReadOnlyList<DebtActionStep> Steps,
    IReadOnlyList<string> Recommendations);
