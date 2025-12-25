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
  InvestmentChartData,
  NetWorthChartData,
  SimulationSnapshot,
  ChartGranularity,
  RecurringContribution,
  ContributionDto
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProjectionService {
  private readonly apiService = inject(ApiService);

  // Reactive state
  readonly debtProjection = signal<SimulationResult | null>(null);
  readonly investmentProjection = signal<InvestmentProjectionResult | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  
  // Recurring contributions state
  private readonly recurringContributions = signal<RecurringContribution[]>([]);
  readonly includeContributions = signal(true);
  readonly granularity = signal<ChartGranularity>('Monthly');

  // Computed chart data
  readonly debtChartData = computed<DebtChartData | null>(() => {
    const projection = this.debtProjection();
    if (!projection || !projection.snapshots || projection.snapshots.length === 0) {
      return null;
    }

    const aggregated = this.aggregateSnapshots(projection.snapshots, this.granularity());

    return {
      dates: aggregated.map(s => s.date),
      debtBalances: aggregated.map(s => s.totalDebt)
    };
  });

  readonly investmentChartData = computed<InvestmentChartData | null>(() => {
    const projection = this.investmentProjection();
    if (!projection || !projection.projections || projection.projections.length === 0) {
      return null;
    }

    // Convert investment projections to snapshots format for aggregation
    const snapshots: SimulationSnapshot[] = projection.projections.map(p => ({
      date: p.date,
      cashBalance: 0,
      totalDebt: 0,
      debtBalances: {},
      nominalValue: p.nominalValue
    })) as SimulationSnapshot[];

    const aggregated = this.aggregateSnapshots(snapshots, this.granularity());

    return {
      dates: aggregated.map(s => s.date),
      values: aggregated.map(s => (s as any).nominalValue)
    };
  });

  readonly netWorthChartData = computed<NetWorthChartData | null>(() => {
    const debt = this.debtProjection();
    const investment = this.investmentProjection();

    // Need at least one projection
    if (!debt?.snapshots?.length && !investment?.projections?.length) {
      return null;
    }

    // Collect all unique dates
    const dateSet = new Set<string>();
    debt?.snapshots?.forEach(s => dateSet.add(s.date));
    investment?.projections?.forEach(p => dateSet.add(p.date));

    // Sort chronologically
    const dates = Array.from(dateSet).sort();

    // Calculate net worth at each date
    const combinedSnapshots: Array<{
      date: string;
      netWorth: number;
      investment: number;
      debt: number;
    }> = [];

    dates.forEach(date => {
      const debtAtDate = debt?.snapshots?.find(s => s.date === date)?.totalDebt ?? 0;
      const investmentAtDate = investment?.projections?.find(p => p.date === date)?.nominalValue ?? 0;
      
      combinedSnapshots.push({
        date,
        netWorth: investmentAtDate - debtAtDate,
        investment: investmentAtDate,
        debt: debtAtDate
      });
    });

    // Convert to snapshot format for aggregation
    const snapshots: SimulationSnapshot[] = combinedSnapshots.map(s => ({
      date: s.date,
      cashBalance: 0,
      totalDebt: s.debt,
      debtBalances: {},
      netWorth: s.netWorth,
      investment: s.investment
    })) as SimulationSnapshot[];

    const aggregated = this.aggregateSnapshots(snapshots, this.granularity());

    return {
      dates: aggregated.map(s => s.date),
      netWorth: aggregated.map(s => (s as any).netWorth),
      investments: aggregated.map(s => (s as any).investment),
      debt: aggregated.map(s => s.totalDebt)
    };
  });

  /**
   * Aggregates simulation snapshots based on the specified granularity.
   * - Daily: Returns all snapshots
   * - Weekly: Returns snapshots for Fridays (end of trading week)
   * - Monthly: Returns snapshots for last day of each month
   * Always includes the final snapshot to close the loop.
   */
  private aggregateSnapshots(snapshots: SimulationSnapshot[], granularity: ChartGranularity): SimulationSnapshot[] {
    if (granularity === 'Daily' || snapshots.length === 0) {
      return snapshots;
    }

    const result: SimulationSnapshot[] = [];
    let lastSnapshot: SimulationSnapshot | null = null;

    for (const snapshot of snapshots) {
      const date = new Date(snapshot.date);
      let shouldInclude = false;

      if (granularity === 'Weekly') {
        // Include Fridays (end of trading week)
        shouldInclude = date.getDay() === 5;
      } else if (granularity === 'Monthly') {
        // Include last day of month
        const nextDay = new Date(date);
        nextDay.setDate(date.getDate() + 1);
        shouldInclude = nextDay.getDate() === 1;
      }

      if (shouldInclude) {
        result.push(snapshot);
      }
      lastSnapshot = snapshot;
    }

    // Always include the final snapshot to close the loop
    if (lastSnapshot && !result.includes(lastSnapshot)) {
      result.push(lastSnapshot);
    }

    return result;
  }

  readonly crossoverDate = computed<string | null>(() => {
    const debt = this.debtProjection();
    const investment = this.investmentProjection();

    // Edge case: No debt or no investments
    if (!debt?.snapshots?.length || !investment?.projections?.length) {
      return null;
    }

    // Edge case: No debt means already "crossed over" (no interest to beat)
    const hasDebt = debt.snapshots.some(s => s.totalDebt > 0);
    if (!hasDebt) {
      return null;
    }

    // Edge case: No investments means can't cross over
    const hasInvestments = investment.projections.some(p => p.nominalValue > 0);
    if (!hasInvestments) {
      return null;
    }

    // Align dates - find common months between projections
    const debtMap = new Map(debt.snapshots.map(s => [s.date, s]));
    
    // Calculate weighted average APR from initial debt balances
    const initialDebts = debt.snapshots[0]?.debtBalances || {};
    let totalDebt = 0;
    let weightedAPR = 0;
    
    for (const [debtName, balance] of Object.entries(initialDebts)) {
      totalDebt += balance;
      // Use default 18% APR (typical credit card rate) as we don't have per-debt APR in snapshots
      weightedAPR += balance * 0.18;
    }
    
    const averageAPR = totalDebt > 0 ? weightedAPR / totalDebt : 0.18;

    // Find crossover point
    for (let i = 1; i < investment.projections.length; i++) {
      const currentDate = investment.projections[i].date;
      const debtSnapshot = debtMap.get(currentDate);
      
      if (!debtSnapshot) continue;

      // Calculate monthly investment return (growth from previous month)
      const monthlyInvestmentReturn = 
        investment.projections[i].nominalValue - investment.projections[i - 1].nominalValue;

      // Calculate monthly debt interest
      const monthlyDebtInterest = debtSnapshot.totalDebt * (averageAPR / 12);

      // Check if investment returns exceed debt interest
      if (monthlyInvestmentReturn > monthlyDebtInterest && debtSnapshot.totalDebt > 0) {
        return currentDate;
      }
    }

    // Crossover not reached in projection period
    return null;
  });

  // Load recurring contributions from API
  loadRecurringContributions(): void {
    this.apiService.getRecurringContributions().subscribe({
      next: (contributions) => {
        this.recurringContributions.set(contributions.filter(c => c.isActive));
      },
      error: (err) => {
        console.error('Error loading recurring contributions:', err);
        this.recurringContributions.set([]);
      }
    });
  }

  // Expand recurring contributions for a date range
  private expandSchedule(
    schedule: RecurringContribution,
    startDate: Date,
    endDate: Date
  ): ContributionDto[] {
    const contributions: ContributionDto[] = [];
    const current = new Date(schedule.nextContributionDate);

    while (current <= endDate) {
      if (current >= startDate) {
        contributions.push({
          date: current.toISOString(),
          amount: schedule.amount
        });
      }

      // Advance to next occurrence based on frequency
      switch (schedule.frequency) {
        case 'Weekly':
          current.setDate(current.getDate() + 7);
          break;
        case 'BiWeekly':
          current.setDate(current.getDate() + 14);
          break;
        case 'SemiMonthly':
          // Add 15 days (approximate)
          current.setDate(current.getDate() + 15);
          break;
        case 'Monthly':
          current.setMonth(current.getMonth() + 1);
          break;
        case 'Quarterly':
          current.setMonth(current.getMonth() + 3);
          break;
        case 'Annually':
          current.setFullYear(current.getFullYear() + 1);
          break;
      }
    }

    return contributions;
  }

  // Get all contributions for projection period
  getContributionsForProjection(
    startDate: Date,
    endDate: Date,
    targetAccountId?: number
  ): ContributionDto[] {
    if (!this.includeContributions()) {
      return [];
    }

    const allContributions: ContributionDto[] = [];

    for (const schedule of this.recurringContributions()) {
      // Optionally filter by target account
      if (targetAccountId && schedule.targetAccountId !== targetAccountId) {
        continue;
      }

      const occurrences = this.expandSchedule(schedule, startDate, endDate);
      allContributions.push(...occurrences);
    }

    return allContributions.sort((a, b) =>
      new Date(a.date).getTime() - new Date(b.date).getTime()
    );
  }

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
      0,
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

    // Load recurring contributions first if not already loaded
    const contributionsCall$ = this.recurringContributions().length === 0
      ? this.apiService.getRecurringContributions()
      : of(this.recurringContributions());

    return forkJoin({
      debt: debtCall$,
      investment: investmentCall$,
      contributions: contributionsCall$
    }).pipe(
      map(results => {
        this.debtProjection.set(results.debt);
        this.investmentProjection.set(results.investment);
        if (results.contributions) {
          this.recurringContributions.set(results.contributions.filter(c => c.isActive));
        }
        this.loading.set(false);
      })
    );
  }

  calculateDebtProjectionWithExtra(
    debtAccounts: Account[],
    timeRangeMonths: number,
    extraPayment: number,
    settings?: UserSettings
  ): Observable<SimulationResult | null> {
    if (debtAccounts.length === 0) {
      return of(null);
    }
    
    const startDate = new Date();
    const endDate = new Date();
    endDate.setMonth(endDate.getMonth() + timeRangeMonths);
    
    const request = this.buildDebtSimulationRequest(
      debtAccounts,
      startDate,
      endDate,
      extraPayment,
      settings
    );
    
    return this.apiService.runSimulation(request).pipe(
      catchError(err => {
        console.error('Error calculating debt projection with extra payment:', err);
        return of(null);
      })
    );
  }

  private buildDebtSimulationRequest(
    debtAccounts: Account[],
    startDate: Date,
    endDate: Date,
    extraPayment: number = 0,
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

    // Generate monthly payment events for each debt
    const events: SimEventDto[] = [];
    const current = new Date(startDate);

    while (current <= endDate) {
      for (const account of debtAccounts) {
        if (account.minimumPayment && account.minimumPayment > 0) {
          // Calculate total payment (minimum + proportional extra payment)
          const extraPerDebt = extraPayment / debtAccounts.length;
          const totalPayment = account.minimumPayment + extraPerDebt;
          
          // Create monthly payment event
          events.push({
            date: current.toISOString(),
            type: 'DebtPayment',
            description: `Payment for ${account.name}${extraPayment > 0 ? ' (with extra)' : ''}`,
            amount: totalPayment,
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

    // Get contributions for all investment accounts
    const contributions = this.getContributionsForProjection(startDate, endDate);

    return {
      initialBalance,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      nominalAnnualReturn,
      inflationRate: 0.03, // 3% inflation
      useMonthly: true, // Monthly granularity for better performance
      contributions
    };
  }
}
