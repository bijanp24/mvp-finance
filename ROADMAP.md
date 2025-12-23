# ROADMAP.md

Last updated: 2025-12-22
Created by: Claude Opus 4.5 (via Claude Code CLI)

## Purpose
Structured work items optimized for parallel agent execution. Each work item is atomic and can be assigned to a single agent.

## How to Use This File
1. Pick an unclaimed work item from the current phase
2. Mark it `[IN PROGRESS]` with your agent identifier
3. Complete the work item
4. Run the verification command
5. Mark it `[DONE]` and update WORKLOG.md
6. Move to next item or hand off

## Parallel Execution Rules
- Items marked `Parallelizable: Yes` can run simultaneously
- Items with `Depends on:` must wait for dependencies
- Backend and frontend items in same feature should run sequentially
- Test items can run in parallel with each other

---

## Phase 1: Quick Wins
**Status:** Complete
**Estimated effort:** 1 session
**All items parallelizable:** Yes

### WI-P1-001: Fix Dashboard Balance Bug
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** None
- **Files:**
  - `dashboard/src/app/pages/dashboard/dashboard.ts` (lines 52-68)
- **Task:** Change `initialBalance` to `currentBalance` in computed totals
- **Details:**
  ```typescript
  // Line ~55: totalCash calculation
  // Line ~60: totalDebt calculation
  // Line ~65: totalInvestments calculation
  // Change: .initialBalance -> .currentBalance
  ```
- **Verification:**
  ```bash
  cd dashboard && npm run build
  # Manual: Check dashboard shows correct balances after transactions
  ```
- **Acceptance:** Dashboard totals reflect actual account balances

### WI-P1-002: Delete Placeholder Test
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** None
- **Files:**
  - `FinanceEngine.Tests/UnitTest1.cs` (delete entire file)
- **Task:** Remove empty placeholder test file
- **Verification:**
  ```bash
  dotnet test
  # Should still pass 88 tests (or 87 if placeholder was counted)
  ```
- **Acceptance:** No placeholder test files remain

### WI-P1-003: Sync Documentation Timestamps
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** None
- **Files:**
  - `AGENTS.md`, `PROGRESS.md`, `TODO_NEXT.md`, `WORKLOG.md`, `ROADMAP.md`
- **Task:** Ensure all "Last updated" timestamps are consistent
- **Verification:** Visual check of all files
- **Acceptance:** All timestamps show same date

---

## Phase 2: Core MVP Features
**Status:** Ready
**Estimated effort:** 2-3 sessions
**Partial parallelization:** See individual items

### WI-P2-001: Scenario Slider - Backend Integration
- **Status:** [DONE] (Combined with WI-P2-002)
- **Parallelizable:** Yes (with WI-P2-003)
- **Depends on:** Phase 1 complete
- **Files:**
  - `dashboard/src/app/pages/projections/projections.ts`
  - `dashboard/src/app/pages/projections/projections.html`
  - `dashboard/src/app/pages/projections/projections.scss`
  - `dashboard/src/app/core/services/projection.service.ts`
- **Task:** Add scenario slider for extra debt payments
- **Details:**
  - Used existing simulation endpoint (no backend changes needed)
  - Added Material slider (0-500 range, $25 steps)
  - Calculates comparison: months saved, interest saved, new payoff date
  - Extra payment distributed equally across all debt accounts
- **Verification:**
  ```bash
  cd dashboard && npm run build
  # Manual: Slider updates comparison stats in real-time
  ```
- **Acceptance:** ✅ All criteria met - slider functional with comparison display

### WI-P2-002: Scenario Slider - UI Component
- **Status:** [DONE] (Combined with WI-P2-001)
- **Parallelizable:** No
- **Depends on:** WI-P2-001
- **Files:** (See WI-P2-001)
- **Task:** (Combined with WI-P2-001)
- **Acceptance:** ✅ User can see impact of extra payments

### WI-P2-003: Crossover Milestone Calculation
- **Status:** [DONE]
- **Parallelizable:** Yes (with WI-P2-001)
- **Depends on:** Phase 1 complete
- **Files:**
  - `dashboard/src/app/core/services/projection.service.ts`
  - `dashboard/src/app/pages/projections/projections.ts`
  - `dashboard/src/app/pages/projections/projections.html`
  - `dashboard/src/app/pages/projections/projections.scss`
- **Task:** Calculate date when investment returns exceed debt interest
- **Details:**
  - Compare monthly investment growth vs monthly debt interest
  - Find crossover point in projection data
  - Add `crossoverDate` signal to service
  - Display milestone card on projections page
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Service exposes crossover date (or null if not reached), milestone card displays when available

