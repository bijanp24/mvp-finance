# ROADMAP.md

Last updated: 2025-12-24
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
**Status:** Complete
**Estimated effort:** 2 sessions

### WI-P3-001: Reconciliation - Database Migration
- **Status:** [DONE]
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
- **Status:** [DONE]
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
- **Status:** [DONE]
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
**Status:** Complete
**Estimated effort:** 2-3 sessions
**All items parallelizable:** Yes

### WI-P4-000: Jest Setup for Angular Testing
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** Phase 3 complete
- **Files:**
  - `dashboard/jest.config.js` (NEW)
  - `dashboard/setup-jest.ts` (NEW)
  - `dashboard/tsconfig.spec.json` (NEW)
  - `dashboard/package.json` (updated scripts)
  - `dashboard/src/app/app.spec.ts` (NEW - smoke test)
- **Task:** Configure Jest testing framework for Angular 21
- **Details:**
  - Installed jest, @types/jest, jest-preset-angular, @testing-library/angular, @testing-library/jest-dom, jest-environment-jsdom
  - Created Jest configuration with preset and setup files
  - Added test scripts: test, test:watch, test:coverage
  - Created smoke test to verify setup
- **Verification:**
  ```bash
  cd dashboard && npm test
  # Should pass 2 tests (Jest Setup suite)
  ```
- **Acceptance:** ✅ All criteria met - Jest runs tests, @testing-library/jest-dom matchers work, TypeScript compilation works

### WI-P4-001: AccountEndpoints Integration Tests
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** Phase 3 complete
- **Files:**
  - `FinanceEngine.Tests/AccountEndpointsTests.cs`
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
- **Acceptance:** ✅ All account endpoints have test coverage

### WI-P4-002: CalculatorEndpoints Integration Tests
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** Phase 3 complete
- **Files:**
  - `FinanceEngine.Tests/CalculatorEndpointsTests.cs`
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
- **Acceptance:** ✅ All calculator endpoints have test coverage

### WI-P4-003: Frontend Dashboard Tests
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** WI-P4-000 (Jest setup)
- **Files:**
  - `dashboard/src/app/pages/dashboard/dashboard.spec.ts`
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
- **Acceptance:** ✅ Dashboard component has test coverage (14 tests)

### WI-P4-004: Frontend Transaction Tests
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** WI-P4-000 (Jest setup)
- **Files:**
  - `dashboard/src/app/pages/transactions/transactions.spec.ts`
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
- **Acceptance:** ✅ Transaction form has test coverage (23 tests)

---

## Phase 5: Polish & UX
**Status:** Complete
**Estimated effort:** 1-2 sessions

### WI-P5-001: Loading Skeletons
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Files:**
  - `dashboard/src/app/pages/dashboard/dashboard.html`
  - `dashboard/src/app/pages/dashboard/dashboard.scss`
  - `dashboard/src/app/pages/transactions/transactions.html`
  - `dashboard/src/app/pages/transactions/transactions.scss`
  - `dashboard/src/app/pages/accounts/accounts.html`
  - `dashboard/src/app/pages/accounts/accounts.scss`
- **Task:** Replace spinners with skeleton loaders
- **Acceptance:** ✅ Loading skeletons implemented on dashboard, accounts, and transactions pages

### WI-P5-002: Extract Balance Calculation
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Files:**
  - `FinanceEngine/Services/BalanceCalculator.cs` (NEW)
  - `FinanceEngine.Api/Services/AccountService.cs` (updated)
- **Task:** DRY up duplicated balance logic
- **Acceptance:** ✅ Single source of truth for balance calculation in shared service

### WI-P5-003: Remove Commented Migration
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Files:**
  - `FinanceEngine.Api/Program.cs`
- **Task:** Remove completed migration code
- **Acceptance:** ✅ Cleaner Program.cs (46 lines removed)

---

