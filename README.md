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

## Documentation
- Product goals and architecture: `mvp-finance.md`
- Current state and known issues: `PROGRESS.md`
- Task backlog: `ROADMAP.md`
- Immediate next steps: `TODO_NEXT.md`
- Work history: `WORKLOG.md`
- Dashboard notes: `dashboard/dashboard.md`
- Angular CLI reference: `dashboard/README.md`

## Workflow Notes
- Preferred branches: `feature/<name>` and `wi/<ticket>-<slug>`
- Each task maps to one commit; WIP commits are allowed
