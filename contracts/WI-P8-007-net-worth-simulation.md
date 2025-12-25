# WI-P8-007: Net Worth Simulation Enhancement

## Objective
Extend the `ForwardSimulationEngine` to track investment accounts with recurring contributions, enabling true net worth projections that combine cash, debt, and investments.

## Context
- Current `ForwardSimulationEngine` tracks:
  - Cash balance
  - Debt balances with daily interest accrual
  - Debt-free date milestone
- Missing:
  - Investment account tracking
  - Investment growth (compound returns)
  - Recurring contributions as cash outflows
  - True net worth (cash + investments - debt)

## Files to Modify
- `FinanceEngine/Calculators/ForwardSimulationEngine.cs`
- `FinanceEngine/Models/SimulationModels.cs`
- `FinanceEngine.Tests/Calculators/ForwardSimulationEngineTests.cs`

## Current Models

```csharp
// Existing
public record ForwardSimulationInput(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal InitialCash,
    IEnumerable<DebtAccountInput> DebtAccounts,
    IEnumerable<SimulationEvent> Events
);

public record ForwardSimulationResult(
    IEnumerable<DailySnapshot> Snapshots,
    DateOnly? DebtFreeDate,
    decimal TotalInterestPaid,
    decimal FinalCashBalance,
    IEnumerable<FinalDebtBalance> FinalDebtBalances
);

public record DailySnapshot(
    DateOnly Date,
    decimal CashBalance,
    IEnumerable<DebtSnapshot> DebtBalances,
    decimal DailyInterestAccrued
);
```

## Enhanced Models

```csharp
// New: Investment account input
public record InvestmentAccountInput(
    string Name,
    decimal InitialBalance,
    decimal AnnualReturnRate  // e.g., 0.07 for 7%
);

// New: Recurring contribution for simulation
public record SimulationContribution(
    DateOnly Date,
    decimal Amount,
    string SourceAccountName,   // For tracking
    string TargetAccountName    // For tracking
);

// Enhanced input
public record ForwardSimulationInput(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal InitialCash,
    IEnumerable<DebtAccountInput> DebtAccounts,
    IEnumerable<InvestmentAccountInput> InvestmentAccounts,  // NEW
    IEnumerable<SimulationEvent> Events,
    IEnumerable<SimulationContribution> RecurringContributions  // NEW
);

// Enhanced snapshot
public record DailySnapshot(
    DateOnly Date,
    decimal CashBalance,
    IEnumerable<DebtSnapshot> DebtBalances,
    IEnumerable<InvestmentSnapshot> InvestmentBalances,  // NEW
    decimal DailyInterestAccrued,
    decimal DailyInvestmentGrowth,  // NEW
    decimal NetWorth  // NEW: Cash + Investments - Debt
);

public record InvestmentSnapshot(
    string AccountName,
    decimal Balance,
    decimal DailyGrowth
);

// Enhanced result
public record ForwardSimulationResult(
    IEnumerable<DailySnapshot> Snapshots,
    DateOnly? DebtFreeDate,
    DateOnly? MillionaireDate,  // NEW: When net worth hits $1M
    decimal TotalInterestPaid,
    decimal TotalInvestmentGrowth,  // NEW
    decimal TotalContributed,  // NEW
    decimal FinalCashBalance,
    decimal FinalInvestmentBalance,  // NEW
    decimal FinalNetWorth,  // NEW
    IEnumerable<FinalDebtBalance> FinalDebtBalances,
    IEnumerable<FinalInvestmentBalance> FinalInvestmentBalances  // NEW
);

public record FinalInvestmentBalance(
    string AccountName,
    decimal Balance,
    decimal TotalGrowth,
    decimal TotalContributed
);
```

## Algorithm Updates

### Daily Investment Growth
```csharp
private static decimal CalculateDailyInvestmentGrowth(
    decimal balance,
    decimal annualReturnRate)
{
    // Convert annual rate to daily rate
    var dailyRate = Math.Pow(1 + (double)annualReturnRate, 1.0 / 365) - 1;
    return balance * (decimal)dailyRate;
}
```

### Process Contributions
```csharp
private static void ProcessContribution(
    DateOnly date,
    SimulationContribution contribution,
    ref decimal cashBalance,
    Dictionary<string, decimal> investmentBalances)
{
    // Deduct from cash
    cashBalance -= contribution.Amount;

    // Add to target investment
    if (investmentBalances.ContainsKey(contribution.TargetAccountName))
    {
        investmentBalances[contribution.TargetAccountName] += contribution.Amount;
    }
}
```

