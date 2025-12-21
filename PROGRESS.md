# MVP Finance - Current Progress

**Last Updated:** December 21, 2025
**Current Commit:** `f0b366b` - Projections feature complete

---

## Quick Start

### Running the Application

**Backend (.NET API):**
```bash
dotnet run --project FinanceEngine.Api
# API runs on: http://localhost:5000
```

**Frontend (Angular Dashboard):**
```bash
cd dashboard
npm start
# Dashboard runs on: http://localhost:4200
# Proxies API calls to http://localhost:5000
```

**Database:**
- SQLite database auto-created on first run
- Location: `FinanceEngine.Api/finance.db`

---

## What's Been Built ✅

### 1. **Accounts Management** (`/accounts`)
- ✅ Full CRUD operations for financial accounts
- ✅ Three account types: Cash, Debt, Investment
- ✅ Dialog-based create/edit forms with reactive validation
- ✅ Type-specific fields (APR for debt, minimum payments, etc.)
- ✅ Soft delete functionality
- ✅ Color-coded account type badges
- ✅ Event-sourced balance calculation

**Files:**
- `dashboard/src/app/pages/accounts/accounts.ts` - Main component
- `dashboard/src/app/pages/accounts/accounts.html` - Template
- `dashboard/src/app/pages/accounts/account-dialog.component.ts` - Create/Edit dialog
- `FinanceEngine.Api/Endpoints/AccountEndpoints.cs` - Backend API

### 2. **Transactions Management** (`/transactions`)
- ✅ Quick-add transaction form with 6 event types
- ✅ Dynamic form validation based on transaction type
- ✅ Smart account filtering (Cash → Debt for payments, etc.)
- ✅ Recent transactions list (last 30 days)
- ✅ Delete functionality
- ✅ Material Design datepicker integration

**Event Types Supported:**
- Income (Cash account receives)
- Expense (Cash account pays)
- Debt Payment (Cash → Debt)
- Debt Charge (Debt account increases)
- Savings Contribution (Cash → Investment)
- Investment Contribution (Cash → Investment)

**Files:**
- `dashboard/src/app/pages/transactions/transactions.ts` - Component
- `dashboard/src/app/pages/transactions/transactions.html` - Template
- `FinanceEngine.Api/Endpoints/EventEndpoints.cs` - Backend API

### 3. **Dashboard** (`/dashboard`)
- ✅ Real-time data from accounts and events APIs
- ✅ Summary tiles: Total Cash, Total Debt, Total Investments
- ✅ Safe to Spend calculator integration
- ✅ Recent transactions feed (last 10)
- ✅ Empty states with call-to-action
- ✅ Loading states

**Files:**
- `dashboard/src/app/pages/dashboard/dashboard.ts`
- `dashboard/src/app/pages/dashboard/dashboard.html`

### 4. **API Layer**
- ✅ Complete API service with all HTTP methods
- ✅ Full TypeScript type definitions
- ✅ Account CRUD endpoints
- ✅ Event/Transaction CRUD endpoints
- ✅ Calculator endpoints (Spendable, Debt Allocation, Simulation)
- ✅ Recent events endpoint

**Files:**
- `dashboard/src/app/core/services/api.service.ts`
- `dashboard/src/app/core/models/api.models.ts`

### 5. **Projections** (`/projections`)
- ✅ Debt projection visualization with interactive charts
- ✅ Investment growth projection with compound interest
- ✅ Time range selector (3mo, 6mo, 1yr, 2yr, 5yr)
- ✅ Debt-free date calculation
- ✅ Total interest projection
- ✅ Final investment value projection
- ✅ Responsive design for mobile/tablet
- ✅ ngx-echarts integration with zoom and tooltips

**Files:**
- `dashboard/src/app/pages/projections/projections.ts` - Main component
- `dashboard/src/app/pages/projections/projections.html` - Template
- `dashboard/src/app/pages/projections/projections.scss` - Responsive styles
- `dashboard/src/app/features/charts/debt-projection-chart.component.ts` - Debt chart
- `dashboard/src/app/features/charts/investment-projection-chart.component.ts` - Investment chart
- `dashboard/src/app/core/services/projection.service.ts` - Projection calculations
- `FinanceEngine.Api/Endpoints/CalculatorEndpoints.cs` - Simulation & projection APIs

