using FinanceEngine.Calculators;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class CalculatorEndpoints
{
    public static RouteGroupBuilder MapCalculatorEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/spendable", CalculateSpendable);
        group.MapPost("/burn-rate", CalculateBurnRate);
        group.MapPost("/debt-allocation", CalculateDebtAllocation);
        group.MapPost("/investment-projection", CalculateInvestmentProjection);
        group.MapPost("/simulation", RunSimulation);

        return group;
    }

    private static async Task<IResult> CalculateSpendable(SpendableRequest request, FinanceDbContext db)
    {
        // Get spending events for burn rate calculation
        var startDate = request.CalculationDate.AddDays(-30);
        var spendingEvents = await db.Events
            .Where(e => e.Type == EventType.Expense && e.Date >= startDate && e.Date <= request.CalculationDate)
            .Select(e => new SpendingEvent(e.Date, e.Amount))
            .ToListAsync();

        // Calculate burn rate first
        var burnRateInput = new BurnRateInput(spendingEvents, request.CalculationDate, new[] { 7, 30 });
        var burnRateResult = BurnRateCalculator.Calculate(burnRateInput);
        var estimatedDailySpend = burnRateResult.BurnRatesByWindow.ContainsKey(7)
            ? burnRateResult.BurnRatesByWindow[7].AverageDailySpend
            : 0m;

        // Build spendable input
        var obligations = request.Obligations?.Select(o => new Obligation(o.DueDate, o.Amount, o.Description))
            ?? Enumerable.Empty<Obligation>();

        var incomeEvents = request.UpcomingIncome?.Select(i => new IncomeEvent(i.Date, i.Amount, i.Description))
            ?? Enumerable.Empty<IncomeEvent>();

        var input = new SpendableInput(
            AvailableCash: request.AvailableCash,
            CalculationDate: request.CalculationDate,
            UpcomingObligations: obligations,
            UpcomingIncome: incomeEvents,
            EstimatedDailySpend: estimatedDailySpend,
            ManualSafetyBuffer: request.ManualSafetyBuffer
        );

        var result = SpendableCalculator.Calculate(input);

        return Results.Ok(new
        {
            spendableNow = result.SpendableNow,
            expectedCashAtNextPaycheck = result.ExpectedCashAtNextPaycheck,
            nextPaycheckDate = result.NextPaycheckDate,
            breakdown = result.Breakdown,
            conservativeScenario = result.ConservativeScenario,
            burnRate = new
            {
                daily7Day = burnRateResult.BurnRatesByWindow.ContainsKey(7) ? burnRateResult.BurnRatesByWindow[7].AverageDailySpend : 0,
                daily30Day = burnRateResult.BurnRatesByWindow.ContainsKey(30) ? burnRateResult.BurnRatesByWindow[30].AverageDailySpend : 0
            }
        });
    }

    private static IResult CalculateBurnRate(BurnRateRequest request)
    {
        var spendingEvents = request.SpendingEvents
            .Select(e => new SpendingEvent(e.Date, e.Amount))
            .ToList();

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: request.CalculationDate,
            WindowDays: request.WindowDays ?? new[] { 7, 30, 90 }
        );

        var result = BurnRateCalculator.Calculate(input);

        return Results.Ok(result);
    }

    private static IResult CalculateDebtAllocation(DebtAllocationRequest request)
    {
        var debts = request.Debts
            .Select(d => new Debt(d.Name, d.Balance, d.AnnualPercentageRate, d.MinimumPayment))
            .ToList();

        if (!Enum.TryParse<AllocationStrategy>(request.Strategy, true, out var strategy))
            strategy = AllocationStrategy.Avalanche;

        var input = new DebtAllocationInput(debts, request.ExtraPaymentAmount, strategy);
        var result = DebtAllocationCalculator.Calculate(input);

        return Results.Ok(result);
    }

    private static IResult CalculateInvestmentProjection(InvestmentProjectionRequest request)
    {
        var contributions = request.Contributions?
            .Select(c => new InvestmentContribution(c.Date, c.Amount))
            .ToList() ?? new List<InvestmentContribution>();

        var input = new InvestmentProjectionInput(
            InitialBalance: request.InitialBalance,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            Contributions: contributions,
            NominalAnnualReturn: request.NominalAnnualReturn,
            InflationRate: request.InflationRate
        );

        var result = request.UseMonthly
            ? InvestmentProjectionCalculator.CalculateMonthly(input)
            : InvestmentProjectionCalculator.Calculate(input);

        return Results.Ok(new
        {
            finalNominalValue = result.FinalNominalValue,
            finalRealValue = result.FinalRealValue,
            totalContributions = result.TotalContributions,
            totalNominalGrowth = result.TotalNominalGrowth,
            totalRealGrowth = result.TotalRealGrowth,
            projections = result.Projections.Select(p => new
            {
                date = p.Date,
                nominalValue = p.NominalValue,
                realValue = p.RealValue
            }).ToList()
        });
    }

    private static IResult RunSimulation(SimulationRequest request)
    {
        var debts = request.Debts?
            .Select(d => new DebtAccount(d.Name, d.Balance, d.AnnualPercentageRate, d.MinimumPayment))
            .ToList() ?? new List<DebtAccount>();

        var events = request.Events?
            .Select(e => new SimulationEvent(
                e.Date,
                Enum.TryParse<SimulationEventType>(e.Type, true, out var type) ? type : SimulationEventType.Expense,
                e.Description,
                e.Amount,
                e.RelatedDebtName
            ))
            .ToList() ?? new List<SimulationEvent>();

        var input = new ForwardSimulationInput(
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            InitialCash: request.InitialCash,
            Debts: debts,
            Events: events
        );

        var result = ForwardSimulationEngine.Simulate(input);

        return Results.Ok(new
        {
            debtFreeDate = result.DebtFreeDate,
            finalCashBalance = result.FinalCashBalance,
            finalDebtBalances = result.FinalDebtBalances,
            totalInterestPaid = result.TotalInterestPaid,
            snapshots = result.Snapshots.Select(s => new
            {
                date = s.Date,
                cashBalance = s.CashBalance,
                totalDebt = s.TotalDebt,
                debtBalances = s.DebtBalances
            }).ToList()
        });
    }
}