## Phase 6: Frontend Redesign
**Status:** Complete
**Estimated effort:** 3-4 sessions
**Partial parallelization:** See individual items

### WI-P6-001: Visual System and Theme Tokens
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** None
- **Files:**
  - `dashboard/src/styles.scss`
  - `dashboard/src/app/app.scss`
- **Task:** Define a new visual system, typography, and base layout tokens for the redesign.
- **Details:**
  - Replace the current dark theme with a new light-forward theme and CSS variables.
  - Introduce expressive fonts and spacing scale.
  - Establish background treatments and card styles used across pages.
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** ✅ Global theme and tokens are in place; font inlining disabled in angular.json.

### WI-P6-002: App Shell and Navigation Redesign
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-P6-001
- **Files:**
  - `dashboard/src/app/app.html`
  - `dashboard/src/app/app.scss`
- **Task:** Redesign the app shell to match the new visual system.
- **Details:**
  - Update top bar and nav to new layout and spacing.
  - Improve mobile nav behavior and focus states.
  - Keep routing and signals unchanged.
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** ✅ App shell redesigned with new logo and navigation styling.

### WI-P6-003: Dashboard Page Redesign
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P6-001)
- **Depends on:** WI-P6-001
- **Files:**
  - `dashboard/src/app/pages/dashboard/dashboard.html`
  - `dashboard/src/app/pages/dashboard/dashboard.scss`
- **Task:** Redesign dashboard layout, cards, and hierarchy.
- **Details:**
  - Introduced hero band and structured metrics grid.
  - Restyled recent transactions with status badges.
  - Added net worth calculation.
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** ✅ Dashboard hierarchy is clear; metrics grid and hero band implemented.

### WI-P6-004: Accounts Page Redesign
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P6-001)
- **Depends on:** WI-P6-001
- **Files:**
  - `dashboard/src/app/pages/accounts/accounts.html`
  - `dashboard/src/app/pages/accounts/accounts.scss`
- **Task:** Redesign accounts layout and card styling.
- **Details:**
  - Grouped accounts by type with section headers.
  - Implemented card-based grid layout emphasizing balance.
  - Added action menus and redesigned metadata display.
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** ✅ Accounts page matches new visual system; cards used instead of table.

### WI-P6-005: Transactions Page Redesign
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P6-001)
- **Depends on:** WI-P6-001
- **Files:**
  - `dashboard/src/app/pages/transactions/transactions.html`
  - `dashboard/src/app/pages/transactions/transactions.scss`
- **Task:** Redesign transaction form and list layout.
- **Details:**
  - Implemented two-column layout with sidebar form.
  - Refined form field grouping and segmented status filters.
  - Redesigned transaction rows with icon categorization and status chips.
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** ✅ Transactions UI matches new design system; sidebar form implemented.

### WI-P6-006: Projections Page Redesign
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P6-001)
- **Depends on:** WI-P6-001
- **Files:**
  - `dashboard/src/app/pages/projections/projections.html`
  - `dashboard/src/app/pages/projections/projections.scss`
- **Task:** Redesign projections layout and chart containers.
- **Details:**
  - Implemented 3-step narrative flow (Inputs -> Insights -> Charts).
  - Redesigned insights grid with highlight cards for key milestones.
  - Standardized chart card styling and input controls.
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** ✅ Projections page follows a logical story flow; charts are standardized in cards.

### WI-P6-007: Calendar and Settings Redesign
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P6-001)
- **Depends on:** WI-P6-001
- **Files:**
  - `dashboard/src/app/features/calendar/calendar.component.html`
  - `dashboard/src/app/features/calendar/calendar.component.scss`
  - `dashboard/src/app/pages/settings/settings.ts`
- **Task:** Refresh calendar and settings pages for visual consistency and a11y polish.
- **Details:**
  - Aligned calendar grid and event badges with the new visual system.
  - Redesigned settings form with a grid layout and improved card hierarchy.
  - Improved responsive behavior for calendar events.