### 6. **Calendar** (`/calendar`)
- ✅ Monthly calendar view with paycheck indicators
- ✅ Debt payment due date markers
- ✅ Navigation between months
- ✅ Today highlighting
- ✅ Integration with user settings for pay frequency
- ✅ Responsive mobile design

**Files:**
- `dashboard/src/app/features/calendar/calendar.component.ts` - Calendar component
- `dashboard/src/app/features/calendar/calendar.component.html` - Template
- `dashboard/src/app/features/calendar/calendar.component.scss` - Calendar styles
- `dashboard/src/app/core/services/calendar.service.ts` - Date calculations

### 7. **Settings API** (Backend Complete)
- ✅ GET/PUT user settings endpoints
- ✅ Pay frequency configuration (Weekly, BiWeekly, SemiMonthly, Monthly)
- ✅ Paycheck amount and next paycheck date
- ✅ Safety buffer setting
- ✅ Database entity and migrations

**Files:**
- `FinanceEngine.Api/Endpoints/SettingsEndpoints.cs` - Settings API
- `FinanceEngine.Data/Entities/UserSettingsEntity.cs` - Database entity

### 8. **Navigation**
- ✅ Sidebar navigation with icons
- ✅ Routes: Dashboard, Transactions, Accounts, Calendar, Projections, Settings
- ✅ Active route highlighting

---

## Architecture Highlights

### Frontend (Angular 21)
- **Signals-based state management** - OnPush change detection throughout
- **Standalone components** - No NgModules
- **Reactive forms** - Dynamic validation based on type selection
- **Computed values** - Derived state for filtered lists, totals
- **Material Design** - Consistent UI with accessibility built-in
- **Modern control flow** - `@if`, `@for`, `@switch` syntax

### Backend (.NET 10)
- **Event sourcing** - Account balances calculated from transaction events
- **Minimal APIs** - Clean, focused endpoint definitions
- **Entity Framework Core** - SQLite for simple deployment
- **Soft deletes** - IsActive flag pattern for accounts

### Key Patterns
- **Type-driven forms** - Form fields change based on account/transaction type
- **Smart filtering** - Only show relevant accounts for each transaction type
- **Error handling** - User-friendly snackbar notifications
- **Empty states** - Helpful prompts when no data exists

---

## Known Issues & Areas for Improvement ⚠️

### 1. **Dashboard Calculation** (Priority: Medium)
**Location:** `dashboard/src/app/pages/dashboard/dashboard.ts:79-116`

**Issue:** Uses hardcoded values for spendable calculation:
- 14-day paycheck cycle (assumes bi-weekly)
- $2500 default paycheck amount
- Generic $100 safety buffer

**Recommended Fix:**
- Add user settings for pay schedule and amount
- Pull actual income events from database
- Make safety buffer configurable

### 2. **Duplicated Balance Logic** (Priority: Low)
**Location:** `FinanceEngine.Api/Endpoints/AccountEndpoints.cs`

**Issue:** Balance calculation logic duplicated in:
- Line 41-59: `CalculateBalance()` method
- Line 147-173: `GetAccountBalance()` endpoint

**Recommended Fix:**
- Extract to shared service or helper class
- Consider caching for performance

### 3. **Error Handling in Dialog** (Priority: Low)
**Location:** `dashboard/src/app/pages/accounts/account-dialog.component.ts:202`

**Issue:** Has TODO comment - errors only logged to console

**Recommended Fix:**
- Show MatSnackBar error notification
- Provide user-friendly error messages

### 4. **Date Timezone Handling** (Priority: Medium)
**Location:** `dashboard/src/app/pages/transactions/transactions.ts:239`

**Issue:** Frontend sends ISO date strings, backend expects DateTime

**Recommended Fix:**
- Verify timezone handling end-to-end
- Consider using UTC consistently
- Test with dates near midnight

### 5. **Line Ending Warnings** (Priority: Low)
Git warns about LF/CRLF in some files - this is cosmetic but can be fixed with `.gitattributes`

---

## What's NOT Built Yet 🚧

### High Priority
- [ ] Settings page (currently just a placeholder)
- [ ] User configuration for:
  - Pay schedule and amount
  - Safety buffer preferences
  - Currency settings
