# WI-P9-001: Projection Data Aggregation

## Objective
Implement a mechanism to aggregate projection data into Weekly or Monthly snapshots to reduce visual noise ("sawtooth" effect) in long-term charts.

## Context
- Current charts plot every single simulation event (daily/per-transaction).
- This creates jerky lines that obscure long-term trends.
- See `contracts/enhancements/charts.md` Option A.

## Files to Modify
- `dashboard/src/app/core/services/projection.service.ts`
- `dashboard/src/app/core/models/api.models.ts`

## Implementation Notes

### 1. Define Granularity Type
```typescript
export type ChartGranularity = 'Daily' | 'Weekly' | 'Monthly';
```

### 2. Aggregation Logic
Add a helper method in `ProjectionService`:
```typescript
aggregateSnapshots(snapshots: SimulationSnapshot[], granularity: ChartGranularity): SimulationSnapshot[] {
  if (granularity === 'Daily') return snapshots;

  const result: SimulationSnapshot[] = [];
  let lastSnapshot: SimulationSnapshot | null = null;

  for (const snapshot of snapshots) {
    const date = new Date(snapshot.date);
    let shouldInclude = false;

    if (granularity === 'Weekly') {
      // Include Fridays (end of trading week)
      shouldInclude = date.getDay() === 5;
    } else if (granularity === 'Monthly') {
      // Include last day of month
      const nextDay = new Date(date);
      nextDay.setDate(date.getDate() + 1);
      shouldInclude = nextDay.getDate() === 1;
    }

    if (shouldInclude) {
      result.push(snapshot);
    }
    lastSnapshot = snapshot;
  }

  // Always include the final snapshot to close the loop
  if (lastSnapshot && !result.includes(lastSnapshot)) {
    result.push(lastSnapshot);
  }

  return result;
}
```

### 3. Update Signals
- Add `granularity` signal to `ProjectionService` (default: 'Monthly' for ranges > 6mo, 'Weekly' otherwise).
- Update `debtChartData`, `investmentChartData`, `netWorthChartData` to use `aggregateSnapshots` before returning data.

## Acceptance Criteria
- [ ] Service supports 'Daily', 'Weekly', 'Monthly' granularity.
- [ ] Computed chart data signals respect the current granularity.
- [ ] "End of Period" logic correctly captures the balance after all transactions for that period.
- [ ] Final data point is always preserved.

## Verification
```bash
cd dashboard && npm run build
# Manual: Verify charts show fewer points in Monthly mode vs Daily mode.
```
