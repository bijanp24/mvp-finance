namespace FinanceEngine.Calculators;

/// <summary>
/// Calculator for "what-if" scenario planning.
/// Takes current financial state and slider adjustments to project future outcomes.
/// </summary>
public static class ScenarioCalculator
{
    /// <summary>
    /// Calculate the impact of scenario adjustments on financial outcomes.
    /// </summary>
    public static ScenarioResult Calculate(ScenarioInput input)
    {
        // Validate input
        if (input.MonthlyDiscretionary < 0)
            throw new ArgumentException("Monthly discretionary cannot be negative", nameof(input));
        if (input.ExtraDebtPayment < 0)
            throw new ArgumentException("Extra debt payment cannot be negative", nameof(input));
        if (input.ExtraInvestmentContribution < 0)
            throw new ArgumentException("Extra investment contribution cannot be negative", nameof(input));

        var baselineMonthlyIncome = input.MonthlyIncome;
        var baselineMonthlyExpenses = input.BaseMonthlyExpenses;
        var baselineDebtPayment = input.BaseDebtPayment;
        var baselineInvestmentContribution = input.BaseInvestmentContribution;

        // Calculate adjusted monthly figures
        var adjustedDiscretionary = input.MonthlyDiscretionary;
        var adjustedDebtPayment = baselineDebtPayment + input.ExtraDebtPayment;
        var adjustedInvestmentContribution = baselineInvestmentContribution + input.ExtraInvestmentContribution;

        // Total monthly outflow
        var totalMonthlyOutflow = baselineMonthlyExpenses + adjustedDiscretionary + adjustedDebtPayment + adjustedInvestmentContribution;
        var monthlySurplus = baselineMonthlyIncome - totalMonthlyOutflow;

        // Calculate safe-to-spend impact
        var adjustedSafeToSpend = CalculateAdjustedSafeToSpend(input, adjustedDiscretionary, adjustedDebtPayment, adjustedInvestmentContribution);

        // Calculate debt payoff projection
        var debtProjection = CalculateDebtProjection(input.TotalDebt, input.WeightedDebtApr, adjustedDebtPayment);

        // Calculate investment projection (12 months)
        var investmentProjection = CalculateInvestmentProjection(
            input.TotalInvestments,
            adjustedInvestmentContribution,
            input.ExpectedInvestmentReturn,
            12);

        // Calculate net worth projection
        var netWorthProjection = CalculateNetWorthProjection(
            input.TotalCash,
            input.TotalDebt,
            input.TotalInvestments,
            monthlySurplus,
            adjustedDebtPayment,
            adjustedInvestmentContribution,
            input.WeightedDebtApr,
            input.ExpectedInvestmentReturn,
            12);

        // Generate comparison with baseline
        var baselineDebtProjection = CalculateDebtProjection(input.TotalDebt, input.WeightedDebtApr, baselineDebtPayment);
        var baselineInvestmentProjection = CalculateInvestmentProjection(
            input.TotalInvestments,
            baselineInvestmentContribution,
            input.ExpectedInvestmentReturn,
            12);

        var monthsSaved = baselineDebtProjection.MonthsToPayoff.HasValue && debtProjection.MonthsToPayoff.HasValue
            ? baselineDebtProjection.MonthsToPayoff.Value - debtProjection.MonthsToPayoff.Value
            : 0;

        var interestSaved = baselineDebtProjection.TotalInterestPaid - debtProjection.TotalInterestPaid;
        var additionalInvestmentGrowth = investmentProjection.ProjectedValue - baselineInvestmentProjection.ProjectedValue;

        return new ScenarioResult(
            AdjustedSafeToSpend: adjustedSafeToSpend,
            MonthlySurplus: monthlySurplus,
            DebtProjection: debtProjection,
            InvestmentProjection: investmentProjection,
            NetWorthProjection: netWorthProjection,
            Comparison: new ScenarioComparison(
                MonthsSavedOnDebt: monthsSaved,
                InterestSaved: interestSaved,
                AdditionalInvestmentGrowth: additionalInvestmentGrowth,
                NetBenefit: interestSaved + additionalInvestmentGrowth
            ),
            SliderSummary: new SliderSummary(
                MonthlyDiscretionary: adjustedDiscretionary,
                ExtraDebtPayment: input.ExtraDebtPayment,
                ExtraInvestmentContribution: input.ExtraInvestmentContribution,
                TotalMonthlyChange: totalMonthlyOutflow - (baselineMonthlyExpenses + input.BaseDiscretionarySpending + baselineDebtPayment + baselineInvestmentContribution)
            )
        );
    }

