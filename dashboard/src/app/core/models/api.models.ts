// Account models
export interface Account {
  id: number;
  name: string;
  type: 'Cash' | 'Debt' | 'Investment';
  initialBalance: number;
  annualPercentageRate?: number;
  minimumPayment?: number;
  currentBalance: number;
  promotionalAnnualPercentageRate?: number;
  promotionalPeriodEndDate?: string;
  balanceTransferFeePercentage?: number;
  statementDayOfMonth?: number;
  statementDateOverride?: string;
  paymentDueDayOfMonth?: number;
  paymentDueDateOverride?: string;
  effectiveAnnualPercentageRate?: number;
}

export interface CreateAccountRequest {
  name: string;
  type: string;
  initialBalance: number;
  annualPercentageRate?: number;
  minimumPayment?: number;
  promotionalAnnualPercentageRate?: number;
  promotionalPeriodEndDate?: string;
  balanceTransferFeePercentage?: number;
  statementDayOfMonth?: number;
  statementDateOverride?: string;
  paymentDueDayOfMonth?: number;
  paymentDueDateOverride?: string;
}

// Event models
export interface FinancialEvent {
  id: number;
  date: string;
  type: string;
  amount: number;
  description: string;
  accountId?: number;
  targetAccountId?: number;
}

export interface CreateEventRequest {
  date: string;
  type: string;
  amount: number;
  description?: string;
  accountId?: number;
  targetAccountId?: number;
}

// Calculator models
export interface SpendableRequest {
  availableCash: number;
  calculationDate: string;
  obligations?: Obligation[];
  upcomingIncome?: Income[];
  manualSafetyBuffer?: number;
}

export interface Obligation {
  dueDate: string;
  amount: number;
  description: string;
}

export interface Income {
  date: string;
  amount: number;
  description: string;
}

export interface SpendableResult {
  spendableNow: number;
  expectedCashAtNextPaycheck: number;
  nextPaycheckDate?: string;
  breakdown: {
    availableCash: number;
    totalObligations: number;
    safetyBuffer: number;
    plannedContributions: number;
    daysUntilNextPaycheck: number;
  };
  conservativeScenario?: {
    scenarioName: string;
    estimatedDailySpend: number;
    spendableAmount: number;
    expectedCashAtPaycheck: number;
  };
  burnRate: {
    daily7Day: number;
    daily30Day: number;
  };
}

export interface DebtAllocationRequest {
  debts: DebtInfo[];
  extraPaymentAmount: number;
  strategy: 'Avalanche' | 'Snowball' | 'Hybrid';
}

export interface DebtInfo {
  name: string;
  balance: number;
  annualPercentageRate: number;
  minimumPayment: number;
  promotionalAnnualPercentageRate?: number;
  promotionalPeriodEndDate?: string;
}

export interface DebtAllocationResult {
  paymentsByDebt: Record<string, DebtPayment>;
  totalPayment: number;
  strategyUsed: string;
}

export interface DebtPayment {
  debtName: string;
  minimumPayment: number;
  extraPayment: number;
  totalPayment: number;
  remainingBalance: number;
}

export interface SimulationRequest {
  startDate: string;
  endDate: string;
  initialCash: number;
  debts?: DebtInfo[];
  events?: SimulationEventInput[];
}

export interface SimulationEventInput {
  date: string;
  type: string;
  description: string;
  amount: number;
  relatedDebtName?: string;
}

export interface SimulationResult {
  debtFreeDate?: string;
  finalCashBalance: number;
  finalDebtBalances: Record<string, number>;
  totalInterestPaid: number;
  snapshots: SimulationSnapshot[];
}

export interface SimulationSnapshot {
  date: string;
  cashBalance: number;
  totalDebt: number;
  debtBalances: Record<string, number>;
}

// Settings models
export interface UserSettings {
  payFrequency: 'Weekly' | 'BiWeekly' | 'SemiMonthly' | 'Monthly';
  paycheckAmount: number;
  safetyBuffer: number;
  nextPaycheckDate?: string;
}

export interface UpdateSettingsRequest {
  payFrequency: string;
  paycheckAmount: number;
  safetyBuffer: number;
  nextPaycheckDate?: string;
}

// Investment Projection Models
export interface InvestmentProjectionRequest {
  initialBalance: number;
  startDate: string;
  endDate: string;
  nominalAnnualReturn: number;
  inflationRate: number;
  useMonthly: boolean;
  contributions?: ContributionDto[];
}

export interface ContributionDto {
  date: string;
  amount: number;
}

export interface InvestmentProjectionResult {
  finalNominalValue: number;
  finalRealValue: number;
  totalContributions: number;
  totalNominalGrowth: number;
  totalRealGrowth: number;
  projections: InvestmentProjectionPoint[];
}

export interface InvestmentProjectionPoint {
  date: string;
  nominalValue: number;
  realValue: number;
}

// Simulation Models (for debt)
export interface SimulationRequest {
  startDate: string;
  endDate: string;
  initialCash: number;
  debts?: SimDebtDto[];
  events?: SimEventDto[];
}

export interface SimDebtDto {
  name: string;
  balance: number;
  annualPercentageRate: number;
  minimumPayment: number;
  promotionalAnnualPercentageRate?: number;
  promotionalPeriodEndDate?: string;
}

export interface SimEventDto {
  date: string;
  type: string;
  description: string;
  amount: number;
  relatedDebtName?: string;
}

export interface SimulationResult {
  debtFreeDate?: string;
  finalCashBalance: number;
  finalDebtBalances: Record<string, number>;
  totalInterestPaid: number;
  snapshots: SimulationSnapshot[];
}

export interface SimulationSnapshot {
  date: string;
  cashBalance: number;
  totalDebt: number;
  debtBalances: Record<string, number>;
}

// Chart-specific models
export interface DebtChartData {
  dates: string[];
  debtBalances: number[];
  interestPaid?: number[];
}

export interface InvestmentChartData {
  dates: string[];
  values: number[];
  contributions?: number[];
}
