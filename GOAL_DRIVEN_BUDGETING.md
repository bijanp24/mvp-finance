# Goal-Driven Budgeting (v2 Vision)

Last updated: 2025-12-25

## Overview

This document outlines the evolution of MVP Finance from a **transaction tracker** (v1) to a **financial planner** (v2). The core shift is from reactive tracking to proactive planning.

**v1 (Current):** "Here's what happened, here's what will happen if nothing changes"
**v2 (Goal-Driven):** "Here are my goals, here's what I can spend while still hitting them"

---

## Phased Rollout

| Phase | Name | Focus | Dependency |
|-------|------|-------|------------|
| A | Budget Categories | Recurring expense planning and visualization | None |
| B | Financial Goals | Target-setting for debt payoff and investment growth | Phase A |
| C | Dynamic Safe-to-Spend | Replace static buffer with goal-based calculation | Phase B |
| D | Scenario Planning | Interactive sliders showing spending trade-offs | Phase C |

---

## Phase A: Budget Categories & Recurring Expenses

### Concept

Users define monthly budgets for spending categories. These planned expenses appear on the calendar and projections, giving visibility into where money goes before it's spent.

### Key Entities

**Category**
- Name (Groceries, Utilities, Transportation, Entertainment, etc.)
- Type: Recurring | One-Time
- Icon/Color (for visual identification)
- IsActive flag

**Budget**
- CategoryId
- Amount (monthly allocation)
- Frequency: Monthly | Bi-weekly | Weekly
- EffectiveDate (when this budget starts)
- Optional: LinkedAccountId (which account pays this)

### User Stories

1. As a user, I can create budget categories (Groceries, Utilities, etc.)
2. As a user, I can set a monthly budget amount for each category
3. As a user, I can see planned recurring expenses on the calendar
4. As a user, I can tag transactions to categories
5. As a user, I can see Budget vs. Actual for each category
6. As a user, I can see a breakdown of spending by category (pie chart, bar chart)

### Calendar Integration

The calendar currently shows:
- Paycheck dates
- Debt payment due dates
- Recurring contributions (Phase 8)

Phase A adds:
- Budgeted expense markers (e.g., "Groceries: $600" on the 1st of each month)
- Visual distinction between income (green), debt (red), contributions (blue), expenses (orange?)

### Dashboard Integration

New widgets:
- **Budget Overview Card:** Shows each category with progress bar (spent / budgeted)
- **Spending Breakdown:** Pie or donut chart by category
- **This Month Summary:** Total budgeted vs. total spent

### Projections Integration

Current projections show debt payoff and investment growth assuming current behavior.

Phase A adds:
- Planned expenses reduce available cash in projections
- More accurate "what if nothing changes" because we know recurring expenses

---

## Phase B: Financial Goals

### Concept

Users define specific financial targets with deadlines. The system calculates what's required to hit them and tracks progress.

### Goal Types

| Type | Example | Key Calculation |
|------|---------|-----------------|
| Debt-Free Date | "Pay off all debt by Dec 2027" | Required monthly payment to hit date |
| Investment Target | "$50k by 2026" | Required monthly contribution |
| Savings Goal | "$3k vacation fund by June" | Required monthly savings |
| Net Worth Milestone | "Positive net worth by Q2" | Combined debt + investment trajectory |

### Key Entities

**Goal**
- Name
- Type: DebtFree | InvestmentTarget | SavingsGoal | NetWorthMilestone
- TargetAmount (for investment/savings goals)
- TargetDate
- LinkedAccountIds (which accounts this goal applies to)
- Priority (for trade-off decisions)
- IsActive flag

**GoalProgress** (calculated, not stored)
- CurrentValue
- TargetValue
- RequiredMonthlyContribution
- ProjectedCompletionDate (at current pace)
- Status: OnTrack | AtRisk | Behind | Ahead

### User Stories