- **Verification:**
  ```bash
  cd dashboard && npm run build
  ```
- **Acceptance:** ✅ Calendar and settings match the redesign and pass basic a11y checks.

---

## Phase 7: Visual Polish (Feedback Response)
**Status:** Complete
**Estimated effort:** 1 session
**Parallelizable:** Yes

### WI-P6-008: Dark Theme Implementation
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** Phase 6
- **Files:**
  - `dashboard/src/styles.scss`
- **Task:** Switch to a dark theme as per user feedback.
- **Details:**
  - Updated root CSS variables for dark background/surface and light text.
  - Switched to `mat.m2-define-dark-theme`.

### WI-P6-009: Fix Account Name Truncation
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** Phase 6
- **Files:**
  - `dashboard/src/app/pages/accounts/account-dialog.component.ts`
  - `dashboard/src/app/pages/accounts/accounts.scss`
- **Task:** Fix visual bug where account name is cut off.
- **Details:**
  - Added `text-overflow: ellipsis` to account cards.
  - Made account dialog responsive.

### WI-P6-010: Enhance Chart Aesthetics
- **Status:** [DONE]
- **Parallelizable:** Yes
- **Depends on:** Phase 6
- **Files:**
  - `dashboard/src/app/features/charts/*.ts`
- **Task:** Improve chart visuals for discrete transactions.
- **Details:**
  - Enabled smoothing (`smooth: true`) and hidden symbols for a cleaner look.
  - Updated colors for dark theme.

---

## Phase 8: Recurring Investments & Contributions
**Status:** Complete
**Estimated effort:** 3-4 sessions
**Partial parallelization:** See individual items

### WI-P8-001: RecurringContributionEntity - Database Entity
- **Status:** [DONE]
- **Parallelizable:** No (must be first in phase)
- **Depends on:** None
- **Contract:** `contracts/WI-P8-001-recurring-contribution-entity.md`
- **Files:**
  - `FinanceEngine.Data/Entities/RecurringContributionEntity.cs` (NEW)
  - `FinanceEngine.Data/FinanceDbContext.cs`
  - New EF migration
- **Task:** Create database entity for recurring contributions (investments, savings)
- **Acceptance:** Entity created with frequency, amount, source/target accounts, next date anchor

### WI-P8-002: RecurringEventExpansionService
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-P8-001
- **Contract:** `contracts/WI-P8-002-recurring-expansion-service.md`
- **Files:**
  - `FinanceEngine/Services/RecurringEventExpansionService.cs` (NEW)
  - `FinanceEngine.Tests/Services/RecurringEventExpansionServiceTests.cs` (NEW)
- **Task:** Create service to expand recurring schedules into event lists for projections
- **Acceptance:** Service generates contribution events from schedule over date range

### WI-P8-003: Recurring Contributions API Endpoints
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-P8-001
- **Contract:** `contracts/WI-P8-003-recurring-contributions-api.md`
- **Files:**
  - `FinanceEngine.Api/Endpoints/RecurringContributionEndpoints.cs` (NEW)
  - `FinanceEngine.Tests/Endpoints/RecurringContributionEndpointsTests.cs` (NEW)
- **Task:** CRUD endpoints for managing recurring contribution schedules
- **Acceptance:** Full CRUD with validation, active/inactive toggle

### WI-P8-004: Settings UI for Recurring Contributions
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-P8-003
- **Contract:** `contracts/WI-P8-004-recurring-contributions-ui.md`
- **Files:**
  - `dashboard/src/app/pages/settings/settings.ts`
  - `dashboard/src/app/pages/settings/settings.html`
  - `dashboard/src/app/pages/settings/settings.scss`
  - `dashboard/src/app/core/models/api.models.ts`
- **Task:** Add recurring contributions management section to Settings page
- **Acceptance:** Users can add/edit/delete recurring contribution schedules

