# MVP Finance Dashboard - Frontend Redesign Plan

## Design Goals
- Create a clear visual hierarchy for money insights and actions.
- Use expressive typography and a warm, editorial feel.
- Reduce visual noise with consistent spacing and card treatments.
- Keep all interactions accessible and keyboard friendly.

## Visual System

### Typography
- Display: "Fraunces" for hero numbers and page titles.
- Body: "Manrope" for UI text and labels.
- Use 1.1 to 1.3 line-height for headings, 1.5 for body.

### Color Direction
- Background: warm off-white with a subtle gradient.
- Text: deep charcoal for primary, muted slate for secondary.
- Accent: sage and amber for emphasis, no purple.
- Status: green for positive, red for negative, amber for pending.

### Tokens and Scale
- Radius: 12px for cards, 8px for inputs and chips.
- Spacing scale: 4, 8, 12, 16, 24, 32, 48.
- Shadow: soft ambient, avoid heavy drop shadows.

### Motion
- Page-load stagger for card groups (150 to 250ms).
- Subtle hover lift on cards (2 to 4px).
- No excessive micro-animations.

## Layout System
- App shell: top bar plus left navigation rail.
- Content width: max 1200px with 24px gutters.
- Cards aligned to a 12-column grid on desktop, 1 column on mobile.
- Use section headers to anchor each page.

## Component Language
- Buttons: primary filled for main actions, tonal for secondary, text for tertiary.
- Cards: soft border, subtle background tint, consistent padding.
- Badges: rounded pills with clear status colors.
- Forms: grouped fields with section titles and helper text.

## Page Guides

### Dashboard
- Hero band with "Safe to Spend" and next paycheck context.
- Metrics grid for cash, debt, and investments.
- Recent activity list with compact rows and aligned amounts.
- Empty states use one CTA with a calm tone.

### Accounts
- Group cards by account type with section headers.
- Show balance as the largest text element in each card.
- Add a mini detail row for APR, minimum payment, or goal.

### Transactions
- Split layout: left form, right recent list on desktop.
- Group form fields by type, with inline helper text.
- Status filter as segmented buttons with clear active state.

### Projections
- Narrative flow: inputs first, then insights, then charts.
- Use consistent chart card height and padding.
- Highlight crossover milestone and debt-free date as featured cards.

### Calendar
- Month header with clear navigation and strong date hierarchy.
- Payday and due dates use distinct badges, not just color.

### Settings
- Single centered card with sections and clear save state.
- Keep form fields in logical groups with short helper text.

## Accessibility Notes
- Minimum contrast of 4.5:1 for text on backgrounds.
- Visible focus rings for all interactive elements.
- Maintain aria labels and keyboard navigation on all controls.
