# WI-P6-008: Dark Theme Implementation

## Objective
Switch the application to a dark theme as per user feedback.

## Context
- Current theme is "light-forward".
- User requested a dark theme.
- We need to update the CSS variables in `styles.scss` to use dark colors.

## Files to Modify
- `dashboard/src/styles.scss`

## Implementation Notes
- Update root CSS variables:
  - `--color-bg`: Dark background (e.g., #121212)
  - `--color-surface`: Dark surface (e.g., #1e1e1e)
  - `--color-text-main`: Light text (e.g., #e0e0e0)
  - `--color-text-muted`: Muted light text (e.g., #a0a0a0)
  - `--color-border`: Dark border (e.g., #333333)
  - `--color-divider`: Dark divider (e.g., #2c2c2c)
- Ensure contrast ratios remain accessible.
- Update background gradient to be subtle on dark mode.

## Acceptance Criteria
- App uses a dark theme.
- Text is legible against dark backgrounds.
- Components (cards, dialogs) look correct in dark mode.

## Verification
```bash
cd dashboard
npm run build
# Manual: Visual check of the app in dark mode.
```
