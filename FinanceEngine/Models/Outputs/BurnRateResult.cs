namespace FinanceEngine.Models.Outputs;

public record BurnRateResult(
    Dictionary<int, WindowBurnRate> BurnRatesByWindow
);

public record WindowBurnRate(
    int WindowDays,
    decimal AverageDailySpend,
    decimal StandardDeviation,
    decimal MinDailySpend,
    decimal MaxDailySpend,
    decimal Percentile25,
    decimal Percentile75,
    decimal Percentile90
);
