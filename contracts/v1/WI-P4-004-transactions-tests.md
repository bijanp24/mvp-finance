# WI-P4-004: Transactions Component Tests

## Objective
Create Jest tests for the Transactions component using @testing-library/angular.

## Prerequisites
- WI-P4-000 (Jest setup) must be completed first

## Context
- Transactions page has a form for creating/editing transactions
- Form fields change based on transaction type
- Displays recent transactions in a table
- Has status filter (All/Pending/Cleared) for reconciliation
- Can toggle transaction status

## Files to Create

### `dashboard/src/app/pages/transactions/transactions.spec.ts`

```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { TransactionsPage } from './transactions';
import { ApiService } from '../../core/services/api.service';
import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';

// Mock data
const mockAccounts = [
  { id: 1, name: 'Checking', type: 'Cash', initialBalance: 5000, currentBalance: 5500 },
  { id: 2, name: 'Savings', type: 'Cash', initialBalance: 10000, currentBalance: 10000 },
  { id: 3, name: 'Credit Card', type: 'Debt', initialBalance: 2000, currentBalance: 1800, annualPercentageRate: 0.1999 },
  { id: 4, name: '401k', type: 'Investment', initialBalance: 50000, currentBalance: 52000 }
];

const mockEvents = [
  { id: 1, date: '2025-01-15', type: 'Expense', amount: 50, description: 'Groceries', accountId: 1, targetAccountId: null, status: 'Cleared' },
  { id: 2, date: '2025-01-14', type: 'Income', amount: 2500, description: 'Salary', accountId: 1, targetAccountId: null, status: 'Pending' },
  { id: 3, date: '2025-01-13', type: 'DebtPayment', amount: 200, description: 'Card payment', accountId: 1, targetAccountId: 3, status: 'Cleared' }
];

describe('TransactionsPage', () => {
  const createMockApiService = (overrides = {}) => ({
    getAccounts: jest.fn().mockReturnValue(of(mockAccounts)),
    getRecentEvents: jest.fn().mockReturnValue(of(mockEvents)),
    createEvent: jest.fn().mockReturnValue(of({ id: 99, ...mockEvents[0] })),
    updateEvent: jest.fn().mockReturnValue(of(mockEvents[0])),
    deleteEvent: jest.fn().mockReturnValue(of(undefined)),
    updateEventStatus: jest.fn().mockReturnValue(of({ ...mockEvents[1], status: 'Cleared' })),
    ...overrides
  });

  const renderComponent = async (apiOverrides = {}) => {
    const mockApi = createMockApiService(apiOverrides);
    return render(TransactionsPage, {
      providers: [
        { provide: ApiService, useValue: mockApi },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations()
      ]
    });
  };

  // Tests go here...
});
```

## Required Tests

### 1. Renders Transaction Form With Type Toggle

```typescript
it('renders transaction form with type toggle', async () => {
  await renderComponent();

  await waitFor(() => {
    expect(screen.getByText('Income')).toBeInTheDocument();
    expect(screen.getByText('Expense')).toBeInTheDocument();
    expect(screen.getByText('Debt Payment')).toBeInTheDocument();
  });
});
```

### 2. Validates Amount Field Is Required

```typescript
it('validates amount field is required', async () => {
  await renderComponent();

  await waitFor(() => {
    const submitButton = screen.getByRole('button', { name: /save/i });
    expect(submitButton).toBeDisabled();
  });
});
```

### 3. Shows Target Account Field For DebtPayment Type

```typescript
it('shows target account field for DebtPayment type', async () => {
  await renderComponent();

  await waitFor(async () => {
    // Click on DebtPayment toggle
    const debtPaymentToggle = screen.getByText('Debt Payment');
    fireEvent.click(debtPaymentToggle);

    // Should show "To Account" field
    expect(screen.getByLabelText(/to account/i)).toBeInTheDocument();
  });
});
```

### 4. Hides Target Account Field For Expense Type

```typescript
it('hides target account field for Expense type', async () => {
  await renderComponent();

  await waitFor(() => {
    // Expense is default, target account should not be visible
    const targetAccount = screen.queryByLabelText(/to account/i);
    expect(targetAccount).not.toBeInTheDocument();
  });
});
```

### 5. Filters Cash Accounts For Source Account Dropdown

