# MVP Finance

Local-first personal finance dashboard with a .NET engine and an Angular UI. The goal is to make "spendable now," compounding velocity, and payoff progress visible in real time.

## Highlights
- Event-sourced ledger for cash, debt, and investment accounts
- Spendable now and burn rate calculations
- Debt payoff and investment projection charts
- Settings-driven calendar and dashboard views

## Tech Stack
- Backend: .NET 10 Minimal API, EF Core, SQLite
- Core library: FinanceEngine calculators and models
- Frontend: Angular 21 with standalone components and signals

## Repository Structure
- `FinanceEngine/` core calculation library
- `FinanceEngine.Api/` Minimal API host
- `FinanceEngine.Data/` EF Core entities and DbContext
- `FinanceEngine.Tests/` xUnit tests
- `dashboard/` Angular UI

## Quick Start

Prerequisites:
- .NET 10 SDK
- Node.js LTS and npm

Backend:
```bash
dotnet build
dotnet test
dotnet run --project FinanceEngine.Api
```
API runs at `http://localhost:5285` by default.

Frontend:
```bash
cd dashboard
npm install
npm start
```
The dashboard runs at `http://localhost:4200` and proxies `/api` to `http://localhost:5285`.

## Database
SQLite database is generated at runtime:
- `FinanceEngine.Api/finance.db`
Do not hand-edit or commit this file.

## Tests
- Backend: `dotnet test`
- Frontend: `cd dashboard && npm test` (not implemented yet)

---

## Multi-Agent Development Pipeline

This repository serves both as a personal finance application and as a practical framework for a continuous, multi-agent development workflow.

### Goal

Sustain delivery by aligning tasks to each tool's strengths and rotating when rate limits or service constraints are reached.

### Principles

- Fit the tool to the task, not the other way around
- Keep handoff documentation current and concise
- Prefer small, verifiable changes over large, risky batches
- Maintain redundancy to avoid single-tool bottlenecks

### Tooling Portfolio

| Tool | Monthly Cost | Primary Strengths |
|------|--------------|-------------------|
| **Claude Code** | $17-100 | Deep reasoning, system design, complex refactors |
| **ChatGPT Codex** | $20 | Documentation, summarization, independent review |
| **GitHub Copilot** | $8-10 | Inline completions, fast single-file changes |
| **Cursor** | $20 | Orchestration, parallel worker execution |

### Complementary Roles

- **Claude Code + Codex**: Analyst and editor. Claude explores deeply; Codex distills findings into clear documentation and validates reasoning.
- **Copilot + Cursor**: Micro and macro. Copilot focuses on line-level edits; Cursor coordinates parallel tasks.
- **Rotation**: When one tool is throttled, switch to another. The handoff documents preserve continuity.

### Cost Efficiency

Instead of scaling a single tool to $100, a blended stack ($17 Claude Code + $8 Copilot + $20 Cursor + $20 Codex = $65) provides:
- More total capacity per day
- Specialized strengths per task
- Redundancy when one service slows or fails

---

## Documentation Architecture

The markdown files form a structured handoff system designed for multi-agent workflows.

### File Purposes

| File | Purpose | When to Read |
|------|---------|--------------|
| `CLAUDE.md` | Auto-load pointer for Claude Code | Automatically loaded |
| `AGENTS.md` | Source of truth for conventions, rules, Angular/backend standards | First, when starting any work |
| `TODO_NEXT.md` | Immediate next actions, parallelizable work items | Second, to pick a task |
| `ROADMAP.md` | Structured work items with dependencies and verification commands | To claim and execute work |
| `WORKLOG.md` | Append-only history of completed work | For context on past decisions |
| `PROGRESS.md` | Deep dive: feature inventory, known issues, architecture notes | For detailed context |
| `Orchestration.md` | Formal multi-agent SOP for Cursor parallel execution | When running parallel workers |
| `mvp-finance.md` | Product spec: goals, algorithms, domain concepts | For understanding the "why" |
| `README.md` | This file: quick start and pipeline philosophy | For new contributors |

### Reading Order for New Agents

1. `AGENTS.md` (conventions and safety rules)
2. `TODO_NEXT.md` (what to do next)
3. `ROADMAP.md` (detailed work items)
4. `WORKLOG.md` (history and context)
5. `PROGRESS.md` (deep dive if needed)

### Handoff Protocol

Before ending a session, agents must:
1. Run verification (tests/build)
2. Update `WORKLOG.md` with changes and decisions
3. Update `TODO_NEXT.md` with next actions
4. Update `ROADMAP.md` work item status
5. Record a working state snapshot (branch, dirty/clean, servers)

This ensures any agent can resume work without asking "what was I doing?"

---

## Standard Workflow

### Solo Agent
1. Read `TODO_NEXT.md`
2. Pick a work item from `ROADMAP.md`
3. Mark it `[IN PROGRESS]` in the Agent Assignment Log
4. Complete the work
5. Run verification
6. Mark it `[DONE]` and update handoff docs

### Parallel Agents (Cursor)
1. Orchestrator reads all handoff docs
2. Orchestrator generates worker contracts (see `Orchestration.md`)
3. Each worker gets one bounded task on its own branch
4. Workers report results
5. Orchestrator integrates and updates docs

---

## Workflow Notes
- Preferred branches: `feature/<name>` and `wi/<ticket>-<slug>`
- Each task maps to one commit; WIP commits are allowed
