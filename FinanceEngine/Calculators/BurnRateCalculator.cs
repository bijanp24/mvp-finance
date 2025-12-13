using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;

namespace FinanceEngine.Calculators;

public static class BurnRateCalculator
{
    public static BurnRateResult Calculate(BurnRateInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.SpendingEvents == null)
            throw new ArgumentNullException(nameof(input.SpendingEvents));

        var burnRatesByWindow = new Dictionary<int, WindowBurnRate>();

        foreach (var windowDays in input.WindowDays)
        {
            if (windowDays <= 0)
                throw new ArgumentException($"Window days must be positive. Got: {windowDays}");

            var windowBurnRate = CalculateWindowBurnRate(
                input.SpendingEvents,
                input.CalculationDate,
                windowDays
            );

            burnRatesByWindow[windowDays] = windowBurnRate;
        }

        return new BurnRateResult(burnRatesByWindow);
    }

    private static WindowBurnRate CalculateWindowBurnRate(
        IEnumerable<SpendingEvent> spendingEvents,
        DateTime calculationDate,
        int windowDays)
    {
        var windowStartDate = calculationDate.AddDays(-windowDays);

        // Filter events within the window
        var eventsInWindow = spendingEvents
            .Where(e => e.Date > windowStartDate && e.Date <= calculationDate)
            .ToList();

        // Group by date and sum amounts per day
        var dailySpending = eventsInWindow
            .GroupBy(e => e.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        // Create a complete list of daily spending amounts, including zero days
        var allDailyAmounts = new List<decimal>();
        for (int i = 0; i < windowDays; i++)
        {
            var date = calculationDate.AddDays(-i).Date;
            var amount = dailySpending.GetValueOrDefault(date, 0m);
            allDailyAmounts.Add(amount);
        }

        // Calculate statistics
        var totalSpend = eventsInWindow.Sum(e => e.Amount);
        var averageDailySpend = windowDays > 0 ? totalSpend / windowDays : 0m;

        var nonZeroAmounts = allDailyAmounts.Where(a => a > 0).ToList();
        var minDailySpend = nonZeroAmounts.Any() ? nonZeroAmounts.Min() : 0m;
        var maxDailySpend = allDailyAmounts.Max();

        var standardDeviation = CalculateStandardDeviation(allDailyAmounts, averageDailySpend);

        var sortedAmounts = allDailyAmounts.OrderBy(a => a).ToList();
        var percentile25 = CalculatePercentile(sortedAmounts, 0.25m);
        var percentile75 = CalculatePercentile(sortedAmounts, 0.75m);
        var percentile90 = CalculatePercentile(sortedAmounts, 0.90m);

        return new WindowBurnRate(
            WindowDays: windowDays,
            AverageDailySpend: averageDailySpend,
            StandardDeviation: standardDeviation,
            MinDailySpend: minDailySpend,
            MaxDailySpend: maxDailySpend,
            Percentile25: percentile25,
            Percentile75: percentile75,
            Percentile90: percentile90
        );
    }

    private static decimal CalculateStandardDeviation(List<decimal> values, decimal mean)
    {
        if (values.Count == 0)
            return 0m;

        var sumOfSquaredDifferences = values.Sum(v => (v - mean) * (v - mean));
        var variance = sumOfSquaredDifferences / values.Count;

        // Convert to double for Sqrt, then back to decimal
        return (decimal)Math.Sqrt((double)variance);
    }

    private static decimal CalculatePercentile(List<decimal> sortedValues, decimal percentile)
    {
        if (sortedValues.Count == 0)
            return 0m;

        if (sortedValues.Count == 1)
            return sortedValues[0];

        // Using nearest rank method
        var index = (int)Math.Ceiling((decimal)sortedValues.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));

        return sortedValues[index];
    }
}
