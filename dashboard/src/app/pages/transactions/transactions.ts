import { Component, ChangeDetectionStrategy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { ApiService } from '../../core/services/api.service';
import { Account, FinancialEvent, CreateEventRequest } from '../../core/models/api.models';

@Component({
  selector: 'app-transactions',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonToggleModule,
    MatIconModule,
    MatSnackBarModule,
    MatTableModule,
    MatChipsModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transactions.html',
  styleUrl: './transactions.scss'
})
export class TransactionsPage {
  private readonly apiService = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  readonly accounts = signal<Account[]>([]);
  readonly recentEvents = signal<FinancialEvent[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly displayedColumns = ['date', 'type', 'description', 'account', 'amount', 'actions'];

  readonly transactionForm: FormGroup;

  readonly eventTypes = [
    { value: 'Income', label: 'Income', icon: 'trending_up' },
    { value: 'Expense', label: 'Expense', icon: 'shopping_cart' },
    { value: 'DebtPayment', label: 'Debt Payment', icon: 'payment' },
    { value: 'DebtCharge', label: 'Debt Charge', icon: 'credit_card' },
    { value: 'SavingsContribution', label: 'Savings', icon: 'savings' },
    { value: 'InvestmentContribution', label: 'Investment', icon: 'trending_up' }
  ];

  readonly showTargetAccount = computed(() => {
    const type = this.transactionForm.get('type')?.value;
    return type === 'DebtPayment' || type === 'SavingsContribution' || type === 'InvestmentContribution';
  });

  readonly filteredTargetAccounts = computed(() => {
    const type = this.transactionForm.get('type')?.value;
    const accounts = this.accounts();

    if (type === 'DebtPayment') {
      return accounts.filter(a => a.type === 'Debt');
    } else if (type === 'SavingsContribution' || type === 'InvestmentContribution') {
      return accounts.filter(a => a.type === 'Investment');
    }
    return accounts;
  });

  readonly cashAccounts = computed(() => {
    return this.accounts().filter(a => a.type === 'Cash');
  });

  constructor() {
    this.transactionForm = this.fb.group({
      type: ['Expense', Validators.required],
      amount: [null, [Validators.required, Validators.min(0.01)]],
      date: [new Date(), Validators.required],
      description: [''],
      accountId: [null],
      targetAccountId: [null]
    });

    // Watch type changes to update validation
    this.transactionForm.get('type')?.valueChanges.subscribe(type => {
      const accountControl = this.transactionForm.get('accountId');
      const targetControl = this.transactionForm.get('targetAccountId');

      if (type === 'Income' || type === 'Expense') {
        accountControl?.setValidators([Validators.required]);
        targetControl?.clearValidators();
        targetControl?.setValue(null);
      } else if (type === 'DebtCharge') {
        targetControl?.setValidators([Validators.required]);
        accountControl?.clearValidators();
        accountControl?.setValue(null);
      } else {
        // DebtPayment, Savings, Investment need both
        accountControl?.setValidators([Validators.required]);
        targetControl?.setValidators([Validators.required]);
      }

      accountControl?.updateValueAndValidity();
      targetControl?.updateValueAndValidity();
    });

    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);

    this.apiService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
      },
      error: (error) => {
        console.error('Error loading accounts:', error);
        this.snackBar.open('Failed to load accounts', 'Close', { duration: 3000 });
      }
    });

    this.apiService.getRecentEvents(30).subscribe({
      next: (events) => {
        this.recentEvents.set(events);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading events:', error);
        this.snackBar.open('Failed to load transactions', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  resetForm(): void {
    this.transactionForm.reset({
      type: 'Expense',
      date: new Date(),
      amount: null,
      description: '',
      accountId: null,
      targetAccountId: null
    });
  }

  onSubmit(): void {
    if (this.transactionForm.valid) {
      this.saving.set(true);
      const formValue = this.transactionForm.value;

      const request: CreateEventRequest = {
        type: formValue.type,
        amount: formValue.amount,
        date: this.formatDate(formValue.date),
        description: formValue.description || '',
        accountId: formValue.accountId,
        targetAccountId: formValue.targetAccountId
      };

      this.apiService.createEvent(request).subscribe({
        next: () => {
          this.saving.set(false);
          this.snackBar.open('Transaction saved successfully', 'Close', { duration: 3000 });
          this.transactionForm.patchValue({
            amount: null,
            description: ''
          });
          this.loadData();
        },
        error: (error) => {
          console.error('Error saving transaction:', error);
          this.snackBar.open('Failed to save transaction', 'Close', { duration: 3000 });
          this.saving.set(false);
        }
      });
    }
  }

  deleteEvent(event: FinancialEvent): void {
    if (confirm(`Are you sure you want to delete this transaction?`)) {
      this.apiService.deleteEvent(event.id).subscribe({
        next: () => {
          this.loadData();
          this.snackBar.open('Transaction deleted successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error deleting transaction:', error);
          this.snackBar.open('Failed to delete transaction', 'Close', { duration: 3000 });
        }
      });
    }
  }

  getAccountName(accountId: number | undefined): string {
    if (!accountId) return '-';
    const account = this.accounts().find(a => a.id === accountId);
    return account?.name || '-';
  }

  getEventTypeColor(type: string): string {
    switch (type) {
      case 'Income': return 'primary';
      case 'Expense': return 'warn';
      case 'DebtPayment': return 'accent';
      case 'DebtCharge': return 'warn';
      case 'SavingsContribution': return 'primary';
      case 'InvestmentContribution': return 'accent';
      default: return '';
    }
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatDisplayDate(dateStr: string): string {
    const date = new Date(dateStr);
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    }).format(date);
  }

  private formatDate(date: Date): string {
    return date.toISOString().split('T')[0];
  }
}
