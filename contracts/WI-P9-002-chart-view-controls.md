# WI-P9-002: Chart View Controls & Stepped Rendering

## Objective
Add UI controls for chart granularity and enhance chart rendering to support "stepped" lines for clearer debt visualization.

## Context
- Users need to toggle between "Trend" (Monthly) and "Detail" (Daily) views.
- Debt reduction is discrete; stepped lines represent this better than diagonal lines (Option C in `charts.md`).

## Files to Modify
- `dashboard/src/app/pages/projections/projections.html`
- `dashboard/src/app/pages/projections/projections.ts`
- `dashboard/src/app/features/charts/debt-projection-chart.component.ts`

## Implementation Notes

### 1. View Toggle UI
Add a segmented control in `projections.html` (Inputs section):
```html
<mat-button-toggle-group [value]="granularity()" (change)="setGranularity($event.value)">
  <mat-button-toggle value="Daily">Daily</mat-button-toggle>
  <mat-button-toggle value="Weekly">Weekly</mat-button-toggle>
  <mat-button-toggle value="Monthly">Monthly</mat-button-toggle>
</mat-button-toggle-group>
```

### 2. Connect to Service
- Expose `granularity` signal from `ProjectionService`.
- Create `setGranularity` method.

### 3. Stepped Line for Debt
Update `debt-projection-chart.component.ts`:
- If `granularity` is 'Daily', set `step: 'end'` in ECharts series config.
- If 'Monthly', disable stepping and use `smooth: true` (already enabled) to show the trend.
- Requires passing `granularity` as an input to the chart component.

## Acceptance Criteria
- [ ] User can toggle between Daily/Weekly/Monthly views.
- [ ] Debt chart renders as a "staircase" in Daily/Weekly mode (showing discrete payments).
- [ ] Debt chart renders as a smooth curve in Monthly mode.
- [ ] Performance is acceptable when switching views.

## Verification
```bash
cd dashboard && npm run build
# Manual: Toggle views and observe chart style changes.
```
