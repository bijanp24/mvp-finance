# ROADMAP-v2.md

Last updated: 2025-12-29
Version: 2.0 (Goal-Driven Budgeting)

## Purpose
Structured work items for v2 features. Optimized for parallel agent execution.

See `GOAL_DRIVEN_BUDGETING.md` for the full v2 vision.

## How to Use This File
1. Pick an unclaimed work item from the current phase
2. Mark it `[IN PROGRESS]` with your agent identifier
3. Complete the work item
4. Run the verification command
5. Mark it `[DONE]` and update WORKLOG-v2.md
6. Move to next item or hand off

## Parallel Execution Rules
- Items marked `Parallelizable: Yes` can run simultaneously
- Items with `Depends on:` must wait for dependencies
- Backend and frontend items in same feature should run sequentially
- Test items can run in parallel with each other

---

## v1 Summary (Phases 1-9 Complete)
All v1 work is complete and archived in `docs/v1-archive/`.
- 156 tests (117 backend + 39 frontend)
- Full CRUD for accounts, transactions, events
- Projections with debt/investment charts
- Recurring contributions
- Dark theme redesign

---

## Phase A: Budget Categories & Recurring Expenses
**Status:** COMPLETE
**Completed:** 2025-12-29
**Vision:** See `GOAL_DRIVEN_BUDGETING.md` Phase A section

### WI-PA-001: Category Entity and Migration
- **Status:** [DONE]
- **Parallelizable:** No (must be first)
- **Depends on:** None
- **Files:**
  - `FinanceEngine.Data/Entities/CategoryEntity.cs` (NEW)
  - `FinanceEngine.Data/FinanceDbContext.cs`
  - New EF migration
- **Task:** Create database entity for expense categories
- **Details:**
  - Name, Type (Recurring/OneTime), Icon, Color, IsActive
  - Seed default categories (Groceries, Utilities, Transportation, etc.)
- **Verification:**
  ```bash
  dotnet build && dotnet test
  ```
- **Acceptance:** Entity created, migration applied, defaults seeded

### WI-PA-002: Budget Entity and Migration
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-PA-001
- **Files:**
  - `FinanceEngine.Data/Entities/BudgetEntity.cs` (NEW)
  - `FinanceEngine.Data/FinanceDbContext.cs`
  - New EF migration
- **Task:** Create database entity for monthly budgets
- **Details:**
  - CategoryId, Amount, Frequency, EffectiveDate, LinkedAccountId (optional)
- **Verification:**
  ```bash
  dotnet build && dotnet test
  ```
- **Acceptance:** Entity created with proper relationships

### WI-PA-003: Category API Endpoints
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PA-001)
- **Depends on:** WI-PA-001
- **Files:**
  - `FinanceEngine.Api/Endpoints/CategoryEndpoints.cs` (NEW)
  - `FinanceEngine.Tests/Endpoints/CategoryEndpointsTests.cs` (NEW)
- **Task:** CRUD endpoints for categories
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~CategoryEndpoints"
  ```
- **Acceptance:** Full CRUD with tests

### WI-PA-004: Budget API Endpoints
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PA-002)
- **Depends on:** WI-PA-002
- **Files:**
  - `FinanceEngine.Api/Endpoints/BudgetEndpoints.cs` (NEW)
  - `FinanceEngine.Tests/Endpoints/BudgetEndpointsTests.cs` (NEW)
- **Task:** CRUD endpoints for budgets
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~BudgetEndpoints"
  ```
- **Acceptance:** Full CRUD with tests

### WI-PA-005: Transaction Category Tagging
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PA-001)
- **Depends on:** WI-PA-001
- **Files:**
  - `FinanceEngine.Data/Entities/FinancialEventEntity.cs`
  - `FinanceEngine.Api/Endpoints/EventEndpoints.cs`
  - New migration (add CategoryId to Events)
- **Task:** Add optional CategoryId to transactions
- **Verification:**
  ```bash
  dotnet build && dotnet test
  ```
- **Acceptance:** Events can be tagged with categories

### WI-PA-006: Budget Management UI
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-PA-003, WI-PA-004
- **Files:**
  - `dashboard/src/app/pages/budgets/` (NEW)
  - `dashboard/src/app/core/models/api.models.ts`
  - `dashboard/src/app/core/services/api.service.ts`
- **Task:** Create budget management page
- **Details:**
  - List categories with budget amounts
  - Add/edit budget dialog
  - Progress bars (spent vs budgeted)
- **Verification:**
  ```bash
  cd dashboard && npm run build && npm test
  ```
- **Acceptance:** Users can manage budgets

### WI-PA-007: Transaction Category Picker UI
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PA-005)
- **Depends on:** WI-PA-005, WI-PA-006
- **Files:**
  - `dashboard/src/app/pages/transactions/transactions.ts`
  - `dashboard/src/app/pages/transactions/transactions.html`
- **Task:** Add category dropdown to transaction form
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can tag transactions with categories