    private static decimal CalculateAdjustedSafeToSpend(
        ScenarioInput input,
        decimal adjustedDiscretionary,
        decimal adjustedDebtPayment,
        decimal adjustedInvestmentContribution)
    {
        // Safe to spend = Available Cash - Upcoming Bills - Required Contributions - Buffer
        var adjustedUpcomingBills = input.UpcomingBills;

        // Account for the scenario adjustments
        var discretionaryChange = adjustedDiscretionary - input.BaseDiscretionarySpending;
        var debtChange = adjustedDebtPayment - input.BaseDebtPayment;
        var investmentChange = adjustedInvestmentContribution - input.BaseInvestmentContribution;

        var totalAdditionalCommitment = discretionaryChange + debtChange + investmentChange;

        return input.CurrentSafeToSpend - totalAdditionalCommitment;
    }

    private static ScenarioDebtProjection CalculateDebtProjection(
        decimal totalDebt,
        decimal weightedApr,
        decimal monthlyPayment)
    {
        if (totalDebt <= 0)
        {
            return new ScenarioDebtProjection(
                MonthsToPayoff: 0,
                TotalInterestPaid: 0,
                FinalPayoffDate: DateOnly.FromDateTime(DateTime.Today),
                MonthlySnapshots: []
            );
        }

        if (monthlyPayment <= 0)
        {
            return new ScenarioDebtProjection(
                MonthsToPayoff: null, // Never pays off
                TotalInterestPaid: 0,
                FinalPayoffDate: null,
                MonthlySnapshots: []
            );
        }

        var monthlyRate = weightedApr / 12;
        var balance = totalDebt;
        var totalInterest = 0m;
        var months = 0;
        var snapshots = new List<ScenarioDebtSnapshot>();
        var startDate = DateOnly.FromDateTime(DateTime.Today);

        // Project up to 360 months (30 years) max
        while (balance > 0 && months < 360)
        {
            var interestThisMonth = balance * monthlyRate;
            var principalThisMonth = Math.Min(monthlyPayment - interestThisMonth, balance);

            if (principalThisMonth <= 0)
            {
                // Payment doesn't cover interest - will never pay off
                return new ScenarioDebtProjection(
                    MonthsToPayoff: null,
                    TotalInterestPaid: 0,
                    FinalPayoffDate: null,
                    MonthlySnapshots: snapshots
                );
            }

            totalInterest += interestThisMonth;
            balance -= principalThisMonth;
            months++;

            // Record monthly snapshots for the first 24 months
            if (months <= 24)
            {
                snapshots.Add(new ScenarioDebtSnapshot(
                    Month: months,
                    Date: startDate.AddMonths(months),
                    RemainingBalance: Math.Max(0, balance),
                    InterestPaid: interestThisMonth,
                    PrincipalPaid: principalThisMonth
                ));
            }
        }

        return new ScenarioDebtProjection(
            MonthsToPayoff: months,
            TotalInterestPaid: totalInterest,
            FinalPayoffDate: startDate.AddMonths(months),
            MonthlySnapshots: snapshots
        );
    }

    private static ScenarioInvestmentProjection CalculateInvestmentProjection(
        decimal initialBalance,
        decimal monthlyContribution,
        decimal expectedAnnualReturn,
        int months)
    {
        var monthlyReturn = expectedAnnualReturn / 12;
        var balance = initialBalance;
        var totalContributions = 0m;
        var snapshots = new List<ScenarioInvestmentSnapshot>();
        var startDate = DateOnly.FromDateTime(DateTime.Today);

        for (int i = 1; i <= months; i++)
        {
            var growthThisMonth = balance * monthlyReturn;
            balance += growthThisMonth;
            balance += monthlyContribution;
            totalContributions += monthlyContribution;

            snapshots.Add(new ScenarioInvestmentSnapshot(
                Month: i,
                Date: startDate.AddMonths(i),
                Value: balance,
                Contributions: totalContributions,
                Growth: balance - initialBalance - totalContributions
            ));
        }

        return new ScenarioInvestmentProjection(
            ProjectedValue: balance,
            TotalContributions: totalContributions,
            TotalGrowth: balance - initialBalance - totalContributions,
            MonthlySnapshots: snapshots
        );
    }

