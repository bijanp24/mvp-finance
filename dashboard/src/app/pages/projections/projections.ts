import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatSliderModule } from '@angular/material/slider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ProjectionService } from '../../core/services/projection.service';
import { DebtProjectionChartComponent } from '../../features/charts/debt-projection-chart.component';
import { InvestmentProjectionChartComponent } from '../../features/charts/investment-projection-chart.component';
import { NetWorthChartComponent } from '../../features/charts/net-worth-chart.component';
import { Account, UserSettings, SimulationResult, ChartGranularity } from '../../core/models/api.models';

@Component({
  selector: 'app-projections',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatButtonToggleModule,
    MatSliderModule,
    MatSlideToggleModule,
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
  readonly extraPayment = signal(0);
  readonly debtProjectionWithExtra = signal<SimulationResult | null>(null);

  readonly debtChartData = this.projectionService.debtChartData;
  readonly investmentChartData = this.projectionService.investmentChartData;
  readonly netWorthChartData = this.projectionService.netWorthChartData;
  readonly loading = this.projectionService.loading;

  readonly debtProjection = this.projectionService.debtProjection;
  readonly investmentProjection = this.projectionService.investmentProjection;
  readonly crossoverDate = this.projectionService.crossoverDate;
  readonly includeContributions = this.projectionService.includeContributions;
  readonly granularity = this.projectionService.granularity;

  readonly debtComparison = computed(() => {
    const baseline = this.debtProjection();
    const withExtra = this.debtProjectionWithExtra();
    
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
  }

  loadData(): void {
    forkJoin({
      accounts: this.apiService.getAccounts(),
      settings: this.apiService.getSettings()
    }).subscribe(({ accounts, settings }) => {
      this.accounts.set(accounts);
      this.settings.set(settings);
      this.calculateProjections();
    });
  }

  onTimeRangeChange(months: number): void {
    this.timeRangeMonths.set(months);
    this.calculateProjections();
    // Recalculate extra payment scenario if active
    if (this.extraPayment() > 0) {
      this.onExtraPaymentChange();
    }
  }

  onExtraPaymentChange(): void {
    if (this.extraPayment() === 0) {
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
      this.extraPayment(),
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
}
