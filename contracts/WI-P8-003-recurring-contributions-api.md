# WI-P8-003: Recurring Contributions API Endpoints

## Objective
Create CRUD API endpoints for managing recurring contribution schedules, following the established Minimal API patterns.

## Context
- Frontend needs to create, read, update, and delete recurring contribution schedules.
- Follows same patterns as `AccountEndpoints`, `EventEndpoints`, `SettingsEndpoints`.
- Enables WI-P8-004 (Settings UI) to function.

## Files to Create/Modify
- `FinanceEngine.Api/Endpoints/RecurringContributionEndpoints.cs` (NEW)
- `FinanceEngine.Tests/Endpoints/RecurringContributionEndpointsTests.cs` (NEW)
- `FinanceEngine.Api/Program.cs` (register endpoints)

## API Design

### Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/recurring-contributions` | List all recurring contributions |
| GET | `/api/recurring-contributions/{id}` | Get single contribution by ID |
| POST | `/api/recurring-contributions` | Create new recurring contribution |
| PUT | `/api/recurring-contributions/{id}` | Update recurring contribution |
| DELETE | `/api/recurring-contributions/{id}` | Soft delete (set IsActive = false) |
| PATCH | `/api/recurring-contributions/{id}/toggle` | Toggle active status |
| GET | `/api/recurring-contributions/{id}/preview` | Preview next N occurrences |

### DTOs

```csharp
public record RecurringContributionDto(
    int Id,
    string Name,
    decimal Amount,
    string Frequency,  // "Weekly", "BiWeekly", "Monthly", etc.
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId,
    string? SourceAccountName,  // Populated on read
    string? TargetAccountName,  // Populated on read
    bool IsActive,
    DateTime CreatedAt
);

public record CreateRecurringContributionRequest(
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId
);

public record UpdateRecurringContributionRequest(
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId,
    bool IsActive
);

public record ContributionPreviewResponse(
    int Id,
    string Name,
    IEnumerable<ContributionOccurrence> Occurrences
);

public record ContributionOccurrence(
    DateTime Date,
    decimal Amount
);
```

### Validation Rules
- `Name`: Required, max 100 characters
- `Amount`: Required, > 0
- `Frequency`: Must be valid enum value
- `NextContributionDate`: Required, should be in future (warning if past)
- `SourceAccountId`: Must exist, must be Cash type
- `TargetAccountId`: Must exist, must be Investment or Cash type
- `SourceAccountId` != `TargetAccountId`

## Implementation Notes

### Endpoint Registration
```csharp
public static class RecurringContributionEndpoints
{
    public static void MapRecurringContributionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recurring-contributions")
            .WithTags("Recurring Contributions");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        group.MapPatch("/{id:int}/toggle", Toggle);
        group.MapGet("/{id:int}/preview", Preview);
    }
}
```

### Preview Endpoint
Uses `RecurringEventExpansionService` to show next 12 occurrences:
```csharp
static async Task<IResult> Preview(int id, FinanceDbContext db)
{
    var contribution = await db.RecurringContributions.FindAsync(id);
    if (contribution is null) return Results.NotFound();

    var endDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1));
    var occurrences = RecurringEventExpansionService.ExpandContributions(
        contribution,
        DateOnly.FromDateTime(DateTime.Today),
        endDate
    ).Take(12);

    return Results.Ok(new ContributionPreviewResponse(
        contribution.Id,
        contribution.Name,
        occurrences.Select(o => new ContributionOccurrence(o.Date.ToDateTime(TimeOnly.MinValue), o.Amount))
    ));
}
```

## Test Cases

```csharp
[Fact]
public async Task GetAll_ReturnsAllContributions()

[Fact]
public async Task GetById_ExistingId_ReturnsContribution()

[Fact]
public async Task GetById_NonExistingId_Returns404()

[Fact]
public async Task Create_ValidRequest_ReturnsCreated()

[Fact]
public async Task Create_InvalidSourceAccount_Returns400()

[Fact]
public async Task Create_SourceEqualsTarget_Returns400()

[Fact]
public async Task Update_ValidRequest_ReturnsUpdated()

[Fact]
public async Task Delete_SetsIsActiveFalse()

[Fact]
public async Task Toggle_FlipsActiveStatus()

[Fact]
public async Task Preview_ReturnsNext12Occurrences()
```

## Acceptance Criteria
- [ ] All 7 endpoints implemented and functional
- [ ] Validation enforced on create/update
- [ ] Account type validation (source = Cash, target = Investment/Cash)
- [ ] Soft delete preserves data
- [ ] Preview uses expansion service correctly
- [ ] Minimum 10 integration tests
- [ ] All tests pass

## Verification
```bash
dotnet test --filter "FullyQualifiedName~RecurringContributionEndpoints"
curl http://localhost:5285/api/recurring-contributions
```

## Dependencies
- WI-P8-001 (entity must exist)
- WI-P8-002 (expansion service for preview endpoint)

## Parallel Execution
- Can run in parallel with WI-P8-002 (except preview endpoint needs expansion service)
- Blocks WI-P8-004 (frontend needs API)
