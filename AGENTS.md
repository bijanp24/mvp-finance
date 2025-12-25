# AGENTS.md

Last updated: 2025-12-25

## Purpose
- Consolidated agent guide for this repo; keep it short and actionable.
- If other agent instruction files exist, treat this as the source of truth.

## Current Version: v2 (Goal-Driven Budgeting)
- v1 (Phases 1-9) is complete and archived in `docs/v1-archive/`
- v2 focuses on budget categories, financial goals, and dynamic planning
- See `GOAL_DRIVEN_BUDGETING.md` for the full v2 vision

## Reading order
- `AGENTS.md` (this file - conventions and safety)
- `TODO_NEXT.md` (immediate context and next actions)
- `docs/v2/ROADMAP-v2.md` (work items for parallel execution)
- `docs/v2/WORKLOG-v2.md` (history and decisions)
- `docs/v2/PROGRESS-v2.md` (deep dive status and references)
- `GOAL_DRIVEN_BUDGETING.md` (v2 vision document)

## Repo overview
- .NET 10 backend: `FinanceEngine/` (core library), `FinanceEngine.Api/` (Minimal API), `FinanceEngine.Data/` (EF Core), `FinanceEngine.Tests/` (xUnit).
- Angular 21 frontend: `dashboard/` (standalone components, signals).
- SQLite database: `FinanceEngine.Api/finance.db` (generated at runtime).

## Key folders
- `FinanceEngine/` - Core library
- `FinanceEngine.Api/` - Minimal API
- `FinanceEngine.Data/` - EF Core entities
- `FinanceEngine.Tests/` - xUnit tests
- `dashboard/` - Angular 21 frontend
- `docs/v2/` - Current v2 documentation
- `docs/v1-archive/` - Archived v1 documentation
- `contracts/v2/` - Current v2 work item contracts
- `contracts/v1/` - Archived v1 contracts
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

## Agent IDE setup rules (Angular)
You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

TypeScript best practices:
- Use strict type checking.
- Prefer type inference when the type is obvious.
- Avoid the `any` type; use `unknown` when type is uncertain.

Angular best practices:
- Always use standalone components over NgModules.
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v20+.
- Use signals for state management.
- Implement lazy loading for feature routes.
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead.
- Use `NgOptimizedImage` for all static images.
- `NgOptimizedImage` does not work for inline base64 images.

Accessibility requirements:
- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.

Components:
- Keep components small and focused on a single responsibility.
- Use `input()` and `output()` functions instead of decorators.
- Use `computed()` for derived state.
- Set `changeDetection: ChangeDetectionStrategy.OnPush` in `@Component` decorator.
- Prefer inline templates for small components.
- Prefer Reactive forms instead of Template-driven ones.
- Do NOT use `ngClass`, use `class` bindings instead.
- Do NOT use `ngStyle`, use `style` bindings instead.
- When using external templates/styles, use paths relative to the component TS file.

State management:
- Use signals for local component state.
- Use `computed()` for derived state.
- Keep state transformations pure and predictable.
- Do NOT use `mutate` on signals, use `update` or `set` instead.

Templates:
- Keep templates simple and avoid complex logic.
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`.
- Use the async pipe to handle observables.
- Do not assume globals like (`new Date()`) are available.
- Do not write arrow functions in templates (they are not supported).

Services:
- Design services around a single responsibility.
- Use the `providedIn: 'root'` option for singleton services.
- Use the `inject()` function instead of constructor injection.

## Safety and no-touch areas
- Do not commit secrets or credentials.
- Do not edit `.git/`, `.claude/`, or generated folders like `bin/`, `obj/`, `dashboard/node_modules/`.
- Treat `FinanceEngine.Api/finance.db` as generated; do not hand-edit.

## Documentation rules
- Before commits, consolidate markdown files into:
  - Root: `mvp-finance.md`
  - Dashboard: `dashboard.md`
- Exceptions (do not consolidate): `CLAUDE.md`, `dashboard/CLAUDE.md`, `README.md`, `AGENTS.md`, `TODO_NEXT.md`, `GOAL_DRIVEN_BUDGETING.md`, `Orchestration.md`, and all files in `docs/` and `contracts/`
- Remove image references during consolidation.
- Do not commit images (stored externally).

## Documentation Update Protocol (Timeout Safety)

When updating multiple markdown files:
1. **START:** Edit TODO_NEXT.md first, add `## SYNC IN PROGRESS` header after title
2. **UPDATE:** Make changes to ROADMAP-v2, WORKLOG-v2, PROGRESS-v2
3. **END:** Remove `## SYNC IN PROGRESS` from TODO_NEXT.md

On session resume:
- If TODO_NEXT.md contains `## SYNC IN PROGRESS` → audit other files
- This indicates previous session timed out mid-update

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
- If parallel work exists, add a short "Parallelizable Work Items" table that references `ROADMAP-v2.md` work item IDs.
- Do not duplicate full task details here; keep details and acceptance criteria in `docs/v2/ROADMAP-v2.md`.
- Keep "Working State Snapshot" factual; use "not checked" when unknown.
- Use ASCII only; avoid special symbols and emojis.

## Multi-Agent Parallel Execution
This repo supports parallel agent workflows (e.g., Claude Code + Codex + Copilot).

**Claiming Work:**
1. Check `docs/v2/ROADMAP-v2.md` for available work items
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
- Update `docs/v2/WORKLOG-v2.md` with your changes
- Mark work item `[DONE]` in `docs/v2/ROADMAP-v2.md`
- Next agent reads TODO_NEXT.md first

## Handoff Procedure
- Run verification (tests/build/lint as applicable).
- Create a checkpoint commit with a clear message (`WIP:` allowed).
- Update `docs/v2/WORKLOG-v2.md` with branch, commit, changes, decisions, and next steps.
- Update `TODO_NEXT.md` with immediate next actions and commands to run.
- Update `docs/v2/ROADMAP-v2.md` work item status.
- Update `docs/v2/PROGRESS-v2.md` if scope or known issues changed.
- Record a working state snapshot (branch, dirty/clean, running servers).

## Handoff checklist
- [ ] Tests/build status recorded (or noted as not run)
- [ ] `docs/v2/WORKLOG-v2.md` updated with decisions and next steps
- [ ] `TODO_NEXT.md` updated with the top next actions and commands
- [ ] `docs/v2/ROADMAP-v2.md` work item status updated
- [ ] `docs/v2/PROGRESS-v2.md` updated if issues or scope changed
- [ ] Working state snapshot recorded (branch, dirty/clean, servers)
- [ ] Agent Assignment Log updated (if using parallel execution)

## Reference docs
- `mvp-finance.md` - system goals, algorithms, architecture.
- `GOAL_DRIVEN_BUDGETING.md` - v2 vision and phased approach.
- `docs/v2/PROGRESS-v2.md` - current status and known issues.
- `docs/v1-archive/` - archived v1 documentation (Phases 1-9).
- `dashboard/README.md` - Angular CLI usage.