```typescript
it('filters cash accounts for source account dropdown', async () => {
  await renderComponent();

  await waitFor(() => {
    // Open the "From Account" dropdown
    const fromAccount = screen.getByLabelText(/from account/i);
    fireEvent.click(fromAccount);

    // Should show only Cash accounts
    expect(screen.getByText('Checking')).toBeInTheDocument();
    expect(screen.getByText('Savings')).toBeInTheDocument();
    // Should NOT show Debt or Investment accounts
    expect(screen.queryByText('Credit Card')).not.toBeInTheDocument();
    expect(screen.queryByText('401k')).not.toBeInTheDocument();
  });
});
```

### 6. Displays Recent Transactions In Table

```typescript
it('displays recent transactions in table', async () => {
  await renderComponent();

  await waitFor(() => {
    expect(screen.getByText('Groceries')).toBeInTheDocument();
    expect(screen.getByText('Salary')).toBeInTheDocument();
    expect(screen.getByText('Card payment')).toBeInTheDocument();
  });
});
```

### 7. Status Filter Buttons Filter Transactions

```typescript
it('status filter buttons filter transactions', async () => {
  await renderComponent();

  await waitFor(async () => {
    // Click "Pending" filter
    const pendingFilter = screen.getByRole('button', { name: /pending/i });
    fireEvent.click(pendingFilter);

    // Should show only pending transactions
    expect(screen.getByText('Salary')).toBeInTheDocument();  // Pending
    expect(screen.queryByText('Groceries')).not.toBeInTheDocument();  // Cleared
  });
});
```

### 8. Clicking Status Button Toggles Between Pending/Cleared

```typescript
it('clicking status button toggles between Pending/Cleared', async () => {
  const mockApi = createMockApiService();
  await render(TransactionsPage, {
    providers: [
      { provide: ApiService, useValue: mockApi },
      provideAnimations()
    ]
  });

  await waitFor(async () => {
    // Find a status button and click it
    const pendingButton = screen.getAllByRole('button', { name: /pending/i })[1];  // Table button, not filter
    fireEvent.click(pendingButton);

    expect(mockApi.updateEventStatus).toHaveBeenCalledWith(2, 'Cleared');
  });
});
```

## Component Details

**Location:** `dashboard/src/app/pages/transactions/transactions.ts`

**Key Signals:**
- `accounts = signal<Account[]>([])`
- `recentEvents = signal<FinancialEvent[]>([])`
- `loading = signal(false)`
- `saving = signal(false)`
- `editingEventId = signal<number | null>(null)`
- `statusFilter = signal<'All' | 'Pending' | 'Cleared'>('All')`

**Key Computed:**
- `showTargetAccount()` - true for DebtPayment, SavingsContribution, InvestmentContribution
- `filteredTargetAccounts()` - Debt accounts for DebtPayment, Investment for others
- `cashAccounts()` - only type === 'Cash'
- `filteredEvents()` - filtered by statusFilter
- `pendingCount()` - count of pending transactions

**Event Types:**
```typescript
readonly eventTypes = [
  { value: 'Income', label: 'Income', icon: 'trending_up' },
  { value: 'Expense', label: 'Expense', icon: 'shopping_cart' },
  { value: 'DebtPayment', label: 'Debt Payment', icon: 'payment' },
  { value: 'DebtCharge', label: 'Debt Charge', icon: 'credit_card' },
  { value: 'SavingsContribution', label: 'Savings', icon: 'savings' },
  { value: 'InvestmentContribution', label: 'Investment', icon: 'trending_up' }
];
```

**Key Methods:**
- `onSubmit()` - creates or updates event
- `deleteEvent(event)` - deletes event
- `toggleStatus(event)` - toggles Pending/Cleared
- `setStatusFilter(filter)` - changes filter

## Testing Notes

- Use `provideAnimations()` for Material components
- Material components may need special handling for dropdowns
- Use `screen.queryBy*` for elements that may not exist
- Status toggle test should verify `updateEventStatus` was called

## Acceptance Criteria

- [ ] All 8 tests pass
- [ ] Form type toggle works
- [ ] Dynamic field visibility works
- [ ] Account filtering works correctly
- [ ] Status filter works
- [ ] Status toggle calls API

## Verification

```bash
cd dashboard
npm test -- --testPathPattern=transactions.spec
# Should output: "Tests: 8 passed"
```

## Existing Code References

- `dashboard/src/app/pages/transactions/transactions.ts` - Component
- `dashboard/src/app/pages/transactions/transactions.html` - Template
- `dashboard/src/app/core/services/api.service.ts` - API service to mock
- `dashboard/src/app/core/models/api.models.ts` - Type definitions (EventStatus, FinancialEvent)
