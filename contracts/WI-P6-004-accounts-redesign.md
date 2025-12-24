# WI-P6-004: Accounts Page Redesign

## Objective
Refresh the accounts page to highlight balances, improve grouping, and match the new system.

## Context
- Accounts include cash, debt, and investment types.
- Loading skeletons should remain.
- Visual treatment should align with WI-P6-001 tokens.

## Files to Modify
- `dashboard/src/app/pages/accounts/accounts.html`
- `dashboard/src/app/pages/accounts/accounts.scss`

## ASCII Wireframe (Desktop)
```
+------------------------------------------------------------------------------+
| Accounts Header                                          [Add Account]       |
+------------------------------------------------------------------------------+
| Cash Accounts                                                                |
| +--------------+  +--------------+  +--------------+                         |
| | Name         |  | Name         |  | Name         |                         |
| | $ Balance    |  | $ Balance    |  | $ Balance    |                         |
| | Details      |  | Details      |  | Details      |                         |
| +--------------+  +--------------+  +--------------+                         |
+------------------------------------------------------------------------------+
| Debt Accounts                                                                |
| +--------------+  +--------------+                                           |
| | Name         |  | Name         |                                           |
| | $ Balance    |  | $ Balance    |                                           |
| | APR / Min    |  | APR / Min    |                                           |
| +--------------+  +--------------+                                           |
+------------------------------------------------------------------------------+
| Investment Accounts                                                         |
| +--------------+  +--------------+                                           |
| | Name         |  | Name         |                                           |
| | $ Balance    |  | $ Balance    |                                           |
| | Details      |  | Details      |                                           |
| +--------------+  +--------------+                                           |
+------------------------------------------------------------------------------+
```

## Concept Art
![Accounts Redesign Concept](./images/Accounts.png)

## Implementation Notes
- Group accounts by type with clear section headers.
- Make balance the primary visual element in each card.
- Use badges for account type and status.
- Preserve existing data bindings and actions.

## Acceptance Criteria
- Accounts page is visually consistent with the redesigned dashboard.
- Empty and loading states remain accessible.
- No functional regressions in account actions.

## Verification
```bash
cd dashboard
npm run build
```
