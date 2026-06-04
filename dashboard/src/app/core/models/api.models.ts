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

// Category models
export type CategoryType = 'Recurring' | 'OneTime';

export interface Category {
  id: number;
  name: string;
  type: CategoryType;
  icon?: string;
  color?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  type: string;
  icon?: string;
  color?: string;
  sortOrder?: number;
}

export interface UpdateCategoryRequest {
  name?: string;
  type?: string;
  icon?: string;
  color?: string;
  sortOrder?: number;
  isActive?: boolean;
}

// Budget models
export type BudgetFrequency = 'Monthly' | 'BiWeekly' | 'Weekly';

export interface Budget {
  id: number;
  categoryId: number;
  categoryName: string;
  amount: number;
  frequency: BudgetFrequency;
  effectiveDate: string;
  endDate?: string;
  linkedAccountId?: number;
  linkedAccountName?: string;
  notes?: string;
  isActive: boolean;
}

export interface CreateBudgetRequest {
  categoryId: number;
  amount: number;
  frequency: string;
  effectiveDate: string;
  endDate?: string;
  linkedAccountId?: number;
  notes?: string;
}

export interface UpdateBudgetRequest {
  categoryId?: number;
  amount?: number;
  frequency?: string;
  effectiveDate?: string;
  endDate?: string;
  clearEndDate?: boolean;
  linkedAccountId?: number;
  clearLinkedAccount?: boolean;
  notes?: string;
  isActive?: boolean;
}

// Event models
export type EventStatus = 'Pending' | 'Cleared';

export interface FinancialEvent {
  id: number;
  date: string;
  type: string;
  amount: number;
  description: string;
  accountId?: number;
  targetAccountId?: number;
  categoryId?: number;
  categoryName?: string;
  status: EventStatus;
}

export interface CreateEventRequest {
  date: string;
  type: string;
  amount: number;
  description?: string;
  accountId?: number;
  targetAccountId?: number;
  categoryId?: number;
}

export interface UpdateEventRequest {
  date?: string;
  type?: string;
  amount?: number;
  description?: string;
  accountId?: number;
  targetAccountId?: number;
  categoryId?: number;
  clearCategory?: boolean;
}

