# MVP Finance - Onboarding Guide

Welcome to MVP Finance! This document will help you get up and running with the project, understand the architecture, and become productive quickly.

Last updated: 2025-12-25

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Prerequisites](#prerequisites)
3. [Quick Start](#quick-start)
4. [Repository Structure](#repository-structure)
5. [Architecture](#architecture)
6. [Tech Stack](#tech-stack)
7. [Development Workflow](#development-workflow)
8. [Multi-Agent Development Pipeline](#multi-agent-development-pipeline)
9. [Documentation System](#documentation-system)
10. [Coding Conventions](#coding-conventions)
11. [Testing](#testing)
12. [Common Tasks](#common-tasks)
13. [Troubleshooting](#troubleshooting)
14. [Next Steps](#next-steps)

---

## Project Overview

MVP Finance is a **local-first personal finance dashboard** that provides real-time visibility into your financial health. The goal is to make "spendable now," compounding velocity, and payoff progress visible in real time.

### Core Philosophy

The application answers three critical questions:
1. **"How much can I spend until my next paycheck?"** - Real-time spendable balance
2. **"Is compounding working for me or against me?"** - Investment vs. debt visualization
3. **"How fast am I paying things down?"** - Debt payoff velocity and projections

### Key Features

- **Event-sourced ledger** for cash, debt, and investment accounts
- **Spendable now** and burn rate calculations
- **Debt payoff and investment projection** charts with scenario analysis
- **Recurring contributions** and recurring transaction support
- **Calendar view** showing income, debt payments, and contributions
- **Dark theme** with modern Material Design UI

### Current Status

- **v1 Complete:** Phases 1-9 are fully implemented (156 tests, full CRUD, projections, dark theme)
- **v2 In Progress:** Goal-Driven Budgeting (budget categories, financial goals, dynamic planning)
- **v3 Planned:** Data export features (CSV, Excel, PDF)

---

## Prerequisites

Before you begin, ensure you have the following installed:

### Required Software

- **.NET 10 SDK** - Backend runtime
  - Check: `dotnet --version` (should show 10.0.x)
  - Download: https://dotnet.microsoft.com/download/dotnet/10.0

- **Node.js LTS** - Frontend runtime (v20.x recommended)
  - Check: `node --version` (should show v20.x)
  - Download: https://nodejs.org/

- **npm** - Package manager (v10.x+)
  - Check: `npm --version` (should show 10.x or 11.x)
  - Comes with Node.js

### Recommended Tools

- **Git** - Version control
- **VS Code** or **Visual Studio 2022** - IDEs with excellent .NET and Angular support
- **VS Code Extensions** (if using VS Code):
  - C# Dev Kit
  - Angular Language Service
  - ESLint
  - Prettier

---

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/bijanp24/mvp-finance.git
cd mvp-finance
```

### 2. Backend Setup

```bash
# Build the backend
dotnet build

# Run tests to verify setup
dotnet test

# Start the API server (runs on http://localhost:5285)
dotnet run --project FinanceEngine.Api
```

The API will be available at `http://localhost:5285`.

### 3. Frontend Setup

Open a new terminal window:

```bash
# Navigate to dashboard directory
cd dashboard

# Install dependencies
npm install

# Start the development server (runs on http://localhost:4200)
npm start
```

The dashboard will automatically open in your browser at `http://localhost:4200`.

### 4. Verify Everything Works

- The dashboard should load and show the main interface
- The API proxy is configured to forward `/api` requests to `http://localhost:5285`
- Try creating a test account or transaction to verify the full stack is working

---

## Repository Structure

```
mvp-finance/
├── FinanceEngine/              # Core calculation library (pure logic, no I/O)
│   ├── Calculators/            # Financial calculation algorithms
│   │   ├── BurnRateCalculator.cs
│   │   ├── DebtAllocationCalculator.cs
│   │   ├── ForwardSimulationEngine.cs
│   │   ├── InvestmentProjectionCalculator.cs
│   │   └── SpendableCalculator.cs
│   ├── Models/                 # Domain models and records
│   │   ├── Inputs/             # Calculator input models
│   │   └── Outputs/            # Calculator output models
│   └── Services/               # Core services
│       ├── BalanceCalculator.cs
│       └── RecurringEventExpansionService.cs
│
├── FinanceEngine.Api/          # Minimal API host
│   ├── Endpoints/              # API endpoint groups
│   │   ├── AccountEndpoints.cs
│   │   ├── CalculatorEndpoints.cs
│   │   ├── EventEndpoints.cs
│   │   ├── RecurringContributionEndpoints.cs
│   │   └── SettingsEndpoints.cs
│   ├── Models/                 # API DTOs
│   ├── Services/               # Application services
│   ├── Program.cs              # Application entry point
│   └── finance.db              # SQLite database (generated at runtime)
│
├── FinanceEngine.Data/         # EF Core persistence layer
│   ├── Entities/               # Database entities
│   │   ├── AccountEntity.cs
│   │   ├── FinancialEventEntity.cs
│   │   ├── IncomeScheduleEntity.cs
│   │   ├── RecurringContributionEntity.cs
│   │   └── UserSettingsEntity.cs
│   ├── Migrations/             # EF Core migrations
│   ├── Repositories/           # Repository pattern implementations
│   └── FinanceDbContext.cs     # EF Core DbContext
│
├── FinanceEngine.Tests/        # xUnit tests (117 tests)
│   ├── Calculators/            # Calculator tests
│   ├── Endpoints/              # API endpoint tests
│   └── Services/               # Service tests
│
├── dashboard/                  # Angular 21 frontend (39 tests)
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/     # UI components
│   │   │   ├── models/         # TypeScript interfaces/types
│   │   │   ├── services/       # Angular services (API clients)
│   │   │   └── app.routes.ts   # Route configuration
│   │   ├── styles.scss         # Global styles
│   │   └── main.ts             # Application bootstrap
│   ├── public/                 # Static assets
│   ├── package.json            # npm dependencies
│   └── angular.json            # Angular CLI configuration
│
├── docs/                       # Documentation
│   ├── v1-archive/             # v1 phase documentation (complete)
│   ├── v2/                     # v2 documentation (in progress)
│   │   ├── ROADMAP-v2.md       # v2 work items
│   │   ├── WORKLOG-v2.md       # v2 history
│   │   └── PROGRESS-v2.md      # v2 status
│   └── v3/                     # v3 documentation (planned)
│
├── contracts/                  # Work item contracts for parallel execution
│   ├── v1/                     # v1 contracts (archived)
│   ├── v2/                     # v2 contracts
│   │   └── GOAL_DRIVEN_BUDGETING.md  # v2 vision document
│   └── .system_prompt.md       # Worker agent prompt template
│
├── README.md                   # Quick start and project overview
├── AGENTS.md                   # Agent conventions and standards (SOURCE OF TRUTH)
├── TODO_NEXT.md                # Immediate next actions
├── ONBOARDING.md              # This file
├── Orchestration.md            # Multi-agent execution protocol
├── mvp-finance.md             # Product specification and algorithms
└── mvp-finance.sln            # .NET solution file
```

### Key Files to Know

| File | Purpose |
|------|---------|
| `AGENTS.md` | **Source of truth** for conventions, rules, and standards |
| `TODO_NEXT.md` | What to work on next (read this first when starting work) |
| `mvp-finance.md` | Product spec, algorithms, domain concepts |
| `README.md` | Quick start guide and multi-agent pipeline philosophy |
| `Orchestration.md` | Protocol for parallel multi-agent execution |
| `docs/v2/ROADMAP-v2.md` | Current work items with dependencies |
| `docs/v2/WORKLOG-v2.md` | History of completed work and decisions |
| `docs/v2/PROGRESS-v2.md` | Deep dive into current status and known issues |

---

## Architecture

### Architectural Layers

The application follows **Clean Architecture** and **Domain-Driven Design** principles:

```
┌─────────────────────────────────────────────────┐
│         Angular UI (dashboard/)                 │
│  Standalone Components + Signals + Material     │
└─────────────────────┬───────────────────────────┘
                      │ HTTP/REST
┌─────────────────────▼───────────────────────────┐
│       FinanceEngine.Api (Minimal API)           │
│    Endpoints + DTOs + Application Services      │
└─────────────────────┬───────────────────────────┘
                      │
          ┌───────────┴──────────┐
          │                      │
┌─────────▼──────────┐  ┌───────▼────────────────┐
│  FinanceEngine     │  │  FinanceEngine.Data    │
│  (Core Library)    │  │  (EF Core + SQLite)    │
│                    │  │                        │
│  • Calculators     │  │  • Entities            │
│  • Domain Models   │  │  • DbContext           │
│  • Pure Logic      │  │  • Migrations          │
│  • No Dependencies │  │  • Repositories        │
└────────────────────┘  └────────────────────────┘
```

### Design Patterns

**Event Sourcing:**
- Account balances are computed from transaction history, not stored directly
- All state changes flow through `FinancialEventEntity` records
- Enables complete audit trail and time-travel debugging

**Calculator Pattern:**
- Static methods in `FinanceEngine.Calculators`
- Record-based inputs and outputs
- Pure functions with no side effects
- Easily testable

**Repository Pattern:**
- Abstract data access behind interfaces
- Currently implemented with EF Core
- Could be swapped for other persistence mechanisms

**Minimal API with Endpoint Groups:**
- Endpoints organized by feature (`AccountEndpoints`, `EventEndpoints`, etc.)
- Lightweight, performant alternative to MVC controllers
- Maps to `/api/accounts`, `/api/events`, etc.

### Key Design Decisions

**APR Storage:**
- Store as decimal (0.0499 = 4.99%), NOT as percentage values
- Prevents common calculation errors

**EF Core Queries:**
- Always materialize with `.ToListAsync()` before mapping to domain models
- Prevents deferred execution issues

**Component State:**
- Use Angular signals for reactive state management
- Use `computed()` for derived state
- Avoid `mutate()` - use `update()` or `set()` instead

**Standalone Components:**
- Angular 21 defaults to standalone (no NgModules)
- Do NOT set `standalone: true` in decorators (it's implicit)
- Use `input()` and `output()` functions instead of decorators

---

## Tech Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 10.0 | Runtime and framework |
| **C#** | Latest | Programming language |
| **Entity Framework Core** | Latest | ORM for database access |
| **SQLite** | Latest | Database (embedded) |
| **xUnit** | Latest | Testing framework |
| **Minimal API** | .NET 10 | Web API framework |

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| **Angular** | 21.0 | Frontend framework |
| **TypeScript** | 5.9 | Programming language |
| **Angular Material** | 21.0 | UI component library |
| **ECharts** | 5.6 | Charting library (via ngx-echarts) |
| **RxJS** | 7.8 | Reactive programming |
| **Jest** | 30.2 | Testing framework |
| **Testing Library** | 18.1 | Component testing utilities |

### Development Tools

| Tool | Purpose |
|------|---------|
| **Angular CLI** | Project scaffolding and build |
| **npm** | Package management |
| **dotnet CLI** | Build, test, and run .NET apps |
| **EF Core CLI** | Database migrations |
| **Git** | Version control |

---

## Development Workflow

### Reading Order (When Starting Work)

Always read these files in order:

1. **`AGENTS.md`** - Conventions, safety rules, coding standards
2. **`TODO_NEXT.md`** - Immediate context and next actions
3. **`docs/v2/ROADMAP-v2.md`** - Detailed work items (if starting new feature)
4. **`docs/v2/WORKLOG-v2.md`** - History and context (if needed)
5. **`docs/v2/PROGRESS-v2.md`** - Deep dive status (if needed)

### Standard Workflow (Solo Developer)

1. **Read** `TODO_NEXT.md` to understand current state
2. **Pick** a work item from `docs/v2/ROADMAP-v2.md`
3. **Mark** it `[IN PROGRESS]` in the Agent Assignment Log
4. **Implement** the feature
5. **Run** verification commands (build, tests, lint)
6. **Mark** it `[DONE]` and update `docs/v2/WORKLOG-v2.md`
7. **Update** `TODO_NEXT.md` with next actions
8. **Commit** with conventional commit format

### Branching Strategy

- **Main branch:** `master` (stable, production-ready)
- **Feature branches:** `feature/<name>` (for larger initiatives)
- **Work item branches:** `wi/<ticket>-<slug>` (for specific tasks)
- Each task roughly maps to one commit
- `WIP:` commits are allowed for checkpoints
- Keep feature history linear: squash/rebase work-item branches before merging

### Commit Message Format

Use conventional commits:

```
<type>: <short description>

<optional body>

<optional footer>
```

**Types:**
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `chore:` Maintenance tasks
- `refactor:` Code refactoring
- `test:` Test additions/changes
- `style:` Code style changes

**Example:**
```
feat: add budget category entity and migration

Created CategoryEntity with Name, Type, Icon, Color, IsActive fields.
Added EF migration and seeded default categories.

Generated with [GitHub Copilot](https://github.com/features/copilot)
```

---

## Multi-Agent Development Pipeline

This repository is designed for efficient multi-agent development using AI coding assistants.

### Supported Agents

| Agent | Cost/Month | Primary Strengths |
|-------|------------|-------------------|
| **Claude Code** | $17-100 | Deep reasoning, system design, complex refactors |
| **ChatGPT Codex** | $20 | Documentation, summarization, independent review |
| **GitHub Copilot** | $8-10 | Inline completions, fast single-file changes |
| **Cursor** | $20 | Orchestration, parallel worker execution |

### Parallel Execution Protocol

See `Orchestration.md` for complete details. Quick overview:

**Roles:**
- **Orchestrator:** Plans tasks, creates worker contracts, integrates results
- **Worker Agents:** Execute one bounded work item each

**Golden Rules:**
- One branch per work item
- No two workers modify the same file
- Shared/central files are owned only by the Orchestrator
- Workers must not expand scope

**Process:**
1. Orchestrator reads all handoff docs
2. Orchestrator generates worker contracts from `docs/v2/ROADMAP-v2.md`
3. Each worker gets one bounded task on its own branch
4. Workers report results with verification evidence
5. Orchestrator integrates and updates docs

### Claiming Work Items

Before starting work:
1. Check `docs/v2/ROADMAP-v2.md` for available items
2. Verify dependencies are met
3. Update Agent Assignment Log with your identifier
4. Mark work item `[IN PROGRESS]`
5. Only work on items marked `Parallelizable: Yes` if others are active

---

## Documentation System

### File Purposes

| File | Purpose | When to Read |
|------|---------|--------------|
| `AGENTS.md` | Conventions, rules, standards | **Always first** |
| `TODO_NEXT.md` | Immediate next actions | **Second** |
| `docs/v2/ROADMAP-v2.md` | Work items with dependencies | When picking tasks |
| `docs/v2/WORKLOG-v2.md` | Append-only history | For context on decisions |
| `docs/v2/PROGRESS-v2.md` | Deep status and known issues | When troubleshooting |
| `mvp-finance.md` | Product spec and algorithms | Understanding "why" |
| `Orchestration.md` | Multi-agent SOP | When coordinating parallel work |
| `ONBOARDING.md` | This file | For new contributors |

### Documentation Update Protocol

When updating multiple markdown files:

1. **START:** Edit `TODO_NEXT.md` first, add `## SYNC IN PROGRESS` header
2. **UPDATE:** Make changes to other files
3. **END:** Remove `## SYNC IN PROGRESS` from `TODO_NEXT.md`

This prevents inconsistencies if a session times out mid-update.

### Handoff Checklist

Before ending a work session:

- [ ] Tests/build status recorded
- [ ] `docs/v2/WORKLOG-v2.md` updated with decisions
- [ ] `TODO_NEXT.md` updated with next actions
- [ ] `docs/v2/ROADMAP-v2.md` work item status updated
- [ ] `docs/v2/PROGRESS-v2.md` updated if scope changed
- [ ] Working state snapshot recorded (branch, dirty/clean, servers)
- [ ] Agent Assignment Log updated (if using parallel execution)

---

## Coding Conventions

### Backend (.NET / C#)

**General:**
- Follow Clean Architecture and DDD principles
- Keep Minimal API endpoints focused and small
- Use record types for immutable data (inputs/outputs)
- Enable nullable reference types (`<Nullable>enable</Nullable>`)

**FinanceEngine (Core Library):**
- Pure calculation logic only
- No I/O, no EF Core, no external dependencies
- Static calculator methods with record-based inputs/outputs
- All financial calculations should be unit tested

**FinanceEngine.Data (Persistence):**
- Use Entity Framework Core conventions
- Entities end with `Entity` suffix (e.g., `AccountEntity`)
- Always materialize queries with `.ToListAsync()` before mapping
- Store APR as decimal (0.0499 = 4.99%), not percentage

**FinanceEngine.Api (Web API):**
- Group endpoints by feature (e.g., `AccountEndpoints.cs`)
- Use DTOs for API contracts
- Keep endpoint methods small and focused
- Validate inputs early

**Testing:**
- Use xUnit for all backend tests
- Follow AAA pattern (Arrange, Act, Assert)
- Test file names should match source file with `Tests` suffix
- Test all edge cases and error conditions

### Frontend (Angular / TypeScript)

**TypeScript:**
- Use strict type checking
- Prefer type inference when obvious
- Avoid `any` - use `unknown` when type is uncertain
- Use interfaces for object shapes

**Angular Components:**
- Always use standalone components (no NgModules)
- Do NOT set `standalone: true` in decorators (it's the default in Angular 21)
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Set `changeDetection: ChangeDetectionStrategy.OnPush`
- Keep components small and focused

**State Management:**
- Use signals for local component state
- Use `computed()` for derived state
- Do NOT use `mutate()` - use `update()` or `set()` instead
- Keep state transformations pure and predictable

**Templates:**
- Use native control flow (`@if`, `@for`, `@switch`)
- Do NOT use `*ngIf`, `*ngFor`, `*ngSwitch`
- Do NOT use `ngClass` - use `class` bindings
- Do NOT use `ngStyle` - use `style` bindings
- Do NOT write arrow functions in templates
- Do NOT assume globals like `new Date()` are available

**Services:**
- Use `providedIn: 'root'` for singleton services
- Use `inject()` function instead of constructor injection
- Design around single responsibility

**Accessibility:**
- Must pass all AXE checks
- Must follow WCAG AA minimums
- Include focus management, color contrast, ARIA attributes

**Testing:**
- Use Jest for unit tests
- Use Testing Library for component tests
- Test files should be `*.spec.ts`
- Run tests with `npm test`

### Common Patterns to Avoid

**Backend:**
- ❌ Don't store percentages as integers (99 for 99%)
- ❌ Don't hand-edit the SQLite database
- ❌ Don't skip `.ToListAsync()` when materializing queries
- ❌ Don't add business logic to entities or DbContext

**Frontend:**
- ❌ Don't use `@HostBinding` or `@HostListener` decorators
- ❌ Don't use NgModules
- ❌ Don't mutate signals directly
- ❌ Don't add complex logic in templates
- ❌ Don't forget to handle observables (use async pipe)

---

## Testing

### Backend Tests

**Run all tests:**
```bash
dotnet test
```

**Run specific test class:**
```bash
dotnet test --filter "FullyQualifiedName~BurnRateCalculatorTests"
```

**Run tests with coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Test organization:**
- Location: `FinanceEngine.Tests/`
- Framework: xUnit
- Current count: 117 tests
- All tests should pass before committing

### Frontend Tests

**Run all tests:**
```bash
cd dashboard
npm test
```

**Run tests in watch mode:**
```bash
npm run test:watch
```

**Run tests with coverage:**
```bash
npm run test:coverage
```

**Test organization:**
- Location: `dashboard/src/app/**/*.spec.ts`
- Framework: Jest + Testing Library
- Current count: 39 tests
- All tests should pass before committing

### Writing Good Tests

**Arrange-Act-Assert Pattern:**
```csharp
[Fact]
public void CalculateSpendable_WithNoObligations_ReturnsFullBalance()
{
    // Arrange
    var input = new SpendableInput(
        CashBalance: 1000m,
        Obligations: [],
        SafetyBuffer: 100m
    );

    // Act
    var result = SpendableCalculator.Calculate(input);

    // Assert
    Assert.Equal(900m, result.SpendableNow);
}
```

**Test naming:**
- Use descriptive names: `MethodName_Scenario_ExpectedResult`
- Be specific about what's being tested
- Include edge cases and error conditions

---

## Common Tasks

### Adding a New Entity

1. Create entity class in `FinanceEngine.Data/Entities/`
2. Add DbSet to `FinanceDbContext.cs`
3. Create migration: `dotnet ef migrations add AddEntityName --project FinanceEngine.Data`
4. Update database: `dotnet ef database update --project FinanceEngine.Api`
5. Create repository interface and implementation
6. Add API endpoints
7. Add frontend service and models
8. Write tests for all layers

### Creating a New Calculator

1. Define input record in `FinanceEngine/Models/Inputs/`
2. Define output record in `FinanceEngine/Models/Outputs/`
3. Create calculator class in `FinanceEngine/Calculators/`
4. Implement static calculation method
5. Write comprehensive unit tests
6. Add API endpoint to expose calculator
7. Add frontend integration

### Adding a New Component

1. Create component file in `dashboard/src/app/components/`
2. Use standalone component with signals
3. Add to `app.routes.ts` if it's a page
4. Import required services with `inject()`
5. Create accompanying test file
6. Follow accessibility guidelines
7. Test manually and with automated tests

### Running Database Migrations

**Create migration:**
```bash
dotnet ef migrations add MigrationName --project FinanceEngine.Data --startup-project FinanceEngine.Api
```

**Apply migrations:**
```bash
dotnet ef database update --project FinanceEngine.Api
```

**Remove last migration (if not applied):**
```bash
dotnet ef migrations remove --project FinanceEngine.Data --startup-project FinanceEngine.Api
```

**Generate SQL script:**
```bash
dotnet ef migrations script --project FinanceEngine.Data --startup-project FinanceEngine.Api
```

### Building for Production

**Backend:**
```bash
dotnet build --configuration Release
dotnet publish --configuration Release
```

**Frontend:**
```bash
cd dashboard
npm run build
```

Output will be in `dashboard/dist/dashboard/browser/`

---

## Troubleshooting

### Backend Issues

**"Database file is locked"**
- Stop all running instances of the API
- Delete `FinanceEngine.Api/finance.db`
- Restart the API (database will be recreated)

**"Migration already exists"**
- Check `FinanceEngine.Data/Migrations/` for existing migrations
- Remove duplicate migration if needed
- Ensure you're in the correct project directory

**"Tests failing after database changes"**
- Tests use in-memory database, not SQLite
- Ensure migrations are applied correctly
- Check for breaking changes in entity relationships

### Frontend Issues

**"Cannot find module '@angular/...' "**
- Run `npm install` in the `dashboard/` directory
- Delete `node_modules/` and `package-lock.json`, then reinstall

**"Port 4200 already in use"**
- Kill existing Angular dev server: `lsof -ti:4200 | xargs kill -9` (Mac/Linux)
- Or change port: `ng serve --port 4201`

**"API calls returning 404"**
- Ensure backend API is running on `http://localhost:5285`
- Check `dashboard/proxy.conf.json` configuration
- Verify API endpoint paths match

**"Signals not updating"**
- Don't use `mutate()` - use `update()` or `set()`
- Ensure `OnPush` change detection is set
- Check that computed signals have correct dependencies

### General Issues

**"Build errors after pulling latest"**
- Backend: `dotnet clean && dotnet build`
- Frontend: `cd dashboard && npm install && npm run build`
- Check for EF migration changes that need to be applied

**"Tests passing locally but failing in CI"**
- Check for platform-specific paths (use `Path.Combine()`)
- Ensure no hardcoded absolute paths
- Verify all test dependencies are in version control

---

## Next Steps

### Immediate Actions

1. **Complete Quick Start** - Get both backend and frontend running
2. **Read Core Documentation:**
   - `AGENTS.md` (conventions and standards)
   - `mvp-finance.md` (product specification)
   - `TODO_NEXT.md` (current status and next actions)
3. **Explore the Code:**
   - Browse `FinanceEngine/Calculators/` to understand core algorithms
   - Look at `dashboard/src/app/components/` to see UI patterns
   - Review tests to understand expected behavior
4. **Pick Your First Task:**
   - Check `docs/v2/ROADMAP-v2.md` for available work items
   - Start with something small to get familiar
   - Mark it `[IN PROGRESS]` before starting

### Learning Resources

**Project-Specific:**
- `mvp-finance.md` - Algorithms and financial concepts
- `contracts/v2/GOAL_DRIVEN_BUDGETING.md` - v2 vision and roadmap
- `docs/v2/PROGRESS-v2.md` - Current status and known issues

**Technology:**
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Angular Documentation](https://angular.dev/)
- [Angular Signals Guide](https://angular.dev/guide/signals)
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [xUnit Documentation](https://xunit.net/)
- [Jest Documentation](https://jestjs.io/)

### Getting Help

- Review `AGENTS.md` for coding standards
- Check `docs/v2/WORKLOG-v2.md` for historical context
- Read test files to understand expected behavior
- Look at similar existing features for patterns

---

## Safety and Best Practices

### What NOT to Do

**Never:**
- Commit secrets or credentials
- Hand-edit `FinanceEngine.Api/finance.db`
- Edit `.git/`, `.claude/`, or generated folders (`bin/`, `obj/`, `node_modules/`)
- Push directly to `master` without a PR
- Make breaking changes without updating tests
- Skip running tests before committing
- Commit without a descriptive commit message

**Database:**
- Do NOT commit `finance.db` to version control
- Do NOT manually edit the SQLite file
- Always use EF migrations for schema changes
- Keep migrations small and focused

**Documentation:**
- Keep documentation in sync with code
- Update `docs/v2/WORKLOG-v2.md` after completing work
- Update `TODO_NEXT.md` with next actions
- Follow the handoff checklist before ending sessions

---

## Summary

You now have everything you need to start contributing to MVP Finance:

✅ **Environment set up** - Backend and frontend running locally
✅ **Architecture understood** - Clean architecture with event sourcing
✅ **Tech stack familiar** - .NET 10, Angular 21, SQLite
✅ **Conventions learned** - Coding standards and best practices
✅ **Workflow clear** - From task selection to commit
✅ **Documentation system** - Know where to find information

**Remember the golden rule:** Always read `AGENTS.md` first, then `TODO_NEXT.md`, then start coding!

Welcome to the team! 🚀
