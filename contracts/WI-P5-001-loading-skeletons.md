# WI-P5-001: Loading Skeletons

## Objective
Replace the existing "Loading..." text states with skeleton loaders on key dashboard pages to improve perceived performance.

## Context
- Current pages show plain text during data fetches.
- Angular 21 uses standalone components with signals and native control flow.
- Maintain accessibility and avoid layout shift.

## Files to Modify
- `dashboard/src/app/pages/dashboard/dashboard.html`
- `dashboard/src/app/pages/dashboard/dashboard.scss`
- `dashboard/src/app/pages/transactions/transactions.html`
- `dashboard/src/app/pages/transactions/transactions.scss`
- `dashboard/src/app/pages/accounts/accounts.html`
- `dashboard/src/app/pages/accounts/accounts.scss`

## Implementation Notes
- Use `@if (loading())` branches to render skeleton markup.
- Use class bindings (no `ngClass`) and style bindings (no `ngStyle`).
- Add `aria-busy="true"` to the container during loading and mark skeleton blocks with `aria-hidden="true"`.
- Keep skeleton shapes aligned with the final layout (cards, table rows, chart panels).

## Suggested Skeleton Structure (example)
```html
@if (loading()) {
  <div class="skeleton-grid" aria-busy="true">
    <div class="skeleton-card" aria-hidden="true">
      <div class="skeleton-line skeleton-title"></div>
      <div class="skeleton-line skeleton-value"></div>
      <div class="skeleton-line skeleton-subtext"></div>
    </div>
  </div>
}
```

## Acceptance Criteria
- Loading text blocks are replaced with skeleton placeholders on all three pages.
- Skeletons match the layout and do not cause layout shift when content loads.
- Accessibility: `aria-busy` on containers and skeletons hidden from screen readers.
- No `ngClass` or `ngStyle` usage.

## Verification
```bash
cd dashboard
npm run build
# Manual: navigate to dashboard, accounts, and transactions; confirm skeletons show briefly and content renders correctly.
```