export interface UpdateStatusRequest {
  status: EventStatus;
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

// Settings models
export type TimeHorizon = 'NextPaycheck' | 'CurrentMonth' | 'RollingTwoWeeks';

export interface UserSettings {
  payFrequency: 'Weekly' | 'BiWeekly' | 'SemiMonthly' | 'Monthly';
  paycheckAmount: number;
  safetyBuffer: number;
  nextPaycheckDate?: string;
  preferredTimeHorizon: TimeHorizon;
}

export interface UpdateSettingsRequest {
  payFrequency: string;
  paycheckAmount: number;
  safetyBuffer: number;
  nextPaycheckDate?: string;
  preferredTimeHorizon?: string;
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

// Recurring Contribution models
export type ContributionFrequency =
  | 'Weekly'
  | 'BiWeekly'
  | 'SemiMonthly'
  | 'Monthly'
  | 'Quarterly'
  | 'Annually';

export interface RecurringContribution {
  id: number;
  name: string;
  amount: number;
  frequency: ContributionFrequency;
  nextContributionDate: string;
  sourceAccountId: number;
  targetAccountId: number;
  sourceAccountName?: string;
  targetAccountName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateRecurringContributionRequest {
  name: string;
  amount: number;
  frequency: string;
  nextContributionDate: string;
  sourceAccountId: number;
  targetAccountId: number;
}

export interface UpdateRecurringContributionRequest {
  name: string;
  amount: number;
  frequency: string;
  nextContributionDate: string;
  sourceAccountId: number;
  targetAccountId: number;
  isActive: boolean;
}

// Goal models
export type GoalType = 'DebtFree' | 'InvestmentTarget' | 'SavingsGoal' | 'NetWorthMilestone';
export type GoalStatus = 'OnTrack' | 'Ahead' | 'AtRisk' | 'Behind';

export interface GoalProgressSummary {
  currentValue: number;
  targetValue: number;
  progressPercentage: number;
  status: GoalStatus;
  monthsRemaining: number;
  statusMessage: string;
}

export interface Goal {
  id: number;
  name: string;
  type: GoalType;
  targetAmount?: number;
  targetDate: string;
  linkedAccountIds: number[];
  priority: number;
  notes?: string;
  isActive: boolean;
  progress: GoalProgressSummary;
}

export interface GoalProgress {
  goalId: number;
  goalName: string;
  currentValue: number;
  targetValue: number;
  progressPercentage: number;
  requiredMonthlyAmount: number;
  projectedCompletionDate?: string;
  status: GoalStatus;
  monthsRemaining: number;
  amountRemaining: number;
  statusMessage: string;
}

export interface CreateGoalRequest {
  name: string;
  type: string;
  targetAmount?: number;
  targetDate: string;
  linkedAccountIds?: number[];
  priority?: number;
  notes?: string;
}

export interface UpdateGoalRequest {
  name?: string;
  type?: string;
  targetAmount?: number;
  targetDate?: string;
  linkedAccountIds?: number[];
  priority?: number;
  notes?: string;
  isActive?: boolean;
}

// Chart-specific models
export type ChartGranularity = 'Daily' | 'Weekly' | 'Monthly';

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

export interface NetWorthChartData {
  dates: string[];
  netWorth: number[];
  investments: number[];
  debt: number[];
}

// Safe-to-Spend Models
export type SafeToSpendStatus = 'Healthy' | 'Tight' | 'AtRisk' | 'Behind';

export interface SafeToSpendResult {
  safeToSpend: number;
  status: SafeToSpendStatus;
  breakdown: SafeToSpendBreakdown;
  goalImpacts: GoalImpact[];
  statusMessage: string;
  horizonEndDate: string;
}

export interface SafeToSpendBreakdown {
  availableCash: number;
  upcomingBills: number;
  requiredGoalContributions: number;
  minimumBuffer: number;
  daysInHorizon: number;
}

export interface GoalImpact {
  goalId: number;
  goalName: string;
  goalType: string;
  currentStatus: string;
  requiredMonthlyContribution: number;
  currentMonthlyContribution: number;
  contributionGap: number;
  delayedMonths?: number;
  impactMessage: string;
}

export interface BudgetAnalysisResult {
  overspentCategories: BudgetOverspend[];
  totalOverspend: number;
  overallGoalImpacts: GoalImpact[];
  hasOverspending: boolean;
}

export interface BudgetOverspend {
  categoryId: number;
  categoryName: string;
  budgetAmount: number;
  spentAmount: number;
  overspendAmount: number;
  goalImpacts: GoalImpact[];
}

export interface SuggestionsResult {
  suggestions: Suggestion[];
  hasUrgentSuggestions: boolean;
  totalPotentialSavings: number;
}

export type SuggestionCategory = 'ReduceSpending' | 'IncreaseContribution' | 'Emergency' | 'Warning' | 'Optimization' | 'Positive';
export type SuggestionPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type SuggestionActionType = 'None' | 'ReduceBudgetCategory' | 'IncreaseGoalContribution' | 'ReviewBudgets' | 'IncreaseBuffer' | 'Monitor';

export interface Suggestion {
  id: string;
  category: SuggestionCategory;
  title: string;
  description: string;
  priority: SuggestionPriority;
  potentialSavings?: number;
  actionType: SuggestionActionType;
  actionTarget?: string;
  impactOnGoals: string[];
}

export interface FullSafeToSpendReport {
  safeToSpend: SafeToSpendResult;
  budgetAnalysis: BudgetAnalysisResult;
  suggestions: SuggestionsResult;
  calculatedAt: string;
}

// Scenario Planning Models
export interface ScenarioRequest {
  monthlyDiscretionary: number;
  extraDebtPayment: number;
  extraInvestmentContribution: number;
}

export interface ScenarioDefaultsResponse {
  baseDiscretionary: number;
  baseDebtPayment: number;
  baseInvestmentContribution: number;
  sliderRanges: SliderRanges;
}

export interface SliderRanges {
  discretionaryMin: number;
  discretionaryMax: number;
  extraDebtMin: number;
  extraDebtMax: number;
  extraInvestmentMin: number;
  extraInvestmentMax: number;
}

export interface ScenarioResponse {
  adjustedSafeToSpend: number;
  monthlySurplus: number;
  debtProjection: ScenarioDebtProjection;
  investmentProjection: ScenarioInvestmentProjection;
  netWorthProjection: ScenarioNetWorthProjection;
  comparison: ScenarioComparison;
  sliderSummary: SliderSummary;
}

export interface ScenarioDebtProjection {
  monthsToPayoff: number | null;
  totalInterestPaid: number;
  finalPayoffDate: string | null;
  monthlySnapshots: ScenarioDebtSnapshot[];
}

export interface ScenarioDebtSnapshot {
  month: number;
  date: string;
  remainingBalance: number;
  interestPaid: number;
  principalPaid: number;
}

export interface ScenarioInvestmentProjection {
  projectedValue: number;
  totalContributions: number;
  totalGrowth: number;
  monthlySnapshots: ScenarioInvestmentSnapshot[];
}

export interface ScenarioInvestmentSnapshot {
  month: number;
  date: string;
  value: number;
  contributions: number;
  growth: number;
}

export interface ScenarioNetWorthProjection {
  projectedNetWorth: number;
  netWorthChange: number;
  monthlySnapshots: ScenarioNetWorthSnapshot[];
}

export interface ScenarioNetWorthSnapshot {
  month: number;
  date: string;
  cash: number;
  debt: number;
  investments: number;
  netWorth: number;
}

export interface ScenarioComparison {
  monthsSavedOnDebt: number;
  interestSaved: number;
  additionalInvestmentGrowth: number;
  netBenefit: number;
}

export interface SliderSummary {
  monthlyDiscretionary: number;
  extraDebtPayment: number;
  extraInvestmentContribution: number;
  totalMonthlyChange: number;
}

// Import Models
export interface ImportPreviewRequest {
  fileName: string;
  fileContent: string; // Base64 encoded
  accountId?: number;
  mapping?: ColumnMapping;
}

export type AmountConvention = 'Standard' | 'CreditCard';

export interface ColumnMapping {
  dateColumn: number;
  descriptionColumn: number;
  amountColumn: number;
  debitColumn?: number;
  creditColumn?: number;
  categoryColumn?: number;
  dateFormat: string;
  hasHeaderRow: boolean;
  // 'Standard' = positive is income (bank account); 'CreditCard' = positive is a charge.
  amountConvention?: AmountConvention;
}

export interface ImportPreviewResponse {
  sessionId: string;
  headers: string[];
  sampleRows: string[][];
  totalRows: number;
  detectedMapping?: ColumnMapping;
  previewTransactions: ImportPreviewRow[];
  warnings: string[];
  errors: string[];
}

export interface ImportPreviewRow {
  rowNumber: number;
  date: string;
  description: string;
  amount: number;
  category?: string;
  isDuplicate: boolean;
  existingTransactionId?: number;
  isValid: boolean;
  validationError?: string;
  selected: boolean;
}

export interface ImportCommitRequest {
  sessionId: string;
  accountId: number;
  mapping: ColumnMapping;
  selectedRows?: number[];
  includeDuplicates: boolean;
}

export interface ImportCommitResponse {
  importedCount: number;
  skippedCount: number;
  duplicateCount: number;
  errorCount: number;
  errors: string[];
}

// Credit Action Plan Models
export type DebtStrategy = 'Avalanche' | 'Snowball' | 'Hybrid';

export interface CreditActionPlanRequest {
  windfall: number;
  emergencyFundMonths?: number;
  monthlyEssentialExpenses?: number;
  strategy?: DebtStrategy;
}

export interface PlanDebtSummary {
  name: string;
  balance: number;
  effectiveAPR: number;
  minimumPayment: number;
}

export interface CreditActionPlanDefaultsResponse {
  suggestedWindfall: number;
  monthlyEssentialExpenses: number;
  monthlyIncome: number;
  defaultEmergencyFundMonths: number;
  debts: PlanDebtSummary[];
}

export interface DebtActionStep {
  order: number;
  debtName: string;
  effectiveAPR: number;
  startingBalance: number;
  minimumPayment: number;
  lumpSumApplied: number;
  balanceAfterLumpSum: number;
  isFullyPaid: boolean;
  monthsToPayoffBefore: number | null;
  monthsToPayoffAfter: number | null;
  interestBefore: number;
  interestAfter: number;
  interestSaved: number;
}

export interface CreditActionPlanResponse {
  strategy: DebtStrategy;
  emergencyFundTarget: number;
  emergencyFundReserved: number;
  isEmergencyFundFunded: boolean;
  monthsOfExpensesCovered: number;
  windfallTotal: number;
  windfallToDebt: number;
  windfallRemaining: number;
  totalDebtBefore: number;
  totalDebtAfter: number;
  totalInterestSaved: number;
  monthsToDebtFreeBefore: number;
  monthsToDebtFreeAfter: number;
  steps: DebtActionStep[];
  recommendations: string[];
}