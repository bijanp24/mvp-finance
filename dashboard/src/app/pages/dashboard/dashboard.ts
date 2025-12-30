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
import {
  Account,
  FinancialEvent,
  SpendableRequest,
  SpendableResult,
  UserSettings,
  Budget,
  Category,
  Goal,
  GoalStatus,
  SafeToSpendResult,
  SafeToSpendStatus,
  SuggestionsResult,
  Suggestion
} from '../../core/models/api.models';
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
  readonly budgets = signal<Budget[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly monthlyEvents = signal<FinancialEvent[]>([]);
  readonly goals = signal<Goal[]>([]);
  readonly loading = signal(true);

  // New SafeToSpend signals
  readonly safeToSpendResult = signal<SafeToSpendResult | null>(null);
  readonly suggestions = signal<SuggestionsResult | null>(null);

  // Projection chart data
  readonly debtChartData = this.projectionService.debtChartData;
  readonly investmentChartData = this.projectionService.investmentChartData;
  readonly projectionsLoading = this.projectionService.loading;

  readonly totalCash = computed(() => {
    return this.accounts()
      .filter(a => a.type === 'Cash')
      .reduce((sum, a) => sum + a.currentBalance, 0);
  });

  readonly totalDebt = computed(() => {
    return this.accounts()
      .filter(a => a.type === 'Debt')
      .reduce((sum, a) => sum + a.currentBalance, 0);
  });

  readonly totalInvestments = computed(() => {
    return this.accounts()
      .filter(a => a.type === 'Investment')
      .reduce((sum, a) => sum + a.currentBalance, 0);
  });

  readonly netWorth = computed(() => {
    return (this.totalCash() + this.totalInvestments()) - this.totalDebt();
  });

  // Budget vs Actual computed data
  readonly budgetSummary = computed(() => {
    const budgets = this.budgets().filter(b => b.isActive);
    const events = this.monthlyEvents();
    const categories = this.categories();

    // Group spending by category
    const spendingByCategory = new Map<number, number>();
    events.forEach(event => {
      if (event.categoryId && event.type === 'Expense') {
        const current = spendingByCategory.get(event.categoryId) || 0;
        spendingByCategory.set(event.categoryId, current + event.amount);
      }
    });

    // Calculate budget vs actual for each budget
    const items = budgets.map(budget => {
      const spent = spendingByCategory.get(budget.categoryId) || 0;
      const monthlyBudget = this.getMonthlyBudgetAmount(budget);
      const percentage = monthlyBudget > 0 ? Math.min((spent / monthlyBudget) * 100, 100) : 0;
      const category = categories.find(c => c.id === budget.categoryId);

      return {
        categoryId: budget.categoryId,
        categoryName: budget.categoryName,
        categoryColor: category?.color || '#757575',
        budgetAmount: monthlyBudget,
        spentAmount: spent,
        remaining: monthlyBudget - spent,
        percentage,
        isOverBudget: spent > monthlyBudget
      };
    });

    // Calculate totals
    const totalBudget = items.reduce((sum, item) => sum + item.budgetAmount, 0);
    const totalSpent = items.reduce((sum, item) => sum + item.spentAmount, 0);

    return {
      items: items.sort((a, b) => b.percentage - a.percentage).slice(0, 5),
      totalBudget,
      totalSpent,
      totalPercentage: totalBudget > 0 ? (totalSpent / totalBudget) * 100 : 0
    };
  });

  private getMonthlyBudgetAmount(budget: Budget): number {
    switch (budget.frequency) {
      case 'Weekly': return budget.amount * 4.33;
      case 'BiWeekly': return budget.amount * 2.17;
      case 'Monthly': return budget.amount;
      default: return budget.amount;
    }
  }

  // Top goals by priority (active only)
  readonly topGoals = computed(() => {
    return this.goals()
      .filter(g => g.isActive)
      .sort((a, b) => a.priority - b.priority)
      .slice(0, 3);
  });

  readonly goalsOnTrack = computed(() => {
    return this.goals()
      .filter(g => g.isActive && (g.progress.status === 'OnTrack' || g.progress.status === 'Ahead'))
      .length;
  });

  readonly goalsAtRisk = computed(() => {
    return this.goals()
      .filter(g => g.isActive && (g.progress.status === 'AtRisk' || g.progress.status === 'Behind'))
      .length;
  });

  constructor() {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading.set(true);

    // Calculate current month date range
    const now = new Date();
    const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
    const endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    const startDate = startOfMonth.toISOString().split('T')[0];
    const endDate = endOfMonth.toISOString().split('T')[0];

    forkJoin({
      accounts: this.apiService.getAccounts(),
      events: this.apiService.getRecentEvents(10),
      settings: this.apiService.getSettings(),
      budgets: this.apiService.getBudgets(),
      categories: this.apiService.getCategories(),
      monthlyEvents: this.apiService.getEvents({ startDate, endDate }),
      goals: this.apiService.getGoals(true),
      safeToSpend: this.apiService.getSafeToSpend(),
      suggestions: this.apiService.getSuggestions({ maxSuggestions: 3 })
    }).subscribe({
      next: ({ accounts, events, settings, budgets, categories, monthlyEvents, goals, safeToSpend, suggestions }) => {
        this.accounts.set(accounts);
        this.recentEvents.set(events);
        this.settings.set(settings);
        this.budgets.set(budgets);
        this.categories.set(categories);
        this.monthlyEvents.set(monthlyEvents);
        this.goals.set(goals);
        this.safeToSpendResult.set(safeToSpend);
        this.suggestions.set(suggestions);
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
    const totalCash = cashAccounts.reduce((sum, a) => sum + a.currentBalance, 0);

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
    let nextPayday: Date;
    
    if (currentSettings.nextPaycheckDate) {
      // Use the configured next paycheck date
      nextPayday = new Date(currentSettings.nextPaycheckDate);
      
      // If that date is in the past, calculate the next occurrence
      while (nextPayday < today) {
        const payFrequencyDays = this.getPayFrequencyDays(currentSettings.payFrequency);
        nextPayday.setDate(nextPayday.getDate() + payFrequencyDays);
      }
    } else {
      // Fall back to estimating based on pay frequency
      nextPayday = new Date(today);
      const payFrequencyDays = this.getPayFrequencyDays(currentSettings.payFrequency);
      nextPayday.setDate(nextPayday.getDate() + payFrequencyDays);
    }

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

  getGoalIcon(type: string): string {
    switch (type) {
      case 'DebtFree': return 'money_off';
      case 'InvestmentTarget': return 'trending_up';
      case 'SavingsGoal': return 'savings';
      case 'NetWorthMilestone': return 'account_balance';
      default: return 'flag';
    }
  }

  getGoalStatusColor(status: GoalStatus): string {
    switch (status) {
      case 'Ahead': return '#4CAF50';
      case 'OnTrack': return '#8BC34A';
      case 'AtRisk': return '#FFC107';
      case 'Behind': return '#F44336';
      default: return '#9E9E9E';
    }
  }

  getGoalStatusIcon(status: GoalStatus): string {
    switch (status) {
      case 'Ahead': return 'rocket_launch';
      case 'OnTrack': return 'check_circle';
      case 'AtRisk': return 'warning';
      case 'Behind': return 'error';
      default: return 'help';
    }
  }

  getGoalProgressClass(status: GoalStatus): string {
    switch (status) {
      case 'Ahead': return 'progress-ahead';
      case 'OnTrack': return 'progress-on-track';
      case 'AtRisk': return 'progress-at-risk';
      case 'Behind': return 'progress-behind';
      default: return '';
    }
  }

  // SafeToSpend status helpers
  getSafeToSpendStatusColor(status: SafeToSpendStatus): string {
    switch (status) {
      case 'Healthy': return '#4CAF50';
      case 'Tight': return '#FFC107';
      case 'AtRisk': return '#FF9800';
      case 'Behind': return '#F44336';
      default: return '#9E9E9E';
    }
  }

  getSafeToSpendStatusIcon(status: SafeToSpendStatus): string {
    switch (status) {
      case 'Healthy': return 'check_circle';
      case 'Tight': return 'warning';
      case 'AtRisk': return 'error_outline';
      case 'Behind': return 'error';
      default: return 'help';
    }
  }

  getSafeToSpendStatusClass(status: SafeToSpendStatus): string {
    switch (status) {
      case 'Healthy': return 'status-healthy';
      case 'Tight': return 'status-tight';
      case 'AtRisk': return 'status-at-risk';
      case 'Behind': return 'status-behind';
      default: return '';
    }
  }

  getSuggestionIcon(category: string): string {
    switch (category) {
      case 'ReduceSpending': return 'trending_down';
      case 'IncreaseContribution': return 'trending_up';
      case 'Emergency': return 'emergency';
      case 'Warning': return 'warning';
      case 'Optimization': return 'lightbulb';
      case 'Positive': return 'celebration';
      default: return 'info';
    }
  }

  getSuggestionPriorityColor(priority: string): string {
    switch (priority) {
      case 'Critical': return '#F44336';
      case 'High': return '#FF9800';
      case 'Medium': return '#FFC107';
      case 'Low': return '#4CAF50';
      default: return '#9E9E9E';
    }
  }
}
