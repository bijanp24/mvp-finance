using FinanceEngine.Calculators;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class ScenarioEndpoints
{
    public static RouteGroupBuilder MapScenarioEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/calculate", CalculateScenario)
            .WithName("CalculateScenario")
            .WithDescription("Calculate scenario projections based on slider inputs");

        group.MapGet("/defaults", GetScenarioDefaults)
            .WithName("GetScenarioDefaults")
            .WithDescription("Get default values for scenario sliders based on current state");

        return group;
    }

    private static async Task<IResult> CalculateScenario(
        ScenarioRequest request,
        FinanceDbContext db,
        CancellationToken ct)
    {
        // Get accounts
        var accounts = await db.Accounts.ToListAsync(ct);
        var cashAccounts = accounts.Where(a => a.Type == AccountType.Cash).ToList();
        var debtAccounts = accounts.Where(a => a.Type == AccountType.Debt).ToList();
        var investmentAccounts = accounts.Where(a => a.Type == AccountType.Investment).ToList();

        // Calculate balances using event-sourced approach
        var events = await db.Events.Where(e => e.Status == EventStatus.Cleared).ToListAsync(ct);

        var totalCash = cashAccounts.Sum(a => CalculateAccountBalance(a, events));
        var totalDebt = debtAccounts.Sum(a => CalculateAccountBalance(a, events));
        var totalInvestments = investmentAccounts.Sum(a => CalculateAccountBalance(a, events));

        // Get settings
        var settings = await db.UserSettings.FirstOrDefaultAsync(ct) ?? new UserSettingsEntity
        {
            PayFrequency = PayFrequency.BiWeekly,
            PaycheckAmount = 0,
            SafetyBuffer = 0
        };

        // Get budgets to estimate discretionary spending
        var budgets = await db.Budgets.Where(b => b.IsActive).ToListAsync(ct);
        var monthlyBudgetTotal = budgets.Sum(b => GetMonthlyAmount(b.Amount, b.Frequency));

        // Get recurring contributions
        var contributions = await db.RecurringContributions
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        var monthlyDebtPayment = contributions
            .Where(c => accounts.Any(a => a.Id == c.TargetAccountId && a.Type == AccountType.Debt))
            .Sum(c => GetMonthlyContributionAmount(c.Amount, c.Frequency));

        var monthlyInvestmentContribution = contributions
            .Where(c => accounts.Any(a => a.Id == c.TargetAccountId && a.Type == AccountType.Investment))
            .Sum(c => GetMonthlyContributionAmount(c.Amount, c.Frequency));

        // Calculate weighted APR for debt accounts
        var debtAccountsWithBalance = debtAccounts.Where(a => a.InitialBalance > 0).ToList();
        var weightedDebtApr = debtAccountsWithBalance.Any()
            ? debtAccountsWithBalance.Sum(a => a.InitialBalance * (a.AnnualPercentageRate ?? 0)) / debtAccountsWithBalance.Sum(a => a.InitialBalance)
            : 0m;

        // Monthly income from settings
        var monthlyIncome = GetMonthlyIncome(settings.PaycheckAmount, settings.PayFrequency);

        // Calculate current safe-to-spend for reference
        var currentSafeToSpend = totalCash - settings.SafetyBuffer;

        // Base monthly expenses (fixed costs, not discretionary)
        var baseMonthlyExpenses = monthlyBudgetTotal * 0.7m; // Assume 70% is fixed, 30% discretionary
        var baseDiscretionarySpending = monthlyBudgetTotal * 0.3m;

        // Build scenario input
        var scenarioInput = new ScenarioInput(
            TotalCash: totalCash,
            TotalDebt: totalDebt,
            TotalInvestments: totalInvestments,
            CurrentSafeToSpend: currentSafeToSpend,
            UpcomingBills: baseMonthlyExpenses,
            MonthlyIncome: monthlyIncome,
            BaseMonthlyExpenses: baseMonthlyExpenses,
            BaseDiscretionarySpending: baseDiscretionarySpending,
            BaseDebtPayment: monthlyDebtPayment,
            BaseInvestmentContribution: monthlyInvestmentContribution,
            WeightedDebtApr: weightedDebtApr,
            ExpectedInvestmentReturn: 0.07m, // Default 7% annual return
            MonthlyDiscretionary: request.MonthlyDiscretionary,
            ExtraDebtPayment: request.ExtraDebtPayment,
            ExtraInvestmentContribution: request.ExtraInvestmentContribution
        );

        try
        {
            var result = ScenarioCalculator.Calculate(scenarioInput);
            return Results.Ok(MapToResponse(result));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetScenarioDefaults(
        FinanceDbContext db,
        CancellationToken ct)
    {
        // Get accounts
        var accounts = await db.Accounts.ToListAsync(ct);

        // Get settings
        var settings = await db.UserSettings.FirstOrDefaultAsync(ct);

        // Get budgets
        var budgets = await db.Budgets.Where(b => b.IsActive).ToListAsync(ct);
        var monthlyBudgetTotal = budgets.Sum(b => GetMonthlyAmount(b.Amount, b.Frequency));

        // Get recurring contributions
        var contributions = await db.RecurringContributions
            .Where(c => c.IsActive)
            .ToListAsync(ct);

        var monthlyDebtPayment = contributions
            .Where(c => accounts.Any(a => a.Id == c.TargetAccountId && a.Type == AccountType.Debt))
            .Sum(c => GetMonthlyContributionAmount(c.Amount, c.Frequency));

        var monthlyInvestmentContribution = contributions
            .Where(c => accounts.Any(a => a.Id == c.TargetAccountId && a.Type == AccountType.Investment))
            .Sum(c => GetMonthlyContributionAmount(c.Amount, c.Frequency));

        var baseDiscretionarySpending = monthlyBudgetTotal * 0.3m;

        return Results.Ok(new ScenarioDefaultsResponse(
            BaseDiscretionary: baseDiscretionarySpending,
            BaseDebtPayment: monthlyDebtPayment,
            BaseInvestmentContribution: monthlyInvestmentContribution,
            SliderRanges: new SliderRanges(
                DiscretionaryMin: 0,
                DiscretionaryMax: Math.Max(1000, baseDiscretionarySpending * 2),
                ExtraDebtMin: 0,
                ExtraDebtMax: 500,
                ExtraInvestmentMin: 0,
                ExtraInvestmentMax: 500
            )
        ));
    }

    private static decimal CalculateAccountBalance(AccountEntity account, List<FinancialEventEntity> events)
    {
        var balance = account.InitialBalance;
        var accountEvents = events.Where(e => e.AccountId == account.Id || e.TargetAccountId == account.Id);

        foreach (var evt in accountEvents)
        {
            var isSource = evt.AccountId == account.Id;
            var isTarget = evt.TargetAccountId == account.Id;

            balance += (evt.Type, account.Type, isSource, isTarget) switch
            {
                // Cash account events
                (EventType.Income, AccountType.Cash, true, _) => evt.Amount,
                (EventType.Expense, AccountType.Cash, true, _) => -evt.Amount,
                (EventType.DebtPayment, AccountType.Cash, true, _) => -evt.Amount,
                (EventType.SavingsContribution, AccountType.Cash, true, _) => -evt.Amount,
                (EventType.InvestmentContribution, AccountType.Cash, true, _) => -evt.Amount,

                // Debt account events
                (EventType.DebtCharge, AccountType.Debt, true, _) => evt.Amount,
                (EventType.DebtPayment, AccountType.Debt, _, true) => -evt.Amount,

                // Investment account events
                (EventType.InvestmentContribution, AccountType.Investment, _, true) => evt.Amount,

                // Savings (treated as investment here)
                (EventType.SavingsContribution, AccountType.Investment, _, true) => evt.Amount,

                _ => 0
            };
        }

        return balance;
    }

    private static decimal GetMonthlyAmount(decimal amount, BudgetFrequency frequency) => frequency switch
    {
        BudgetFrequency.Weekly => amount * 4.33m,
        BudgetFrequency.BiWeekly => amount * 2.17m,
        BudgetFrequency.Monthly => amount,
        _ => amount
    };

    private static decimal GetMonthlyContributionAmount(decimal amount, ContributionFrequency frequency) => frequency switch
    {
        ContributionFrequency.Weekly => amount * 4.33m,
        ContributionFrequency.BiWeekly => amount * 2.17m,
        ContributionFrequency.SemiMonthly => amount * 2,
        ContributionFrequency.Monthly => amount,
        ContributionFrequency.Quarterly => amount / 3,
        ContributionFrequency.Annually => amount / 12,
        _ => amount
    };

    private static decimal GetMonthlyIncome(decimal paycheck, PayFrequency frequency) => frequency switch
    {
        PayFrequency.Weekly => paycheck * 4.33m,
        PayFrequency.BiWeekly => paycheck * 2.17m,
        PayFrequency.SemiMonthly => paycheck * 2,
        PayFrequency.Monthly => paycheck,
        _ => paycheck
    };

    private static ScenarioResponse MapToResponse(ScenarioResult result) => new(
        AdjustedSafeToSpend: result.AdjustedSafeToSpend,
        MonthlySurplus: result.MonthlySurplus,
        DebtProjection: new DebtProjectionResponse(
            MonthsToPayoff: result.DebtProjection.MonthsToPayoff,
            TotalInterestPaid: result.DebtProjection.TotalInterestPaid,
            FinalPayoffDate: result.DebtProjection.FinalPayoffDate?.ToString("yyyy-MM-dd"),
            MonthlySnapshots: result.DebtProjection.MonthlySnapshots.Select(s => new DebtSnapshotResponse(
                Month: s.Month,
                Date: s.Date.ToString("yyyy-MM-dd"),
                RemainingBalance: s.RemainingBalance,
                InterestPaid: s.InterestPaid,
                PrincipalPaid: s.PrincipalPaid
            )).ToList()
        ),
        InvestmentProjection: new InvestmentProjectionResponse(
            ProjectedValue: result.InvestmentProjection.ProjectedValue,
            TotalContributions: result.InvestmentProjection.TotalContributions,
            TotalGrowth: result.InvestmentProjection.TotalGrowth,
            MonthlySnapshots: result.InvestmentProjection.MonthlySnapshots.Select(s => new InvestmentSnapshotResponse(
                Month: s.Month,
                Date: s.Date.ToString("yyyy-MM-dd"),
                Value: s.Value,
                Contributions: s.Contributions,
                Growth: s.Growth
            )).ToList()
        ),
        NetWorthProjection: new NetWorthProjectionResponse(
            ProjectedNetWorth: result.NetWorthProjection.ProjectedNetWorth,
            NetWorthChange: result.NetWorthProjection.NetWorthChange,
            MonthlySnapshots: result.NetWorthProjection.MonthlySnapshots.Select(s => new NetWorthSnapshotResponse(
                Month: s.Month,
                Date: s.Date.ToString("yyyy-MM-dd"),
                Cash: s.Cash,
                Debt: s.Debt,
                Investments: s.Investments,
                NetWorth: s.NetWorth
            )).ToList()
        ),
        Comparison: new ComparisonResponse(
            MonthsSavedOnDebt: result.Comparison.MonthsSavedOnDebt,
            InterestSaved: result.Comparison.InterestSaved,
            AdditionalInvestmentGrowth: result.Comparison.AdditionalInvestmentGrowth,
            NetBenefit: result.Comparison.NetBenefit
        ),
        SliderSummary: new SliderSummaryResponse(
            MonthlyDiscretionary: result.SliderSummary.MonthlyDiscretionary,
            ExtraDebtPayment: result.SliderSummary.ExtraDebtPayment,
            ExtraInvestmentContribution: result.SliderSummary.ExtraInvestmentContribution,
            TotalMonthlyChange: result.SliderSummary.TotalMonthlyChange
        )
    );
}

// Request/Response DTOs
public record ScenarioRequest(
    decimal MonthlyDiscretionary,
    decimal ExtraDebtPayment,
    decimal ExtraInvestmentContribution
);

public record ScenarioDefaultsResponse(
    decimal BaseDiscretionary,
    decimal BaseDebtPayment,
    decimal BaseInvestmentContribution,
    SliderRanges SliderRanges
);

public record SliderRanges(
    decimal DiscretionaryMin,
    decimal DiscretionaryMax,
    decimal ExtraDebtMin,
    decimal ExtraDebtMax,
    decimal ExtraInvestmentMin,
    decimal ExtraInvestmentMax
);

public record ScenarioResponse(
    decimal AdjustedSafeToSpend,
    decimal MonthlySurplus,
    DebtProjectionResponse DebtProjection,
    InvestmentProjectionResponse InvestmentProjection,
    NetWorthProjectionResponse NetWorthProjection,
    ComparisonResponse Comparison,
    SliderSummaryResponse SliderSummary
);

public record DebtProjectionResponse(
    int? MonthsToPayoff,
    decimal TotalInterestPaid,
    string? FinalPayoffDate,
    IReadOnlyList<DebtSnapshotResponse> MonthlySnapshots
);

public record DebtSnapshotResponse(
    int Month,
    string Date,
    decimal RemainingBalance,
    decimal InterestPaid,
    decimal PrincipalPaid
);

public record InvestmentProjectionResponse(
    decimal ProjectedValue,
    decimal TotalContributions,
    decimal TotalGrowth,
    IReadOnlyList<InvestmentSnapshotResponse> MonthlySnapshots
);

public record InvestmentSnapshotResponse(
    int Month,
    string Date,
    decimal Value,
    decimal Contributions,
    decimal Growth
);

public record NetWorthProjectionResponse(
    decimal ProjectedNetWorth,
    decimal NetWorthChange,
    IReadOnlyList<NetWorthSnapshotResponse> MonthlySnapshots
);

public record NetWorthSnapshotResponse(
    int Month,
    string Date,
    decimal Cash,
    decimal Debt,
    decimal Investments,
    decimal NetWorth
);

public record ComparisonResponse(
    int MonthsSavedOnDebt,
    decimal InterestSaved,
    decimal AdditionalInvestmentGrowth,
    decimal NetBenefit
);

public record SliderSummaryResponse(
    decimal MonthlyDiscretionary,
    decimal ExtraDebtPayment,
    decimal ExtraInvestmentContribution,
    decimal TotalMonthlyChange
);
