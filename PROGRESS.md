# MVP Finance - Current Progress

Last updated: 2025-12-24
Current commit: 745f3a1 (Codex Review Refactor)
Working tree: clean

## When to Read This
Use this file for deep dive context: feature inventory, file references, known issues, architecture notes, and backlog.

## Quick Start

### Running the Application

Backend (.NET API):
```bash
dotnet run --project FinanceEngine.Api
# API runs on: http://localhost:5000
```

Frontend (Angular Dashboard):
```bash
cd dashboard
npm start
# Dashboard runs on: http://localhost:4200
# Proxies API calls to http://localhost:5000
```

Database:
- SQLite database auto-created on first run
- Location: `FinanceEngine.Api/finance.db`

## Built Features

### Accounts Management (`/accounts`)
- [x] Full CRUD operations for financial accounts (Cash, Debt, Investment)
- [x] Dialog-based create/edit forms with reactive validation
- [x] Type-specific fields (APR for debt, minimum payments)
- [x] Soft delete functionality
- [x] Color-coded account type badges
- [x] Event-sourced balance calculation (centralized in BalanceCalculator service)
- [x] Loading skeletons for better perceived performance

Files:
- `dashboard/src/app/pages/accounts/accounts.ts`
- `dashboard/src/app/pages/accounts/accounts.html`
- `dashboard/src/app/pages/accounts/account-dialog.component.ts`
- `FinanceEngine.Api/Endpoints/AccountEndpoints.cs`

### Transactions Management (`/transactions`)
- [x] Quick-add transaction form with 6 event types
- [x] Dynamic form validation based on transaction type
- [x] Smart account filtering (Cash + Debt/Investment as needed)
- [x] Recent transactions list (last 30 days)
- [x] Delete and edit functionality
- [x] Material Design datepicker integration
- [x] Reconciliation: Pending/Cleared status toggle
- [x] Status filter (All/Pending/Cleared)
- [x] Loading skeletons for better perceived performance

Event Types Supported:
- Income (Cash account receives)
- Expense (Cash account pays)
- Debt Payment (Cash + Debt)
- Debt Charge (Debt account increases)
- Savings Contribution (Cash + Investment)
- Investment Contribution (Cash + Investment)

Files:
- `dashboard/src/app/pages/transactions/transactions.ts`
- `dashboard/src/app/pages/transactions/transactions.html`
- `FinanceEngine.Api/Endpoints/EventEndpoints.cs`

### Dashboard (`/dashboard`)
- [x] Real-time data from accounts and events APIs
- [x] Summary tiles: Total Cash, Total Debt, Total Investments
- [x] Safe to spend calculator integration (uses settings)
- [x] Recent transactions feed (last 10)
- [x] Empty states with call-to-action
- [x] Loading skeletons for better perceived performance

Files:
- `dashboard/src/app/pages/dashboard/dashboard.ts`
- `dashboard/src/app/pages/dashboard/dashboard.html`

### Settings (`/settings`)
- [x] Pay frequency, paycheck amount, safety buffer, next paycheck date
- [x] Date validation and timezone-safe save
- [x] API endpoints and database entity

Files:
- `dashboard/src/app/pages/settings/settings.ts`
- `FinanceEngine.Api/Endpoints/SettingsEndpoints.cs`
- `FinanceEngine.Data/Entities/UserSettingsEntity.cs`

### Projections (`/projections`)
- [x] Debt projection visualization with charts
- [x] Investment growth projection with compound interest
- [x] Time range selector (3mo, 6mo, 1yr, 2yr, 5yr)
- [x] Debt-free date calculation
- [x] Total interest projection
- [x] Final investment value projection
- [x] Responsive design for mobile/tablet
- [x] Scenario slider for extra debt payments (0-500, $25 steps)
- [x] Crossover milestone calculation (when investment returns exceed debt interest)
- [x] Net worth curve visualization (assets minus debts)

Files:
- `dashboard/src/app/pages/projections/projections.ts`
- `dashboard/src/app/pages/projections/projections.html`
- `dashboard/src/app/pages/projections/projections.scss`
- `dashboard/src/app/features/charts/debt-projection-chart.component.ts`
- `dashboard/src/app/features/charts/investment-projection-chart.component.ts`
- `dashboard/src/app/features/charts/net-worth-chart.component.ts`
- `dashboard/src/app/core/services/projection.service.ts`
- `FinanceEngine.Api/Endpoints/CalculatorEndpoints.cs`

### Calendar (`/calendar`)
- [x] Monthly calendar view with paycheck indicators
- [x] Debt payment due date markers
- [x] Navigation between months and today highlighting
- [x] Integration with user settings for pay frequency
- [x] Responsive mobile design

Files:
- `dashboard/src/app/features/calendar/calendar.component.ts`
- `dashboard/src/app/features/calendar/calendar.component.html`
- `dashboard/src/app/features/calendar/calendar.component.scss`
- `dashboard/src/app/core/services/calendar.service.ts`

### API Layer and Navigation
- [x] Complete API service with all HTTP methods
- [x] TypeScript type definitions
- [x] Sidebar navigation with icons and active route highlighting

Files:
- `dashboard/src/app/core/services/api.service.ts`
- `dashboard/src/app/core/models/api.models.ts`

## Architecture Highlights

Frontend (Angular 21):
- Signals-based state management with OnPush change detection
- Standalone components and reactive forms
- Computed values for derived state
- Material Design with accessibility focus
- Modern control flow (`@if`, `@for`, `@switch`)

Backend (.NET 10):
- Event sourcing for account balances
- Minimal APIs with focused endpoints
- Entity Framework Core with SQLite
- Soft deletes via `IsActive` on accounts

