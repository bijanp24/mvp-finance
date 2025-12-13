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

        // Order events by date
        var events = input.Events.OrderBy(e => e.Date).ToList();
        var eventIndex = 0;

        // Calculate daily interest rates
        var dailyRates = debtAPRs.ToDictionary(
            kvp => kvp.Key,
            kvp => (decimal)(Math.Pow(1.0 + (double)kvp.Value, 1.0 / 365.0) - 1.0)
        );

        var currentDate = input.StartDate;

        while (currentDate <= input.EndDate)
        {
            var interestAccruedToday = 0m;
            var eventDescription = "Daily balance";

            // Accrue interest on all debts (if not the first day)
            if (currentDate > input.StartDate)
            {
                foreach (var debtName in debtBalances.Keys.ToList())
                {
                    if (debtBalances[debtName] > 0)
                    {
                        var dailyInterest = debtBalances[debtName] * dailyRates[debtName];
                        debtBalances[debtName] += dailyInterest;
                        interestAccruedToday += dailyInterest;
                        totalInterestPaid += dailyInterest;
                    }
                }
            }

            // Process all events for this date
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

            // Create snapshot
            var totalDebt = debtBalances.Values.Sum();
            snapshots.Add(new SimulationSnapshot(
                Date: currentDate,
                CashBalance: currentCash,
                DebtBalances: new Dictionary<string, decimal>(debtBalances),
                TotalDebt: totalDebt,
                InterestAccruedThisPeriod: interestAccruedToday,
                EventDescription: eventDescription
            ));

            // Check if debt-free
            if (debtFreeDate == null && totalDebt <= 0.01m) // Using small threshold for rounding
            {
                debtFreeDate = currentDate;
            }

            currentDate = currentDate.AddDays(1);
        }

        return new ForwardSimulationResult(
            Snapshots: snapshots,
            DebtFreeDate: debtFreeDate,
            FinalCashBalance: currentCash,
            FinalDebtBalances: debtBalances,
            TotalInterestPaid: totalInterestPaid
        );
    }
}
