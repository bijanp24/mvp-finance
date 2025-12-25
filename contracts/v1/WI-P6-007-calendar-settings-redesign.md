# WI-P6-007: Calendar and Settings Redesign

## Objective
Update the calendar and settings pages to match the new visual system and accessibility standards.

## Context
- Calendar is a shared feature with its own component and styles.
- Settings uses an inline template in `settings.ts`.
- Ensure strong contrast and visible focus indicators.

## Files to Modify
- `dashboard/src/app/features/calendar/calendar.component.html`
- `dashboard/src/app/features/calendar/calendar.component.scss`
- `dashboard/src/app/pages/settings/settings.ts`

## ASCII Wireframe (Desktop)
```
Calendar
+------------------------------------------------------------------------------+
| Calendar Header               [Prev] [Month YYYY] [Next] [Today]             |
+------------------------------------------------------------------------------+
| Su  Mo  Tu  We  Th  Fr  Sa                                                   |
| 01  02  03  04  05  06  07                                                   |
| 08  09  10  11  12  13  14                                                   |
| 15  16  17  18  19  20  21                                                   |
| 22  23  24  25  26  27  28                                                   |
| 29  30  31                                                                   |
| Badges: [Payday] [Due]                                                       |
+------------------------------------------------------------------------------+

Settings
+------------------------------------------------------------------------------+
| Settings Header                                                              |
+------------------------------------------------------------------------------+
| Card: Income & Safety Buffer                                                 |
| [Pay Frequency]                                                              |
| [Next Paycheck Date]                                                         |
| [Paycheck Amount]                                                            |
| [Safety Buffer]                                                              |
|                                               [Save Settings]                |
+------------------------------------------------------------------------------+
```
## Concept Art
![Calendar and Settings Redesign Concept](./images/Calendar.png)

## Implementation Notes
- Align typography and spacing with the new tokens.
- Use badge styles for paydays and due dates on the calendar.
- Group settings fields with clear section headers.
- Keep reactive form logic and validation unchanged.

## Acceptance Criteria
- Calendar and settings visuals align with redesigned pages.
- Focus and hover states are visible and consistent.
- No regressions in settings save flow or calendar navigation.

## Verification
```bash
cd dashboard
npm run build
```
