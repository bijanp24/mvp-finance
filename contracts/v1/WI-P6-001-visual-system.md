# WI-P6-001: Visual System and Theme Tokens

## Objective
Establish a new visual system for the dashboard with updated typography, colors, spacing, and global layout tokens.

## Context
- Current theme uses a dark Material palette and default Roboto.
- Redesign should feel warmer, editorial, and more intentional.
- Follow Angular and accessibility conventions from `AGENTS.md`.

## Files to Modify
- `dashboard/src/styles.scss`
- `dashboard/src/app/app.scss`

## Implementation Notes
- Replace the dark theme with a light-forward theme and a new palette (no purple).
- Import and apply expressive fonts (e.g., Fraunces for headings, Manrope for body).
- Add CSS variables for colors, spacing, radius, and shadows.
- Introduce a subtle background gradient or pattern on the app body.
- Keep all changes ASCII and avoid inline base64 images.

## Acceptance Criteria
- Global typography and color tokens are defined and used by default.
- Base layout spacing and card styling are consistent across the app shell.
- No build errors, and no regression in existing component behavior.

## Verification
```bash
cd dashboard
npm run build
```
