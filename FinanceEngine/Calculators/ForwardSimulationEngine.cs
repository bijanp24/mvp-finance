using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;

namespace FinanceEngine.Calculators;

public static class ForwardSimulationEngine
{
    public static ForwardSimulationResult Simulate(ForwardSimulationInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.EndDate < input.StartDate)
            throw new ArgumentException("End date must not be before start date.", nameof(input.EndDate));

        if (input.Debts == null)
            throw new ArgumentNullException(nameof(input.Debts));

        if (input.Events == null)
            throw new ArgumentNullException(nameof(input.Events));

        // Initialize state
        var currentCash = input.InitialCash;
        var debtBalances = input.Debts.ToDictionary(d => d.Name, d => d.CurrentBalance);
        var debtAPRs = input.Debts.ToDictionary(d => d.Name, d => d.AnnualPercentageRate);
        var snapshots = new List<SimulationSnapshot>();
        var totalInterestPaid = 0m;
        DateTime? debtFreeDate = null;

        // Initialize investment tracking
        var investmentAccounts = input.InvestmentAccounts ?? Enumerable.Empty<InvestmentAccount>();
        var investmentBalances = investmentAccounts.ToDictionary(i => i.Name, i => i.InitialBalance);
        var investmentRates = investmentAccounts.ToDictionary(i => i.Name, i => i.AnnualReturnRate);
        var investmentGrowthTracking = investmentAccounts.ToDictionary(i => i.Name, i => 0m);
        var investmentContributionTracking = investmentAccounts.ToDictionary(i => i.Name, i => 0m);
        var totalInvestmentGrowth = 0m;
        var totalContributed = 0m;
        DateTime? millionaireDate = null;

        // Order events and contributions by date
        var events = input.Events.OrderBy(e => e.Date).ToList();
        var contributions = (input.RecurringContributions ?? Enumerable.Empty<SimulationContribution>())
            .OrderBy(c => c.Date).ToList();
        var eventIndex = 0;
        var contributionIndex = 0;

        // Calculate daily interest rates for debts
        var dailyDebtRates = debtAPRs.ToDictionary(
            kvp => kvp.Key,
            kvp => (decimal)(Math.Pow(1.0 + (double)kvp.Value, 1.0 / 365.0) - 1.0)
        );

        var currentDate = input.StartDate;