// Request DTOs
public record SpendableRequest(
    decimal AvailableCash,
    DateTime CalculationDate,
    List<ObligationDto>? Obligations = null,
    List<IncomeDto>? UpcomingIncome = null,
    decimal? ManualSafetyBuffer = null
);

public record ObligationDto(DateTime DueDate, decimal Amount, string Description);
public record IncomeDto(DateTime Date, decimal Amount, string Description);

public record BurnRateRequest(
    List<SpendingEventDto> SpendingEvents,
    DateTime CalculationDate,
    int[]? WindowDays = null
);

public record SpendingEventDto(DateTime Date, decimal Amount);

public record DebtAllocationRequest(
    List<DebtDto> Debts,
    decimal ExtraPaymentAmount,
    string Strategy = "Avalanche"
);

public record DebtDto(string Name, decimal Balance, decimal AnnualPercentageRate, decimal MinimumPayment);

public record InvestmentProjectionRequest(
    decimal InitialBalance,
    DateTime StartDate,
    DateTime EndDate,
    decimal NominalAnnualReturn,
    decimal InflationRate = 0.03m,
    bool UseMonthly = true,
    List<ContributionDto>? Contributions = null
);

public record ContributionDto(DateTime Date, decimal Amount);

public record SimulationRequest(
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialCash,
    List<SimDebtDto>? Debts = null,
    List<SimEventDto>? Events = null
);

public record SimDebtDto(string Name, decimal Balance, decimal AnnualPercentageRate, decimal MinimumPayment);
public record SimEventDto(DateTime Date, string Type, string Description, decimal Amount, string? RelatedDebtName = null);