Key Patterns:
- Type-driven forms for account and transaction types
- Smart account filtering for each event type
- Error handling with snackbar notifications
- Empty states with guided prompts

## Known Issues and Improvement Opportunities

### Priority 1: Bug Fixes (Critical)
- None currently tracked.

### Priority 2: UX and Visual Design
- Frontend needs a full visual redesign to improve hierarchy, typography, and layout consistency.
- Phase 6 work items now define the redesign plan and contracts.

### Priority 3: MVP Spec Gaps
- All MVP spec items complete (Phases 1-5)

### Priority 4: Data Integrity
| Feature | Spec Reference | Status |
|---------|---------------|--------|
| Reconciliation | "Pending vs Cleared" | Complete (Phase 3) |
| Statement Cycles | Already in DB | Not yet used in calculators |

### Priority 5: Test Coverage
| Area | Tests | Status |
|------|-------|--------|
| AccountEndpoints | Integration tests | Complete |
| CalculatorEndpoints | Integration tests | Complete |
| Dashboard component | 14 unit tests | Complete |
| Transactions component | 23 unit tests | Complete |

### Priority 6: Code Quality
- All Phase 5 items complete:
  - Balance calculation centralized in `FinanceEngine/Services/BalanceCalculator.cs`
  - Loading skeletons implemented on dashboard, accounts, transactions
  - Commented migration removed from Program.cs

## Testing Status

Manual Testing:
- Not formally tracked; run smoke tests when resuming work

Automated Testing (156 total tests):
- Backend (117 tests):
  - Settings endpoints (GET/PUT, validation, defaults)
  - Event endpoints (CRUD operations)
  - Account endpoints (CRUD, balance calculation)
  - Calculator endpoints (spendable, burn rate, debt allocation, projections)
- Frontend (39 tests):
  - Dashboard component (14 tests: API calls, balance calculations, formatting)
  - Transactions component (23 tests: form validation, filtering, status toggle)
  - Jest + @testing-library/angular setup

## Database Schema

Tables:
- `Accounts` - Financial accounts (Cash, Debt, Investment)
- `Events` - Financial transactions/events (with Status: Pending/Cleared)
- `UserSettings` - Pay frequency, paycheck amount, safety buffer, next paycheck date

Event Sourcing:
Account balances are calculated by `FinanceEngine/Services/BalanceCalculator.cs`:
1. `InitialBalance`
2. Sum of related `Events` (additions/subtractions by type)

## Backlog and Next Session Options

Option 1: Frontend Redesign (Phase 6)
- Visual system and app shell refresh
- Page-level redesigns (dashboard, accounts, transactions, projections, calendar, settings)
- Accessibility and contrast polish

Option 2: Advanced Features
- Debt payoff calculator UI with strategy comparison
- Investment projection scenarios
- Transaction categories/tags
- Recurring transactions
- Budget tracking

Option 3: Enhanced Testing
- Frontend component tests (Jasmine/Karma)
- E2E tests (Playwright/Cypress)
- Performance testing
- Accessibility audits

Option 4: Deployment and DevOps
- Create GitHub repository
- Set up CI/CD pipeline
- Docker containerization
- Production deployment (Azure/AWS)
- Add README badges

Option 5: Refactoring and Optimization
- Extract duplicated balance calculation logic
- Add caching for performance
- Implement loading skeletons
- Improve mobile responsiveness
- Add dark mode

## Git Workflow Reminder

Branch Structure:
- `master` - Production/deployable code
- `feature/<name>` - Feature branches
- `wi/<ticket>-<slug>` - Work items under a feature

Before any commit:
1. Consolidate markdown files into `mvp-finance.md` and `dashboard.md`
2. Remove image references from markdown
3. Ensure `WORKLOG.md` and `TODO_NEXT.md` are up to date

Current status: Working tree clean (as of 2025-12-24)

## File Structure Overview

```
mvp-finance/
  CLAUDE.md
  PROGRESS.md
  mvp-finance.md
  contracts/                        # Work item specifications
  FinanceEngine/
    Services/
      BalanceCalculator.cs          # Centralized balance calculation
  FinanceEngine.Data/
    Entities/
    FinanceDbContext.cs
  FinanceEngine.Api/
    Endpoints/
      AccountEndpoints.cs
      EventEndpoints.cs
      CalculatorEndpoints.cs
      SettingsEndpoints.cs
    Services/
      AccountService.cs             # Uses BalanceCalculator
  FinanceEngine.Tests/
    AccountEndpointsTests.cs
    CalculatorEndpointsTests.cs
  dashboard/
    CLAUDE.md
    dashboard.md
    jest.config.js                  # Jest testing config
    setup-jest.ts
    src/app/
      core/
        models/api.models.ts
        services/api.service.ts
      pages/
        dashboard/
          dashboard.spec.ts         # 14 tests
        accounts/
        transactions/
          transactions.spec.ts      # 23 tests
        settings/
        projections/
      features/
        charts/
          net-worth-chart.component.ts
        calendar/
```

## Key Commands Reference

```bash
# Backend
dotnet build                                    # Build solution
dotnet test                                     # Run tests (117 tests)
dotnet run --project FinanceEngine.Api          # Start API

# Frontend
cd dashboard
npm install                                     # Install dependencies
npm start                                       # Dev server (4200)
npm run build                                   # Production build
npm test                                        # Run tests (39 tests)

# Git
git status                                      # Check current state
git log --oneline -10                           # Recent commits
git diff                                        # Uncommitted changes
```

## Questions to Consider

1. Should transaction categories/tags be part of the MVP?
2. Is recurring transaction support needed soon?
3. Do we need data export or backups before deployment?
4. Should we plan for multi-currency, or stick to USD?
