# WI-P8-006: Investment Projection Integration

## Objective
Automatically populate investment projection calculations with scheduled recurring contributions, eliminating manual contribution entry and improving projection accuracy.

## Context
- `InvestmentProjectionCalculator` accepts a list of `InvestmentContribution` with dates and amounts.
- Currently, users must manually specify each contribution or the projection uses only initial balance.
- With recurring contributions, projections can auto-generate contribution lists.
- This dramatically improves the usefulness of investment growth projections.

## Files to Modify
- `dashboard/src/app/core/services/projection.service.ts`
- `dashboard/src/app/pages/projections/projections.ts`
- `dashboard/src/app/pages/projections/projections.html`

## Current State

### Existing Projection Flow
1. User enters initial balance, return rate, inflation rate
2. Projection service calls `/api/calculators/investment-projection`
3. API accepts contributions as explicit list (currently empty or manual)
4. Chart displays investment growth over time

### Problem
Without recurring contributions:
- Projections only show compound growth on initial balance
- Underestimates true investment trajectory
- User has no way to model regular contributions

## Implementation

### Projection Service Updates

```typescript
// projection.service.ts

// Signal for recurring contributions (loaded from API)
private recurringContributions = signal<RecurringContribution[]>([]);

// Load contributions for projection
loadRecurringContributions(): void {
  this.api.getRecurringContributions().subscribe(contributions => {
    this.recurringContributions.set(contributions.filter(c => c.isActive));
  });
}

// Expand contributions for projection period
getContributionsForProjection(
  startDate: Date,
  endDate: Date,
  targetAccountId?: number
): InvestmentContribution[] {
  const allContributions: InvestmentContribution[] = [];

  for (const schedule of this.recurringContributions()) {
    // Optionally filter by target account
    if (targetAccountId && schedule.targetAccountId !== targetAccountId) {
      continue;
    }

    const occurrences = this.expandSchedule(schedule, startDate, endDate);
    allContributions.push(...occurrences);
  }

  return allContributions.sort((a, b) =>
    new Date(a.date).getTime() - new Date(b.date).getTime()
  );
}

// Make projection request with auto-generated contributions
calculateInvestmentProjection(
  initialBalance: number,
  startDate: Date,
  endDate: Date,
  annualReturn: number,
  inflationRate: number,
  targetAccountId?: number
): Observable<InvestmentProjectionResult> {
  const contributions = this.getContributionsForProjection(
    startDate,
    endDate,
    targetAccountId
  );

  return this.api.calculateInvestmentProjection({
    initialBalance,
    startDate: startDate.toISOString(),
    endDate: endDate.toISOString(),
    contributions,
    nominalAnnualReturnRate: annualReturn,
    inflationRate
  });
}
```

### Projections Page Updates

```typescript
// projections.ts

// Display contribution summary
totalScheduledContributions = computed(() => {
  const contributions = this.projectionService.getContributionsForProjection(
    this.startDate(),
    this.endDate()
  );
  return contributions.reduce((sum, c) => sum + c.amount, 0);
});

contributionCount = computed(() => {
  return this.projectionService.getContributionsForProjection(
    this.startDate(),
    this.endDate()
  ).length;
});

// Show in template
// "Includes 24 scheduled contributions totaling $12,000"
```

### Template Updates

```html
<!-- projections.html -->

<!-- Add contribution summary in insights section -->
<div class="insight-card contributions">
  <span class="insight-label">Scheduled Contributions</span>
  <span class="insight-value">{{ contributionCount() }} contributions</span>
  <span class="insight-detail">{{ totalScheduledContributions() | currency }} over projection period</span>
</div>

<!-- Add toggle to include/exclude contributions -->
<mat-slide-toggle
  [checked]="includeContributions()"
  (change)="toggleContributions($event)">
  Include recurring contributions
</mat-slide-toggle>
```

## API Request Format

```typescript
interface InvestmentProjectionRequest {
  initialBalance: number;
  startDate: string;
  endDate: string;
  contributions: Array<{
    date: string;
    amount: number;
  }>;
  nominalAnnualReturnRate: number;
  inflationRate: number;
  useMonthly?: boolean;
}
```

## User Experience Improvements

### Before
- User sees flat or minimal growth curve
- No indication of contribution impact
- Manual entry required for realistic projections

### After
- Projection automatically includes scheduled contributions
- Summary shows contribution count and total
- Toggle allows comparison: with vs. without contributions
- More accurate financial planning

## Edge Cases
- No recurring contributions: behaves as before (initial balance only)
- Contributions to multiple accounts: filter by selected account if applicable
- Very long projection periods: cap at 30 years to avoid performance issues
- Inactive contributions: excluded from projections

## Acceptance Criteria
- [ ] Projection service loads recurring contributions on init
- [ ] `getContributionsForProjection` expands schedules correctly
- [ ] Investment projection chart reflects contributions
- [ ] Contribution summary displays count and total
- [ ] Toggle allows enabling/disabling contributions in projection
- [ ] Filter by target account works (if applicable)
- [ ] Comparison view: "with" vs. "without" contributions (optional)
- [ ] Performance acceptable for 30-year projections with weekly contributions

## Verification
```bash
cd dashboard && npm run build
# Manual: Create recurring contributions, verify projection chart reflects them
```

## Dependencies
- WI-P8-002 (expansion logic) or frontend reimplementation
- WI-P8-003 (API to fetch contributions)

## Parallel Execution
- Can run in parallel with WI-P8-004, WI-P8-005, WI-P8-007
- Depends on WI-P8-002 or WI-P8-003 being complete
