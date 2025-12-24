import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { TransactionsPage } from './transactions';
import { ApiService } from '../../core/services/api.service';
import { of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';

// Mock data
const mockAccounts = [
  { id: 1, name: 'Checking', type: 'Cash' as const, initialBalance: 5000, currentBalance: 5500, annualPercentageRate: 0 },
  { id: 2, name: 'Savings', type: 'Cash' as const, initialBalance: 10000, currentBalance: 10000, annualPercentageRate: 0.01 },
  { id: 3, name: 'Credit Card', type: 'Debt' as const, initialBalance: 2000, currentBalance: 1800, annualPercentageRate: 0.1999, minimumPayment: 50 },
  { id: 4, name: '401k', type: 'Investment' as const, initialBalance: 50000, currentBalance: 52000, annualPercentageRate: 0.07 }
];

const mockEvents = [
  { id: 1, date: '2025-01-15', type: 'Expense', amount: 50, description: 'Groceries', accountId: 1, targetAccountId: null, status: 'Cleared' as const },
  { id: 2, date: '2025-01-14', type: 'Income', amount: 2500, description: 'Salary', accountId: 1, targetAccountId: null, status: 'Pending' as const },
  { id: 3, date: '2025-01-13', type: 'DebtPayment', amount: 200, description: 'Card payment', accountId: 1, targetAccountId: 3, status: 'Cleared' as const }
];

describe('TransactionsPage', () => {
  let component: TransactionsPage;
  let fixture: ComponentFixture<TransactionsPage>;
  let mockApiService: jest.Mocked<Partial<ApiService>>;

  const createMockApiService = (overrides = {}) => ({
    getAccounts: jest.fn().mockReturnValue(of(mockAccounts)),
    getRecentEvents: jest.fn().mockReturnValue(of(mockEvents)),
    createEvent: jest.fn().mockReturnValue(of({ id: 99, ...mockEvents[0] })),
    updateEvent: jest.fn().mockReturnValue(of(mockEvents[0])),
    deleteEvent: jest.fn().mockReturnValue(of(undefined)),
    updateEventStatus: jest.fn().mockReturnValue(of({ ...mockEvents[1], status: 'Cleared' as const })),
    ...overrides
  });

  beforeEach(async () => {
    mockApiService = createMockApiService();

    await TestBed.configureTestingModule({
      imports: [TransactionsPage],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionsPage);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should have 6 event types defined', () => {
    expect(component.eventTypes.length).toBe(6);
    expect(component.eventTypes.map(t => t.value)).toEqual([
      'Income', 'Expense', 'DebtPayment', 'DebtCharge', 'SavingsContribution', 'InvestmentContribution'
    ]);
  });

  it('should load accounts and events on init', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockApiService.getAccounts).toHaveBeenCalled();
    expect(mockApiService.getRecentEvents).toHaveBeenCalledWith(30);
    expect(component.accounts().length).toBe(4);
    expect(component.recentEvents().length).toBe(3);
  }));

  it('should filter cash accounts correctly', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    const cashAccounts = component.cashAccounts();
    expect(cashAccounts.length).toBe(2);
    expect(cashAccounts.every(a => a.type === 'Cash')).toBe(true);
  }));

  it('should show target account for DebtPayment', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    component.selectedType.set('DebtPayment');
    expect(component.showTargetAccount()).toBe(true);
  }));

  it('should show target account for SavingsContribution', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    component.selectedType.set('SavingsContribution');
    expect(component.showTargetAccount()).toBe(true);
  }));

  it('should show target account for InvestmentContribution', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    component.selectedType.set('InvestmentContribution');
    expect(component.showTargetAccount()).toBe(true);
  }));

  it('should hide target account for Expense', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    component.selectedType.set('Expense');
    expect(component.showTargetAccount()).toBe(false);
  }));

  it('should hide target account for Income', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    component.selectedType.set('Income');
    expect(component.showTargetAccount()).toBe(false);
  }));

  it('should filter target accounts to Debt for DebtPayment', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    component.selectedType.set('DebtPayment');
    const targetAccounts = component.filteredTargetAccounts();
    expect(targetAccounts.length).toBe(1);
    expect(targetAccounts[0].type).toBe('Debt');
  }));

  it('should filter target accounts to Investment for SavingsContribution', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    component.selectedType.set('SavingsContribution');
    const targetAccounts = component.filteredTargetAccounts();
    expect(targetAccounts.length).toBe(1);
    expect(targetAccounts[0].type).toBe('Investment');
  }));

  it('should calculate pending count correctly', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(component.pendingCount()).toBe(1); // Only mockEvents[1] is Pending
  }));

  it('should filter events by status', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    // All events
    expect(component.filteredEvents().length).toBe(3);

    // Pending only
    component.setStatusFilter('Pending');
    expect(component.filteredEvents().length).toBe(1);
    expect(component.filteredEvents()[0].status).toBe('Pending');

    // Cleared only
    component.setStatusFilter('Cleared');
    expect(component.filteredEvents().length).toBe(2);
    expect(component.filteredEvents().every(e => e.status === 'Cleared')).toBe(true);

    // Back to all
    component.setStatusFilter('All');
    expect(component.filteredEvents().length).toBe(3);
  }));

  it('should toggle status from Pending to Cleared', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    const pendingEvent = mockEvents[1]; // status: Pending
    component.toggleStatus(pendingEvent);
    tick();

    expect(mockApiService.updateEventStatus).toHaveBeenCalledWith(pendingEvent.id, 'Cleared');
  }));

  it('should toggle status from Cleared to Pending', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    const clearedEvent = mockEvents[0]; // status: Cleared
    component.toggleStatus(clearedEvent);
    tick();

    expect(mockApiService.updateEventStatus).toHaveBeenCalledWith(clearedEvent.id, 'Pending');
  }));

  it('should require amount field', () => {
    const amountControl = component.transactionForm.get('amount');
    amountControl?.setValue(null);
    expect(amountControl?.valid).toBe(false);
    expect(amountControl?.hasError('required')).toBe(true);
  });

  it('should require minimum amount of 0.01', () => {
    const amountControl = component.transactionForm.get('amount');
    amountControl?.setValue(0);
    expect(amountControl?.valid).toBe(false);
    expect(amountControl?.hasError('min')).toBe(true);

    amountControl?.setValue(0.01);
    expect(amountControl?.hasError('min')).toBe(false);
  });

  it('should have maximum amount of 1000000', () => {
    const amountControl = component.transactionForm.get('amount');
    amountControl?.setValue(1000001);
    expect(amountControl?.hasError('max')).toBe(true);

    amountControl?.setValue(1000000);
    expect(amountControl?.hasError('max')).toBe(false);
  });

  it('should get account name by id', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(component.getAccountName(1)).toBe('Checking');
    expect(component.getAccountName(3)).toBe('Credit Card');
    expect(component.getAccountName(999)).toBe('-');
    expect(component.getAccountName(undefined)).toBe('-');
  }));

  it('should return correct event type colors', () => {
    expect(component.getEventTypeColor('Income')).toBe('primary');
    expect(component.getEventTypeColor('Expense')).toBe('warn');
    expect(component.getEventTypeColor('DebtPayment')).toBe('accent');
    expect(component.getEventTypeColor('DebtCharge')).toBe('warn');
    expect(component.getEventTypeColor('SavingsContribution')).toBe('primary');
    expect(component.getEventTypeColor('InvestmentContribution')).toBe('accent');
    expect(component.getEventTypeColor('Unknown')).toBe('');
  });

  it('should format currency correctly', () => {
    expect(component.formatCurrency(1234.56)).toBe('$1,234.56');
    expect(component.formatCurrency(0)).toBe('$0.00');
  });

  it('should reset form correctly', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    // Set some values
    component.editingEventId.set(5);
    component.transactionForm.patchValue({
      type: 'Income',
      amount: 100,
      description: 'Test'
    });

    // Reset
    component.resetForm();

    expect(component.editingEventId()).toBeNull();
    expect(component.transactionForm.get('type')?.value).toBe('Expense');
    expect(component.transactionForm.get('amount')?.value).toBeNull();
    expect(component.transactionForm.get('description')?.value).toBe('');
  }));

  it('should set editing event when editEvent is called', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    const event = mockEvents[0];
    component.editEvent(event);

    expect(component.editingEventId()).toBe(event.id);
    expect(component.transactionForm.get('type')?.value).toBe(event.type);
    expect(component.transactionForm.get('amount')?.value).toBe(event.amount);
  }));
});