1. As a user, I can create financial goals with target dates
2. As a user, I can see progress toward each goal
3. As a user, I can see if I'm on track, at risk, or behind
4. As a user, I can see what monthly contribution is needed to hit my goal
5. As a user, I can prioritize goals (if I can't fund all, which comes first?)

### Goal Tracking UI

- **Goals Dashboard:** List of goals with progress bars and status indicators
- **Goal Detail View:** Shows trajectory chart, required vs. actual contributions
- **Milestone Celebrations:** Visual feedback when goals are achieved

---

## Phase C: Dynamic Safe-to-Spend

### Concept

Replace the static safety buffer with a calculated amount based on what's needed to stay on track for all goals.

### Current Formula (v1)

```
Safe to Spend = Cash Balance - Static Buffer
```

User sets buffer arbitrarily (e.g., $500). No connection to actual financial health.

### New Formula (v2)

```
Safe to Spend = Available Cash
              - Upcoming Bills (from budgets)
              - Required Goal Contributions
              - Minimum Emergency Buffer
```

Where:
- **Available Cash:** Current cash account balances
- **Upcoming Bills:** Sum of budgeted expenses before next income
- **Required Goal Contributions:** What must be set aside to stay on track
- **Minimum Emergency Buffer:** Small safety margin (could be user-configured or % of income)

### Status Indicators

| Status | Meaning | Visual |
|--------|---------|--------|
| Healthy | On track for all goals with spending room | Green |
| Tight | On track but little discretionary room | Yellow |
| At Risk | Current spending pace will miss goals | Orange |
| Behind | Already off track, corrective action needed | Red |

### Adjustment Suggestions

When status is At Risk or Behind, suggest:
- "Reduce grocery budget by $50/month to get back on track"
- "Delay vacation goal by 2 months"
- "Add $100/month to debt payment to hit target date"

---

## Phase D: Scenario Planning

### Concept

Interactive exploration of spending trade-offs. Users adjust sliders and immediately see impact on goals.

### Scenario Sliders

| Slider | Range | Shows Impact On |
|--------|-------|-----------------|
| Monthly discretionary | $0 - $1000 | All goals, debt-free date, investment growth |
| Grocery budget | $300 - $1000 | Available discretionary, goal timelines |
| Vacation budget | $0 - $500/mo | Savings goal date, other goal impact |
| Extra debt payment | $0 - $500 | Debt-free date, interest saved (existing) |
| Extra investment | $0 - $500 | Investment target date, compound growth |

### Comparison View

Side-by-side scenarios:
- **Scenario A (Conservative):** Minimal spending, aggressive goals
- **Scenario B (Balanced):** Moderate spending, on-track goals
- **Scenario C (Current):** Actual current behavior projected

Visual: Timeline showing when each scenario hits debt-free, investment targets, etc.

### "What If" Questions

- "What if I increase grocery budget by $100?" → See goal impact
- "What if I take a $5k vacation?" → See how long to recover
- "What if I get a $500/month raise?" → See accelerated goal completion

---

## Open Questions (To Be Decided)

### Goal Granularity
- One debt-free goal for all debt, or per-account?
- Example: "Pay off credit card first, then car loan" vs. "Debt-free by 2027"

### Budget Flexibility
- Fixed monthly amounts, or percentage of income?
- Important for variable income (freelancers, commission-based)

### Savings Goals vs. Investment Contributions
- Are they the same mechanism?
- Vacation fund = short-term cash savings
- Retirement = long-term investment account
- Different treatment in projections?

### Time Horizon for Safe-to-Spend
- This paycheck only?
- This month?
- Rolling 2-week window?

### Overspending Handling
- If user overspends on groceries:
  - Pull from discretionary?
  - Delay goals automatically?
  - Just show warning?

### Category Defaults
- Provide starter categories or start blank?
- Suggested budgets based on income (50/30/20 rule)?

---

## Relationship to v1 Features

| v1 Feature | v2 Evolution |
|------------|--------------|
| Transaction tracking | + Category tagging |
| Account balances | + Goal progress tracking |
| Debt projections | + Goal-aware projections |
| Investment projections | + Contribution requirements from goals |
| Static safety buffer | → Dynamic safe-to-spend |
| Scenario slider (debt) | + Full scenario planning |
| Calendar (paychecks, debt) | + Budgeted expenses |
| Recurring contributions | + Recurring budget expenses |

---

## Success Metrics

How we'll know v2 is working:

1. **Budget Accuracy:** Users can predict monthly spending within 10%
2. **Goal Achievement:** Users hit target dates more often than miss
3. **Reduced Anxiety:** "Safe to spend" feels trustworthy and actionable
4. **Engagement:** Users check projections and adjust budgets regularly

---

## Next Steps

1. Design Phase A work items (Budget Categories)
2. Define category and budget entities
3. Plan calendar and dashboard integration
4. Create contracts for implementation

---

## Appendix: UI Sketches (Placeholder)

Future: Add wireframes or mockups for:
- Budget management page
- Category breakdown charts
- Goal tracking dashboard
- Scenario comparison view
