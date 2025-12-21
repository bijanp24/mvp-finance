# MVP Finance - AI Coding Agent Instructions

> **⚠️ IMPORTANT:** This project has comprehensive coding standards in [CLAUDE.md](../CLAUDE.md) (backend) and [dashboard/CLAUDE.md](../dashboard/CLAUDE.md) (frontend). Those files are authoritative for all coding conventions, Git workflow, and commit rules. This document focuses on architecture and workflows not obvious from file inspection alone.

## Project Architecture

**Three-tier finance application:** .NET 10 library + ASP.NET Core API + Angular 21 dashboard

### Component Boundaries
- **FinanceEngine** - Pure calculation library (no I/O, no EF Core). Stateless calculators with record-based inputs/outputs.
- **FinanceEngine.Data** - EF Core layer with `FinanceDbContext`, entities, and SQLite persistence.
- **FinanceEngine.Api** - Minimal API using endpoint groups (`/api/accounts`, `/api/events`, `/api/calculators`). No controllers.
- **dashboard/** - Standalone Angular 21 app (no NgModules). Signals for state, Material Design UI.

### Key Design Patterns
- **Event sourcing**: Account balances computed from transaction history, not stored directly.
- **Calculator pattern**: Static methods in `FinanceEngine.Calculators` namespace. Input/Output record types in `Models/Inputs` and `Models/Outputs`.
- **Endpoint groups**: API endpoints organized via `MapGroup()` with extension methods (see [CalculatorEndpoints.cs](../FinanceEngine.Api/Endpoints/CalculatorEndpoints.cs)).

## Critical Developer Workflows

### Running the Application
**Backend:**
```bash
dotnet run --project FinanceEngine.Api
# Runs on http://localhost:5000
```

**Frontend:**
```bash
cd dashboard
npm start
# Runs on http://localhost:4200 with API proxy to :5000
```

**Database:** SQLite auto-created at `FinanceEngine.Api/finance.db` on first run.

### Testing
```bash
dotnet test FinanceEngine.Tests
# Uses xUnit with Arrange-Act-Assert pattern
```

Tests in [FinanceEngine.Tests/Calculators/](../FinanceEngine.Tests/Calculators/) validate calculator logic in isolation using record-based inputs.

### Building
```bash
dotnet build mvp-finance.sln  # Backend
cd dashboard && ng build       # Frontend
```

## Project-Specific Conventions

> **See [CLAUDE.md](../CLAUDE.md) for complete Git workflow, commit rules, and .NET conventions.**  
> **See [dashboard/CLAUDE.md](../dashboard/CLAUDE.md) for complete Angular/TypeScript conventions.**

### Key Architecture Decisions
- **Records over classes** for DTOs/inputs/outputs (immutable by default).
- **Static calculators** - No instance state, pure functions.
- **Decimal precision:** APRs stored as decimals (0.0499 = 4.99%), not percentages. Use `HasPrecision(8,4)` in EF Core.
- **Minimal APIs** - Endpoint methods in static classes, not controllers.
- **Request/Response DTOs** - Separate types for API contracts (e.g., `SpendableRequest`, not domain `SpendableInput`).
- **Entity Framework queries** - Materialize before mapping to domain models:
  ```csharp
  var events = await db.Events.Where(...).ToListAsync();
  var domainModels = events.Select(e => new SpendingEvent(e.Date, e.Amount));
  ```

## Integration Points

### Frontend ↔ API Communication
- **Proxy config:** [dashboard/proxy.conf.json](../dashboard/proxy.conf.json) forwards `/api` to `localhost:5000`.
- **CORS policy:** `AllowAngular` policy permits `http://localhost:4200` in dev mode.
- **Service layer:** [dashboard/src/app/core/services/api.service.ts](../dashboard/src/app/core/services/api.service.ts) wraps all HTTP calls with typed observables.

### Calculator Orchestration
Calculators often compose each other. Example from [CalculatorEndpoints.cs](../FinanceEngine.Api/Endpoints/CalculatorEndpoints.cs#L22-L35):
1. Query spending events from DB
2. Call `BurnRateCalculator` to get daily spend estimate
3. Pass result into `SpendableCalculator` as input
4. Return combined result to frontend

### Event Sourcing Flow
1. User creates transaction via [transactions page](../dashboard/src/app/pages/transactions/transactions.ts)
2. API saves `FinancialEventEntity` to DB via [EventEndpoints](../FinanceEngine.Api/Endpoints/EventEndpoints.cs)
3. Account balance computed on-demand by summing events (not stored)

## Key Files to Reference

- **[mvp-finance.md](../mvp-finance.md)** - Full feature spec and algorithm definitions
- **[PROGRESS.md](../PROGRESS.md)** - What's built, what's next, file locations
- **[CLAUDE.md](../CLAUDE.md)** - Git workflow, commit rules, documentation conventions
- **[FinanceEngine/Models/](../FinanceEngine/Models/)** - Domain models and calculator I/O types
- **[FinanceEngine/Calculators/](../FinanceEngine/Calculators/)** - Core business logic (SpendableCalculator, ForwardSimulationEngine, etc.)

## Common Pitfalls

1. **APR storage:** Always store as decimal (0.0499), not percentage (4.99). Frontend displays as percentage.
2. **EF Core queries:** Don't pass `IQueryable` to calculators. Use `.ToListAsync()` first.
3. **Angular signals:** Don't mutate signal values. Use `update()` or `set()` for changes.
4. **Git merges:** Never fast-forward. Always `git merge --no-ff`.
5. **Image commits:** Images excluded from repo. Remove references from consolidated `.md` files.
