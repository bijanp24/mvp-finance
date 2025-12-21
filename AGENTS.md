# AGENTS.md

## Purpose
- Consolidated agent guide for this repo; keep it short and actionable.
- If other agent instruction files exist, treat this as the source of truth.

## Repo overview
- .NET 10 backend: `FinanceEngine/` (core library), `FinanceEngine.Api/` (Minimal API), `FinanceEngine.Data/` (EF Core), `FinanceEngine.Tests/` (xUnit).
- Angular 21 frontend: `dashboard/` (standalone components, signals).
- SQLite database: `FinanceEngine.Api/finance.db` (generated at runtime).

## Key folders
- `FinanceEngine/`
- `FinanceEngine.Api/`
- `FinanceEngine.Data/`
- `FinanceEngine.Tests/`
- `dashboard/`
- `.claude/` (local settings; do not edit or commit)

## Build/Test/Run
Backend:
- `dotnet build`
- `dotnet test`
- `dotnet run --project FinanceEngine.Api`

Frontend:
- `cd dashboard`
- `npm install`
- `npm start`
- `npm run build`
- `npm test`

## Lint/Format
- No dedicated lint/format commands are documented; use project defaults if added.

## Branching and PR workflow (preferred)
- Feature branches: `feature/<name>`
- Work-item branches (off feature): `wi/<ticket>-<slug>`
- Each task roughly maps to one commit; `WIP:` commits are allowed for checkpoints.
- Keep feature history linear: squash/rebase work-item branches before merging.
- Final PR merges feature -> main/master.

## Backend conventions
- Follow the Clean Architecture/DDD intent and event-sourcing model in `mvp-finance.md`.
- Keep Minimal API endpoints focused and small.

## Frontend conventions (dashboard)
- Standalone components only (do not set `standalone: true` in decorators).
- Use `ChangeDetectionStrategy.OnPush`.
- Use signals for state; use `computed()` for derived values; do not use `mutate`.
- Use `input()` / `output()` functions instead of decorators.
- Prefer Reactive Forms.
- Use `inject()` instead of constructor injection.
- Avoid `@HostBinding` / `@HostListener`; use the `host` metadata instead.
- Avoid `ngClass` / `ngStyle`; use `class` / `style` bindings.
- Use native control flow: `@if`, `@for`, `@switch`.
- No arrow functions in templates; do not assume globals like `new Date()` in templates.
- Use `NgOptimizedImage` for static images (no base64 inline).
- Accessibility must pass AXE checks and WCAG AA.

## Safety and no-touch areas
- Do not commit secrets or credentials.
- Do not edit `.git/`, `.claude/`, or generated folders like `bin/`, `obj/`, `dashboard/node_modules/`.
- Treat `FinanceEngine.Api/finance.db` as generated; do not hand-edit.

## Documentation rules
- Before commits, consolidate markdown files into:
  - Root: `mvp-finance.md`
  - Dashboard: `dashboard.md`
- Exceptions (do not consolidate): `CLAUDE.md`, `dashboard/CLAUDE.md`, `README.md`, `AGENTS.md`, `WORKLOG.md`, `TODO_NEXT.md`, `PROGRESS.md`
- Remove image references during consolidation.
- Do not commit images (stored externally).

## Handoff Procedure
- Run verification (tests/build/lint as applicable).
- Create a checkpoint commit with a clear message (`WIP:` allowed).
- Update `WORKLOG.md` with branch, commit, changes, decisions, and next steps.
- Update `TODO_NEXT.md` with immediate next actions and commands to run.

## Reference docs
- `mvp-finance.md` - system goals, algorithms, architecture.
- `PROGRESS.md` - current status and known issues.
- `dashboard/README.md` - Angular CLI usage.
