# WI-P4-003: Dashboard Component Tests

## Objective
Create Jest tests for the Dashboard component using @testing-library/angular.

## Prerequisites
- WI-P4-000 (Jest setup) must be completed first

## Context
- Dashboard displays account totals (Cash, Debt, Investments)
- Shows recent transactions
- Displays safe-to-spend calculation
- Has loading and empty states

## Files to Create

### `dashboard/src/app/pages/dashboard/dashboard.spec.ts`

```typescript
import { render, screen, waitFor } from '@testing-library/angular';
import { DashboardPage } from './dashboard';
import { ApiService } from '../../core/services/api.service';
import { of, delay } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

// Mock data
const mockAccounts = [
  { id: 1, name: 'Checking', type: 'Cash', initialBalance: 5000, currentBalance: 5500 },
  { id: 2, name: 'Savings', type: 'Cash', initialBalance: 10000, currentBalance: 10000 },
  { id: 3, name: 'Credit Card', type: 'Debt', initialBalance: 2000, currentBalance: 1800 },
  { id: 4, name: '401k', type: 'Investment', initialBalance: 50000, currentBalance: 52000 }
];

const mockEvents = [
  { id: 1, date: '2025-01-15', type: 'Expense', amount: 50, description: 'Groceries', accountId: 1, status: 'Cleared' },
  { id: 2, date: '2025-01-14', type: 'Income', amount: 2500, description: 'Salary', accountId: 1, status: 'Cleared' }
];

const mockSettings = {
  payFrequency: 'BiWeekly',
  paycheckAmount: 2500,
  safetyBuffer: 100,
  nextPaycheckDate: '2025-01-31'
};

describe('DashboardPage', () => {
  const createMockApiService = (overrides = {}) => ({
    getAccounts: jest.fn().mockReturnValue(of(mockAccounts)),
    getRecentEvents: jest.fn().mockReturnValue(of(mockEvents)),
    getSettings: jest.fn().mockReturnValue(of(mockSettings)),
    calculateSpendable: jest.fn().mockReturnValue(of({ spendableNow: 1000, breakdown: {} })),
    ...overrides
  });

  // Tests go here...
});
```

## Required Tests

### 1. Renders Loading State Initially

```typescript
it('renders loading state initially', async () => {
  const mockApi = createMockApiService({
    getAccounts: jest.fn().mockReturnValue(of(mockAccounts).pipe(delay(100)))
  });

  await render(DashboardPage, {
    providers: [
      { provide: ApiService, useValue: mockApi },
      provideHttpClient(),
      provideHttpClientTesting()
    ]
  });

  expect(screen.getByText(/loading/i)).toBeInTheDocument();
});
```

### 2. Displays Total Cash From Cash Accounts

```typescript
it('displays total cash from cash accounts', async () => {
  const mockApi = createMockApiService();

  await render(DashboardPage, {
    providers: [{ provide: ApiService, useValue: mockApi }]
  });

  await waitFor(() => {
    // Cash = Checking (5500) + Savings (10000) = 15500
    expect(screen.getByText(/\$15,500/)).toBeInTheDocument();
  });
});
```

### 3. Displays Total Debt From Debt Accounts

```typescript
it('displays total debt from debt accounts', async () => {
  const mockApi = createMockApiService();

  await render(DashboardPage, {
    providers: [{ provide: ApiService, useValue: mockApi }]
  });

  await waitFor(() => {
    // Debt = Credit Card (1800)
    expect(screen.getByText(/\$1,800/)).toBeInTheDocument();
  });
});
```

### 4. Displays Total Investments

```typescript
it('displays total investments from investment accounts', async () => {
  const mockApi = createMockApiService();

  await render(DashboardPage, {
    providers: [{ provide: ApiService, useValue: mockApi }]
  });

  await waitFor(() => {
    // Investments = 401k (52000)
    expect(screen.getByText(/\$52,000/)).toBeInTheDocument();
  });
});
```

### 5. Shows Recent Transactions After Load

```typescript
it('shows recent transactions after load', async () => {
  const mockApi = createMockApiService();

  await render(DashboardPage, {
    providers: [{ provide: ApiService, useValue: mockApi }]
  });

  await waitFor(() => {
    expect(screen.getByText('Groceries')).toBeInTheDocument();
    expect(screen.getByText('Salary')).toBeInTheDocument();
  });
});
```

### 6. Shows Empty State When No Accounts Exist

```typescript
it('shows empty state when no accounts exist', async () => {
  const mockApi = createMockApiService({
    getAccounts: jest.fn().mockReturnValue(of([]))
  });

  await render(DashboardPage, {
    providers: [{ provide: ApiService, useValue: mockApi }]
  });

  await waitFor(() => {
    expect(screen.getByText(/no accounts/i)).toBeInTheDocument();
    // Or check for "Get started" / "Add your first account" text
  });
});
```

## Component Details

**Location:** `dashboard/src/app/pages/dashboard/dashboard.ts`

**Key Signals/Computed:**
- `accounts = signal<Account[]>([])`
- `recentEvents = signal<FinancialEvent[]>([])`
- `loading = signal(true)`
- `totalCash = computed(() => ...)` - sum of Cash account balances
- `totalDebt = computed(() => ...)` - sum of Debt account balances
- `totalInvestments = computed(() => ...)` - sum of Investment account balances

**API Calls on Init:**
- `apiService.getAccounts()`
- `apiService.getRecentEvents(10)` - last 10 events
- `apiService.getSettings()`
- `apiService.calculateSpendable()` - if settings exist

## Testing Notes

- Use `waitFor` for async operations
- Mock `ApiService` methods with `jest.fn().mockReturnValue(of(...))`
- Test computed values by checking rendered output
- Use `@testing-library/angular` patterns:
  - `render()` to mount component
  - `screen.getByText()` to find elements
  - `waitFor()` to wait for async updates

## Acceptance Criteria

- [ ] All 6 tests pass
- [ ] Tests mock ApiService correctly
- [ ] Loading state is tested
- [ ] Empty state is tested
- [ ] Account totals display correctly

## Verification

```bash
cd dashboard
npm test -- --testPathPattern=dashboard.spec
# Should output: "Tests: 6 passed"
```

## Existing Code References

- `dashboard/src/app/pages/dashboard/dashboard.ts` - Component
- `dashboard/src/app/pages/dashboard/dashboard.html` - Template
- `dashboard/src/app/core/services/api.service.ts` - API service to mock
- `dashboard/src/app/core/models/api.models.ts` - Type definitions
