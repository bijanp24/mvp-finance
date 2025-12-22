import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../core/services/api.service';
import { Account, CreateAccountRequest } from '../../core/models/api.models';

export interface AccountDialogData {
  mode: 'create' | 'edit';
  account?: Account;
}

@Component({
  selector: 'app-account-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Create Account' : 'Edit Account' }}</h2>

    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline">
          <mat-label>Account Name</mat-label>
          <input matInput formControlName="name" placeholder="e.g., Chase Checking" required>
          @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
            <mat-error>Account name is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Account Type</mat-label>
          <mat-select formControlName="type" required>
            <mat-option value="Cash">Cash</mat-option>
            <mat-option value="Debt">Debt</mat-option>
            <mat-option value="Investment">Investment</mat-option>
          </mat-select>
          @if (form.get('type')?.hasError('required') && form.get('type')?.touched) {
            <mat-error>Account type is required</mat-error>
          }
          @if (data.mode === 'edit') {
            <mat-hint>Account type cannot be changed after creation</mat-hint>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Initial Balance</mat-label>
          <input matInput type="number" formControlName="initialBalance" placeholder="0.00" required>
          <span matTextPrefix>$&nbsp;</span>
          @if (form.get('initialBalance')?.hasError('required') && form.get('initialBalance')?.touched) {
            <mat-error>Initial balance is required</mat-error>
          }
          @if (data.mode === 'edit') {
            <mat-hint>Initial balance cannot be changed - use transactions to adjust balance</mat-hint>
          }
        </mat-form-field>

        @if (showAprField()) {
          <mat-form-field appearance="outline">
            <mat-label>
              @if (form.get('type')?.value === 'Debt') {
                Annual Percentage Rate (APR)
              } @else if (form.get('type')?.value === 'Investment') {
                Expected Annual Return (%)
              } @else {
                Interest Rate (%) - Optional
              }
            </mat-label>
            <input matInput type="number" formControlName="annualPercentageRate" placeholder="0.00">
            <span matTextSuffix>%</span>
            @if (form.get('annualPercentageRate')?.hasError('required') && form.get('annualPercentageRate')?.touched) {
              <mat-error>
                @if (form.get('type')?.value === 'Debt') {
                  APR is required for debt accounts
                } @else {
                  Expected return is required for investment accounts
                }
              </mat-error>
            }
          </mat-form-field>
        }

        @if (showMinimumPayment()) {
          <mat-form-field appearance="outline">
            <mat-label>Minimum Payment (Optional)</mat-label>
            <input matInput type="number" formControlName="minimumPayment" placeholder="Auto-calculated if left empty">
            <span matTextPrefix>$&nbsp;</span>
            <mat-hint>Leave empty for auto-calculation: 2% for 0% promo, 4% otherwise</mat-hint>
          </mat-form-field>
        }

        @if (showDebtFeatures()) {
          <div class="section-divider">
            <h3>Additional Debt Features (Optional)</h3>
          </div>

          <mat-form-field appearance="outline">
            <mat-label>Promotional APR (%)</mat-label>
            <input matInput type="number" formControlName="promotionalAnnualPercentageRate" placeholder="0.00">
            <span matTextSuffix>%</span>
            <mat-hint>Intro rate (e.g., 0% for 12 months)</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Promo End Date</mat-label>
            <input matInput type="date" formControlName="promotionalPeriodEndDate">
            <mat-hint>When promotional APR expires</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Balance Transfer Fee (%)</mat-label>
            <input matInput type="number" formControlName="balanceTransferFeePercentage" placeholder="0.00">
            <span matTextSuffix>%</span>
            <mat-hint>Typically 3-5%</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Statement Day of Month</mat-label>
            <input matInput type="number" formControlName="statementDayOfMonth" placeholder="1-31" min="1" max="31">
            <mat-hint>1-31, recurring monthly</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Statement Override (Optional)</mat-label>
            <input matInput type="date" formControlName="statementDateOverride">
            <mat-hint>One-time specific date override</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Payment Due Day of Month</mat-label>
            <input matInput type="number" formControlName="paymentDueDayOfMonth" placeholder="1-31" min="1" max="31">
            <mat-hint>1-31, recurring monthly</mat-hint>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Due Date Override (Optional)</mat-label>
            <input matInput type="date" formControlName="paymentDueDateOverride">
            <mat-hint>One-time specific date override</mat-hint>
          </mat-form-field>
        }
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
      min-width: 500px;
      max-height: 70vh;
      overflow-y: auto;
      padding-top: 20px;
    }

    form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    mat-form-field {
      width: 100%;
    }

    .section-divider {
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid rgba(0, 0, 0, 0.12);
    }

    .section-divider h3 {
      margin: 0 0 8px 0;
      font-size: 14px;
      font-weight: 500;
      color: rgba(0, 0, 0, 0.6);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    mat-dialog-actions {
      padding: 16px 24px;
    }
  `]
})
export class AccountDialogComponent {
  private readonly apiService = inject(ApiService);
  private readonly dialogRef = inject(MatDialogRef<AccountDialogComponent>);
  private readonly fb = inject(FormBuilder);
  readonly data = inject<AccountDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly form: FormGroup;

  showAprField(): boolean {
    const type = this.form.get('type')?.value;
    return type === 'Debt' || type === 'Investment' || type === 'Cash';
  }

  showMinimumPayment(): boolean {
    return this.form.get('type')?.value === 'Debt';
  }

  showDebtFeatures(): boolean {
    return this.form.get('type')?.value === 'Debt';
  }

  constructor() {
    // Convert decimal to percentage for display (0.18 -> 18%)
    const account = this.data.account;
    this.form = this.fb.group({
      name: [account?.name || '', Validators.required],
      type: [account?.type || '', Validators.required],
      initialBalance: [account?.initialBalance || 0, Validators.required],
      annualPercentageRate: [account?.annualPercentageRate != null ? account.annualPercentageRate * 100 : null],
      minimumPayment: [account?.minimumPayment || null],
      promotionalAnnualPercentageRate: [account?.promotionalAnnualPercentageRate != null ? account.promotionalAnnualPercentageRate * 100 : null],
      promotionalPeriodEndDate: [account?.promotionalPeriodEndDate || null],
      balanceTransferFeePercentage: [account?.balanceTransferFeePercentage != null ? account.balanceTransferFeePercentage * 100 : null],
      statementDayOfMonth: [account?.statementDayOfMonth || null],
      statementDateOverride: [account?.statementDateOverride || null],
      paymentDueDayOfMonth: [account?.paymentDueDayOfMonth || null],
      paymentDueDateOverride: [account?.paymentDueDateOverride || null]
    });

    // Watch type changes to conditionally validate fields
    this.form.get('type')?.valueChanges.subscribe(type => {
      const aprControl = this.form.get('annualPercentageRate');
      const minPaymentControl = this.form.get('minimumPayment');

      if (type === 'Debt' || type === 'Investment') {
        // Debt and Investment require APR
        aprControl?.setValidators([Validators.required]);
      } else if (type === 'Cash') {
        // Cash has optional APR (for HYSA)
        aprControl?.clearValidators();
      } else {
        aprControl?.clearValidators();
      }

      // Minimum payment is only for Debt (and it's optional)
      minPaymentControl?.clearValidators();

      // Add validation for new debt fields
      if (type === 'Debt') {
        const statementDayControl = this.form.get('statementDayOfMonth');
        const paymentDueDayControl = this.form.get('paymentDueDayOfMonth');
        const btFeeControl = this.form.get('balanceTransferFeePercentage');

        statementDayControl?.setValidators([Validators.min(1), Validators.max(31)]);
        paymentDueDayControl?.setValidators([Validators.min(1), Validators.max(31)]);
        btFeeControl?.setValidators([Validators.min(0), Validators.max(100)]);

        statementDayControl?.updateValueAndValidity();
        paymentDueDayControl?.updateValueAndValidity();
        btFeeControl?.updateValueAndValidity();
      }

      aprControl?.updateValueAndValidity();
      minPaymentControl?.updateValueAndValidity();
    });

    // Disable type and initialBalance fields in edit mode
    if (this.data.mode === 'edit') {
      this.form.get('type')?.disable();
      this.form.get('initialBalance')?.disable();
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.valid) {
      this.saving.set(true);

      // Convert percentage fields to decimal (18% -> 0.18) for backend
      const formData = { ...this.form.value };
      if (formData.annualPercentageRate != null) {
        formData.annualPercentageRate = formData.annualPercentageRate / 100;
      }
      if (formData.promotionalAnnualPercentageRate != null) {
        formData.promotionalAnnualPercentageRate = formData.promotionalAnnualPercentageRate / 100;
      }
      if (formData.balanceTransferFeePercentage != null) {
        formData.balanceTransferFeePercentage = formData.balanceTransferFeePercentage / 100;
      }

      const operation = this.data.mode === 'create'
        ? this.apiService.createAccount(formData)
        : this.apiService.updateAccount(this.data.account!.id, {
            name: formData.name,
            annualPercentageRate: formData.annualPercentageRate,
            minimumPayment: formData.minimumPayment,
            promotionalAnnualPercentageRate: formData.promotionalAnnualPercentageRate,
            promotionalPeriodEndDate: formData.promotionalPeriodEndDate,
            balanceTransferFeePercentage: formData.balanceTransferFeePercentage,
            statementDayOfMonth: formData.statementDayOfMonth,
            statementDateOverride: formData.statementDateOverride,
            paymentDueDayOfMonth: formData.paymentDueDayOfMonth,
            paymentDueDateOverride: formData.paymentDueDateOverride
          });

      operation.subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Error saving account:', error);
          this.saving.set(false);
          
          // Show user-friendly error message
          let errorMessage = 'Failed to save account';
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
