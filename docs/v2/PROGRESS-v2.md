# PROGRESS-v2.md

Last updated: 2025-12-29
Version: 2.0 (Goal-Driven Budgeting)
Current commit: ecb401d
Working tree: dirty

## Purpose
Track v2 feature progress and provide deep dive context.

For full v2 vision, see `GOAL_DRIVEN_BUDGETING.md`.

---

## v1 Summary (Complete)

All v1 features are complete and archived in `docs/v1-archive/`.

**Completed Phases:**
| Phase | Name | Items | Tests |
|-------|------|-------|-------|
| 1 | Quick Wins | 3/3 | - |
| 2 | Core MVP Features | 5/5 | - |
| 3 | Data Integrity | 3/3 | - |
| 4 | Test Coverage | 5/5 | 156 total |
| 5 | Polish & UX | 3/3 | - |
| 6 | Frontend Redesign | 7/7 | - |
| 7 | Visual Polish | 3/3 | - |
| 8 | Recurring Contributions | 7/7 | - |
| 9 | Chart Enhancements | 2/2 | - |

**v1 Test Coverage:**
- Backend: 117 tests (accounts, events, calculator, settings endpoints)
- Frontend: 39 tests (dashboard, transactions components)

**v1 Features:**
- Account management (Cash, Debt, Investment)
- Transaction tracking with reconciliation (Pending/Cleared)
- Debt and investment projections
- Scenario slider for extra debt payments
- Recurring contributions
- Calendar with paycheck/debt/contribution markers
- Dark theme redesign

---

## v2 Feature Status

### Phase A: Budget Categories & Recurring Expenses
**Status:** COMPLETE (2025-12-29)
**Goal:** Enable expense planning and category-based spending visibility

| Work Item | Description | Status |
|-----------|-------------|--------|
| WI-PA-001 | Category Entity | [DONE] |
| WI-PA-002 | Budget Entity | [DONE] |
| WI-PA-003 | Category API | [DONE] |
| WI-PA-004 | Budget API | [DONE] |
| WI-PA-005 | Transaction Category Tagging | [DONE] |
| WI-PA-006 | Budget Management UI | [DONE] |
| WI-PA-007 | Transaction Category Picker | [DONE] |
| WI-PA-008 | Budget vs Actual Dashboard | [DONE] |
| WI-PA-009 | Calendar Budget Markers | [DONE] |

### Phase B: Financial Goals
**Status:** COMPLETE (2025-12-29)
**Goal:** Target-setting for debt payoff and investment growth

| Work Item | Description | Status |
|-----------|-------------|--------|
| WI-PB-001 | Goal Entity | [DONE] |
| WI-PB-002 | Goal Progress Calculator | [DONE] |
| WI-PB-003 | Goal API | [DONE] |
| WI-PB-004 | Goals Page UI | [DONE] |
| WI-PB-005 | Goal Create/Edit Dialog | [DONE] |
| WI-PB-006 | Goal Detail View | [DEFERRED] |
| WI-PB-007 | Dashboard Goal Widget | [DONE] |
| WI-PB-008 | Projections Goal Integration | [DONE] |

### Phase C: Dynamic Safe-to-Spend
**Status:** Not Started
**Goal:** Replace static buffer with goal-based calculation

### Phase D: Scenario Planning
**Status:** Not Started
**Goal:** Interactive sliders showing spending trade-offs

---

## Quick Start

### Running the Application

Backend (.NET 10):
```bash
dotnet run --project FinanceEngine.Api
# API runs on: http://localhost:5000
```

Frontend (Angular 21):
```bash
cd dashboard
npm start
# Dashboard runs on: http://localhost:4200
```

Database:
- SQLite auto-created at runtime
- Location: `FinanceEngine.Api/finance.db`

---

## Architecture Notes

**Frontend (Angular 21):**
- Signals-based state management with OnPush
- Standalone components
- Material Design with dark theme

**Backend (.NET 10):**
- Event sourcing for account balances
- Minimal APIs with focused endpoints
- Entity Framework Core with SQLite

**Key Services:**
- `BalanceCalculator.cs` - Centralized balance calculation
- `RecurringEventExpansionService.cs` - Schedule expansion
- `ForwardSimulationEngine.cs` - Projection calculations

---

## File Structure

```
mvp-finance/
├── docs/
│   ├── v1-archive/           # Historical v1 docs
│   └── v2/                   # Current v2 docs
│       ├── ROADMAP-v2.md
│       ├── WORKLOG-v2.md
│       └── PROGRESS-v2.md
├── contracts/
│   ├── v1/                   # v1 work item contracts
│   ├── v2/                   # v2 work item contracts
│   └── .system_prompt.md     # Worker agent prompt
├── AGENTS.md                 # Conventions (points to v2)
├── TODO_NEXT.md              # Immediate next actions
├── GOAL_DRIVEN_BUDGETING.md  # v2 vision document
├── Orchestration.md          # Multi-agent workflow
├── FinanceEngine/            # Core library
├── FinanceEngine.Api/        # Minimal API
├── FinanceEngine.Data/       # EF Core
├── FinanceEngine.Tests/      # xUnit tests
└── dashboard/                # Angular 21 frontend
```

---

## Commands Reference

```bash
# Backend
dotnet build                                    # Build solution
dotnet test                                     # Run tests
dotnet run --project FinanceEngine.Api          # Start API

# Frontend
cd dashboard
npm install                                     # Install dependencies
npm start                                       # Dev server (4200)
npm run build                                   # Production build
npm test                                        # Run tests

# Git
git status                                      # Check current state
git log --oneline -10                           # Recent commits
```
