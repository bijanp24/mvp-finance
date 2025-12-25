# TODO_NEXT.md

Last updated: 2025-12-24

Read this first when resuming work.

## Top Priority Next Step
- **Phase 8: Recurring Investments & Contributions** is ready for implementation.
- Start with WI-P8-001 (database entity) - blocking item for rest of phase.
- Contracts created in `contracts/WI-P8-*.md` for all 7 work items.

## Current Status
**Phases 1-7 Complete:**
- **Phase 1:** Quick Wins (3/3 done)
- **Phase 2:** Core MVP Features (5/5 done)
- **Phase 3:** Data Integrity - Reconciliation (3/3 done)
- **Phase 4:** Test Coverage (5/5 done) - 117 backend + 39 frontend = 156 tests
- **Phase 5:** Polish & UX (3/3 done) - Loading skeletons, balance calculator refactor, code cleanup
- **Phase 6:** Frontend Redesign (7/7 done) - Full app visual refresh complete
**Phase 7:** Visual Polish (3/3 done) - Dark theme, account fix, chart aesthetics
**Phase 8:** Recurring Contributions (0/7 done) - Planned
**Phase 9:** Chart Enhancements (0/2 done) - Planned

**Branch:** wi/p7-visual-polish
**Working tree:** dirty

## Parallelizable Work Items
| Work Item ID | Description | Parallelizable | Notes |
|-------------|-------------|----------------|-------|
| WI-P8-001 | Recurring Contribution Entity | No | Start of Phase 8 |
| WI-P9-001 | Data Aggregation | Yes | Independent frontend logic |
| WI-P9-002 | Chart Controls | No | Depends on WI-P9-001 |


## Phase 8 Work Items
| Work Item ID | Description | Parallelizable | Dependencies |
|-------------|-------------|----------------|--------------|
| WI-P8-001 | RecurringContributionEntity | No | None (start here) |
| WI-P8-002 | RecurringEventExpansionService | No | WI-P8-001 |
| WI-P8-003 | API Endpoints | No | WI-P8-001 |
| WI-P8-004 | Settings UI | No | WI-P8-003 |
| WI-P8-005 | Calendar Integration | Yes | WI-P8-002, WI-P8-003 |
| WI-P8-006 | Projection Integration | Yes | WI-P8-002 |
| WI-P8-007 | Net Worth Simulation | Yes | WI-P8-002 |

**Execution Order:**
1. WI-P8-001 (entity) - must complete first
2. WI-P8-002 + WI-P8-003 can run in parallel after WI-P8-001
3. WI-P8-004 waits for WI-P8-003
4. WI-P8-005, WI-P8-006, WI-P8-007 can run in parallel after WI-P8-002

## Recently Completed (2025-12-25)
- WI-P6-001: Visual System and Theme Tokens land in `styles.scss` and `index.html`.
- WI-P6-002: App Shell and Navigation Redesign in `app.html` and `app.scss`.

### Phase 4: Test Coverage
- WI-P4-000: Jest setup for Angular testing
- WI-P4-001: AccountEndpoints integration tests
- WI-P4-002: CalculatorEndpoints integration tests
- WI-P4-003: Dashboard component tests (14 tests)
- WI-P4-004: Transaction component tests (23 tests)

### Phase 3: Data Integrity
- WI-P3-001: EventStatus enum and migration (Pending/Cleared)
- WI-P3-002: Status filter and PATCH endpoint
- WI-P3-003: Frontend reconciliation UI

## Feature Entry Points
- Accounts: `dashboard/src/app/pages/accounts/`, `FinanceEngine.Api/Endpoints/AccountEndpoints.cs`
- Transactions: `dashboard/src/app/pages/transactions/`, `FinanceEngine.Api/Endpoints/EventEndpoints.cs`
- Dashboard: `dashboard/src/app/pages/dashboard/`, `dashboard/src/app/core/services/api.service.ts`
- Projections: `dashboard/src/app/pages/projections/`, `dashboard/src/app/features/charts/`, `FinanceEngine.Api/Endpoints/CalculatorEndpoints.cs`
- Calendar: `dashboard/src/app/features/calendar/`, `dashboard/src/app/core/services/calendar.service.ts`
- Settings: `dashboard/src/app/pages/settings/settings.ts`, `FinanceEngine.Api/Endpoints/SettingsEndpoints.cs`
- API models: `dashboard/src/app/core/models/api.models.ts`
- Balance calculation: `FinanceEngine/Services/BalanceCalculator.cs`
- Recurring contributions (Phase 8): `contracts/WI-P8-*.md`

## Test Coverage
- Backend: 117 tests (settings, events, accounts, calculator endpoints)
- Frontend: 39 tests (dashboard, transactions components)
- Total: 156 tests

## Future Phases (After Phase 8)

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
- Unify all recurring events (income, expenses, contributions)
- Auto-materialization background service
- Single management UI

### Deployment and DevOps
- Set up CI/CD pipeline
- Docker containerization
- Production deployment (Azure/AWS)
- Add README badges

### E2E Testing
- Playwright or Cypress setup
- Critical path tests
- Performance testing
- Accessibility audits

## Commands Reference
```bash
# Backend
dotnet build                                    # Build solution
dotnet test                                     # Run all tests (117 tests)
dotnet run --project FinanceEngine.Api          # Start API

# Frontend
cd dashboard
npm install                                     # Install dependencies
npm start                                       # Dev server (4200)
npm run build                                   # Production build
npm test                                        # Run tests (39 tests)

# Git
git status                                      # Check current state
git log --oneline --graph --all                 # Visual commit history
```

## Architecture Notes
- Settings integration: Dashboard and Calendar pull user settings from API
- Transaction editing: Reuses the create form with an editing state signal
- Testing: WebApplicationFactory with in-memory database for API tests
- Balance calculation: Centralized in `FinanceEngine/Services/BalanceCalculator.cs`
- Loading states: Skeleton loaders for better perceived performance
- Git workflow: feature/<name> and wi/<ticket>-<slug> branches with merge and cleanup
