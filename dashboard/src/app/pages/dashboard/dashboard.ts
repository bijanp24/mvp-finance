import { Component, ChangeDetectionStrategy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { ApiService } from '../../core/services/api.service';
import { ProjectionService } from '../../core/services/projection.service';
import { Account, FinancialEvent, SpendableRequest, SpendableResult, UserSettings } from '../../core/models/api.models';
import { CalendarComponent } from '../../features/calendar/calendar.component';
import { DebtProjectionChartComponent } from '../../features/charts/debt-projection-chart.component';
import { InvestmentProjectionChartComponent } from '../../features/charts/investment-projection-chart.component';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatProgressBarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    CalendarComponent,
    DebtProjectionChartComponent,
    InvestmentProjectionChartComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardPage {
  private readonly apiService = inject(ApiService);
  private readonly projectionService = inject(ProjectionService);

  readonly accounts = signal<Account[]>([]);
  readonly recentEvents = signal<FinancialEvent[]>([]);
  readonly spendableResult = signal<SpendableResult | null>(null);
  readonly settings = signal<UserSettings | null>(null);
  readonly loading = signal(true);

  // Projection chart data
  readonly debtChartData = this.projectionService.debtChartData;
  readonly investmentChartData = this.projectionService.investmentChartData;
  readonly projectionsLoading = this.projectionService.loading;

  readonly totalCash = computed(() => {
    return this.accounts()
      .filter(a => a.type === 'Cash')
      .reduce((sum, a) => sum + a.initialBalance, 0);
  });

  readonly totalDebt = computed(() => {
    return this.accounts()
      .filter(a => a.type === 'Debt')
      .reduce((sum, a) => sum + a.initialBalance, 0);
  });

  readonly totalInvestments = computed(() => {
    return this.accounts()
      .filter(a => a.type === 'Investment')
      .reduce((sum, a) => sum + a.initialBalance, 0);
  });

  constructor() {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading.set(true);

    forkJoin({
      accounts: this.apiService.getAccounts(),
      events: this.apiService.getRecentEvents(10),
      settings: this.apiService.getSettings()
    }).subscribe({
      next: ({ accounts, events, settings }) => {
        this.accounts.set(accounts);
        this.recentEvents.set(events);
        this.settings.set(settings);
        this.calculateSpendable(accounts);

        // Calculate projections
        this.projectionService.calculateProjections(accounts, 12, settings).subscribe();
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        this.loading.set(false);
      }
    });
  }

  calculateSpendable(accounts: Account[]): void {
    const cashAccounts = accounts.filter(a => a.type === 'Cash');
    const totalCash = cashAccounts.reduce((sum, a) => sum + a.initialBalance, 0);

    if (totalCash === 0) {
      this.loading.set(false);
      return;
    }

    const currentSettings = this.settings();
    if (!currentSettings) {
      this.loading.set(false);
      return;
    }

    // Calculate next payday based on settings
    const today = new Date();
    const nextPayday = new Date(today);
    const payFrequencyDays = this.getPayFrequencyDays(currentSettings.payFrequency);
    nextPayday.setDate(nextPayday.getDate() + payFrequencyDays);

    const request: SpendableRequest = {
      availableCash: totalCash,
      calculationDate: today.toISOString().split('T')[0],
      upcomingIncome: [
        {
          date: nextPayday.toISOString().split('T')[0],
          amount: currentSettings.paycheckAmount,
          description: 'Paycheck'
        }
      ],
      obligations: [],
      manualSafetyBuffer: currentSettings.safetyBuffer
    };

    this.apiService.calculateSpendable(request).subscribe({
      next: (result) => {
        this.spendableResult.set(result);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error calculating spendable:', error);
        this.loading.set(false);
      }
    });
  }

  getEventIcon(type: string): string {
    switch (type) {
      case 'Income': return 'account_balance';
      case 'Expense': return 'shopping_cart';
      case 'DebtPayment': return 'payment';
      case 'DebtCharge': return 'credit_card';
      case 'SavingsContribution': return 'savings';
      case 'InvestmentContribution': return 'trending_up';
      default: return 'receipt';
    }
  }

  getAccountName(accountId: number | undefined): string {
    if (!accountId) return 'Unknown';
    const account = this.accounts().find(a => a.id === accountId);
    return account?.name || 'Unknown';
  }

  formatCurrency(value: number | undefined): string {
    if (value === undefined) return '$0.00';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatDisplayDate(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays} days ago`;

    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  private getPayFrequencyDays(frequency: string): number {
    switch (frequency) {
      case 'Weekly': return 7;
      case 'BiWeekly': return 14;
      case 'SemiMonthly': return 15;
      case 'Monthly': return 30;
      default: return 14; // Default to bi-weekly
    }
  }

  isIncome(type: string): boolean {
    return type === 'Income';
  }

  isExpense(type: string): boolean {
    return type === 'Expense' || type === 'DebtPayment' || type === 'DebtCharge' ||
           type === 'SavingsContribution' || type === 'InvestmentContribution';
  }
}
