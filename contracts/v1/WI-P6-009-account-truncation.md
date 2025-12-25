# WI-P6-009: Fix Account Name Truncation

## Objective
Fix the visual bug where the account name gets cut off during account editing.

## Context
- User reported "account edit has a visual bug, the account name gets cut off".
- likely refers to the dialog input or the card display.
- We will ensure text wrapping and overflow handling is robust.

## Files to Modify
- `dashboard/src/app/pages/accounts/account-dialog.component.ts` (styles)
- `dashboard/src/app/pages/accounts/accounts.scss` (card styles)

## Implementation Notes
- In `account-dialog.component.ts`: Ensure `mat-form-field` has width `100%` and handles long text. Check if `min-width` on dialog content is causing issues on small screens.
- In `accounts.scss`: Add `word-wrap: break-word` or `text-overflow: ellipsis` to `.account-name` in the card to prevent truncation or layout breakage.

## Acceptance Criteria
- Long account names are fully visible or gracefully truncated with ellipses/tooltips.
- Dialog layout does not break with long names.

## Verification
```bash
cd dashboard
npm run build
# Manual: Test with a very long account name.
```