### WI-P8-005: Calendar Integration for Contributions
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P8-002)
- **Depends on:** WI-P8-002, WI-P8-003
- **Contract:** `contracts/WI-P8-005-calendar-integration.md`
- **Files:**
  - `dashboard/src/app/core/services/calendar.service.ts`
  - `dashboard/src/app/features/calendar/calendar.component.ts`
  - `dashboard/src/app/features/calendar/calendar.component.html`
- **Task:** Display scheduled contribution dates on calendar alongside paychecks
- **Acceptance:** Calendar shows contribution markers with amount and target account

### WI-P8-006: Investment Projection Integration
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P8-002)
- **Depends on:** WI-P8-002
- **Contract:** `contracts/WI-P8-006-projection-integration.md`
- **Files:**
  - `dashboard/src/app/core/services/projection.service.ts`
  - `dashboard/src/app/pages/projections/projections.ts`
- **Task:** Auto-populate investment projections from recurring contribution schedules
- **Acceptance:** Investment curves reflect scheduled recurring contributions

### WI-P8-007: Net Worth Simulation Enhancement
- **Status:** [DONE]
- **Parallelizable:** Yes (after WI-P8-002)
- **Depends on:** WI-P8-002
- **Contract:** `contracts/WI-P8-007-net-worth-simulation.md`
- **Files:**
  - `FinanceEngine/Calculators/ForwardSimulationEngine.cs`
  - `FinanceEngine/Models/SimulationModels.cs`
  - `FinanceEngine.Tests/Calculators/ForwardSimulationEngineTests.cs`
- **Task:** Extend simulation engine to track investment accounts with recurring contributions
- **Acceptance:** Simulation outputs include investment growth and true net worth over time

---

## Phase 9: Chart Enhancements
**Status:** Complete
**Estimated effort:** 1-2 sessions
**Parallelizable:** Yes

### WI-P9-001: Projection Data Aggregation
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** Phase 7
- **Contract:** `contracts/WI-P9-001-data-aggregation.md`
- **Files:**
  - `dashboard/src/app/core/services/projection.service.ts`
- **Task:** Implement service logic to aggregate daily data into Weekly/Monthly snapshots
- **Acceptance:** Service produces clean trend data without "sawtooth" noise

### WI-P9-002: Chart View Controls & Stepped Rendering
- **Status:** [DONE]
- **Parallelizable:** No
- **Depends on:** WI-P9-001
- **Contract:** `contracts/WI-P9-002-chart-view-controls.md`
- **Files:**
  - `dashboard/src/app/pages/projections/projections.html`
  - `dashboard/src/app/features/charts/*.ts`
- **Task:** Add granularity toggle and stepped line rendering for debt
- **Acceptance:** Users can switch between granular details and smooth trends

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

### Generic Recurring Transactions
- Unify all recurring events (income, expenses, contributions) into single entity
- Auto-materialization background service
- Management UI for all recurring event types

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
| WI-P3-001 | Claude Opus 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P3-002 | Claude Opus 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P3-003 | Claude Opus 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P4-000 | Claude Sonnet 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P4-001 | Claude Sonnet 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P4-002 | Claude Sonnet 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P4-003 | Claude Sonnet 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P4-004 | Claude Sonnet 4.5 | 2025-12-24 | 2025-12-24 |
| WI-P5-001 | Codex (GPT-5) | 2025-12-23 | 2025-12-23 |
| WI-P5-002 | Codex (GPT-5) | 2025-12-23 | 2025-12-23 |
| WI-P5-003 | Codex (GPT-5) | 2025-12-23 | 2025-12-23 |
| WI-P6-001 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-002 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-003 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-004 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-005 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-006 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-007 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-008 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-009 | Interactive Agent | 2025-12-25 | 2025-12-25 |
| WI-P6-010 | Interactive Agent | 2025-12-25 | 2025-12-25 |

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
