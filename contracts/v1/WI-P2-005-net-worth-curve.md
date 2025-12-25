# WI-P2-005: Net Worth Curve

## Objective
Add a combined "Net Worth" line to the projections page that shows investments minus debt over time.

## Context
- `ProjectionService` already calculates debt and investment projections separately
- Both have monthly snapshots with dates and values
- Need to combine them into a single net worth visualization

## Files to Modify

### 1. `dashboard/src/app/core/services/projection.service.ts`

Add new computed signal for net worth data:

```typescript
readonly netWorthChartData = computed<NetWorthChartData | null>(() => {
  const debt = this.debtProjection();
  const investment = this.investmentProjection();

  // Need at least one of them
  if (!debt?.snapshots?.length && !investment?.projections?.length) {
    return null;
  }

  // Merge dates and calculate net worth at each point
  // Net Worth = Investment Value - Debt Balance
  // ...
});
```

### 2. `dashboard/src/app/core/models/api.models.ts`

Add interface (if not exists):

```typescript
export interface NetWorthChartData {
  dates: string[];
  netWorth: number[];
  investments: number[];  // Optional: for stacked view
  debt: number[];         // Optional: for stacked view
}
```

### 3. `dashboard/src/app/features/charts/net-worth-chart.component.ts` (NEW FILE)

Create a new chart component similar to existing chart components:

```typescript
import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NetWorthChartData } from '../../core/models/api.models';

@Component({
  selector: 'app-net-worth-chart',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chart-container" [style.height]="height">
      <!-- Chart implementation using canvas or SVG -->
    </div>
  `
})
export class NetWorthChartComponent {
  @Input() data: NetWorthChartData | null = null;
  @Input() height = '300px';
}
```

Look at existing chart components for the pattern:
- `dashboard/src/app/features/charts/debt-projection-chart.component.ts`
- `dashboard/src/app/features/charts/investment-projection-chart.component.ts`

### 4. `dashboard/src/app/pages/projections/projections.ts`

Add:
```typescript
import { NetWorthChartComponent } from '../../features/charts/net-worth-chart.component';

// In imports array:
NetWorthChartComponent

// Expose signal:
readonly netWorthChartData = this.projectionService.netWorthChartData;
```

### 5. `dashboard/src/app/pages/projections/projections.html`

Add new card after Investment Projection (around line 90):

```html
<!-- Net Worth Projection -->
<mat-card class="projection-card" appearance="outlined">
  <mat-card-header>
    <mat-card-title>Net Worth Projection</mat-card-title>
  </mat-card-header>
  <mat-card-content>
    @if (loading()) {
      <div class="loading">Calculating projections...</div>
    } @else if (netWorthChartData()) {
      <app-net-worth-chart
        [data]="netWorthChartData()"
        [height]="'400px'">
      </app-net-worth-chart>

      <div class="summary">
        <h3>Summary</h3>
        <div class="summary-stats">
          <div class="stat">
            <span class="label">Current Net Worth:</span>
            <span class="value">{{ formatCurrency(netWorthChartData()!.netWorth[0]) }}</span>
          </div>
          <div class="stat">
            <span class="label">Projected Net Worth:</span>
            <span class="value">{{ formatCurrency(netWorthChartData()!.netWorth[netWorthChartData()!.netWorth.length - 1]) }}</span>
          </div>
        </div>
      </div>
    } @else {
      <div class="empty-state">Add accounts to see net worth projection</div>
    }
  </mat-card-content>
</mat-card>
```

## Implementation Details

### Merging Projection Data

The debt and investment projections may have different date ranges. Strategy:

```typescript
// Get all unique dates from both projections
const allDates = new Set<string>();
debt?.snapshots?.forEach(s => allDates.add(s.date));
investment?.projections?.forEach(p => allDates.add(p.date));

// Sort dates chronologically
const sortedDates = Array.from(allDates).sort();

// For each date, calculate net worth
const netWorth = sortedDates.map(date => {
  const debtAtDate = debt?.snapshots?.find(s => s.date === date)?.totalDebt ?? 0;
  const investmentAtDate = investment?.projections?.find(p => p.date === date)?.nominalValue ?? 0;
  return investmentAtDate - debtAtDate;
});
```

### Chart Styling

- Use a distinct color (suggest green for positive, red for negative)
- Consider showing a horizontal line at $0
- Net worth can be negative (more debt than investments)

## Edge Cases

1. **Only debt, no investments** - Net worth = -debt (negative values)
2. **Only investments, no debt** - Net worth = investments
3. **Neither** - Return null, show empty state
4. **Date mismatch** - Interpolate or use last known value

## Acceptance Criteria

- [ ] `ProjectionService` exposes `netWorthChartData` computed signal
- [ ] Net worth chart component created and displays data
- [ ] Projections page shows net worth section
- [ ] Handles negative net worth gracefully
- [ ] Shows current and projected net worth in summary
- [ ] Works with:
  - Only debt accounts
  - Only investment accounts
  - Both account types
  - Neither (empty state)

## Verification

```bash
cd dashboard && npm run build
# Manual: Check projections page shows net worth chart
# Verify values = investments - debt
```

## Existing Code References

- `projection.service.ts:29-50` - Pattern for computed chart data
- `debt-projection-chart.component.ts` - Chart component pattern
- `investment-projection-chart.component.ts` - Chart component pattern
- `projections.html:56-89` - Existing card layout pattern