    private static ScenarioNetWorthProjection CalculateNetWorthProjection(
        decimal totalCash,
        decimal totalDebt,
        decimal totalInvestments,
        decimal monthlySurplus,
        decimal monthlyDebtPayment,
        decimal monthlyInvestmentContribution,
        decimal debtApr,
        decimal investmentReturn,
        int months)
    {
        var cash = totalCash;
        var debt = totalDebt;
        var investments = totalInvestments;
        var snapshots = new List<ScenarioNetWorthSnapshot>();
        var startDate = DateOnly.FromDateTime(DateTime.Today);

        var monthlyDebtRate = debtApr / 12;
        var monthlyInvestmentRate = investmentReturn / 12;

        for (int i = 1; i <= months; i++)
        {
            // Debt accrues interest and gets paid down
            var debtInterest = debt * monthlyDebtRate;
            debt = Math.Max(0, debt + debtInterest - monthlyDebtPayment);

            // Investments grow and receive contributions
            investments = investments * (1 + monthlyInvestmentRate) + monthlyInvestmentContribution;

            // Cash changes by surplus (income - all expenses including debt payment and investment)
            cash += monthlySurplus;
            // Adjust for double-counting (surplus already includes debt payment and investment effects)
            // Actually, monthly surplus is after all outflows, so cash just accumulates the surplus

            var netWorth = cash + investments - debt;

            snapshots.Add(new ScenarioNetWorthSnapshot(
                Month: i,
                Date: startDate.AddMonths(i),
                Cash: cash,
                Debt: debt,
                Investments: investments,
                NetWorth: netWorth
            ));
        }

        var finalNetWorth = cash + investments - debt;
        var initialNetWorth = totalCash + totalInvestments - totalDebt;

        return new ScenarioNetWorthProjection(
            ProjectedNetWorth: finalNetWorth,
            NetWorthChange: finalNetWorth - initialNetWorth,
            MonthlySnapshots: snapshots
        );
    }
}

// Input record
public record ScenarioInput(
    // Current state
    decimal TotalCash,
    decimal TotalDebt,
    decimal TotalInvestments,
    decimal CurrentSafeToSpend,
    decimal UpcomingBills,

    // Income and base expenses
    decimal MonthlyIncome,
    decimal BaseMonthlyExpenses,
    decimal BaseDiscretionarySpending,
    decimal BaseDebtPayment,
    decimal BaseInvestmentContribution,

    // Rates
    decimal WeightedDebtApr,
    decimal ExpectedInvestmentReturn,

    // Slider inputs (0 = baseline)
    decimal MonthlyDiscretionary,
    decimal ExtraDebtPayment,
    decimal ExtraInvestmentContribution
);

// Output records
public record ScenarioResult(
    decimal AdjustedSafeToSpend,
    decimal MonthlySurplus,
    ScenarioDebtProjection DebtProjection,
    ScenarioInvestmentProjection InvestmentProjection,
    ScenarioNetWorthProjection NetWorthProjection,
    ScenarioComparison Comparison,
    SliderSummary SliderSummary
);

public record ScenarioComparison(
    int MonthsSavedOnDebt,
    decimal InterestSaved,
    decimal AdditionalInvestmentGrowth,
    decimal NetBenefit
);

public record SliderSummary(
    decimal MonthlyDiscretionary,
    decimal ExtraDebtPayment,
    decimal ExtraInvestmentContribution,
    decimal TotalMonthlyChange
);

public record ScenarioDebtProjection(
    int? MonthsToPayoff,
    decimal TotalInterestPaid,
    DateOnly? FinalPayoffDate,
    IReadOnlyList<ScenarioDebtSnapshot> MonthlySnapshots
);

public record ScenarioDebtSnapshot(
    int Month,
    DateOnly Date,
    decimal RemainingBalance,
    decimal InterestPaid,
    decimal PrincipalPaid
);

public record ScenarioInvestmentProjection(
    decimal ProjectedValue,
    decimal TotalContributions,
    decimal TotalGrowth,
    IReadOnlyList<ScenarioInvestmentSnapshot> MonthlySnapshots
);

public record ScenarioInvestmentSnapshot(
    int Month,
    DateOnly Date,
    decimal Value,
    decimal Contributions,
    decimal Growth
);

public record ScenarioNetWorthProjection(
    decimal ProjectedNetWorth,
    decimal NetWorthChange,
    IReadOnlyList<ScenarioNetWorthSnapshot> MonthlySnapshots
);

public record ScenarioNetWorthSnapshot(
    int Month,
    DateOnly Date,
    decimal Cash,
    decimal Debt,
    decimal Investments,
    decimal NetWorth
);
