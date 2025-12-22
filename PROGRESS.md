# MVP Finance - Current Progress

Last updated: 2025-12-21
Current commit: 27c4cb9 (latest as of 2025-12-21)
Working tree: may be dirty (local `.claude/settings.local.json`, do not commit)

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
- [x] Event-sourced balance calculation

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
- [x] Loading states

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

Files:
- `dashboard/src/app/pages/projections/projections.ts`
- `dashboard/src/app/pages/projections/projections.html`
- `dashboard/src/app/pages/projections/projections.scss`
- `dashboard/src/app/features/charts/debt-projection-chart.component.ts`
- `dashboard/src/app/features/charts/investment-projection-chart.component.ts`
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
| Issue | Location | Impact | Effort |
|-------|----------|--------|--------|
| Dashboard uses `initialBalance` not `currentBalance` | `dashboard/src/app/pages/dashboard/dashboard.ts:52-68` | Shows wrong totals | 5 min |
| Placeholder test file | `FinanceEngine.Tests/UnitTest1.cs` | Clutter | 1 min |

### Priority 2: MVP Spec Gaps
| Feature | Spec Reference | Current State | Files Affected |
|---------|---------------|---------------|----------------|
| Scenario Slider | "Toggle extra $100/paycheck" | Backend ready, no UI | `projections/`, new component |
| Crossover Milestone | "Investment > Debt interest date" | Not calculated | `projection.service.ts` |
| Net Worth Curve | "Assets minus Debts" | Separate curves only | `projections/` |

### Priority 3: Data Integrity
| Feature | Spec Reference | Database Change | Backend | Frontend |
|---------|---------------|-----------------|---------|----------|
| Reconciliation | "Pending vs Cleared" | Add Status enum | Filter endpoint | Toggle UI |
| Statement Cycles | Already in DB | None | Use in calculators | Calendar markers |

### Priority 4: Test Coverage Gaps
| Area | Current | Needed | Parallelizable |
|------|---------|--------|----------------|
| AccountEndpoints | 0 tests | Integration tests | Yes |
| CalculatorEndpoints | 0 tests | Integration tests | Yes |
| Frontend components | 0 tests | Unit tests | Yes (per component) |

### Priority 5: Code Quality
- Balance calculation duplicated in `AccountEndpoints.cs` (lines 41-59 and 147-173)
- Frontend component tests not implemented
- Loading skeletons not implemented
- Commented migration in `Program.cs:50-94` (completed, can remove)

## Testing Status

Manual Testing:
- Not formally tracked; run smoke tests when resuming work

Automated Testing:
- Backend xUnit tests for settings and event endpoints
- Backend calculator tests (existing)
- No frontend component tests yet

## Database Schema

Tables:
- `Accounts` - Financial accounts (Cash, Debt, Investment)
- `Events` - Financial transactions/events
- `UserSettings` - Pay frequency, paycheck amount, safety buffer, next paycheck date

Event Sourcing:
Account balances are calculated from:
1. `InitialBalance`
2. Sum of related `Events` (additions/subtractions by type)

## Backlog and Next Session Options

Option 1: Advanced Features
- Debt payoff calculator UI with strategy comparison
- Investment projection scenarios
- Transaction categories/tags
- Recurring transactions
- Budget tracking

Option 2: Enhanced Testing
- Frontend component tests (Jasmine/Karma)
- E2E tests (Playwright/Cypress)
- Performance testing
- Accessibility audits

Option 3: Deployment and DevOps
- Create GitHub repository
- Set up CI/CD pipeline
- Docker containerization
- Production deployment (Azure/AWS)
- Add README badges

Option 4: Refactoring and Optimization
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

Current status: Working tree may be dirty due to local `.claude/` settings (do not commit)

## File Structure Overview

```
mvp-finance/
  CLAUDE.md
  PROGRESS.md
  mvp-finance.md
  FinanceEngine/
  FinanceEngine.Data/
    Entities/
    FinanceDbContext.cs
  FinanceEngine.Api/
    Endpoints/
      AccountEndpoints.cs
      EventEndpoints.cs
      CalculatorEndpoints.cs
      SettingsEndpoints.cs
  FinanceEngine.Tests/
  dashboard/
    CLAUDE.md
    dashboard.md
    src/app/
      core/
        models/api.models.ts
        services/api.service.ts
      pages/
        dashboard/
        accounts/
        transactions/
        settings/
        projections/
      features/
        charts/
        calendar/
```

## Key Commands Reference

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
npm test                                        # Run tests (when implemented)

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