### WI-PA-008: Budget vs Actual Dashboard Widget
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PA-006)
- **Depends on:** WI-PA-006
- **Files:**
  - `dashboard/src/app/pages/dashboard/dashboard.ts`
  - `dashboard/src/app/pages/dashboard/dashboard.html`
- **Task:** Add spending breakdown widget to dashboard
- **Details:**
  - Category breakdown (pie/donut chart)
  - Budget progress bars
  - This month summary
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Dashboard shows spending by category

### WI-PA-009: Calendar Budget Markers
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PA-004)
- **Depends on:** WI-PA-004
- **Files:**
  - `dashboard/src/app/features/calendar/calendar.component.ts`
  - `dashboard/src/app/features/calendar/calendar.component.html`
  - `dashboard/src/app/core/services/calendar.service.ts`
- **Task:** Show budgeted expenses on calendar
- **Details:**
  - Display recurring budget markers
  - Visual distinction from income/debt/contributions
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Calendar shows planned expenses

---

## Phase B: Financial Goals
**Status:** COMPLETE
**Completed:** 2025-12-29
**Vision:** See `contracts/v2/GOAL_DRIVEN_BUDGETING.md` Phase B section

### WI-PB-001: Goal Entity and Migration
- **Status:** [DONE]
- **Parallelizable:** No (must be first)
- **Depends on:** Phase A
- **Files:**
  - `FinanceEngine.Data/Entities/GoalEntity.cs` (NEW)
  - `FinanceEngine.Data/FinanceDbContext.cs`
  - New EF migration
- **Task:** Create database entity for financial goals
- **Details:**
  - Name, Type (DebtFree/InvestmentTarget/SavingsGoal/NetWorthMilestone)
  - TargetAmount (nullable - not needed for DebtFree)
  - TargetDate
  - LinkedAccountIds (JSON array or junction table)
  - Priority (int, for ordering)
  - IsActive flag
- **Verification:**
  ```bash
  dotnet build && dotnet test
  ```
- **Acceptance:** Entity created, migration applied

### WI-PB-002: Goal Progress Calculator Service
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PB-001)
- **Depends on:** WI-PB-001
- **Files:**
  - `FinanceEngine/Services/GoalProgressCalculator.cs` (NEW)
  - `FinanceEngine.Tests/Services/GoalProgressCalculatorTests.cs` (NEW)
- **Task:** Calculate goal progress and projections
- **Details:**
  - CurrentValue: sum of linked account balances
  - TargetValue: from goal definition
  - RequiredMonthlyContribution: calculate based on remaining time
  - ProjectedCompletionDate: at current pace
  - Status: OnTrack/AtRisk/Behind/Ahead based on trajectory
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~GoalProgressCalculator"
  ```
- **Acceptance:** Accurate calculations for all goal types

### WI-PB-003: Goal API Endpoints
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PB-001)
- **Depends on:** WI-PB-001, WI-PB-002
- **Files:**
  - `FinanceEngine.Api/Endpoints/GoalEndpoints.cs` (NEW)
  - `FinanceEngine.Tests/Endpoints/GoalEndpointsTests.cs` (NEW)
- **Task:** CRUD endpoints for goals with progress
- **Details:**
  - GET /api/goals - list all with progress
  - GET /api/goals/{id} - single goal with detailed progress
  - POST /api/goals - create
  - PUT /api/goals/{id} - update
  - DELETE /api/goals/{id} - delete
  - GET /api/goals/{id}/progress - detailed progress data
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~GoalEndpoints"
  ```
- **Acceptance:** Full CRUD with progress calculation

### WI-PB-004: Goals Page UI
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-PB-003
- **Files:**
  - `dashboard/src/app/pages/goals/goals.ts` (NEW)
  - `dashboard/src/app/pages/goals/goals.html` (NEW)
  - `dashboard/src/app/pages/goals/goals.scss` (NEW)
  - `dashboard/src/app/core/models/api.models.ts`
  - `dashboard/src/app/core/services/api.service.ts`
  - `dashboard/src/app/app.routes.ts`
- **Task:** Create goals management page
- **Details:**
  - List goals with progress bars
  - Status indicators (OnTrack=green, AtRisk=yellow, Behind=red, Ahead=blue)
  - Required monthly contribution display
  - Projected completion date
  - Reorder by priority (drag or buttons)
- **Verification:**
  ```bash
  cd dashboard && npm run build && npm test
  ```
- **Acceptance:** Users can view all goals with progress

### WI-PB-005: Goal Create/Edit Dialog
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PB-004)
- **Depends on:** WI-PB-004
- **Files:**
  - `dashboard/src/app/pages/goals/goal-dialog.component.ts` (NEW)
  - `dashboard/src/app/pages/goals/goals.ts`
- **Task:** Dialog for creating and editing goals
- **Details:**
  - Goal type selector with dynamic fields
  - Account multi-select for linking
  - Target date picker
  - Target amount (when applicable)
  - Priority setting
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can create/edit all goal types

