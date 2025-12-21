import { Injectable, inject, signal, computed } from '@angular/core';
import { forkJoin, Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  Account,
  UserSettings,
  SimulationRequest,
  SimulationResult,
  SimEventDto,
  SimDebtDto,
  InvestmentProjectionRequest,
  InvestmentProjectionResult,
  DebtChartData,
  InvestmentChartData
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProjectionService {
  private readonly apiService = inject(ApiService);

  // Reactive state
  readonly debtProjection = signal<SimulationResult | null>(null);
  readonly investmentProjection = signal<InvestmentProjectionResult | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // Computed chart data
  readonly debtChartData = computed<DebtChartData | null>(() => {
    const projection = this.debtProjection();
    if (!projection || !projection.snapshots || projection.snapshots.length === 0) {
      return null;
    }

    return {
      dates: projection.snapshots.map(s => s.date),
      debtBalances: projection.snapshots.map(s => s.totalDebt)
    };
  });

  readonly investmentChartData = computed<InvestmentChartData | null>(() => {
    const projection = this.investmentProjection();
    if (!projection || !projection.projections || projection.projections.length === 0) {
      return null;
    }

    return {
      dates: projection.projections.map(p => p.date),
      values: projection.projections.map(p => p.nominalValue)
    };
  });

  calculateProjections(
    accounts: Account[],
    timeRangeMonths: number = 12,
    settings?: UserSettings
  ): Observable<void> {
    this.loading.set(true);
    this.error.set(null);

    const startDate = new Date();
    const endDate = new Date();
    endDate.setMonth(endDate.getMonth() + timeRangeMonths);

    // Build requests
    const debtAccounts = accounts.filter(a => a.type === 'Debt');
    const investmentAccounts = accounts.filter(a => a.type === 'Investment');

    const debtRequest = this.buildDebtSimulationRequest(
      debtAccounts,
      startDate,
      endDate,
      settings
    );

    const investmentRequest = this.buildInvestmentProjectionRequest(
      investmentAccounts,
      startDate,
      endDate
    );

    // Only call APIs if we have accounts to project
    const debtCall$ = debtAccounts.length > 0
      ? this.apiService.runSimulation(debtRequest)
      : of(null);

    const investmentCall$ = investmentAccounts.length > 0
      ? this.apiService.calculateInvestmentProjection(investmentRequest)
      : of(null);

    return forkJoin({
      debt: debtCall$,
      investment: investmentCall$
    }).pipe(
      map(results => {
        this.debtProjection.set(results.debt);
        this.investmentProjection.set(results.investment);
        this.loading.set(false);
      })
    );
  }

  private buildDebtSimulationRequest(
    debtAccounts: Account[],
    startDate: Date,
    endDate: Date,
    settings?: UserSettings
  ): SimulationRequest {
    // Sum cash accounts for initial cash (or default to 0)
    const initialCash = 0;

    // Convert accounts to simulation debt format
    // APR is stored as decimal (0.18 for 18%)
    const debts: SimDebtDto[] = debtAccounts.map(a => ({
      name: a.name,
      balance: a.currentBalance,
      annualPercentageRate: a.annualPercentageRate || 0,
      minimumPayment: a.minimumPayment || 0,
      promotionalAnnualPercentageRate: a.promotionalAnnualPercentageRate ?? undefined,
      promotionalPeriodEndDate: a.promotionalPeriodEndDate
    }));

    // Generate monthly minimum payment events for each debt
    const events: SimEventDto[] = [];
    const current = new Date(startDate);

    while (current <= endDate) {
      for (const account of debtAccounts) {
        if (account.minimumPayment && account.minimumPayment > 0) {
          // Create monthly payment event
          events.push({
            date: current.toISOString(),
            type: 'DebtPayment',
            description: `Minimum payment for ${account.name}`,
            amount: account.minimumPayment,
            relatedDebtName: account.name
          });
        }
      }

      // Move to next month
      current.setMonth(current.getMonth() + 1);
    }

    return {
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      initialCash,
      debts,
      events
    };
  }

  private buildInvestmentProjectionRequest(
    investmentAccounts: Account[],
    startDate: Date,
    endDate: Date
  ): InvestmentProjectionRequest {
    // Sum all investment balances
    const initialBalance = investmentAccounts.reduce(
      (sum, a) => sum + a.currentBalance,
      0
    );

    // Use default 7% annual return (industry standard for diversified portfolio)
    // Backend expects decimal format (0.07 for 7%)
    const nominalAnnualReturn = 0.07;

    return {
      initialBalance,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      nominalAnnualReturn,
      inflationRate: 0.03, // 3% inflation
      useMonthly: true, // Monthly granularity for better performance
      contributions: [] // No recurring contributions for now
    };
  }
}
