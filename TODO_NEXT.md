# TODO_NEXT.md

Last updated: 2025-12-30

Read this first when resuming work.

## Top Priority Next Step
- **v2 COMPLETE:** All phases (A-D) fully implemented
- **v3 Available:** Data Import/Export - see `docs/v3/ROADMAP-v3.md`
- **v4 Planned:** Cloud Integrations - see `docs/v4/ROADMAP-v4.md`

## Current Status

**Branch:** master
**Tests:** 316 backend tests passing

## Version Summary

| Version | Focus | Status |
|---------|-------|--------|
| v1 | Core Transaction Tracking | COMPLETE (archived) |
| v2 | Goal-Driven Budgeting | COMPLETE (Phases A-D) |
| v3 | Data Import/Export | Planned (v3.1-v3.4) |
| v4 | Cloud Integrations | Planned (Google + Microsoft) |

## v2: Goal-Driven Budgeting

**Phase A Complete:** Budget Categories (2025-12-29)
- Category and Budget entities with migrations
- Full CRUD API endpoints with tests
- Transaction category tagging
- Budget management UI
- Dashboard spending breakdown widget
- Calendar budget markers

**Phase B Complete:** Financial Goals (2025-12-29)
- Goal entity with 4 types (DebtFree, InvestmentTarget, SavingsGoal, NetWorthMilestone)
- Goal progress calculator with status tracking
- Full CRUD API with progress endpoints
- Goals page with progress bars and status indicators
- Dashboard goal widget (top 3 by priority)
- Projections goal integration

**Phase C Complete:** Dynamic Safe-to-Spend (2025-12-30)
- SafeToSpendCalculator with status determination (Healthy/Tight/AtRisk/Behind)
- BudgetAnalysisCalculator for overspending detection
- AdjustmentSuggestionCalculator for smart recommendations
- Dashboard hero widget with live Safe-to-Spend display
- Settings page with buffer, time horizon configuration
- Suggestions panel with potential savings

**Phase D Complete:** Scenario Planning (2025-12-30)
- ScenarioCalculator for what-if analysis
- Scenarios page with interactive sliders
- Monthly discretionary, extra debt payment, extra investment sliders
- Real-time timeline chart visualization
- Net worth projections

## v3: Data Import/Export

**Phase v3.1:** Core Spreadsheet Exports (CSV, Excel)
**Phase v3.2:** PDF Chart Export
**Phase v3.3:** Extended Data Exports
**Phase v3.4:** Transaction Import (CSV/Excel from bank statements)

See `docs/v3/EXPORT_FEATURES.md` for full vision.

## v4: Cloud Integrations

**Phase v4.1:** Google Workspace (OAuth, Sheets, Docs, Drive)
**Phase v4.2:** Microsoft 365 (OAuth, Excel Online, Word, OneDrive)
**Phase v4.3:** Report Templates (optional)

See `docs/v4/CLOUD_INTEGRATIONS.md` for full vision.

## File Structure

```
docs/
  v1-archive/         # Archived v1 docs
  v2/                 # Goal-Driven Budgeting
  v3/                 # Data Import/Export
  v4/                 # Cloud Integrations

contracts/
  v1/                 # Archived v1 contracts
  v2/                 # v2 contracts
```

## Key Entry Points

- v2 Vision: `contracts/v2/GOAL_DRIVEN_BUDGETING.md`
- v3 Vision: `docs/v3/EXPORT_FEATURES.md`
- v4 Vision: `docs/v4/CLOUD_INTEGRATIONS.md`
- Conventions: `AGENTS.md`

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
