# WI-P2-003: Crossover Milestone Calculation

## Objective
Calculate and display the date when monthly investment returns exceed monthly debt interest - the "crossover point" where your investments start working harder than your debts.

## Context
- `ProjectionService` already has `debtProjection` and `investmentProjection` signals
- Both contain monthly snapshots we can use to calculate returns/interest
- This is purely a frontend calculation using existing projection data

## Files to Modify

### 1. `dashboard/src/app/core/services/projection.service.ts`

Add a new computed signal that finds the crossover point:

```typescript
readonly crossoverDate = computed<string | null>(() => {
  const debt = this.debtProjection();
  const investment = this.investmentProjection();

  if (!debt?.snapshots?.length || !investment?.projections?.length) {
    return null;
  }

  // Calculate monthly changes and find crossover
  // ... implementation
});
```

**Logic:**
1. For each month in the projections, calculate:
   - Monthly debt interest = debt balance * (APR / 12)
   - Monthly investment return = (current value - previous value) or use 7% / 12 of balance
2. Find the first month where investment return > debt interest
3. Return that date, or null if never reached

### 2. `dashboard/src/app/pages/projections/projections.ts`

Expose the crossover signal:
```typescript
readonly crossoverDate = this.projectionService.crossoverDate;
```

### 3. `dashboard/src/app/pages/projections/projections.html`

Add a milestone card (suggested location: between header and debt projection):

```html
@if (crossoverDate()) {
  <mat-card class="milestone-card" appearance="outlined">
    <mat-card-header>
      <mat-card-title>Crossover Milestone</mat-card-title>
    </mat-card-header>
    <mat-card-content>
      <p class="milestone-date">{{ formatDate(crossoverDate()) }}</p>
      <p class="milestone-description">
        Your investment returns will exceed your debt interest costs
      </p>
    </mat-card-content>
  </mat-card>
}
```

## Implementation Details

### Calculating Monthly Interest
For debt accounts, interest is calculated as:
```typescript
monthlyInterest = totalDebtBalance * (weightedAverageAPR / 12)
```

The `debtProjection.snapshots` has `totalDebt` for each date. You'll need to estimate interest from balance changes or use a fixed average APR (suggest 18% as default credit card rate).

### Calculating Monthly Investment Return
From `investmentProjection.projections`:
```typescript
monthlyReturn = projections[i].nominalValue - projections[i-1].nominalValue
```

Or simplified:
```typescript
monthlyReturn = currentBalance * (0.07 / 12)  // 7% annual return
```

### Finding Crossover
```typescript
for (let i = 1; i < months; i++) {
  const investmentReturn = calculateMonthlyReturn(i);
  const debtInterest = calculateMonthlyInterest(i);

  if (investmentReturn > debtInterest) {
    return projections[i].date;
  }
}
return null;  // Crossover not reached in projection period
```

## Edge Cases to Handle

1. **No debt accounts** - Return null (no crossover needed)
2. **No investment accounts** - Return null (can't cross over)
3. **Already crossed over** - Return current date or first projection date
4. **Never crosses in projection period** - Return null

## Acceptance Criteria

- [ ] `ProjectionService` exposes `crossoverDate` computed signal
- [ ] Returns ISO date string when crossover is found
- [ ] Returns null when:
  - No debt accounts
  - No investment accounts
  - Crossover not reached in projection period
- [ ] Projections page displays crossover milestone when available
- [ ] Milestone card has clear, user-friendly messaging

## Verification

```bash
cd dashboard && npm run build
# Manual: Create accounts with both debt and investments
# Verify crossover date appears and is reasonable
```

## Existing Code References

- `projection.service.ts:23-24` - Existing projection signals
- `projection.service.ts:29-50` - Existing computed chart data pattern
- `api.models.ts` - SimulationResult and InvestmentProjectionResult types
