# TODO_NEXT.md

Last updated: 2025-12-24

Read this first when resuming work.

## Top Priority Next Step
- **Phase 4 Started!** Jest testing framework configured for Angular dashboard.
- WI-P4-000 (Jest Setup) complete - 2 passing smoke tests
- Ready to implement frontend component tests (WI-P4-003, WI-P4-004)
- Backend integration tests (WI-P4-001, WI-P4-002) can run in parallel
- Commands to verify: `cd dashboard; npm test` (should pass 2 tests)
- Next: Implement WI-P4-003 (Dashboard Tests) or WI-P4-004 (Transaction Tests)

## Phase 4 Work Items (Test Coverage)

| Work Item | File(s) | Status |
|------|---------|--------|
| WI-P4-000: Jest setup | `jest.config.js`, `setup-jest.ts`, `tsconfig.spec.json` | DONE (2025-12-24) |
| WI-P4-001: AccountEndpoints tests | Backend integration tests | Not Started |
| WI-P4-002: CalculatorEndpoints tests | Backend integration tests | Not Started |
| WI-P4-003: Dashboard component tests | `dashboard.spec.ts` | Not Started |
| WI-P4-004: Transaction component tests | `transactions.spec.ts` | Not Started |

## Phase 3 Work Items (Data Integrity - Reconciliation)

| Work Item | File(s) | Status |
|------|---------|--------|
| WI-P3-001: EventStatus enum and migration | `FinancialEventEntity.cs`, `FinanceDbContext.cs` | DONE (2025-12-24) |
| WI-P3-002: Status filter and PATCH endpoint | `EventEndpoints.cs` | DONE (2025-12-24) |
| WI-P3-003: Frontend reconciliation UI | `transactions.ts`, `transactions.html`, `api.models.ts` | DONE (2025-12-24) |

## Working State Snapshot
- Branch: master (as of 2025-12-24)
- Working tree: dirty (Phase 3 + WI-P4-000 changes uncommitted)
- Servers: not running
- Last activity: WI-P4-000 Jest Setup completed (Claude Sonnet 4.5)

## Current Status
**Last Completed:** WI-P4-000 Jest Setup (testing framework configured)
**Phase 2:** Complete! All 5 work items done.
**Phase 3:** Complete! All 3 work items done.
**Phase 4:** In Progress (1 of 5 work items done)
**Branch:** master
**Ready for:** Frontend component tests (WI-P4-003, WI-P4-004) or backend integration tests (WI-P4-001, WI-P4-002)

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
- Jest testing framework: configured and working
- Frontend component tests: ready to implement

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
