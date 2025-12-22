import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Account,
  CreateAccountRequest,
  FinancialEvent,
  CreateEventRequest,
  UpdateEventRequest,
  SpendableRequest,
  SpendableResult,
  DebtAllocationRequest,
  DebtAllocationResult,
  SimulationRequest,
  SimulationResult,
  InvestmentProjectionRequest,
  InvestmentProjectionResult,
  UserSettings,
  UpdateSettingsRequest
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

  // Event endpoints
  getEvents(params?: {
    accountId?: number;
    type?: string;
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
}
