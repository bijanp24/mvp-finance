import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { Account } from '../../core/models/api.models';
import { AccountDialogComponent } from './account-dialog.component';

@Component({
  selector: 'app-accounts',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './accounts.html',
  styleUrl: './accounts.scss'
})
export class AccountsPage {
  private readonly apiService = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly accounts = signal<Account[]>([]);
  readonly loading = signal(false);
  readonly displayedColumns = ['name', 'type', 'balance', 'apr', 'actions'];

  constructor() {
    this.loadAccounts();
  }

  loadAccounts(): void {
    this.loading.set(true);
    this.apiService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading accounts:', error);
        this.snackBar.open('Failed to load accounts', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(AccountDialogComponent, {
      width: '600px',
      data: { mode: 'create' }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadAccounts();
        this.snackBar.open('Account created successfully', 'Close', { duration: 3000 });
      }
    });
  }

  openEditDialog(account: Account): void {
    const dialogRef = this.dialog.open(AccountDialogComponent, {
      width: '600px',
      data: { mode: 'edit', account }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadAccounts();
        this.snackBar.open('Account updated successfully', 'Close', { duration: 3000 });
      }
    });
  }

  deleteAccount(account: Account): void {
    if (confirm(`Are you sure you want to delete "${account.name}"?`)) {
      this.apiService.deleteAccount(account.id).subscribe({
        next: () => {
          this.loadAccounts();
          this.snackBar.open('Account deleted successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error deleting account:', error);
          this.snackBar.open('Failed to delete account', 'Close', { duration: 3000 });
        }
      });
    }
  }

  getAccountTypeColor(type: string): string {
    switch (type) {
      case 'Cash': return 'primary';
      case 'Debt': return 'warn';
      case 'Investment': return 'accent';
      default: return '';
    }
  }

  formatCurrency(value: number | undefined): string {
    if (value === undefined) return '-';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatPercent(value: number | null | undefined): string {
    if (value == null) return '-';
    return `${value.toFixed(2)}%`;
  }

  formatAPR(account: Account): string {
    if (account.effectiveAnnualPercentageRate != null) {
      const isPromo = account.promotionalPeriodEndDate &&
                      new Date(account.promotionalPeriodEndDate) > new Date();
      // Convert decimal to percentage (0.18 -> 18%)
      const rate = (account.effectiveAnnualPercentageRate * 100).toFixed(2);
      return isPromo ? `${rate}% (promo)` : `${rate}%`;
    }
    return '-';
  }
}
