# WI-P5-002: Extract Balance Calculation

## Objective
Move account balance calculation into a shared service to remove duplication and centralize the business rule.

## Context
- Balance calculation currently lives inside API service logic.
- The same logic should be reusable across API endpoints and services.
- Keep the core calculation in the `FinanceEngine` project.

## Files to Create/Modify
- New: `FinanceEngine/Services/BalanceCalculator.cs`
- Modify: `FinanceEngine.Api/Services/AccountService.cs`
- Modify (if needed): `FinanceEngine.Api/Endpoints/AccountEndpoints.cs`

## Implementation Notes
- Create a small, pure calculation helper in `FinanceEngine`:
  - Inputs: account type and a list of events (type + amount).
  - Output: calculated balance as `decimal`.
- Map `AccountEntity` + `FinancialEventEntity` to the calculator inputs in `AccountService`.
- Replace the existing private `CalculateBalance` method with the new service.
- Keep behavior identical (including treatment of event types and account types).

## Acceptance Criteria
- All balance calculations use the shared calculator.
- Behavior matches current output (no regression in current balance values).
- No duplicated balance logic remains in API classes.

## Verification
```bash
dotnet test
# Manual: load dashboard and accounts page; balances should match prior behavior.
```
