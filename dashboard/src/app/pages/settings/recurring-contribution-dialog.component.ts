import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { Account, RecurringContribution, CreateRecurringContributionRequest, UpdateRecurringContributionRequest, ContributionFrequency } from '../../core/models/api.models';

export interface RecurringContributionDialogData {
  mode: 'create' | 'edit';
  contribution?: RecurringContribution;
  accounts: Account[];
}

@Component({
  selector: 'app-recurring-contribution-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Add Recurring Contribution' : 'Edit Recurring Contribution' }}</h2>

    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" placeholder="e.g., 401k Contribution" required>
          @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
            <mat-error>Name is required</mat-error>
          }
          @if (form.get('name')?.hasError('maxlength')) {
            <mat-error>Name must not exceed 100 characters</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Amount</mat-label>
          <input matInput type="number" formControlName="amount" placeholder="0.00" required>
          <span matTextPrefix>$&nbsp;</span>
          @if (form.get('amount')?.hasError('required') && form.get('amount')?.touched) {
            <mat-error>Amount is required</mat-error>
          }
          @if (form.get('amount')?.hasError('min')) {
            <mat-error>Amount must be greater than zero</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Frequency</mat-label>
          <mat-select formControlName="frequency" required>
            <mat-option value="Weekly">Weekly (Every 7 days)</mat-option>
            <mat-option value="BiWeekly">Bi-Weekly (Every 14 days)</mat-option>
            <mat-option value="SemiMonthly">Semi-Monthly (Twice per month)</mat-option>
            <mat-option value="Monthly">Monthly (Once per month)</mat-option>
            <mat-option value="Quarterly">Quarterly (Every 3 months)</mat-option>
            <mat-option value="Annually">Annually (Once per year)</mat-option>
          </mat-select>
          @if (form.get('frequency')?.hasError('required') && form.get('frequency')?.touched) {
            <mat-error>Frequency is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Next Contribution Date</mat-label>
          <input matInput type="date" formControlName="nextContributionDate" required>
          <mat-hint>When the next contribution should occur</mat-hint>
          @if (form.get('nextContributionDate')?.hasError('required') && form.get('nextContributionDate')?.touched) {
            <mat-error>Next contribution date is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>From Account</mat-label>
          <mat-select formControlName="sourceAccountId" required>
            @for (account of cashAccounts(); track account.id) {
              <mat-option [value]="account.id">{{ account.name }}</mat-option>
            }
          </mat-select>
          <mat-hint>Source account (Cash only)</mat-hint>
          @if (form.get('sourceAccountId')?.hasError('required') && form.get('sourceAccountId')?.touched) {
            <mat-error>Source account is required</mat-error>
          }
          @if (cashAccounts().length === 0) {
            <mat-hint>No cash accounts available</mat-hint>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>To Account</mat-label>
          <mat-select formControlName="targetAccountId" required>
            @for (account of investmentAndCashAccounts(); track account.id) {
              <mat-option [value]="account.id">{{ account.name }} ({{ account.type }})</mat-option>
            }
          </mat-select>
          <mat-hint>Target account (Investment or Cash)</mat-hint>
          @if (form.get('targetAccountId')?.hasError('required') && form.get('targetAccountId')?.touched) {
            <mat-error>Target account is required</mat-error>
          }
          @if (form.get('targetAccountId')?.hasError('sameAccount')) {
            <mat-error>Target account must be different from source account</mat-error>
          }
          @if (investmentAndCashAccounts().length === 0) {
            <mat-hint>No investment or cash accounts available</mat-hint>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancel</button>
      <button
        mat-raised-button
        color="primary"
        (click)="onSave()"
        [disabled]="!form.valid || saving()">
        {{ saving() ? 'Saving...' : 'Save' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      width: 100%;
      max-width: 500px;
      max-height: 70vh;
      overflow-y: auto;
      padding-top: 20px;
      box-sizing: border-box;
    }

    form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    mat-form-field {
      width: 100%;
    }

    mat-dialog-actions {
      padding: 16px 24px;
    }
  `]
})
export class RecurringContributionDialogComponent {
  private readonly apiService = inject(ApiService);
  private readonly dialogRef = inject(MatDialogRef<RecurringContributionDialogComponent>);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  readonly data = inject<RecurringContributionDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly form: FormGroup;

  readonly cashAccounts = computed(() => 
    this.data.accounts.filter(a => a.type === 'Cash')
  );

  readonly investmentAndCashAccounts = computed(() => 
    this.data.accounts.filter(a => a.type === 'Investment' || a.type === 'Cash')
  );

  constructor() {
    const contribution = this.data.contribution;
    
    // Convert ISO date to YYYY-MM-DD format for date input
    let formattedDate: string | null = null;
    if (contribution?.nextContributionDate) {
      const date = new Date(contribution.nextContributionDate);
      formattedDate = date.toISOString().split('T')[0];
    }

    this.form = this.fb.group({
      name: [contribution?.name || '', [Validators.required, Validators.maxLength(100)]],
      amount: [contribution?.amount || null, [Validators.required, Validators.min(0.01)]],
      frequency: [contribution?.frequency || 'Monthly', Validators.required],
      nextContributionDate: [formattedDate, Validators.required],
      sourceAccountId: [contribution?.sourceAccountId || null, Validators.required],
      targetAccountId: [contribution?.targetAccountId || null, Validators.required]
    });

    // Add custom validator to ensure source and target are different
    this.form.get('sourceAccountId')?.valueChanges.subscribe(() => {
      this.validateAccounts();
    });
    this.form.get('targetAccountId')?.valueChanges.subscribe(() => {
      this.validateAccounts();
    });
  }

  private validateAccounts(): void {
    const sourceId = this.form.get('sourceAccountId')?.value;
    const targetId = this.form.get('targetAccountId')?.value;
    const targetControl = this.form.get('targetAccountId');

    if (sourceId && targetId && sourceId === targetId) {
      targetControl?.setErrors({ sameAccount: true });
    } else if (targetControl?.hasError('sameAccount')) {
      // Clear the error if accounts are now different
      const errors = { ...targetControl.errors };
      delete errors['sameAccount'];
      targetControl.setErrors(Object.keys(errors).length > 0 ? errors : null);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.valid) {
      this.saving.set(true);

      // Convert date string to ISO DateTime for the API
      const formValue = this.form.value;
      const date = new Date(formValue.nextContributionDate + 'T12:00:00Z');
      const nextContributionDate = date.toISOString();

      const operation = this.data.mode === 'create'
        ? this.apiService.createRecurringContribution({
            name: formValue.name,
            amount: formValue.amount,
            frequency: formValue.frequency,
            nextContributionDate,
            sourceAccountId: formValue.sourceAccountId,
            targetAccountId: formValue.targetAccountId
          })
        : this.apiService.updateRecurringContribution(this.data.contribution!.id, {
            name: formValue.name,
            amount: formValue.amount,
            frequency: formValue.frequency,
            nextContributionDate,
            sourceAccountId: formValue.sourceAccountId,
            targetAccountId: formValue.targetAccountId,
            isActive: this.data.contribution!.isActive
          });

      operation.subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Error saving recurring contribution:', error);
          this.saving.set(false);
          
          // Show user-friendly error message
          let errorMessage = 'Failed to save recurring contribution';
          if (error.error?.message) {
            errorMessage = error.error.message;
          } else if (error.message) {
            errorMessage = error.message;
          }
          
          this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
        }
      });
    }
  }
}