### Net Worth Calculation
```csharp
private static decimal CalculateNetWorth(
    decimal cash,
    IEnumerable<decimal> investmentBalances,
    IEnumerable<decimal> debtBalances)
{
    var totalInvestments = investmentBalances.Sum();
    var totalDebt = debtBalances.Sum();
    return cash + totalInvestments - totalDebt;
}
```

### Main Loop Enhancement
```csharp
public static ForwardSimulationResult Calculate(ForwardSimulationInput input)
{
    var snapshots = new List<DailySnapshot>();
    var cash = input.InitialCash;
    var debts = /* existing initialization */;
    var investments = input.InvestmentAccounts
        .ToDictionary(i => i.Name, i => i.InitialBalance);
    var investmentRates = input.InvestmentAccounts
        .ToDictionary(i => i.Name, i => i.AnnualReturnRate);

    DateOnly? millionaireDate = null;
    decimal totalInvestmentGrowth = 0;
    decimal totalContributed = 0;

    for (var date = input.StartDate; date <= input.EndDate; date = date.AddDays(1))
    {
        // 1. Apply daily investment growth
        var dailyGrowth = 0m;
        foreach (var (name, balance) in investments.ToList())
        {
            var growth = CalculateDailyInvestmentGrowth(balance, investmentRates[name]);
            investments[name] += growth;
            dailyGrowth += growth;
        }
        totalInvestmentGrowth += dailyGrowth;

        // 2. Apply daily interest accrual (existing)

        // 3. Process contributions for this date
        var todayContributions = input.RecurringContributions
            .Where(c => c.Date == date);
        foreach (var contribution in todayContributions)
        {
            ProcessContribution(date, contribution, ref cash, investments);
            totalContributed += contribution.Amount;
        }

        // 4. Process other events (existing)

        // 5. Calculate net worth
        var netWorth = CalculateNetWorth(cash, investments.Values, debts.Values);

        // 6. Check millionaire milestone
        if (millionaireDate is null && netWorth >= 1_000_000)
        {
            millionaireDate = date;
        }

        // 7. Create snapshot
        snapshots.Add(new DailySnapshot(
            date,
            cash,
            debts.Select(d => new DebtSnapshot(d.Key, d.Value, /* interest */)),
            investments.Select(i => new InvestmentSnapshot(i.Key, i.Value, /* growth */)),
            dailyInterest,
            dailyGrowth,
            netWorth
        ));
    }

    return new ForwardSimulationResult(
        snapshots,
        debtFreeDate,
        millionaireDate,
        totalInterestPaid,
        totalInvestmentGrowth,
        totalContributed,
        cash,
        investments.Values.Sum(),
        snapshots.Last().NetWorth,
        /* existing debt finals */,
        investments.Select(i => new FinalInvestmentBalance(/* ... */))
    );
}
```

## Test Cases

```csharp
[Fact]
public void Calculate_WithInvestments_TracksGrowth()
{
    var input = new ForwardSimulationInput(
        new DateOnly(2025, 1, 1),
        new DateOnly(2025, 12, 31),
        10000,
        Array.Empty<DebtAccountInput>(),
        new[] { new InvestmentAccountInput("401k", 50000, 0.07m) },
        Array.Empty<SimulationEvent>(),
        Array.Empty<SimulationContribution>()
    );

    var result = ForwardSimulationEngine.Calculate(input);

    Assert.True(result.FinalInvestmentBalance > 50000);
    Assert.True(result.TotalInvestmentGrowth > 0);
}

[Fact]
public void Calculate_WithContributions_ReducesCashIncreasesInvestments()

[Fact]
public void Calculate_NetWorth_CombinesAllAccounts()

[Fact]
public void Calculate_MillionaireDate_DetectedCorrectly()

[Fact]
public void Calculate_NoInvestments_BackwardsCompatible()
```

## API Endpoint Updates
Consider updating `/api/calculators/simulation` to accept investment accounts and contributions, or create a new endpoint `/api/calculators/net-worth-simulation`.

## Acceptance Criteria
- [ ] `ForwardSimulationInput` accepts investment accounts and contributions
- [ ] Daily investment growth calculated using compound interest
- [ ] Contributions deduct from cash and add to target investment
- [ ] Net worth calculated at each snapshot
- [ ] Millionaire date milestone tracked
- [ ] Result includes investment growth totals
- [ ] Backwards compatible: works without investments
- [ ] Minimum 8 new tests for investment/contribution logic
- [ ] All tests pass

## Verification
```bash
dotnet test --filter "FullyQualifiedName~ForwardSimulationEngine"
```

## Dependencies
- WI-P8-002 (expansion service for generating contribution list)

## Parallel Execution
- Can run in parallel with WI-P8-004, WI-P8-005, WI-P8-006
- Depends on WI-P8-002 for contribution expansion
