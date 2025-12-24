import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { DashboardPage } from './dashboard';
import { ApiService } from '../../core/services/api.service';
import { ProjectionService } from '../../core/services/projection.service';
import { of, delay } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';

// Mock data
const mockAccounts = [
  { id: 1, name: 'Checking', type: 'Cash' as const, initialBalance: 5000, currentBalance: 5500, annualPercentageRate: 0 },
  { id: 2, name: 'Savings', type: 'Cash' as const, initialBalance: 10000, currentBalance: 10000, annualPercentageRate: 0.01 },
  { id: 3, name: 'Credit Card', type: 'Debt' as const, initialBalance: 2000, currentBalance: 1800, annualPercentageRate: 0.1999, minimumPayment: 50 },
  { id: 4, name: '401k', type: 'Investment' as const, initialBalance: 50000, currentBalance: 52000, annualPercentageRate: 0.07 }
];

const mockEvents = [
  { id: 1, date: '2025-01-15', type: 'Expense', amount: 50, description: 'Groceries', accountId: 1, targetAccountId: null, status: 'Cleared' as const },
  { id: 2, date: '2025-01-14', type: 'Income', amount: 2500, description: 'Salary', accountId: 1, targetAccountId: null, status: 'Cleared' as const }
];

const mockSettings = {
  payFrequency: 'BiWeekly' as const,
  paycheckAmount: 2500,
  safetyBuffer: 100,
  nextPaycheckDate: '2025-01-31'
};

const mockSpendableResult = {
  spendableNow: 1000,
  expectedCashAtNextPaycheck: 500,
  nextPaycheckDate: '2025-01-31',
  breakdown: {
    availableCash: 15500,
    totalObligations: 0,
    safetyBuffer: 100,
    plannedContributions: 0,
    daysUntilNextPaycheck: 14
  },
  burnRate: {
    daily7Day: 50,
    daily30Day: 45
  }
};

describe('DashboardPage', () => {
  let component: DashboardPage;
  let fixture: ComponentFixture<DashboardPage>;
  let mockApiService: jest.Mocked<Partial<ApiService>>;
  let mockProjectionService: Partial<ProjectionService>;

  const createMockApiService = (overrides = {}) => ({
    getAccounts: jest.fn().mockReturnValue(of(mockAccounts)),
    getRecentEvents: jest.fn().mockReturnValue(of(mockEvents)),
    getSettings: jest.fn().mockReturnValue(of(mockSettings)),
    calculateSpendable: jest.fn().mockReturnValue(of(mockSpendableResult)),
    ...overrides
  });

  const createMockProjectionService = () => ({
    debtChartData: signal(null),
    investmentChartData: signal(null),
    loading: signal(false),
    calculateProjections: jest.fn().mockReturnValue(of(null))
  });

  beforeEach(async () => {
    mockApiService = createMockApiService();
    mockProjectionService = createMockProjectionService();

    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        { provide: ProjectionService, useValue: mockProjectionService },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPage);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should call API services on initialization', () => {
    // Component constructor calls loadDashboardData which triggers API calls
    expect(mockApiService.getAccounts).toHaveBeenCalled();
    expect(mockApiService.getRecentEvents).toHaveBeenCalledWith(10);
    expect(mockApiService.getSettings).toHaveBeenCalled();
  });

  it('should load accounts on init', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockApiService.getAccounts).toHaveBeenCalled();
    expect(component.accounts().length).toBe(4);
  }));

  it('should calculate total cash from cash accounts', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    // Cash = Checking (5500) + Savings (10000) = 15500
    expect(component.totalCash()).toBe(15500);
  }));

  it('should calculate total debt from debt accounts', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    // Debt = Credit Card (1800)
    expect(component.totalDebt()).toBe(1800);
  }));

  it('should calculate total investments from investment accounts', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    // Investments = 401k (52000)
    expect(component.totalInvestments()).toBe(52000);
  }));

  it('should load recent events', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockApiService.getRecentEvents).toHaveBeenCalledWith(10);
    expect(component.recentEvents().length).toBe(2);
  }));

  it('should set loading to false after data loads', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(component.loading()).toBe(false);
  }));

  it('should call calculateSpendable with correct data', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(mockApiService.calculateSpendable).toHaveBeenCalled();
    const callArg = mockApiService.calculateSpendable.mock.calls[0][0];
    expect(callArg.availableCash).toBe(15500);
    expect(callArg.manualSafetyBuffer).toBe(100);
  }));

  it('should handle empty accounts gracefully', fakeAsync(() => {
    mockApiService.getAccounts = jest.fn().mockReturnValue(of([]));

    fixture = TestBed.createComponent(DashboardPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    tick();

    expect(component.totalCash()).toBe(0);
    expect(component.totalDebt()).toBe(0);
    expect(component.totalInvestments()).toBe(0);
    expect(component.loading()).toBe(false);
  }));

  it('should format currency correctly', () => {
    expect(component.formatCurrency(1234.56)).toBe('$1,234.56');
    expect(component.formatCurrency(0)).toBe('$0.00');
    expect(component.formatCurrency(undefined)).toBe('$0.00');
  });

  it('should return correct event icons', () => {
    expect(component.getEventIcon('Income')).toBe('account_balance');
    expect(component.getEventIcon('Expense')).toBe('shopping_cart');
    expect(component.getEventIcon('DebtPayment')).toBe('payment');
    expect(component.getEventIcon('DebtCharge')).toBe('credit_card');
    expect(component.getEventIcon('SavingsContribution')).toBe('savings');
    expect(component.getEventIcon('InvestmentContribution')).toBe('trending_up');
    expect(component.getEventIcon('Unknown')).toBe('receipt');
  });

  it('should identify income vs expense correctly', () => {
    expect(component.isIncome('Income')).toBe(true);
    expect(component.isIncome('Expense')).toBe(false);

    expect(component.isExpense('Expense')).toBe(true);
    expect(component.isExpense('DebtPayment')).toBe(true);
    expect(component.isExpense('Income')).toBe(false);
  });

  it('should get account name by id', fakeAsync(() => {
    fixture.detectChanges();
    tick();

    expect(component.getAccountName(1)).toBe('Checking');
    expect(component.getAccountName(3)).toBe('Credit Card');
    expect(component.getAccountName(999)).toBe('Unknown');
    expect(component.getAccountName(undefined)).toBe('Unknown');
  }));
});