### WI-PB-006: Goal Detail View
- **Status:** [DEFERRED] (progress visible in Goals page)
- **Parallelizable:** Yes (after WI-PB-004)
- **Depends on:** WI-PB-004
- **Files:**
  - `dashboard/src/app/pages/goals/goal-detail.component.ts` (NEW)
  - `dashboard/src/app/pages/goals/goals.ts`
- **Task:** Detailed goal view with trajectory
- **Details:**
  - Progress chart (current vs target over time)
  - Contribution history
  - Projected vs required trajectory
  - Milestone markers
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Users can see detailed goal progress

### WI-PB-007: Dashboard Goal Widget
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PB-003)
- **Depends on:** WI-PB-003
- **Files:**
  - `dashboard/src/app/pages/dashboard/dashboard.ts`
  - `dashboard/src/app/pages/dashboard/dashboard.html`
- **Task:** Add goal summary widget to dashboard
- **Details:**
  - Top 3 goals by priority
  - Mini progress bars
  - Status indicators
  - Link to full goals page
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Dashboard shows goal overview

### WI-PB-008: Projections Goal Integration
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-PB-003)
- **Depends on:** WI-PB-003
- **Files:**
  - `dashboard/src/app/pages/projections/projections.ts`
  - `dashboard/src/app/pages/projections/projections.html`
  - `dashboard/src/app/core/services/projection.service.ts`
- **Task:** Show goal milestones on projection charts
- **Details:**
  - Vertical lines at goal target dates
  - Annotations showing goal name
  - Color by goal status
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Projection charts show goal targets

---

## Phase C: Dynamic Safe-to-Spend
**Status:** COMPLETE
**Completed:** 2025-12-30 (discovered already implemented)
**Depends on:** Phase B complete
**Vision:** See `GOAL_DRIVEN_BUDGETING.md` Phase C section

### Design Decisions
- **Time Horizon:** Until next paycheck (configurable: NextPaycheck/CurrentMonth/RollingTwoWeeks)
- **Overspending:** Warning only (user decides how to handle)
- **Suggestions:** Informational only (no auto-apply)
- **Buffer:** User-configurable amount (SafetyBuffer in settings)

### Implementation Summary
All Phase C functionality was already implemented:

**Backend:**
- `FinanceEngine.Data/Entities/UserSettingsEntity.cs` - PayFrequency, PaycheckAmount, SafetyBuffer, NextPaycheckDate, PreferredTimeHorizon
- `FinanceEngine.Api/Endpoints/SettingsEndpoints.cs` - GET/PUT /api/settings
- `FinanceEngine/Calculators/SafeToSpendCalculator.cs` - Core calculation engine
- `FinanceEngine/Calculators/BudgetAnalysisCalculator.cs` - Budget overspending analysis
- `FinanceEngine/Calculators/AdjustmentSuggestionCalculator.cs` - Suggestion generation
- `FinanceEngine.Api/Endpoints/SafeToSpendEndpoints.cs` - GET /, /analysis, /suggestions, /full

**Frontend:**
- `dashboard/src/app/pages/settings/settings.ts` - Full settings form with all fields
- `dashboard/src/app/pages/dashboard/dashboard.ts` - Safe-to-Spend hero widget, suggestions panel
- `dashboard/src/app/pages/dashboard/dashboard.html` - Full UI with breakdown stats

**Tests:** 58 tests passing for SafeToSpend, BudgetAnalysis, AdjustmentSuggestion, Settings

---

## Phase D: Scenario Planning
**Status:** COMPLETE
**Completed:** 2025-12-30 (discovered already implemented)
**Depends on:** Phase C complete
**Vision:** See `GOAL_DRIVEN_BUDGETING.md` Phase D section

### Implementation Summary
All Phase D functionality was already implemented:

**Backend:**
- `FinanceEngine/Calculators/ScenarioCalculator.cs` - Scenario calculation engine
- `FinanceEngine.Tests/Calculators/ScenarioCalculatorTests.cs` - Tests
- `FinanceEngine.Api/Endpoints/ScenarioEndpoints.cs` - API endpoints

**Frontend:**
- `dashboard/src/app/pages/scenarios/scenarios.ts` - Scenario planning page
- `dashboard/src/app/pages/scenarios/scenarios.html` - UI with sliders
- `dashboard/src/app/pages/scenarios/scenarios.scss` - Styling

**Features:**
- Monthly discretionary spending slider
- Extra debt payment slider
- Extra investment contribution slider
- Debounced real-time recalculation
- Timeline chart visualization
- Net worth projections

---

## Agent Assignment Log

| Work Item | Agent | Started | Completed |
|-----------|-------|---------|-----------|
| (none yet) | | | |

---

## Verification Commands Reference

```bash
# Full verification suite
dotnet build && dotnet test && cd dashboard && npm run build

# Backend only
dotnet build && dotnet test

# Frontend only
cd dashboard && npm run build && npm test

# Run servers
dotnet run --project FinanceEngine.Api  # Terminal 1
cd dashboard && npm start               # Terminal 2
```
