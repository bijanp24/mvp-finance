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
import { ApiService } from '../../core/services/api.service';
import { Budget, Category } from '../../core/models/api.models';
import { BudgetDialogComponent } from './budget-dialog.component';

@Component({
  selector: 'app-budgets',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatMenuModule,
    MatProgressBarModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './budgets.html',
  styleUrl: './budgets.scss'
})
export class BudgetsPage {
  private readonly apiService = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly budgets = signal<Budget[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);

  readonly recurringBudgets = computed(() =>
    this.budgets().filter(b => this.getCategoryType(b.categoryId) === 'Recurring')
  );

  readonly oneTimeBudgets = computed(() =>
    this.budgets().filter(b => this.getCategoryType(b.categoryId) === 'OneTime')
  );

  readonly totalMonthlyBudget = computed(() =>
    this.budgets()
      .filter(b => b.isActive)
      .reduce((sum, b) => sum + this.getMonthlyAmount(b), 0)
  );

  constructor() {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);

    // Load categories first, then budgets
    this.apiService.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loadBudgets();
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.snackBar.open('Failed to load categories', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  private loadBudgets(): void {
    this.apiService.getBudgets().subscribe({
      next: (budgets) => {
        this.budgets.set(budgets);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading budgets:', error);
        this.snackBar.open('Failed to load budgets', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  getCategoryType(categoryId: number): string {
    const category = this.categories().find(c => c.id === categoryId);
    return category?.type || 'OneTime';
  }

  getCategoryIcon(categoryId: number): string {
    const category = this.categories().find(c => c.id === categoryId);
    return category?.icon || 'category';
  }

  getCategoryColor(categoryId: number): string {
    const category = this.categories().find(c => c.id === categoryId);
    return category?.color || '#9E9E9E';
  }

  getMonthlyAmount(budget: Budget): number {
    switch (budget.frequency) {
      case 'Weekly': return budget.amount * 4.33;
      case 'BiWeekly': return budget.amount * 2.17;
      case 'Monthly': return budget.amount;
      default: return budget.amount;
    }
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(BudgetDialogComponent, {
      width: '500px',
      data: { mode: 'create', categories: this.categories() }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadBudgets();
        this.snackBar.open('Budget created successfully', 'Close', { duration: 3000 });
      }
    });
  }

  openEditDialog(budget: Budget): void {
    const dialogRef = this.dialog.open(BudgetDialogComponent, {
      width: '500px',
      data: { mode: 'edit', budget, categories: this.categories() }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadBudgets();
        this.snackBar.open('Budget updated successfully', 'Close', { duration: 3000 });
      }
    });
  }

  toggleBudgetActive(budget: Budget): void {
    this.apiService.updateBudget(budget.id, { isActive: !budget.isActive }).subscribe({
      next: () => {
        this.loadBudgets();
        const status = budget.isActive ? 'deactivated' : 'activated';
        this.snackBar.open(`Budget ${status}`, 'Close', { duration: 3000 });
      },
      error: (error) => {
        console.error('Error updating budget:', error);
        this.snackBar.open('Failed to update budget', 'Close', { duration: 3000 });
      }
    });
  }

  deleteBudget(budget: Budget): void {
    if (confirm(`Are you sure you want to delete the budget for "${budget.categoryName}"?`)) {
      this.apiService.deleteBudget(budget.id).subscribe({
        next: () => {
          this.loadBudgets();
          this.snackBar.open('Budget deleted successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error deleting budget:', error);
          this.snackBar.open('Failed to delete budget', 'Close', { duration: 3000 });
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

  formatFrequency(frequency: string): string {
    switch (frequency) {
      case 'Weekly': return '/week';
      case 'BiWeekly': return '/2 weeks';
      case 'Monthly': return '/month';
      default: return '';
    }
  }
}
