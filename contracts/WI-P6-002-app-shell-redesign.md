# WI-P6-002: App Shell and Navigation Redesign

## Objective
Redesign the global app shell, top bar, and navigation to align with the new visual system.

## Context
- App shell lives in `app.html` and `app.scss`.
- Routing and signals must remain unchanged.
- Mobile usability and focus states need improvement.

## Files to Modify
- `dashboard/src/app/app.html`
- `dashboard/src/app/app.scss`

## ASCII Wireframe (Desktop)
```
+------------------------------------------------------------------------------+
| Top Bar: [Menu] MVP Finance                           [User/Actions]         |
+--------------------+---------------------------------------------------------+
| Nav Rail           | Content Area                                            |
| - Dashboard        | +-----------------------------------------------------+ |
| - Transactions     | | Page Header + Actions                               | |
| - Accounts         | +-----------------------------------------------------+ |
| - Calendar         | | Page Content Grid / Cards / Tables                  | |
| - Projections      | |                                                     | |
| - Settings         | +-----------------------------------------------------+ |
+--------------------+---------------------------------------------------------+
```

## Concept Art
![App Shell Redesign Concept](./images/AppLayout.png)

## Implementation Notes
- Update top bar layout, typography, and spacing to match new tokens.
- Refresh sidenav styling with a clear active state and focus ring.
- Use class bindings only (no `ngClass` or `ngStyle`).
- Keep `mat-sidenav` behavior and accessibility attributes intact.

## Acceptance Criteria
- App shell reflects the redesign without breaking navigation.
- Keyboard focus is visible on all nav items and buttons.
- Layout adapts to mobile widths without overflow.

## Verification
```bash
cd dashboard
npm run build
# Manual: verify nav toggle, focus states, and routing behavior.
```
