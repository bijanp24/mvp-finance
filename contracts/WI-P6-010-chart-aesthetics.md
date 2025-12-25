# WI-P6-010: Enhance Chart Aesthetics

## Objective
Improve the visual appeal of charts, specifically addressing the "discrete moments" issue.

## Context
- User feels charts look unappealing due to discrete transactions (jagged lines).
- We will use smoothing and potentially step-line interpolation where appropriate.

## Files to Modify
- `dashboard/src/app/features/charts/debt-projection-chart.component.ts`
- `dashboard/src/app/features/charts/investment-projection-chart.component.ts`
- `dashboard/src/app/features/charts/net-worth-chart.component.ts`

## Implementation Notes
- Enable `smooth: true` (monotone spline) for all line series to create clearer trends.
- Consider adding a `symbol: 'none'` (or small symbol) to reduce visual noise from many data points.
- Ensure area fills use gradients that look good in dark mode (update colors if needed).

## Acceptance Criteria
- Charts look smoother and less jagged.
- Visual style aligns with the new dark theme.

## Verification
```bash
cd dashboard
npm run build
# Manual: Check charts with transaction data.
```
