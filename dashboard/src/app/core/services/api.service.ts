import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Account,
  CreateAccountRequest,
  Category,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  Budget,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  FinancialEvent,
  CreateEventRequest,
  UpdateEventRequest,
  UpdateStatusRequest,
  EventStatus,
  SpendableRequest,
  SpendableResult,
  DebtAllocationRequest,
  DebtAllocationResult,
  SimulationRequest,
  SimulationResult,
  InvestmentProjectionRequest,
  InvestmentProjectionResult,
  UserSettings,
  UpdateSettingsRequest,
  RecurringContribution,
  CreateRecurringContributionRequest,
  UpdateRecurringContributionRequest,
  Goal,
  GoalProgress,
  CreateGoalRequest,
  UpdateGoalRequest,
  SafeToSpendResult,
  BudgetAnalysisResult,
  SuggestionsResult,
  FullSafeToSpendReport,
  TimeHorizon,
  ScenarioRequest,
  ScenarioResponse,
  ScenarioDefaultsResponse
} from '../models/api.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = '/api';

  constructor(private http: HttpClient) {}

  // Account endpoints
  getAccounts(): Observable<Account[]> {
    return this.http.get<Account[]>(`${this.baseUrl}/accounts`);
  }

  getAccount(id: number): Observable<Account> {
    return this.http.get<Account>(`${this.baseUrl}/accounts/${id}`);
  }

  createAccount(request: CreateAccountRequest): Observable<Account> {
    return this.http.post<Account>(`${this.baseUrl}/accounts`, request);
  }

  updateAccount(id: number, request: Partial<CreateAccountRequest>): Observable<Account> {
    return this.http.put<Account>(`${this.baseUrl}/accounts/${id}`, request);
  }

  deleteAccount(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/accounts/${id}`);
  }

  getAccountBalance(id: number): Observable<{ accountId: number; balance: number }> {
    return this.http.get<{ accountId: number; balance: number }>(`${this.baseUrl}/accounts/${id}/balance`);
  }

  // Category endpoints
  getCategories(activeOnly: boolean = true): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`, { params: { activeOnly } });
  }

  getCategory(id: number): Observable<Category> {
    return this.http.get<Category>(`${this.baseUrl}/categories/${id}`);
  }

  createCategory(request: CreateCategoryRequest): Observable<Category> {
    return this.http.post<Category>(`${this.baseUrl}/categories`, request);
  }

  updateCategory(id: number, request: UpdateCategoryRequest): Observable<Category> {
    return this.http.put<Category>(`${this.baseUrl}/categories/${id}`, request);
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/categories/${id}`);
  }

  // Budget endpoints
  getBudgets(params?: { activeOnly?: boolean; categoryId?: number }): Observable<Budget[]> {
    return this.http.get<Budget[]>(`${this.baseUrl}/budgets`, { params: params as any });
  }

  getBudget(id: number): Observable<Budget> {
    return this.http.get<Budget>(`${this.baseUrl}/budgets/${id}`);
  }

  createBudget(request: CreateBudgetRequest): Observable<Budget> {
    return this.http.post<Budget>(`${this.baseUrl}/budgets`, request);
  }

  updateBudget(id: number, request: UpdateBudgetRequest): Observable<Budget> {
    return this.http.put<Budget>(`${this.baseUrl}/budgets/${id}`, request);
  }

  deleteBudget(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/budgets/${id}`);
  }

  // Event endpoints
  getEvents(params?: {
    accountId?: number;
    type?: string;
    status?: EventStatus;
    categoryId?: number;
    startDate?: string;
    endDate?: string;
    limit?: number;
  }): Observable<FinancialEvent[]> {
    return this.http.get<FinancialEvent[]>(`${this.baseUrl}/events`, { params: params as any });
  }

  getRecentEvents(days: number = 30): Observable<FinancialEvent[]> {
    return this.http.get<FinancialEvent[]>(`${this.baseUrl}/events/recent`, { params: { days } });
  }

  createEvent(request: CreateEventRequest): Observable<FinancialEvent> {
    return this.http.post<FinancialEvent>(`${this.baseUrl}/events`, request);
  }

  updateEvent(id: number, request: UpdateEventRequest): Observable<FinancialEvent> {
    return this.http.put<FinancialEvent>(`${this.baseUrl}/events/${id}`, request);
  }

  deleteEvent(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/events/${id}`);
  }

  updateEventStatus(id: number, status: EventStatus): Observable<FinancialEvent> {
    return this.http.patch<FinancialEvent>(`${this.baseUrl}/events/${id}/status`, { status });
  }

  // Calculator endpoints
  calculateSpendable(request: SpendableRequest): Observable<SpendableResult> {
    return this.http.post<SpendableResult>(`${this.baseUrl}/calculators/spendable`, request);
  }

  calculateDebtAllocation(request: DebtAllocationRequest): Observable<DebtAllocationResult> {
    return this.http.post<DebtAllocationResult>(`${this.baseUrl}/calculators/debt-allocation`, request);
  }

  runSimulation(request: SimulationRequest): Observable<SimulationResult> {
    return this.http.post<SimulationResult>(`${this.baseUrl}/calculators/simulation`, request);
  }

  calculateInvestmentProjection(request: InvestmentProjectionRequest): Observable<InvestmentProjectionResult> {
    return this.http.post<InvestmentProjectionResult>(
      `${this.baseUrl}/calculators/investment-projection`,
      request
    );
  }

  // Settings endpoints
  getSettings(): Observable<UserSettings> {
    return this.http.get<UserSettings>(`${this.baseUrl}/settings`);
  }

  updateSettings(request: UpdateSettingsRequest): Observable<UserSettings> {
    return this.http.put<UserSettings>(`${this.baseUrl}/settings`, request);
  }

  // Recurring Contribution endpoints
  getRecurringContributions(): Observable<RecurringContribution[]> {
    return this.http.get<RecurringContribution[]>(`${this.baseUrl}/recurring-contributions`);
  }

  createRecurringContribution(request: CreateRecurringContributionRequest): Observable<RecurringContribution> {
    return this.http.post<RecurringContribution>(`${this.baseUrl}/recurring-contributions`, request);
  }

  updateRecurringContribution(id: number, request: UpdateRecurringContributionRequest): Observable<RecurringContribution> {
    return this.http.put<RecurringContribution>(`${this.baseUrl}/recurring-contributions/${id}`, request);
  }

  deleteRecurringContribution(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/recurring-contributions/${id}`);
  }

  toggleRecurringContribution(id: number): Observable<RecurringContribution> {
    return this.http.patch<RecurringContribution>(`${this.baseUrl}/recurring-contributions/${id}/toggle`, {});
  }

  // Goal endpoints
  getGoals(activeOnly: boolean = true): Observable<Goal[]> {
    return this.http.get<Goal[]>(`${this.baseUrl}/goals`, { params: { activeOnly } });
  }

  getGoal(id: number): Observable<Goal> {
    return this.http.get<Goal>(`${this.baseUrl}/goals/${id}`);
  }

  getGoalProgress(id: number): Observable<GoalProgress> {
    return this.http.get<GoalProgress>(`${this.baseUrl}/goals/${id}/progress`);
  }

  createGoal(request: CreateGoalRequest): Observable<Goal> {
    return this.http.post<Goal>(`${this.baseUrl}/goals`, request);
  }

  updateGoal(id: number, request: UpdateGoalRequest): Observable<Goal> {
    return this.http.put<Goal>(`${this.baseUrl}/goals/${id}`, request);
  }

  deleteGoal(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/goals/${id}`);
  }

  // Safe-to-Spend endpoints
  getSafeToSpend(params?: {
    timeHorizon?: TimeHorizon;
    nextPaycheckDate?: string;
    minimumBuffer?: number;
  }): Observable<SafeToSpendResult> {
    return this.http.get<SafeToSpendResult>(`${this.baseUrl}/safe-to-spend`, { params: params as any });
  }

  getBudgetAnalysis(periodDays?: number): Observable<BudgetAnalysisResult> {
    const params = periodDays ? { periodDays } : {};
    return this.http.get<BudgetAnalysisResult>(`${this.baseUrl}/safe-to-spend/analysis`, { params: params as any });
  }

  getSuggestions(params?: {
    maxSuggestions?: number;
    timeHorizon?: TimeHorizon;
  }): Observable<SuggestionsResult> {
    return this.http.get<SuggestionsResult>(`${this.baseUrl}/safe-to-spend/suggestions`, { params: params as any });
  }

  getFullSafeToSpendReport(params?: {
    timeHorizon?: TimeHorizon;
    maxSuggestions?: number;
  }): Observable<FullSafeToSpendReport> {
    return this.http.get<FullSafeToSpendReport>(`${this.baseUrl}/safe-to-spend/full`, { params: params as any });
  }

  // Scenario endpoints
  getScenarioDefaults(): Observable<ScenarioDefaultsResponse> {
    return this.http.get<ScenarioDefaultsResponse>(`${this.baseUrl}/scenarios/defaults`);
  }

  calculateScenario(request: ScenarioRequest): Observable<ScenarioResponse> {
    return this.http.post<ScenarioResponse>(`${this.baseUrl}/scenarios/calculate`, request);
  }
}