### WI-P2-004: Crossover Milestone UI
- **Status:** [DONE] (Completed as part of WI-P2-003)
- **Parallelizable:** No
- **Depends on:** WI-P2-003
- **Files:**
  - `dashboard/src/app/pages/projections/projections.html`
  - `dashboard/src/app/pages/dashboard/dashboard.html`
- **Task:** Display crossover milestone in UI
- **Details:**
  - Add card/badge showing crossover date
  - Show on projections page (dashboard implementation deferred)
  - Handle null case (not reached yet)
- **Verification:** Visual check
- **Acceptance:** Crossover date visible on projections page when applicable

### WI-P2-005: Net Worth Curve
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** Phase 1 complete
- **Files:**
  - `dashboard/src/app/core/services/projection.service.ts`
  - `dashboard/src/app/core/models/api.models.ts`
  - `dashboard/src/app/features/charts/net-worth-chart.component.ts` (NEW)
  - `dashboard/src/app/pages/projections/projections.ts`
  - `dashboard/src/app/pages/projections/projections.html`
- **Task:** Add combined net worth visualization
- **Details:**
  - Combine investment projection - debt projection
  - Add toggle to show/hide net worth line
  - Use different color for net worth
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** Net worth curve visible on projections page

---

## Phase 3: Data Integrity
**Status:** Blocked by Phase 2
**Estimated effort:** 2 sessions

### WI-P3-001: Reconciliation - Database Migration
- **Status:** [ ] Not Started
- **Parallelizable:** No (must be first in phase)
- **Depends on:** Phase 2 complete
- **Files:**
  - `FinanceEngine.Data/Entities/FinancialEventEntity.cs`
  - New migration file
- **Task:** Add Status enum (Pending, Cleared) to events
- **Details:**
  - Add `EventStatus` enum
  - Add `Status` property to entity
  - Create EF migration
  - Default existing events to Cleared
- **Verification:**
  ```bash
  dotnet build
  dotnet run --project FinanceEngine.Api
  # Check database has new column
  ```
- **Acceptance:** Events have Status column

### WI-P3-002: Reconciliation - Backend Endpoint
- **Status:** [ ] Not Started
- **Parallelizable:** No
- **Depends on:** WI-P3-001
- **Files:**
  - `FinanceEngine.Api/Endpoints/EventEndpoints.cs`
- **Task:** Add status filter to events endpoint
- **Details:**
  - Add `?status=Pending` query parameter
  - Add PATCH endpoint to update status
- **Verification:**
  ```bash
  curl "http://localhost:5285/api/events?status=Pending"
  ```
- **Acceptance:** Can filter and update event status

### WI-P3-003: Reconciliation - Frontend UI
- **Status:** [ ] Not Started
- **Parallelizable:** No
- **Depends on:** WI-P3-002
- **Files:**
  - `dashboard/src/app/pages/transactions/transactions.ts`
  - `dashboard/src/app/pages/transactions/transactions.html`
- **Task:** Add reconciliation UI
- **Details:**
  - Add filter toggle (All/Pending/Cleared)
  - Add checkbox or button to mark cleared
  - Visual indicator for pending items
- **Verification:** Manual testing
- **Acceptance:** Users can filter and clear transactions

---

## Phase 4: Test Coverage
**Status:** Blocked by Phase 3
**Estimated effort:** 2-3 sessions
**All items parallelizable:** Yes

### WI-P4-001: AccountEndpoints Integration Tests
- **Status:** [ ] Not Started
- **Parallelizable:** Yes
- **Depends on:** Phase 3 complete
- **Files:**
  - New file: `FinanceEngine.Tests/AccountEndpointsTests.cs`
