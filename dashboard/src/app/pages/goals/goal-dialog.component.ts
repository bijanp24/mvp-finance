import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ApiService } from '../../core/services/api.service';
import { Goal, Account, CreateGoalRequest, UpdateGoalRequest, GoalType } from '../../core/models/api.models';

interface DialogData {
  mode: 'create' | 'edit';
  goal?: Goal;
  accounts: Account[];
}

@Component({
  selector: 'app-goal-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Create Goal' : 'Edit Goal' }}</h2>

    <mat-dialog-content>
      <form [formGroup]="form" class="goal-form">
        <mat-form-field appearance="outline">
          <mat-label>Goal Name</mat-label>
          <input matInput formControlName="name" placeholder="e.g., Emergency Fund, Debt Free by 2026" required>
          @if (form.get('name')?.hasError('required')) {
            <mat-error>Name is required</mat-error>
          }
          @if (form.get('name')?.hasError('maxlength')) {
            <mat-error>Name cannot exceed 100 characters</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Goal Type</mat-label>
          <mat-select formControlName="type" required>
            <mat-option value="DebtFree">Debt Free - Pay off all linked debt</mat-option>
            <mat-option value="SavingsGoal">Savings Goal - Save for a specific purpose</mat-option>
            <mat-option value="InvestmentTarget">Investment Target - Reach investment milestone</mat-option>
            <mat-option value="NetWorthMilestone">Net Worth Milestone - Hit net worth target</mat-option>
          </mat-select>
          @if (form.get('type')?.hasError('required')) {
            <mat-error>Type is required</mat-error>
          }
        </mat-form-field>

        @if (selectedType() !== 'DebtFree') {
          <mat-form-field appearance="outline">
            <mat-label>Target Amount</mat-label>
            <input matInput type="number" formControlName="targetAmount" min="1" step="100" required>
            <span matTextPrefix>$&nbsp;</span>
            @if (form.get('targetAmount')?.hasError('required')) {
              <mat-error>Target amount is required</mat-error>
            }
            @if (form.get('targetAmount')?.hasError('min')) {
              <mat-error>Amount must be greater than 0</mat-error>
            }
          </mat-form-field>
        }

        <mat-form-field appearance="outline">
          <mat-label>Target Date</mat-label>
          <input matInput [matDatepicker]="targetPicker" formControlName="targetDate" required [min]="minDate">
          <mat-datepicker-toggle matIconSuffix [for]="targetPicker"></mat-datepicker-toggle>
          <mat-datepicker #targetPicker></mat-datepicker>
          @if (form.get('targetDate')?.hasError('required')) {
            <mat-error>Target date is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Linked Accounts (optional)</mat-label>
          <mat-select formControlName="linkedAccountIds" multiple>
            @for (account of filteredAccounts(); track account.id) {
              <mat-option [value]="account.id">
                {{ account.name }} ({{ account.type }})
              </mat-option>
            }
          </mat-select>
          <mat-hint>{{ getAccountHint() }}</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Priority</mat-label>
          <input matInput type="number" formControlName="priority" min="1" max="99">
          <mat-hint>Lower number = higher priority</mat-hint>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Notes (optional)</mat-label>
          <textarea matInput formControlName="notes" rows="2" placeholder="Any additional details about this goal"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="form.invalid || saving">
        {{ saving ? 'Saving...' : (data.mode === 'create' ? 'Create' : 'Save') }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .goal-form {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 400px;
      padding-top: 8px;
    }

    mat-form-field {
      width: 100%;
    }

    mat-dialog-content {
      padding-top: 0;
    }
  `]
})
export class GoalDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly apiService = inject(ApiService);
  private readonly dialogRef = inject(MatDialogRef<GoalDialogComponent>);
  readonly data: DialogData = inject(MAT_DIALOG_DATA);

  saving = false;
  minDate = new Date();

  readonly selectedType = signal<GoalType | null>(this.data.goal?.type || null);

  form: FormGroup = this.fb.group({
    name: [this.data.goal?.name || '', [Validators.required, Validators.maxLength(100)]],
    type: [this.data.goal?.type || 'SavingsGoal', Validators.required],
    targetAmount: [this.data.goal?.targetAmount || null, [Validators.min(1)]],
    targetDate: [this.data.goal?.targetDate ? new Date(this.data.goal.targetDate) : null, Validators.required],
    linkedAccountIds: [this.data.goal?.linkedAccountIds || []],
    priority: [this.data.goal?.priority || 1, [Validators.min(1), Validators.max(99)]],
    notes: [this.data.goal?.notes || '']
  });

  readonly filteredAccounts = computed(() => {
    const type = this.selectedType();
    if (!type) return this.data.accounts;

    switch (type) {
      case 'DebtFree':
        return this.data.accounts.filter(a => a.type === 'Debt');
      case 'InvestmentTarget':
        return this.data.accounts.filter(a => a.type === 'Investment');
      case 'SavingsGoal':
        return this.data.accounts.filter(a => a.type === 'Cash' || a.type === 'Investment');
      case 'NetWorthMilestone':
        return this.data.accounts;
      default:
        return this.data.accounts;
    }
  });

  constructor() {
    // Update selectedType when form type changes
    this.form.get('type')?.valueChanges.subscribe(type => {
      this.selectedType.set(type);
      // Clear linked accounts when type changes
      if (this.data.mode === 'create') {
        this.form.get('linkedAccountIds')?.setValue([]);
      }
    });

    // Set initial type
    this.selectedType.set(this.form.get('type')?.value);
  }

  getAccountHint(): string {
    const type = this.selectedType();
    switch (type) {
      case 'DebtFree':
        return 'Select debt accounts to track';
      case 'InvestmentTarget':
        return 'Select investment accounts to track';
      case 'SavingsGoal':
        return 'Select cash or investment accounts';
      case 'NetWorthMilestone':
        return 'All accounts contribute to net worth';
      default:
        return '';
    }
  }

  save(): void {
    if (this.form.invalid) return;

    this.saving = true;
    const formValue = this.form.value;

    // Format date to ISO string (date only)
    const targetDate = formValue.targetDate instanceof Date
      ? formValue.targetDate.toISOString().split('T')[0]
      : formValue.targetDate;

    if (this.data.mode === 'create') {
      const request: CreateGoalRequest = {
        name: formValue.name,
        type: formValue.type,
        targetAmount: formValue.type === 'DebtFree' ? undefined : formValue.targetAmount,
        targetDate: targetDate,
        linkedAccountIds: formValue.linkedAccountIds?.length > 0 ? formValue.linkedAccountIds : undefined,
        priority: formValue.priority,
        notes: formValue.notes || undefined
      };

      this.apiService.createGoal(request).subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Error creating goal:', error);
          this.saving = false;
        }
      });
    } else {
      const request: UpdateGoalRequest = {
        name: formValue.name,
        type: formValue.type,
        targetAmount: formValue.type === 'DebtFree' ? undefined : formValue.targetAmount,
        targetDate: targetDate,
        linkedAccountIds: formValue.linkedAccountIds,
        priority: formValue.priority,
        notes: formValue.notes || undefined
      };

      this.apiService.updateGoal(this.data.goal!.id, request).subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Error updating goal:', error);
          this.saving = false;
        }
      });
    }
  }
}
