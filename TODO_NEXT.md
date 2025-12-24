# TODO_NEXT.md

Last updated: 2025-12-24

Read this first when resuming work.

## Top Priority Next Step
- **Phase 3 Complete!** Transaction reconciliation (Pending/Cleared) implemented.
- Manual testing: Start both servers and verify Phase 3 features work correctly
- Commands: `dotnet run --project FinanceEngine.Api` (terminal 1) and `cd dashboard; npm start` (terminal 2)
- Test scenarios:
  - Transaction status: Click status button to toggle between Pending/Cleared
  - Status filter: Use All/Pending/Cleared filter buttons
  - Pending badge: Verify count shows in header when pending transactions exist
- Next: Commit Phase 3 changes or proceed to Phase 4 (Test Coverage)

## Phase 3 Work Items (Data Integrity - Reconciliation)

| Work Item | File(s) | Status |
|------|---------|--------|
| WI-P3-001: EventStatus enum and migration | `FinancialEventEntity.cs`, `FinanceDbContext.cs` | DONE (2025-12-24) |
| WI-P3-002: Status filter and PATCH endpoint | `EventEndpoints.cs` | DONE (2025-12-24) |
| WI-P3-003: Frontend reconciliation UI | `transactions.ts`, `transactions.html`, `api.models.ts` | DONE (2025-12-24) |

## Working State Snapshot
- Branch: master (as of 2025-12-24)
- Working tree: dirty (Phase 3 changes uncommitted)
- Servers: not running
- Last activity: WI-P3-003 Reconciliation UI completed (Claude Opus 4.5)

## Current Status
**Last Completed:** WI-P3-003 Reconciliation UI (status toggle, filter, pending badge)
**Phase 2:** Complete! All 5 work items done.
**Phase 3:** Complete! All 3 work items done.
**Branch:** master
**Ready for:** Manual testing and commit, or proceed to Phase 4 (Test Coverage)

## Recently Completed (2025-12-22)
- Phase 1: Dashboard totals use currentBalance
- Phase 1: Placeholder test file removed
- Phase 1: Documentation timestamps synced

## Recently Completed (2025-12-21)
- Module 1: Settings integration (API tests, dashboard settings, calendar settings, date validation)
- Module 2: Transaction editing (backend PUT, API client, UI form reuse)
- Module 3: Validation and error handling (amount max, account dialog errors)
- Module 4: Testing infrastructure (event endpoint integration tests)
- Module 5: Polish and UX (.gitattributes line endings)
- Planning: Full codebase analysis and prioritized roadmap (Claude Opus 4.5)

## Feature Entry Points
- Accounts: `dashboard/src/app/pages/accounts/`, `FinanceEngine.Api/Endpoints/AccountEndpoints.cs`
- Transactions: `dashboard/src/app/pages/transactions/`, `FinanceEngine.Api/Endpoints/EventEndpoints.cs`
- Dashboard: `dashboard/src/app/pages/dashboard/`, `dashboard/src/app/core/services/api.service.ts`
- Projections: `dashboard/src/app/pages/projections/`, `dashboard/src/app/features/charts/`, `FinanceEngine.Api/Endpoints/CalculatorEndpoints.cs`
- Calendar: `dashboard/src/app/features/calendar/`, `dashboard/src/app/core/services/calendar.service.ts`
- Settings: `dashboard/src/app/pages/settings/settings.ts`, `FinanceEngine.Api/Endpoints/SettingsEndpoints.cs`
- API models: `dashboard/src/app/core/models/api.models.ts`

## Test Coverage
- Settings endpoints (GET/PUT, validation, defaults)
- Event endpoints (CRUD operations)
- Backend calculator tests (existing)
- Frontend component tests: not implemented

## Known Issues and Risks
- Balance calculation duplication in `FinanceEngine.Api/Endpoints/AccountEndpoints.cs`
- No frontend component tests yet

## Potential Next Steps

### Option 1: Advanced Features
- Debt payoff calculator UI with strategy comparison
- Investment projection with different scenarios
- Transaction categories/tags
- Recurring transactions
- Budget tracking

### Option 2: Enhanced Testing
- Frontend component tests (Jasmine/Karma)
- E2E tests (Playwright/Cypress)
- Performance testing
- Accessibility audits

### Option 3: Deployment and DevOps
- Create GitHub repository
- Set up CI/CD pipeline
- Docker containerization
- Production deployment (Azure/AWS)
- Add README badges

### Option 4: Refactoring and Optimization
- Extract duplicated balance calculation logic
- Add caching for performance
- Implement loading skeletons
- Improve mobile responsiveness
- Add dark mode

## Commands Reference
```bash
# Backend
dotnet build                                    # Build solution
dotnet test                                     # Run all tests
dotnet run --project FinanceEngine.Api          # Start API

# Frontend
cd dashboard
npm install                                     # Install dependencies
npm start                                       # Dev server (4200)
npm run build                                   # Production build
npm test                                        # Run tests (when implemented)

# Git
git status                                      # Check current state
git log --oneline --graph --all                 # Visual commit history
```

## Architecture Notes
- Settings integration: Dashboard and Calendar pull user settings from API
- Transaction editing: Reuses the create form with an editing state signal
- Testing: WebApplicationFactory with in-memory database for API tests
- Git workflow: feature/<name> and wi/<ticket>-<slug> branches with merge and cleanup
