# WI-P8-005: Calendar Integration for Contributions

## Objective
Display scheduled contribution dates on the calendar alongside paycheck indicators, giving users a complete view of upcoming financial events.

## Context
- Calendar currently shows paycheck dates using `UserSettingsEntity.NextPaycheckDate` and frequency.
- No contribution markers exist.
- Adding contributions creates a unified view of cash flow events.
- Uses `RecurringEventExpansionService` to generate future dates.

## Files to Modify
- `dashboard/src/app/core/services/calendar.service.ts`
- `dashboard/src/app/features/calendar/calendar.component.ts`
- `dashboard/src/app/features/calendar/calendar.component.html`
- `dashboard/src/app/features/calendar/calendar.component.scss`
- `dashboard/src/app/core/services/api.service.ts` (if not already added)

## UI Design

### Calendar Day with Events
```
+--------+
|   15   |
| [PAY]  |  <- Paycheck indicator (existing)
| [401k] |  <- Contribution indicator (new)
| [$250] |  <- Amount badge
+--------+
```

### Event Badge Styling
- Paychecks: Green badge (existing)
- Contributions: Blue/Purple badge (new)
- Hover tooltip: "401k Contribution - $500 to Brokerage"

### Legend Addition
```
[PAY] Paycheck    [CON] Contribution
```

## Implementation Notes

### Calendar Service Updates

```typescript
// calendar.service.ts

// Add new signal for contributions
recurringContributions = signal<RecurringContribution[]>([]);

// Load contributions on init
loadContributions(): void {
  this.api.getRecurringContributions().subscribe(contributions => {
    this.recurringContributions.set(contributions.filter(c => c.isActive));
  });
}

// Expand contributions for displayed month range
getContributionDates(startDate: Date, endDate: Date): ContributionEvent[] {
  const contributions: ContributionEvent[] = [];

  for (const schedule of this.recurringContributions()) {
    const occurrences = this.expandSchedule(schedule, startDate, endDate);
    contributions.push(...occurrences);
  }

  return contributions;
}

// Helper to expand a single schedule
private expandSchedule(
  schedule: RecurringContribution,
  startDate: Date,
  endDate: Date
): ContributionEvent[] {
  const events: ContributionEvent[] = [];
  let current = new Date(schedule.nextContributionDate);
  const frequencyDays = this.getFrequencyDays(schedule.frequency);

  while (current <= endDate) {
    if (current >= startDate) {
      events.push({
        date: new Date(current),
        name: schedule.name,
        amount: schedule.amount,
        targetAccountName: schedule.targetAccountName
      });
    }
    current = this.addFrequency(current, schedule.frequency);
  }

  return events;
}
```

### Calendar Component Updates

```typescript
// calendar.component.ts

// Computed signal for contribution events in current month
contributionEvents = computed(() => {
  const start = this.monthStart();
  const end = this.monthEnd();
  return this.calendarService.getContributionDates(start, end);
});

// Helper to get contributions for a specific day
getContributionsForDay(day: Date): ContributionEvent[] {
  return this.contributionEvents().filter(e =>
    this.isSameDay(e.date, day)
  );
}

// Check if day has contributions
hasContributions(day: Date): boolean {
  return this.getContributionsForDay(day).length > 0;
}
```

### Template Updates

```html
<!-- calendar.component.html -->

<div class="calendar-day" [class.has-events]="hasPaycheck(day) || hasContributions(day)">
  <span class="day-number">{{ day.getDate() }}</span>

  @if (hasPaycheck(day)) {
    <span class="event-badge paycheck" title="Paycheck">PAY</span>
  }

  @for (contribution of getContributionsForDay(day); track contribution.name) {
    <span
      class="event-badge contribution"
      [title]="contribution.name + ' - $' + contribution.amount + ' to ' + contribution.targetAccountName">
      {{ contribution.name | slice:0:4 }}
    </span>
  }
</div>
```

### Styles

```scss
// calendar.component.scss

.event-badge {
  &.paycheck {
    background: var(--color-success);
    color: var(--color-on-success);
  }

  &.contribution {
    background: var(--color-info);
    color: var(--color-on-info);
  }
}
```

## Data Models

```typescript
interface ContributionEvent {
  date: Date;
  name: string;
  amount: number;
  targetAccountName?: string;
}
```

## Performance Considerations
- Only expand schedules for currently displayed month (+/- 1 month buffer)
- Cache expanded events until month changes
- Limit to active schedules only

## Accessibility
- Badge colors have sufficient contrast
- Tooltip text is descriptive
- Screen reader announces "Contribution: 401k, $500 to Brokerage"

## Acceptance Criteria
- [ ] Contribution badges appear on correct calendar days
- [ ] Multiple contributions on same day all shown
- [ ] Badges show abbreviated name (max 4 chars)
- [ ] Hover tooltip shows full details
- [ ] Only active contributions displayed
- [ ] Legend updated with contribution indicator
- [ ] Performance acceptable with multiple schedules
- [ ] Styling consistent with paycheck badges

## Verification
```bash
cd dashboard && npm run build
# Manual: Create recurring contributions, verify calendar display
```

## Dependencies
- WI-P8-002 (expansion logic, can be reimplemented in frontend)
- WI-P8-003 (API to fetch contributions)

## Parallel Execution
- Can run in parallel with WI-P8-004, WI-P8-006, WI-P8-007
- Depends on WI-P8-002 or WI-P8-003 being complete
