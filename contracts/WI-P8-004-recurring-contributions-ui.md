# WI-P8-004: Settings UI for Recurring Contributions

## Objective
Add a "Recurring Contributions" section to the Settings page where users can manage their scheduled investment and savings contributions.

## Context
- Settings page currently has paycheck configuration (amount, frequency, next date).
- No equivalent UI exists for investment contributions.
- Users need to add/edit/delete recurring contribution schedules.
- Mirrors the paycheck section in structure and behavior.

## Files to Modify
- `dashboard/src/app/pages/settings/settings.ts`
- `dashboard/src/app/pages/settings/settings.html`
- `dashboard/src/app/pages/settings/settings.scss`
- `dashboard/src/app/core/models/api.models.ts`
- `dashboard/src/app/core/services/api.service.ts`

## UI Design

### Section Layout
Add new section below existing paycheck settings:

```
+------------------------------------------+
|  Recurring Contributions                  |
+------------------------------------------+
|                                          |
|  [+ Add Contribution]                    |
|                                          |
|  +------------------------------------+  |
|  | 401k Contribution                  |  |
|  | $500 Monthly -> Brokerage          |  |
|  | Next: Jan 15, 2025                 |  |
|  | [Edit] [Delete]           [Active] |  |
|  +------------------------------------+  |
|                                          |
|  +------------------------------------+  |
|  | IRA Contribution                   |  |
|  | $250 BiWeekly -> Roth IRA          |  |
|  | Next: Jan 10, 2025                 |  |
|  | [Edit] [Delete]         [Inactive] |  |
|  +------------------------------------+  |
|                                          |
+------------------------------------------+
```

### Add/Edit Dialog
Use Material Dialog (similar to account dialog):

```
+------------------------------------------+
|  Add Recurring Contribution              |
+------------------------------------------+
|                                          |
|  Name:        [401k Contribution      ]  |
|                                          |
|  Amount:      [$] [500.00             ]  |
|                                          |
|  Frequency:   [Monthly            v]     |
|               Weekly | BiWeekly |        |
|               SemiMonthly | Monthly |    |
|               Quarterly | Annually       |
|                                          |
|  Next Date:   [2025-01-15         ]      |
|                                          |
|  From Account: [Checking          v]     |
|               (Cash accounts only)       |
|                                          |
|  To Account:   [Brokerage         v]     |
|               (Investment/Savings)       |
|                                          |
|  [Cancel]                    [Save]      |
+------------------------------------------+
```

## TypeScript Models

```typescript
// api.models.ts
export interface RecurringContribution {
  id: number;
  name: string;
  amount: number;
  frequency: ContributionFrequency;
  nextContributionDate: string;  // ISO date
  sourceAccountId: number;
  targetAccountId: number;
  sourceAccountName?: string;
  targetAccountName?: string;
  isActive: boolean;
  createdAt: string;
}

export type ContributionFrequency =
  | 'Weekly'
  | 'BiWeekly'
  | 'SemiMonthly'
  | 'Monthly'
  | 'Quarterly'
  | 'Annually';

export interface CreateRecurringContributionRequest {
  name: string;
  amount: number;
  frequency: ContributionFrequency;
  nextContributionDate: string;
  sourceAccountId: number;
  targetAccountId: number;
}
```

## API Service Methods

```typescript
// api.service.ts
getRecurringContributions(): Observable<RecurringContribution[]> {
  return this.http.get<RecurringContribution[]>(`${this.baseUrl}/recurring-contributions`);
}

createRecurringContribution(request: CreateRecurringContributionRequest): Observable<RecurringContribution> {
  return this.http.post<RecurringContribution>(`${this.baseUrl}/recurring-contributions`, request);
}

updateRecurringContribution(id: number, request: CreateRecurringContributionRequest): Observable<RecurringContribution> {
  return this.http.put<RecurringContribution>(`${this.baseUrl}/recurring-contributions/${id}`, request);
}

deleteRecurringContribution(id: number): Observable<void> {
  return this.http.delete<void>(`${this.baseUrl}/recurring-contributions/${id}`);
}

toggleRecurringContribution(id: number): Observable<RecurringContribution> {
  return this.http.patch<RecurringContribution>(`${this.baseUrl}/recurring-contributions/${id}/toggle`, {});
}
```

## Component Implementation Notes

### Signals
```typescript
contributions = signal<RecurringContribution[]>([]);
cashAccounts = computed(() => this.accounts().filter(a => a.type === 'Cash'));
investmentAccounts = computed(() => this.accounts().filter(a => a.type === 'Investment' || a.type === 'Cash'));
```

### Dialog Component
Create `RecurringContributionDialogComponent` similar to `AccountDialogComponent`:
- Reactive form with validation
- Account dropdowns filtered by type
- Frequency dropdown
- Date picker for next contribution

### Form Validation
- Name: required, maxLength 100
- Amount: required, min 0.01
- Frequency: required
- Next date: required
- Source account: required, must be Cash type
- Target account: required, cannot equal source

## Accessibility Requirements
- All form controls labeled
- Error messages announced to screen readers
- Focus management on dialog open/close
- Keyboard navigation for all actions

## Acceptance Criteria
- [ ] "Recurring Contributions" section visible on Settings page
- [ ] List displays all recurring contributions with key details
- [ ] Add button opens dialog with form
- [ ] Edit button populates form with existing data
- [ ] Delete button removes contribution (with confirmation)
- [ ] Toggle button switches active/inactive status
- [ ] Form validation prevents invalid submissions
- [ ] Account dropdowns filter by appropriate type
- [ ] Changes persist via API calls
- [ ] Loading states shown during API operations
- [ ] Matches existing Settings page styling

## Verification
```bash
cd dashboard && npm run build
# Manual: Navigate to Settings, test CRUD operations
```

## Dependencies
- WI-P8-003 (API endpoints must exist)

## Parallel Execution
- Blocked by WI-P8-003
- Can run in parallel with WI-P8-005, WI-P8-006, WI-P8-007
