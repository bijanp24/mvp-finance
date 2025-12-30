import { Component, ChangeDetectionStrategy, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSliderModule } from '@angular/material/slider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { forkJoin, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ApiService } from '../../core/services/api.service';
import { ProjectionService } from '../../core/services/projection.service';
import { DebtProjectionChartComponent } from '../../features/charts/debt-projection-chart.component';
import { InvestmentProjectionChartComponent } from '../../features/charts/investment-projection-chart.component';
import { NetWorthChartComponent } from '../../features/charts/net-worth-chart.component';
import { Account, UserSettings, SimulationResult, ChartGranularity, Goal } from '../../core/models/api.models';

@Component({
  selector: 'app-projections',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatCardModule,
    MatButtonToggleModule,
    MatSliderModule,
    MatSlideToggleModule,
    MatIconModule,
    MatProgressBarModule,
    DebtProjectionChartComponent,
    InvestmentProjectionChartComponent,
    NetWorthChartComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './projections.html',
  styleUrl: './projections.scss'
})
export class ProjectionsPage {
  private readonly apiService = inject(ApiService);
  private readonly projectionService = inject(ProjectionService);

  readonly timeRangeMonths = signal(12); // 1 year default
  readonly accounts = signal<Account[]>([]);
  readonly settings = signal<UserSettings | null>(null);
  readonly goals = signal<Goal[]>([]);
  readonly extraPayment = signal(0);
  readonly debtProjectionWithExtra = signal<SimulationResult | null>(null);

  private extraPaymentSubject = new Subject<number>();

  // Goals within projection timeframe
  readonly upcomingGoals = computed(() => {
    const now = new Date();
    const endDate = new Date();
    endDate.setMonth(endDate.getMonth() + this.timeRangeMonths());

    return this.goals()
      .filter(g => g.isActive)
      .filter(g => {
        const targetDate = new Date(g.targetDate);
        return targetDate >= now && targetDate <= endDate;
      })
      .sort((a, b) => new Date(a.targetDate).getTime() - new Date(b.targetDate).getTime());
  });

  // Goals on track or ahead
  readonly goalsOnTrack = computed(() => {
    return this.goals()
      .filter(g => g.isActive && (g.progress.status === 'OnTrack' || g.progress.status === 'Ahead'))
      .length;
  });

  // Goals at risk or behind
  readonly goalsAtRisk = computed(() => {
    return this.goals()
      .filter(g => g.isActive && (g.progress.status === 'AtRisk' || g.progress.status === 'Behind'))
      .length;
  });

  // Use effective projection (scenario or baseline) for charts
  readonly effectiveDebtProjection = computed(() => {
    return this.debtProjectionWithExtra() ?? this.projectionService.debtProjection();
  });

  // Re-computed chart data using the effective projection
  readonly debtChartData = computed(() => {
    return this.projectionService.getDebtChartData(this.effectiveDebtProjection());
  });

  readonly investmentChartData = this.projectionService.investmentChartData;

  readonly netWorthChartData = computed(() => {
    return this.projectionService.getNetWorthChartData(
      this.effectiveDebtProjection(),
      this.projectionService.investmentProjection()
    );
  });

  readonly loading = this.projectionService.loading;

  readonly debtProjection = this.projectionService.debtProjection;
  readonly investmentProjection = this.projectionService.investmentProjection;
  readonly crossoverDate = this.projectionService.crossoverDate;
  readonly includeContributions = this.projectionService.includeContributions;
  readonly granularity = this.projectionService.granularity;

  readonly debtComparison = computed(() => {
    const baseline = this.projectionService.debtProjection(); // Always compare against baseline
    const withExtra = this.effectiveDebtProjection();
    
    if (!baseline || !withExtra || !baseline.debtFreeDate || !withExtra.debtFreeDate) {
      return null;
    }
    
    const baselineDate = new Date(baseline.debtFreeDate);
    const withExtraDate = new Date(withExtra.debtFreeDate);
    const monthsSaved = Math.round((baselineDate.getTime() - withExtraDate.getTime()) / (1000 * 60 * 60 * 24 * 30));
    const interestSaved = baseline.totalInterestPaid - withExtra.totalInterestPaid;
    
    return {
      newPayoffDate: withExtra.debtFreeDate,
      monthsSaved,
      interestSaved
    };
  });