        while (currentDate <= input.EndDate)
        {
            var interestAccruedToday = 0m;
            var dailyInvestmentGrowth = 0m;
            var eventDescription = "Daily balance";

            // 1. Apply daily investment growth
            foreach (var accountName in investmentBalances.Keys.ToList())
            {
                var balance = investmentBalances[accountName];
                var growth = CalculateDailyInvestmentGrowth(balance, investmentRates[accountName]);
                investmentBalances[accountName] += growth;
                investmentGrowthTracking[accountName] += growth;
                dailyInvestmentGrowth += growth;
            }
            totalInvestmentGrowth += dailyInvestmentGrowth;

            // 2. Accrue interest on all debts (if not the first day)
            if (currentDate > input.StartDate)
            {
                foreach (var debtName in debtBalances.Keys.ToList())
                {
                    if (debtBalances[debtName] > 0)
                    {
                        var dailyInterest = debtBalances[debtName] * dailyDebtRates[debtName];
                        debtBalances[debtName] += dailyInterest;
                        interestAccruedToday += dailyInterest;
                        totalInterestPaid += dailyInterest;
                    }
                }
            }

            // 3. Process contributions for this date
            while (contributionIndex < contributions.Count && contributions[contributionIndex].Date.Date == currentDate.Date)
            {
                var contribution = contributions[contributionIndex];
                ProcessContribution(contribution, ref currentCash, investmentBalances, investmentContributionTracking);
                totalContributed += contribution.Amount;
                contributionIndex++;
            }

            // 4. Process all events for this date
            while (eventIndex < events.Count && events[eventIndex].Date.Date == currentDate.Date)
            {
                var evt = events[eventIndex];
                eventDescription = evt.Description;

                switch (evt.Type)
                {
                    case SimulationEventType.Income:
                        currentCash += evt.Amount;
                        break;

                    case SimulationEventType.Expense:
                        currentCash -= evt.Amount;
                        break;

                    case SimulationEventType.DebtPayment:
                        if (!string.IsNullOrEmpty(evt.RelatedDebtName) && debtBalances.ContainsKey(evt.RelatedDebtName))
                        {
                            var payment = Math.Min(evt.Amount, debtBalances[evt.RelatedDebtName]);
                            debtBalances[evt.RelatedDebtName] -= payment;
                            currentCash -= payment;
                        }
                        break;

                    case SimulationEventType.DebtCharge:
                        if (!string.IsNullOrEmpty(evt.RelatedDebtName) && debtBalances.ContainsKey(evt.RelatedDebtName))
                        {
                            debtBalances[evt.RelatedDebtName] += evt.Amount;
                        }
                        break;

                    case SimulationEventType.InterestAccrual:
                        // Manual interest accrual (if needed for specific events)
                        if (!string.IsNullOrEmpty(evt.RelatedDebtName) && debtBalances.ContainsKey(evt.RelatedDebtName))
                        {
                            debtBalances[evt.RelatedDebtName] += evt.Amount;
                            interestAccruedToday += evt.Amount;
                            totalInterestPaid += evt.Amount;
                        }
                        break;
                }

                eventIndex++;
            }

            // 5. Calculate net worth
            var totalDebt = debtBalances.Values.Sum();
            var totalInvestments = investmentBalances.Values.Sum();
            var netWorth = CalculateNetWorth(currentCash, totalInvestments, totalDebt);

            // 6. Check millionaire milestone
            if (millionaireDate == null && netWorth >= 1_000_000m)
            {
                millionaireDate = currentDate;
            }

            // 7. Create snapshot
            var investmentSnapshots = investmentBalances.ToDictionary(
                kvp => kvp.Key,
                kvp => new InvestmentSnapshot(kvp.Key, kvp.Value, 0m) // Daily growth per account would need tracking
            );

            snapshots.Add(new SimulationSnapshot(
                Date: currentDate,
                CashBalance: currentCash,
                DebtBalances: new Dictionary<string, decimal>(debtBalances),
                InvestmentBalances: investmentSnapshots,
                TotalDebt: totalDebt,
                InterestAccruedThisPeriod: interestAccruedToday,
                DailyInvestmentGrowth: dailyInvestmentGrowth,
                NetWorth: netWorth,
                EventDescription: eventDescription
            ));

            // Check if debt-free
            if (debtFreeDate == null && totalDebt <= 0.01m) // Using small threshold for rounding
            {
                debtFreeDate = currentDate;
            }

            currentDate = currentDate.AddDays(1);
        }

        // Build final investment balances
        var finalInvestmentBalances = investmentBalances.ToDictionary(
            kvp => kvp.Key,
            kvp => new FinalInvestmentBalance(
                kvp.Key,
                kvp.Value,
                investmentGrowthTracking[kvp.Key],
                investmentContributionTracking[kvp.Key]
            )
        );

        return new ForwardSimulationResult(
            Snapshots: snapshots,
            DebtFreeDate: debtFreeDate,
            MillionaireDate: millionaireDate,
            FinalCashBalance: currentCash,
            FinalInvestmentBalance: investmentBalances.Values.Sum(),
            FinalNetWorth: snapshots.Last().NetWorth,
            FinalDebtBalances: debtBalances,
            FinalInvestmentBalances: finalInvestmentBalances,
            TotalInterestPaid: totalInterestPaid,
            TotalInvestmentGrowth: totalInvestmentGrowth,
            TotalContributed: totalContributed
        );
    }

    private static decimal CalculateDailyInvestmentGrowth(decimal balance, decimal annualReturnRate)
    {
        // Convert annual rate to daily rate using compound interest formula
        var dailyRate = Math.Pow(1.0 + (double)annualReturnRate, 1.0 / 365.0) - 1.0;
        return balance * (decimal)dailyRate;
    }

    private static void ProcessContribution(
        SimulationContribution contribution,
        ref decimal cashBalance,
        Dictionary<string, decimal> investmentBalances,
        Dictionary<string, decimal> contributionTracking)
    {
        // Deduct from cash
        cashBalance -= contribution.Amount;

        // Add to target investment
        if (investmentBalances.ContainsKey(contribution.TargetAccountName))
        {
            investmentBalances[contribution.TargetAccountName] += contribution.Amount;
            contributionTracking[contribution.TargetAccountName] += contribution.Amount;
        }
    }

    private static decimal CalculateNetWorth(decimal cash, decimal totalInvestments, decimal totalDebt)
    {
        return cash + totalInvestments - totalDebt;
    }
}
