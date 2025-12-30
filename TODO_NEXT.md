# TODO_NEXT.md

Last updated: 2025-12-29

Read this first when resuming work.

## Top Priority Next Step
- **Phase A Complete:** All 9 work items done (Budget Categories)
- **Phase B Complete:** 7/8 work items done (Financial Goals)
- **Next:** Phase C (Dynamic Safe-to-Spend) - see `docs/v2/ROADMAP-v2.md`

## Current Status

**Version:** v2 (Goal-Driven Budgeting)
**Branch:** master
**Tests:** 236 backend tests passing

**v1 Complete:** Phases 1-9 archived in `docs/v1-archive/`
- 156 tests (117 backend + 39 frontend)
- Full transaction tracking with reconciliation
- Debt/investment projections with scenario slider
- Recurring contributions
- Dark theme redesign

**Phase A Complete:** Budget Categories (2025-12-29)
- Category and Budget entities with migrations
- Full CRUD API endpoints with tests
- Transaction category tagging
- Budget management UI
- Dashboard spending breakdown widget
- Calendar budget markers

**Phase B Complete:** Financial Goals (2025-12-29)
- Goal entity with 4 types (DebtFree, InvestmentTarget, SavingsGoal, NetWorthMilestone)
- Goal progress calculator with status tracking (OnTrack, Ahead, AtRisk, Behind)
- Full CRUD API with progress endpoints
- Goals page with progress bars and status indicators
- Goal create/edit dialog with type-specific fields
- Dashboard goal widget (top 3 by priority)
- Projections goal integration (milestones in timeframe)
- 39 new tests (19 calculator + 20 endpoints)

**v2 Next:** Phase C (Dynamic Safe-to-Spend) - TO BE DEFINED
- See `contracts/v2/GOAL_DRIVEN_BUDGETING.md` for vision

## v2 Phase B Work Items (Financial Goals) - COMPLETE

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

## v2 Phase A Work Items (COMPLETE)

| Work Item | Description | Status |
|-----------|-------------|--------|
| WI-PA-001 | Category Entity | [DONE] |
| WI-PA-002 | Budget Entity | [DONE] |
| WI-PA-003 | Category API | [DONE] |
| WI-PA-004 | Budget API | [DONE] |
| WI-PA-005 | Transaction Tagging | [DONE] |
| WI-PA-006 | Budget Management UI | [DONE] |
| WI-PA-007 | Category Picker UI | [DONE] |
| WI-PA-008 | Dashboard Widget | [DONE] |
| WI-PA-009 | Calendar Markers | [DONE] |

## File Structure (v2)

```
docs/
  v1-archive/         # Archived: ROADMAP-v1, WORKLOG-v1, PROGRESS-v1
  v2/                 # Current: ROADMAP-v2, WORKLOG-v2, PROGRESS-v2

contracts/
  v1/                 # Archived v1 contracts
  v2/                 # New v2 contracts
  .system_prompt.md   # Worker agent prompt
```

## Key Entry Points

- Vision: `GOAL_DRIVEN_BUDGETING.md`
- Work Items: `docs/v2/ROADMAP-v2.md`
- Conventions: `AGENTS.md`
- Multi-Agent: `Orchestration.md`

## Commands Reference

```bash
# Backend
dotnet build && dotnet test
dotnet run --project FinanceEngine.Api

# Frontend
cd dashboard && npm run build && npm test
cd dashboard && npm start

# Full verification
dotnet build && dotnet test && cd dashboard && npm run build
```
