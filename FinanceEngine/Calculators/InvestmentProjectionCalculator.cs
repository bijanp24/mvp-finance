using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;

namespace FinanceEngine.Calculators;

public static class InvestmentProjectionCalculator
{
    public static InvestmentProjectionResult Calculate(InvestmentProjectionInput input)
    {
        // TODO: Verify calculation accuracy.
        // - Ensure dailyNominalRate correctly reflects nominalAnnualReturn.
        // - Validate contribution timing (beginning vs end of day).
        // - Quantify any rounding errors over long periods.
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.InitialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative.", nameof(input.InitialBalance));

        if (input.EndDate < input.StartDate)
            throw new ArgumentException("End date must not be before start date.", nameof(input.EndDate));

        if (input.Contributions == null)
            throw new ArgumentNullException(nameof(input.Contributions));

        // Convert annual rates to daily rates for compound interest
        var dailyNominalRate = Math.Pow(1.0 + (double)input.NominalAnnualReturn, 1.0 / 365.0) - 1.0;
        var dailyInflationRate = Math.Pow(1.0 + (double)input.InflationRate, 1.0 / 365.0) - 1.0;

        var projections = new List<InvestmentProjectionPoint>();
        var contributions = input.Contributions.OrderBy(c => c.Date).ToList();

        var currentNominalValue = input.InitialBalance;
        var totalContributed = input.InitialBalance;
        var currentDate = input.StartDate;
        var contributionIndex = 0;

        // Project day by day
        while (currentDate <= input.EndDate)
        {
            // Apply any contributions for this date
            while (contributionIndex < contributions.Count &&
                   contributions[contributionIndex].Date.Date == currentDate.Date)
            {
                currentNominalValue += contributions[contributionIndex].Amount;
                totalContributed += contributions[contributionIndex].Amount;
                contributionIndex++;
            }

            // Apply daily growth (compound interest)
            if (currentDate > input.StartDate)
            {
                currentNominalValue *= (decimal)(1.0 + dailyNominalRate);
            }

            // Calculate real value (inflation-adjusted)
            var daysSinceStart = (currentDate - input.StartDate).Days;
            var inflationAdjustment = (decimal)Math.Pow(1.0 + dailyInflationRate, daysSinceStart);
            var currentRealValue = currentNominalValue / inflationAdjustment;

            // Calculate growth
            var nominalGrowth = currentNominalValue - totalContributed;
            var realGrowth = currentRealValue - totalContributed;

            projections.Add(new InvestmentProjectionPoint(
                Date: currentDate,
                NominalValue: currentNominalValue,
                RealValue: currentRealValue,
                TotalContributed: totalContributed,
                NominalGrowth: nominalGrowth,
                RealGrowth: realGrowth
            ));

            currentDate = currentDate.AddDays(1);
        }

        var finalProjection = projections.Last();

        return new InvestmentProjectionResult(
            Projections: projections,
            FinalNominalValue: finalProjection.NominalValue,
            FinalRealValue: finalProjection.RealValue,
            TotalContributions: finalProjection.TotalContributed,
            TotalNominalGrowth: finalProjection.NominalGrowth,
            TotalRealGrowth: finalProjection.RealGrowth
        );
    }

    public static InvestmentProjectionResult CalculateMonthly(InvestmentProjectionInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.InitialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative.", nameof(input.InitialBalance));

        if (input.EndDate < input.StartDate)
            throw new ArgumentException("End date must not be before start date.", nameof(input.EndDate));

        if (input.Contributions == null)
            throw new ArgumentNullException(nameof(input.Contributions));

        // Convert annual rates to monthly rates
        var monthlyNominalRate = Math.Pow(1.0 + (double)input.NominalAnnualReturn, 1.0 / 12.0) - 1.0;
        var monthlyInflationRate = Math.Pow(1.0 + (double)input.InflationRate, 1.0 / 12.0) - 1.0;

        var projections = new List<InvestmentProjectionPoint>();
        var contributions = input.Contributions.OrderBy(c => c.Date).ToList();

        var currentNominalValue = input.InitialBalance;
        var totalContributed = input.InitialBalance;
        var currentDate = input.StartDate;

        // Project month by month
        while (currentDate <= input.EndDate)
        {
            // Apply contributions for this month
            var monthlyContributions = contributions
                .Where(c => c.Date.Year == currentDate.Year && c.Date.Month == currentDate.Month)
                .Sum(c => c.Amount);

            if (monthlyContributions > 0)
            {
                currentNominalValue += monthlyContributions;
                totalContributed += monthlyContributions;
            }

            // Apply monthly growth
            if (currentDate > input.StartDate)
            {
                currentNominalValue *= (decimal)(1.0 + monthlyNominalRate);
            }

            // Calculate real value (inflation-adjusted)
            var monthsSinceStart = ((currentDate.Year - input.StartDate.Year) * 12) +
                                   (currentDate.Month - input.StartDate.Month);
            var inflationAdjustment = (decimal)Math.Pow(1.0 + monthlyInflationRate, monthsSinceStart);
            var currentRealValue = currentNominalValue / inflationAdjustment;

            // Calculate growth
            var nominalGrowth = currentNominalValue - totalContributed;
            var realGrowth = currentRealValue - totalContributed;

            projections.Add(new InvestmentProjectionPoint(
                Date: currentDate,
                NominalValue: currentNominalValue,
                RealValue: currentRealValue,
                TotalContributed: totalContributed,
                NominalGrowth: nominalGrowth,
                RealGrowth: realGrowth
            ));

            currentDate = currentDate.AddMonths(1);
        }

        var finalProjection = projections.Last();

        return new InvestmentProjectionResult(
            Projections: projections,
            FinalNominalValue: finalProjection.NominalValue,
            FinalRealValue: finalProjection.RealValue,
            TotalContributions: finalProjection.TotalContributed,
            TotalNominalGrowth: finalProjection.NominalGrowth,
            TotalRealGrowth: finalProjection.RealGrowth
        );
    }
}
