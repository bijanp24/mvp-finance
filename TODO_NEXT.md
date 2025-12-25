# TODO_NEXT.md

Last updated: 2025-12-25

Read this first when resuming work.

## Top Priority Next Step
- **Documentation Reorganized:** v1 archived, v2 structure in place.
- **Next:** Begin Phase A implementation (Budget Categories)
- **Start with:** WI-PA-001 (Category Entity) - see `docs/v2/ROADMAP-v2.md`

## Current Status

**Version:** v2 (Goal-Driven Budgeting)
**Branch:** master
**Working tree:** dirty (reorganization in progress)

**v1 Complete:** Phases 1-9 archived in `docs/v1-archive/`
- 156 tests (117 backend + 39 frontend)
- Full transaction tracking with reconciliation
- Debt/investment projections with scenario slider
- Recurring contributions
- Dark theme redesign

**v2 In Progress:** Phase A (Budget Categories)
- See `GOAL_DRIVEN_BUDGETING.md` for full vision
- See `docs/v2/ROADMAP-v2.md` for work items

## v2 Phase A Work Items

| Work Item | Description | Status | Parallelizable |
|-----------|-------------|--------|----------------|
| WI-PA-001 | Category Entity | [ ] | No (start here) |
| WI-PA-002 | Budget Entity | [ ] | No |
| WI-PA-003 | Category API | [ ] | Yes (after 001) |
| WI-PA-004 | Budget API | [ ] | Yes (after 002) |
| WI-PA-005 | Transaction Tagging | [ ] | Yes (after 001) |
| WI-PA-006 | Budget Management UI | [ ] | No |
| WI-PA-007 | Category Picker UI | [ ] | Yes |
| WI-PA-008 | Dashboard Widget | [ ] | Yes |
| WI-PA-009 | Calendar Markers | [ ] | Yes |

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
