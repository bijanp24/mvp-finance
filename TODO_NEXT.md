# TODO_NEXT.md

Last updated: 2025-12-23

Read this first when resuming work.

## Top Priority Next Step
- **Phase 2 Complete!** All core MVP features implemented.
- Manual testing: Start both servers and verify all Phase 2 features work correctly
- Commands: `dotnet run --project FinanceEngine.Api` (terminal 1) and `cd dashboard; npm start` (terminal 2)
- Test scenarios:
  - Scenario Slider: Move slider 0-500, verify comparison stats update
  - Net Worth: Verify chart displays and toggle works
  - Crossover Milestone: Verify purple card displays with correct date
- Next: Consider Phase 3 (Data Integrity - Reconciliation) or commit Phase 2 changes

## Parallelizable Work Items (Phase 2 Core MVP Features)
Remaining items can be done in parallel by different agents. Details in `ROADMAP.md`.

| Work Item | File(s) | Effort | Status |
|------|---------|--------|--------|
| WI-P2-001: Scenario slider backend integration | `dashboard/src/app/pages/projections/` | 30-45 min | DONE (2025-12-23) |
| WI-P2-002: Scenario slider UI | `dashboard/src/app/pages/projections/` | 30-45 min | DONE (2025-12-23) |
| WI-P2-003: Crossover milestone calculation | `dashboard/src/app/core/services/projection.service.ts` | 30-45 min | DONE (2025-12-23) |
| WI-P2-004: Crossover milestone UI | `dashboard/src/app/pages/projections/projections.html` | 15-30 min | DONE (2025-12-23) |
| WI-P2-005: Net worth curve | Multiple files | 45-60 min | DONE (2025-12-23) |

## Working State Snapshot
- Branch: master (as of 2025-12-23)
- Working tree: dirty (All Phase 2 changes uncommitted: WI-P2-001 through WI-P2-005)
- Servers: not running
- Last activity: WI-P2-003/004 Crossover Milestone completed (Claude Sonnet 4.5)

## Current Status
**Last Completed:** WI-P2-003/004 Crossover Milestone Calculation and UI
**Phase 2:** Complete! All 5 work items done.
**Branch:** master
**Ready for:** Manual testing and commit, or proceed to Phase 3 (Data Integrity)

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