- **Task:** Add integration tests for account CRUD
- **Details:**
  - Test GET all accounts
  - Test GET single account
  - Test POST create account
  - Test PUT update account
  - Test DELETE (soft delete)
  - Test balance calculation
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~AccountEndpoints"
  ```
- **Acceptance:** All account endpoints have test coverage

### WI-P4-002: CalculatorEndpoints Integration Tests
- **Status:** [ ] Not Started
- **Parallelizable:** Yes
- **Depends on:** Phase 3 complete
- **Files:**
  - New file: `FinanceEngine.Tests/CalculatorEndpointsTests.cs`
- **Task:** Add integration tests for calculator endpoints
- **Details:**
  - Test spendable calculation
  - Test burn rate calculation
  - Test debt allocation (all 3 strategies)
  - Test investment projection
  - Test simulation
- **Verification:**
  ```bash
  dotnet test --filter "FullyQualifiedName~CalculatorEndpoints"
  ```
- **Acceptance:** All calculator endpoints have test coverage

### WI-P4-003: Frontend Dashboard Tests
- **Status:** [ ] Not Started
- **Parallelizable:** Yes
- **Depends on:** Phase 3 complete
- **Files:**
  - New file: `dashboard/src/app/pages/dashboard/dashboard.spec.ts`
- **Task:** Add unit tests for dashboard component
- **Details:**
  - Test balance calculations
  - Test loading states
  - Test empty states
  - Mock API service
- **Verification:**
  ```bash
  cd dashboard && npm test
  ```
- **Acceptance:** Dashboard component has test coverage

### WI-P4-004: Frontend Transaction Tests
- **Status:** [ ] Not Started
- **Parallelizable:** Yes
- **Depends on:** Phase 3 complete
- **Files:**
  - New file: `dashboard/src/app/pages/transactions/transactions.spec.ts`
- **Task:** Add unit tests for transaction form
- **Details:**
  - Test form validation
  - Test dynamic field visibility
  - Test account filtering logic
  - Mock API service
- **Verification:**
  ```bash
  cd dashboard && npm test
  ```
- **Acceptance:** Transaction form has test coverage

---

## Phase 5: Polish & UX
**Status:** Blocked by Phase 4
**Estimated effort:** 1-2 sessions

### WI-P5-001: Loading Skeletons
- **Status:** [ ] Not Started
- **Parallelizable:** Yes
- **Files:**
  - `dashboard/src/app/pages/dashboard/dashboard.html`
  - `dashboard/src/app/pages/transactions/transactions.html`
  - `dashboard/src/app/pages/accounts/accounts.html`
- **Task:** Replace spinners with skeleton loaders
- **Acceptance:** Better perceived performance

### WI-P5-002: Extract Balance Calculation
- **Status:** [ ] Not Started
- **Parallelizable:** Yes
- **Files:**
  - `FinanceEngine.Api/Endpoints/AccountEndpoints.cs`
  - New file: `FinanceEngine/Services/BalanceCalculator.cs` (or similar)
- **Task:** DRY up duplicated balance logic
- **Acceptance:** Single source of truth for balance calculation

### WI-P5-003: Remove Commented Migration
- **Status:** [ ] Not Started
- **Parallelizable:** Yes
- **Files:**
  - `FinanceEngine.Api/Program.cs` (lines 50-94)
- **Task:** Remove completed migration code
- **Acceptance:** Cleaner Program.cs

---

## Future Phases (Not Scheduled)

### Categories & Tags
- Add category enum/table
- Tag many-to-many relationship
- Category picker in transaction form
- Filter by category
- Budget tracking by category

### CSV Import
- File upload endpoint
- CSV parser with column mapping
- Preview/confirm UI
- Duplicate detection

### Recurring Transactions
- Recurrence pattern entity
- Auto-generation service
- Management UI

---

## Agent Assignment Log
Track which agent is working on what to prevent conflicts.

| Work Item | Agent | Started | Completed |
|-----------|-------|---------|-----------|
| WI-P1-001 | Codex (GPT-5) | 2025-12-22 | 2025-12-22 |
| WI-P1-002 | Codex (GPT-5) | 2025-12-22 | 2025-12-22 |
| WI-P1-003 | Codex (GPT-5) | 2025-12-22 | 2025-12-22 |
| WI-P2-005 | Claude Sonnet 4.5 | 2025-12-23 | 2025-12-23 |
| WI-P2-001 | Claude Sonnet 4.5 | 2025-12-23 | 2025-12-23 |
| WI-P2-002 | Claude Sonnet 4.5 | 2025-12-23 | 2025-12-23 |
| WI-P2-003 | Claude Sonnet 4.5 | 2025-12-23 | 2025-12-23 |
| WI-P2-004 | Claude Sonnet 4.5 | 2025-12-23 | 2025-12-23 |

---

## Verification Commands Reference

```bash
# Full verification suite
dotnet build && dotnet test && cd dashboard && npm run build

# Backend only
dotnet build && dotnet test

# Frontend only
cd dashboard && npm run build

# Run servers
dotnet run --project FinanceEngine.Api  # Terminal 1
cd dashboard && npm start               # Terminal 2

# API health check
curl http://localhost:5285/api/accounts
```
