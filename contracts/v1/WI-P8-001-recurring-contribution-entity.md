# WI-P8-001: RecurringContributionEntity - Database Entity

## Objective
Create a database entity to store recurring contribution schedules for investments and savings, mirroring the structure used for paycheck tracking (`IncomeScheduleEntity`).

## Context
- Paychecks currently have `IncomeScheduleEntity` with frequency and next date anchor.
- Investments have no equivalent - contributions are one-off events only.
- This creates asymmetry: users can project paycheck income but not scheduled investment contributions.
- Enabling recurring contributions improves net worth projections and financial planning.

## Files to Create/Modify
- `FinanceEngine.Data/Entities/RecurringContributionEntity.cs` (NEW)
- `FinanceEngine.Data/Entities/ContributionFrequency.cs` (NEW - or reuse existing)
- `FinanceEngine.Data/FinanceDbContext.cs` (add DbSet)
- New EF Core migration

## Entity Design

```csharp
public enum ContributionFrequency
{
    Weekly = 7,
    BiWeekly = 14,
    SemiMonthly = 15,
    Monthly = 30,
    Quarterly = 90,
    Annually = 365
}

public class RecurringContributionEntity
{
    public int Id { get; set; }

    // Description
    public string Name { get; set; } = string.Empty;  // e.g., "401k Contribution"

    // Amount per occurrence
    public decimal Amount { get; set; }

    // Schedule
    public ContributionFrequency Frequency { get; set; }
    public DateTime NextContributionDate { get; set; }  // Anchor for forward calculation

    // Accounts (double-entry)
    public int SourceAccountId { get; set; }   // Cash account (debit)
    public int TargetAccountId { get; set; }   // Investment/Savings account (credit)

    // Foreign key navigation
    public AccountEntity? SourceAccount { get; set; }
    public AccountEntity? TargetAccount { get; set; }

    // Metadata
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## Implementation Notes
- Reuse `PayFrequency` enum if appropriate, or create `ContributionFrequency` with additional options (Quarterly, Annually).
- `NextContributionDate` is the anchor - all future dates are calculated from this.
- Source account should be validated as `AccountType.Cash`.
- Target account should be validated as `AccountType.Investment` or `AccountType.Cash` (for savings).
- Add foreign key relationships to `AccountEntity`.
- Consider adding `EndDate` property for limited-duration contributions (optional).

## Database Migration
```bash
dotnet ef migrations add AddRecurringContributions --project FinanceEngine.Data --startup-project FinanceEngine.Api
dotnet ef database update --project FinanceEngine.Data --startup-project FinanceEngine.Api
```

## Acceptance Criteria
- [ ] `RecurringContributionEntity` exists with all required properties
- [ ] `ContributionFrequency` enum includes at least: Weekly, BiWeekly, SemiMonthly, Monthly
- [ ] DbContext includes `DbSet<RecurringContributionEntity>`
- [ ] EF migration created and applied successfully
- [ ] Foreign keys to AccountEntity configured correctly
- [ ] Build succeeds with no errors

## Verification
```bash
dotnet build
dotnet run --project FinanceEngine.Api
# Check database has RecurringContributions table with correct columns
sqlite3 FinanceEngine.Api/finance.db ".schema RecurringContributions"
```

## Dependencies
- None (first item in Phase 8)

## Parallel Execution
- This work item must complete before WI-P8-002, WI-P8-003