- [ ] Edit transaction functionality (currently can only delete)
- [ ] Account balance history/trends
- [ ] Input validation improvements (negative amounts, etc.)

### Medium Priority
- [ ] Debt payoff calculator UI
- [ ] Financial simulation UI
- [ ] Bulk transaction import (CSV, etc.)
- [ ] Categories/tags for transactions
- [ ] Budget tracking
- [ ] Reports and charts

### Low Priority
- [ ] Multi-user support / authentication
- [ ] Data export functionality
- [ ] Recurring transactions
- [ ] Mobile-responsive improvements
- [ ] Dark mode

---

## Testing Status

### Manual Testing
- ✅ Account CRUD operations work
- ✅ Transaction creation works for all types
- ✅ Dashboard loads real data
- ✅ Navigation works correctly

### Automated Testing
- ❌ No frontend tests yet
- ✅ Backend has xUnit test project (basic tests)

**TODO:** Add component tests for:
- Account dialog validation logic
- Transaction form dynamic validation
- Dashboard calculations

---

## Database Schema

**Tables:**
- `Accounts` - Financial accounts (Cash, Debt, Investment)
- `Events` - Financial transactions/events
- Soft deletes via `IsActive` column on Accounts

**Event Sourcing:**
Account balances are NOT stored - they're calculated from:
1. `InitialBalance` (starting point)
2. Sum of all related `Events` (additions/subtractions based on type)

---

## Next Session Recommendations

### Option 1: Complete Basic Features
1. Build out Settings page
2. Add edit functionality for transactions
3. Improve error handling
4. Add input validation

### Option 2: Add Advanced Features
1. Implement debt payoff calculator UI
2. Add financial simulation visualization
3. Create charts/graphs for dashboard

### Option 3: Polish & Testing
1. Write component tests
2. Fix known issues listed above
3. Improve mobile responsiveness
4. Add loading skeletons

---

## Git Workflow Reminder

This project follows a structured Git workflow (see `CLAUDE.md`):

**Branch Structure:**
- `master` - Production/deployable code
- `module/<name>` - Feature modules
- `work-item/<key>-<desc>` - Individual work items

**Before ANY commit:**
1. Consolidate `.md` files in each folder
2. Delete redundant markdown files
3. Remove image references from markdown
4. Use conventional commit format

**Current Status:** Clean working directory on `master`

---

## File Structure Overview

```
mvp-finance/
├── CLAUDE.md                           # Project-wide development rules
├── PROGRESS.md                         # This file - current status
├── mvp-finance.md                      # Consolidated documentation
├── FinanceEngine/                      # Core calculation library
├── FinanceEngine.Data/                 # EF Core data layer
│   ├── Entities/                       # Account, Event entities
│   └── FinanceDbContext.cs             # Database context
├── FinanceEngine.Api/                  # ASP.NET Core Web API
│   └── Endpoints/
│       ├── AccountEndpoints.cs         # Account CRUD + balance
│       ├── EventEndpoints.cs           # Transaction CRUD
│       └── CalculatorEndpoints.cs      # Financial calculators
├── FinanceEngine.Tests/                # xUnit test suite
└── dashboard/                          # Angular 21 frontend
    ├── CLAUDE.md                       # Angular-specific rules
    ├── dashboard.md                    # Consolidated frontend docs
    └── src/app/
        ├── core/
        │   ├── models/api.models.ts    # TypeScript API contracts
        │   └── services/api.service.ts # HTTP client service
        └── pages/
            ├── dashboard/              # Home page with summary
            ├── accounts/               # Account management
            ├── transactions/           # Transaction entry & history
            └── settings/               # Settings (placeholder)
```

---

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

---

## Questions to Consider

1. **User Settings:** Where should we store user preferences? (Pay schedule, safety buffer, etc.)
   - Add to database with new UserSettings table?
   - Use localStorage for MVP?

2. **Transaction Categories:** Should transactions have categories for budgeting?

3. **Recurring Transactions:** Important enough for MVP or defer?

4. **Multi-currency:** Needed or stick with USD for MVP?

5. **Data Backup/Export:** How important for initial release?

---

**Ready to continue development!** 🚀

Pick up where you left off by running the backend and frontend, then choose from the "Next Session Recommendations" above.
