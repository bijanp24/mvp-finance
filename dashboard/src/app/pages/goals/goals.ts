import { Component, ChangeDetectionStrategy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../core/services/api.service';
import { Goal, GoalStatus, GoalType, Account } from '../../core/models/api.models';
import { GoalDialogComponent } from './goal-dialog.component';

@Component({
  selector: 'app-goals',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatMenuModule,
    MatProgressBarModule,
    MatTooltipModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './goals.html',
  styleUrl: './goals.scss'
})
export class GoalsPage {
  private readonly apiService = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly Math = Math; // Expose Math for template

  readonly goals = signal<Goal[]>([]);
  readonly accounts = signal<Account[]>([]);
  readonly loading = signal(false);
  readonly showInactive = signal(false);

  readonly activeGoals = computed(() =>
    this.goals().filter(g => g.isActive)
  );

  readonly inactiveGoals = computed(() =>
    this.goals().filter(g => !g.isActive)
  );

  readonly onTrackCount = computed(() =>
    this.activeGoals().filter(g => g.progress.status === 'OnTrack' || g.progress.status === 'Ahead').length
  );

  readonly atRiskCount = computed(() =>
    this.activeGoals().filter(g => g.progress.status === 'AtRisk' || g.progress.status === 'Behind').length
  );

  constructor() {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);

    this.apiService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
        this.loadGoals();
      },
      error: (error) => {
        console.error('Error loading accounts:', error);
        this.loadGoals();
      }
    });
  }

  private loadGoals(): void {
    this.apiService.getGoals(false).subscribe({
      next: (goals) => {
        this.goals.set(goals);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading goals:', error);
        this.snackBar.open('Failed to load goals', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  getGoalIcon(type: GoalType): string {
    switch (type) {
      case 'DebtFree': return 'money_off';
      case 'InvestmentTarget': return 'trending_up';
      case 'SavingsGoal': return 'savings';
      case 'NetWorthMilestone': return 'account_balance';
      default: return 'flag';
    }
  }

  getStatusColor(status: GoalStatus): string {
    switch (status) {
      case 'Ahead': return '#4CAF50';
      case 'OnTrack': return '#8BC34A';
      case 'AtRisk': return '#FFC107';
      case 'Behind': return '#F44336';
      default: return '#9E9E9E';
    }
  }

  getStatusIcon(status: GoalStatus): string {
    switch (status) {
      case 'Ahead': return 'rocket_launch';
      case 'OnTrack': return 'check_circle';
      case 'AtRisk': return 'warning';
      case 'Behind': return 'error';
      default: return 'help';
    }
  }

  getProgressClass(status: GoalStatus): string {
    switch (status) {
      case 'Ahead': return 'progress-ahead';
      case 'OnTrack': return 'progress-on-track';
      case 'AtRisk': return 'progress-at-risk';
      case 'Behind': return 'progress-behind';
      default: return '';
    }
  }

  getGoalTypeLabel(type: GoalType): string {
    switch (type) {
      case 'DebtFree': return 'Debt Free';
      case 'InvestmentTarget': return 'Investment';
      case 'SavingsGoal': return 'Savings';
      case 'NetWorthMilestone': return 'Net Worth';
      default: return type;
    }
  }

  getLinkedAccountNames(goal: Goal): string {
    if (!goal.linkedAccountIds || goal.linkedAccountIds.length === 0) {
      return 'No linked accounts';
    }
    return goal.linkedAccountIds
      .map(id => this.accounts().find(a => a.id === id)?.name || `Account #${id}`)
      .join(', ');
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(GoalDialogComponent, {
      width: '500px',
      data: { mode: 'create', accounts: this.accounts() }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadGoals();
        this.snackBar.open('Goal created successfully', 'Close', { duration: 3000 });
      }
    });
  }

  openEditDialog(goal: Goal): void {
    const dialogRef = this.dialog.open(GoalDialogComponent, {
      width: '500px',
      data: { mode: 'edit', goal, accounts: this.accounts() }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadGoals();
        this.snackBar.open('Goal updated successfully', 'Close', { duration: 3000 });
      }
    });
  }

  toggleGoalActive(goal: Goal): void {
    this.apiService.updateGoal(goal.id, { isActive: !goal.isActive }).subscribe({
      next: () => {
        this.loadGoals();
        const status = goal.isActive ? 'deactivated' : 'activated';
        this.snackBar.open(`Goal ${status}`, 'Close', { duration: 3000 });
      },
      error: (error) => {
        console.error('Error updating goal:', error);
        this.snackBar.open('Failed to update goal', 'Close', { duration: 3000 });
      }
    });
  }

  deleteGoal(goal: Goal): void {
    if (confirm(`Are you sure you want to delete "${goal.name}"?`)) {
      this.apiService.deleteGoal(goal.id).subscribe({
        next: () => {
          this.loadGoals();
          this.snackBar.open('Goal deleted successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error deleting goal:', error);
          this.snackBar.open('Failed to delete goal', 'Close', { duration: 3000 });
        }
      });
    }
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      month: 'short',
      year: 'numeric'
    });
  }

  toggleShowInactive(): void {
    this.showInactive.update(v => !v);
  }
}
