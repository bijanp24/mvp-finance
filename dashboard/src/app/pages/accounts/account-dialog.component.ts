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
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Initial Balance</mat-label>
          <input matInput type="number" formControlName="initialBalance" placeholder="0.00" required>
          <span matTextPrefix>$&nbsp;</span>
          @if (form.get('initialBalance')?.hasError('required') && form.get('initialBalance')?.touched) {
            <mat-error>Initial balance is required</mat-error>
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
            <input matInput type="number" formControlName="minimumPayment" placeholder="0.00">
            <span matTextPrefix>$&nbsp;</span>
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
      min-width: 400px;
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

  constructor() {
    this.form = this.fb.group({
      name: [this.data.account?.name || '', Validators.required],
      type: [this.data.account?.type || '', Validators.required],
      initialBalance: [this.data.account?.initialBalance || 0, Validators.required],
      annualPercentageRate: [this.data.account?.annualPercentageRate || null],
      minimumPayment: [this.data.account?.minimumPayment || null]
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

      aprControl?.updateValueAndValidity();
      minPaymentControl?.updateValueAndValidity();
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.valid) {
      this.saving.set(true);
      const request: CreateAccountRequest = this.form.value;

      const operation = this.data.mode === 'create'
        ? this.apiService.createAccount(request)
        : this.apiService.updateAccount(this.data.account!.id, request);

      operation.subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Error saving account:', error);
          this.saving.set(false);
          // TODO: Show error message to user
        }
      });
    }
  }
}
