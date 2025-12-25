# WI-P5-003: Remove Commented Migration

## Objective
Remove the completed, commented one-time migration block from `Program.cs` to keep the entry point clean.

## Context
- The migration endpoint for APR fix is already executed and commented out.
- Keeping commented blocks increases noise and maintenance cost.

## Files to Modify
- `FinanceEngine.Api/Program.cs`

## Implementation Steps
1. Delete the entire commented migration block (the "COMPLETED" section and commented endpoint).
2. Confirm no functional code is removed.

## Acceptance Criteria
- `Program.cs` no longer contains the commented migration block.
- Build and run behavior remains unchanged.

## Verification
```bash
dotnet build
```