  readonly totalScheduledContributions = computed(() => {
    const startDate = new Date();
    const endDate = new Date();
    endDate.setMonth(endDate.getMonth() + this.timeRangeMonths());
    
    const contributions = this.projectionService.getContributionsForProjection(
      startDate,
      endDate
    );
    return contributions.reduce((sum, c) => sum + c.amount, 0);
  });

  readonly contributionCount = computed(() => {
    const startDate = new Date();
    const endDate = new Date();
    endDate.setMonth(endDate.getMonth() + this.timeRangeMonths());
    
    return this.projectionService.getContributionsForProjection(
      startDate,
      endDate
    ).length;
  });

  constructor() {
    this.loadData();
    
    // Debounce slider inputs to avoid API spam
    this.extraPaymentSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(amount => {
      this.calculateExtraPaymentScenario(amount);
    });
  }

  loadData(): void {
    forkJoin({
      accounts: this.apiService.getAccounts(),
      settings: this.apiService.getSettings(),
      goals: this.apiService.getGoals(true)
    }).subscribe(({ accounts, settings, goals }) => {
      this.accounts.set(accounts);
      this.settings.set(settings);
      this.goals.set(goals);
      this.calculateProjections();
    });
  }

  onTimeRangeChange(months: number): void {
    this.timeRangeMonths.set(months);
    this.calculateProjections();
    // Recalculate extra payment scenario if active
    if (this.extraPayment() > 0) {
      this.calculateExtraPaymentScenario(this.extraPayment());
    }
  }

  onExtraPaymentChange(): void {
    // Push to subject for debouncing
    this.extraPaymentSubject.next(this.extraPayment());
  }

  private calculateExtraPaymentScenario(amount: number): void {
    if (amount === 0) {
      // Reset to baseline
      this.debtProjectionWithExtra.set(null);
      return;
    }
    
    const accounts = this.accounts();
    const debtAccounts = accounts.filter(a => a.type === 'Debt');
    
    if (debtAccounts.length === 0) return;
    
    // Recalculate with extra payment
    this.projectionService.calculateDebtProjectionWithExtra(
      debtAccounts,
      this.timeRangeMonths(),
      amount,
      this.settings() || undefined
    ).subscribe(result => {
      this.debtProjectionWithExtra.set(result);
    });
  }

  toggleContributions(include: boolean): void {
    this.projectionService.includeContributions.set(include);
    this.calculateProjections();
  }

  setGranularity(granularity: ChartGranularity): void {
    this.projectionService.granularity.set(granularity);
  }

  private calculateProjections(): void {
    this.projectionService.calculateProjections(
      this.accounts(),
      this.timeRangeMonths(),
      this.settings() || undefined
    ).subscribe();
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatDate(dateString?: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long'
    });
  }

  getGoalIcon(type: string): string {
    switch (type) {
      case 'DebtFree': return 'money_off';
      case 'InvestmentTarget': return 'trending_up';
      case 'SavingsGoal': return 'savings';
      case 'NetWorthMilestone': return 'account_balance';
      default: return 'flag';
    }
  }

  getGoalStatusColor(status: string): string {
    switch (status) {
      case 'Ahead': return '#4CAF50';
      case 'OnTrack': return '#8BC34A';
      case 'AtRisk': return '#FFC107';
      case 'Behind': return '#F44336';
      default: return '#9E9E9E';
    }
  }

  getMonthsUntilGoal(targetDate: string): number {
    const now = new Date();
    const target = new Date(targetDate);
    const diffMs = target.getTime() - now.getTime();
    return Math.max(0, Math.ceil(diffMs / (1000 * 60 * 60 * 24 * 30)));
  }
}
