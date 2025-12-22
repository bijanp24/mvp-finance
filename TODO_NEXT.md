# TODO_NEXT.md

Last updated: 2025-12-21

Read this first when resuming work.

## Top Priority Next Step
- Claim one Phase 1 work item in `ROADMAP.md` and mark it [IN PROGRESS] in the Agent Assignment Log.
- Execute the work item and run its verification command.
- Update `WORKLOG.md` and this file with results and timestamps.

## Parallelizable Work Items (Phase 1 Quick Wins)
All items below can be done in parallel by different agents. Details and acceptance criteria live in `ROADMAP.md`.

| Work Item | File(s) | Effort | Parallelizable |
|------|---------|--------|----------------|
| WI-P1-001: Fix dashboard balance bug | `dashboard/src/app/pages/dashboard/dashboard.ts` | 5 min | Yes |
| WI-P1-002: Delete placeholder test | `FinanceEngine.Tests/UnitTest1.cs` | 1 min | Yes |
| WI-P1-003: Sync documentation timestamps | `AGENTS.md`, `PROGRESS.md`, `TODO_NEXT.md`, `WORKLOG.md`, `ROADMAP.md` | 5 min | Yes |

## Working State Snapshot
- Branch: master (as of 2025-12-21)
- Working tree: dirty (local `.claude/settings.local.json`, do not commit)
- Servers: not checked
- Last activity: Codebase analysis and priority planning (Claude Opus 4.5)

## Current Status
**Last Completed:** All roadmap modules 1-5 (settings integration, transaction edit, validation, testing, polish)
**Planning Completed:** Priority roadmap created with 5 phases (see `ROADMAP.md`)
**Branch:** master
**Ready for:** Phase 1 quick wins, then Phase 2 core features

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
