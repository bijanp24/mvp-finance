# AGENTS.md

Last updated: 2025-12-21

## Purpose
- Consolidated agent guide for this repo; keep it short and actionable.
- If other agent instruction files exist, treat this as the source of truth.

## Reading order
- `AGENTS.md` (this file - conventions and safety)
- `TODO_NEXT.md` (immediate context and next actions)
- `ROADMAP.md` (work items for parallel execution)
- `WORKLOG.md` (history and decisions)
- `PROGRESS.md` (deep dive status and references)

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
- Use strict typing; avoid `any` and prefer inference when types are obvious.
- Use the async pipe for observables.
- Use `providedIn: 'root'` for singleton services.
- Prefer lazy loading for feature routes when possible.

## Safety and no-touch areas
- Do not commit secrets or credentials.
- Do not edit `.git/`, `.claude/`, or generated folders like `bin/`, `obj/`, `dashboard/node_modules/`.
- Treat `FinanceEngine.Api/finance.db` as generated; do not hand-edit.

## Documentation rules
- Before commits, consolidate markdown files into:
  - Root: `mvp-finance.md`
  - Dashboard: `dashboard.md`
- Exceptions (do not consolidate): `CLAUDE.md`, `dashboard/CLAUDE.md`, `README.md`, `AGENTS.md`, `WORKLOG.md`, `TODO_NEXT.md`, `PROGRESS.md`, `ROADMAP.md`
- Remove image references during consolidation.
- Do not commit images (stored externally).

## Commit message format
Use conventional commit format:
```
<type>: <short description>

<optional body>

<optional footer>
```

Types:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `chore:` Maintenance tasks
- `refactor:` Code refactoring
- `test:` Test additions/changes
- `style:` Code style changes (formatting, etc.)

Footer (when applicable):
```
Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

## TODO_NEXT.md update rules
- Keep "Top Priority Next Step" to 1-3 atomic tasks with commands when possible.
- If parallel work exists, add a short "Parallelizable Work Items" table that references `ROADMAP.md` work item IDs.
- Do not duplicate full task details here; keep details and acceptance criteria in `ROADMAP.md`.
- Keep "Working State Snapshot" factual; use "not checked" when unknown.
- Use ASCII only; avoid special symbols and emojis.

## Multi-Agent Parallel Execution
This repo supports parallel agent workflows (e.g., Claude Code + Codex + Copilot).

**Claiming Work:**
1. Check `ROADMAP.md` for available work items
2. Update the Agent Assignment Log before starting
3. Mark work item `[IN PROGRESS]` with your identifier
4. Avoid items with unmet dependencies

**Avoiding Conflicts:**
- Items marked `Parallelizable: Yes` are safe to run simultaneously
- Backend and frontend for same feature: run sequentially
- Test files can be created in parallel (different files)
- Documentation updates: coordinate or batch at end

**Handoff Between Agents:**
- Complete your work item fully before handing off
- Update WORKLOG.md with your changes
- Mark work item `[DONE]` in ROADMAP.md
- Next agent reads TODO_NEXT.md first

## Handoff Procedure
- Run verification (tests/build/lint as applicable).
- Create a checkpoint commit with a clear message (`WIP:` allowed).
- Update `WORKLOG.md` with branch, commit, changes, decisions, and next steps.
- Update `TODO_NEXT.md` with immediate next actions and commands to run.
- Update `ROADMAP.md` work item status.
- Update `PROGRESS.md` if scope or known issues changed.
- Record a working state snapshot (branch, dirty/clean, running servers).

## Handoff checklist
- [ ] Tests/build status recorded (or noted as not run)
- [ ] `WORKLOG.md` updated with decisions and next steps
- [ ] `TODO_NEXT.md` updated with the top next actions and commands
- [ ] `ROADMAP.md` work item status updated
- [ ] `PROGRESS.md` updated if issues or scope changed
- [ ] Working state snapshot recorded (branch, dirty/clean, servers)
- [ ] Agent Assignment Log updated (if using parallel execution)

## Reference docs
- `mvp-finance.md` - system goals, algorithms, architecture.
- `PROGRESS.md` - current status and known issues.
- `dashboard/README.md` - Angular CLI usage.
