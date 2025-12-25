# ROADMAP-v2.md

Last updated: 2025-12-25
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
**Status:** Planning
**Estimated effort:** 3-4 sessions
**Vision:** See `GOAL_DRIVEN_BUDGETING.md` Phase A section

### WI-PA-001: Category Entity and Migration
- **Status:** [ ]
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
- **Status:** [ ]
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
- **Status:** [ ]
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
- **Status:** [ ]
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
- **Status:** [ ]
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
- **Status:** [ ]
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
- **Status:** [ ]
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
- **Status:** [ ]
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
- **Status:** [ ]
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
**Status:** Not Started
**Depends on:** Phase A complete
**Vision:** See `GOAL_DRIVEN_BUDGETING.md` Phase B section

(Work items to be defined when Phase A nears completion)

---

## Phase C: Dynamic Safe-to-Spend
**Status:** Not Started
**Depends on:** Phase B complete
**Vision:** See `GOAL_DRIVEN_BUDGETING.md` Phase C section

(Work items to be defined when Phase B nears completion)

---

## Phase D: Scenario Planning
**Status:** Not Started
**Depends on:** Phase C complete
**Vision:** See `GOAL_DRIVEN_BUDGETING.md` Phase D section

(Work items to be defined when Phase C nears completion)

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
