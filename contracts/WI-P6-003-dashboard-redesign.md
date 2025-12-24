# WI-P6-003: Dashboard Page Redesign

## Objective
Rebuild the dashboard page layout to improve hierarchy, clarity, and visual polish.

## Context
- Dashboard shows totals, spendable summary, and recent activity.
- Loading skeletons already exist and should remain.
- Use the new visual tokens from WI-P6-001.

## Files to Modify
- `dashboard/src/app/pages/dashboard/dashboard.html`
- `dashboard/src/app/pages/dashboard/dashboard.scss`

## ASCII Wireframe (Desktop)
```
+------------------------------------------------------------------------------+
| Dashboard Header                                                             |
+------------------------------------------------------------------------------+
| Hero Band                                                                    |
| [Safe to Spend $X,XXX]  [Next Paycheck: Date]  [Primary CTA]                 |
+------------------------------------------------------------------------------+
| Metrics Grid (3-4 cards)                                                     |
| [Total Cash]  [Total Debt]  [Total Investments]  [Net Worth/Other]           |
+------------------------------------------------------------------------------+
| Recent Activity                                                              |
| [Date] [Description]                                     [$ Amount]          |
| [Date] [Description]                                     [$ Amount]          |
| [Date] [Description]                                     [$ Amount]          |
|                                                  [View All]                  |
+------------------------------------------------------------------------------+
```

## Concept Art
![Dashboard Redesign Concept](./images/Dashboard.png)

## Implementation Notes
- Create a hero band for "Safe to Spend" with context and CTA.
- Arrange key totals in a structured metrics grid.
- Restyle recent transactions list with tighter alignment and badges.
- Keep `@if` and `@for` control flow and avoid template logic.

## Acceptance Criteria
- Dashboard hierarchy is clear at a glance on desktop and mobile.
- Loading and empty states remain functional and accessible.
- No `ngClass` or `ngStyle` introduced.

## Verification
```bash
cd dashboard
npm run build
# Manual: confirm data, loading, and empty states display correctly.
```
