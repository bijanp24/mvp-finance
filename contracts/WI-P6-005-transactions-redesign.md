# WI-P6-005: Transactions Page Redesign

## Objective
Improve the transactions page layout and form hierarchy without changing behavior.

## Context
- Page includes a quick-add form and recent transactions list.
- Status filter and reconciliation actions must remain.
- Use the new visual system and spacing scale.

## Files to Modify
- `dashboard/src/app/pages/transactions/transactions.html`
- `dashboard/src/app/pages/transactions/transactions.scss`

## ASCII Wireframe (Desktop)
```
+------------------------------------------------------------------------------+
| Transactions Header                                                          |
+-------------------------------+----------------------------------------------+
| Quick Add Form                | Recent Transactions                          |
| +---------------------------+ | [Status: All | Pending | Cleared]            |
| | Amount / Type / Date       | | +------------------------------------------+|
| | Account Selectors          | | | Date  Desc                      Amount   ||
| | Category / Notes           | | | Date  Desc                      Amount   ||
| | [Save] [Cancel]            | | | Date  Desc                      Amount   ||
| +---------------------------+ | +------------------------------------------+ |
+-------------------------------+----------------------------------------------+
```
## Concept Art
![Transactions Redesign Concept](./images/Transactions.png)

## Implementation Notes
- Organize form fields into clear sections with labels.
- Restyle status filters as segmented controls.
- Improve list row alignment and amount emphasis.
- Keep validation and signals unchanged.

## Acceptance Criteria
- Form is easier to scan and uses consistent spacing.
- Status filter is clear and keyboard accessible.
- No `ngClass` or `ngStyle` introduced.

## Verification
```bash
cd dashboard
npm run build
```
