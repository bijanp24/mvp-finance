# WI-P6-006: Projections Page Redesign

## Objective
Rework the projections page layout to tell a clear story from inputs to outcomes.

## Context
- Page includes sliders, cards, and multiple charts.
- Crossover milestone and net worth curve are key highlights.
- Use the new visual system for chart containers and spacing.

## Files to Modify
- `dashboard/src/app/pages/projections/projections.html`
- `dashboard/src/app/pages/projections/projections.scss`

## ASCII Wireframe (Desktop)
```
+------------------------------------------------------------------------------+
| Projections Header                                                           |
+------------------------------------------------------------------------------+
| 1) Inputs                                                                    |
| [Time Range] [Extra Payment Slider] [Strategy/Controls]                      |
+------------------------------------------------------------------------------+
| 2) Key Insights                                                              |
| [Debt Free Date] [Interest Saved] [Crossover] [Net Worth]                    |
+------------------------------------------------------------------------------+
| 3) Charts                                                                    |
| +--------------------------------------------------------------------------+ |
| | Debt Projection Chart                                                     ||
| +--------------------------------------------------------------------------+ |
| +--------------------------------------------------------------------------+ |
| | Investment Projection Chart                                               ||
| +--------------------------------------------------------------------------+ |
| +--------------------------------------------------------------------------+ |
| | Net Worth Curve                                                           ||
| +--------------------------------------------------------------------------+ |
+------------------------------------------------------------------------------+
```
## Concept Art
![Projections Redesign Concept](./images/Projections.png)

## Implementation Notes
- Place inputs and scenarios near the top, results directly below.
- Standardize chart card padding and titles.
- Ensure milestone and net worth cards use the new accent styles.
- Keep existing chart components and data bindings intact.

## Acceptance Criteria
- Projections page flows logically on desktop and mobile.
- Charts remain readable and aligned within the new card system.
- No functional changes to projection calculations.

## Verification
```bash
cd dashboard
npm run build
```
