# TODO_NEXT.md

Read this first when resuming work.

## Current focus
**Feature:** Projections (Debt/Investment Visualization + Calendar Integration)
**Branch:** `feature/projections`
**Current Work Item:** WI-001-verify-models

## Work Items for Projections Feature
1. ✅ **WI-001-verify-models** - Verify TypeScript models and API contracts
2. **WI-002-calendar-component** - Complete CalendarComponent integration
3. **WI-003-navigation-link** - Add projections to sidebar navigation
4. **WI-004-projections-styles** - Add SCSS styles for projections page
5. **WI-005-integration-testing** - End-to-end testing and bug fixes

## Next actions (do now)
1. Create work-item branch: `git checkout -b wi/001-verify-models`
2. Check api.models.ts for missing TypeScript interfaces (SimulationResult, InvestmentProjectionResult, UserSettings, etc.)
3. Verify all API contracts match backend DTOs
4. Run `npm start` in dashboard to check for TypeScript errors
5. Commit fixes, merge to feature/projections
6. Move to WI-002

## Commands to run
```bash
# Create work-item branch
git checkout -b wi/001-verify-models

# Check for TS errors
cd dashboard && npm start

# After fixing, commit and merge
git add -A
git commit -m "feat: verify and complete TypeScript models for projections API"
git checkout feature/projections
git merge --no-ff wi/001-verify-models
git branch -d wi/001-verify-models
```

## What's already done (WIP from previous commit)
- ✅ Backend APIs: SettingsEndpoints, CalculatorEndpoints (simulation + investment projection)
- ✅ ProjectionService with chart data computed signals
- ✅ CalendarService for paycheck/debt due date calculations
- ✅ Chart components (DebtProjectionChartComponent, InvestmentProjectionChartComponent)
- ✅ ProjectionsPage component with time range toggle
- ✅ CalendarComponent files created (need to verify completeness)
- ✅ Routes configured
- ✅ Dependencies installed (ngx-echarts)
