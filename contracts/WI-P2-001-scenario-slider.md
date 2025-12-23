# WI-P2-001: Scenario Slider for Extra Debt Payments

## Objective
Add a slider to the Projections page that lets users see how extra monthly payments affect their debt payoff timeline.

## Context
- The backend endpoint `/api/calculators/debt-allocation` already exists and accepts `extraPaymentAmount`
- The frontend `ApiService.calculateDebtAllocation()` method exists
- The current projections page uses `runSimulation()` which doesn't support extra payments

## Files to Modify

### 1. `dashboard/src/app/pages/projections/projections.ts`
Add:
- Import `MatSliderModule` from `@angular/material/slider`
- Import `FormsModule` for ngModel binding
- Add signal: `extraPayment = signal(0);`
- Add method to handle slider changes and recalculate debt projection
- Wire up the debt allocation API to show impact

### 2. `dashboard/src/app/pages/projections/projections.html`
Add after the time range toggle (around line 17):
```html
<div class="extra-payment-slider">
  <label>Extra Monthly Payment: {{ formatCurrency(extraPayment()) }}</label>
  <mat-slider min="0" max="500" step="25" discrete>
    <input matSliderThumb [(ngModel)]="extraPayment" (change)="onExtraPaymentChange()">
  </mat-slider>
</div>
```

### 3. `dashboard/src/app/core/services/projection.service.ts`
Add:
- New signal: `debtAllocationResult = signal<DebtAllocationResult | null>(null);`
- New method: `calculateDebtAllocation(accounts: Account[], extraPayment: number)`
- New computed: compare baseline vs extra payment scenarios

## API Details

**Endpoint:** `POST /api/calculators/debt-allocation`

**Request:**
```typescript
interface DebtAllocationRequest {
  debts: DebtDto[];
  extraPaymentAmount: number;  // The slider value
  strategy: string;            // "Avalanche" | "Snowball" | "MinimumOnly"
}

interface DebtDto {
  name: string;
  balance: number;
  annualPercentageRate: number;  // Decimal: 0.18 = 18%
  minimumPayment: number;
}
```

**Response:**
```typescript
interface DebtAllocationResult {
  allocations: { debtName: string; payment: number }[];
  totalPayment: number;
  projectedPayoffDate: string;
  totalInterestPaid: number;
}
```

## Implementation Steps

1. Add `MatSliderModule` and `FormsModule` to imports in `projections.ts`
2. Add `extraPayment` signal initialized to 0
3. Add slider HTML in the header card
4. Create `onExtraPaymentChange()` method that:
   - Builds `DebtAllocationRequest` from debt accounts
   - Calls `apiService.calculateDebtAllocation()`
   - Stores result to display comparison
5. Display comparison stats:
   - Current payoff date vs new payoff date
   - Interest saved with extra payments
   - Months saved

## Acceptance Criteria

- [ ] Slider appears on projections page (0-500 range, $25 steps)
- [ ] Moving slider calls the debt allocation API
- [ ] UI shows:
  - New projected debt-free date
  - Interest saved compared to minimum payments
  - Months/years saved
- [ ] Slider defaults to $0 (no extra payment)
- [ ] Works with no debt accounts (graceful empty state)

## Verification

```bash
cd dashboard && npm run build
# Manual: Open projections page, verify slider updates stats
```

## Existing Code References

- `CalculatorEndpoints.cs:89-108` - Backend debt allocation endpoint
- `api.service.ts:87-89` - Frontend API method
- `api.models.ts` - Type definitions (verify DebtAllocationRequest/Result exist)
