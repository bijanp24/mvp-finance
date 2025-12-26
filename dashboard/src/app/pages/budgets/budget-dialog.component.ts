import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ApiService } from '../../core/services/api.service';
import { Budget, Category, CreateBudgetRequest, UpdateBudgetRequest } from '../../core/models/api.models';

interface DialogData {
  mode: 'create' | 'edit';
  budget?: Budget;
  categories: Category[];
}

@Component({
  selector: 'app-budget-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Create Budget' : 'Edit Budget' }}</h2>

    <mat-dialog-content>
      <form [formGroup]="form" class="budget-form">
        <mat-form-field appearance="outline">
          <mat-label>Category</mat-label>
          <mat-select formControlName="categoryId" required>
            @for (category of data.categories; track category.id) {
              <mat-option [value]="category.id">
                {{ category.name }} ({{ category.type }})
              </mat-option>
            }
          </mat-select>
          @if (form.get('categoryId')?.hasError('required')) {
            <mat-error>Category is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Amount</mat-label>
          <input matInput type="number" formControlName="amount" min="0.01" step="0.01" required>
          <span matTextPrefix>$&nbsp;</span>
          @if (form.get('amount')?.hasError('required')) {
            <mat-error>Amount is required</mat-error>
          }
          @if (form.get('amount')?.hasError('min')) {
            <mat-error>Amount must be greater than 0</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Frequency</mat-label>
          <mat-select formControlName="frequency" required>
            <mat-option value="Weekly">Weekly</mat-option>
            <mat-option value="BiWeekly">Bi-Weekly</mat-option>
            <mat-option value="Monthly">Monthly</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Effective Date</mat-label>
          <input matInput [matDatepicker]="effectivePicker" formControlName="effectiveDate" required>
          <mat-datepicker-toggle matIconSuffix [for]="effectivePicker"></mat-datepicker-toggle>
          <mat-datepicker #effectivePicker></mat-datepicker>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Notes (optional)</mat-label>
          <textarea matInput formControlName="notes" rows="2"></textarea>
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
    .budget-form {
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
export class BudgetDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly apiService = inject(ApiService);
  private readonly dialogRef = inject(MatDialogRef<BudgetDialogComponent>);
  readonly data: DialogData = inject(MAT_DIALOG_DATA);

  saving = false;

  form: FormGroup = this.fb.group({
    categoryId: [this.data.budget?.categoryId || null, Validators.required],
    amount: [this.data.budget?.amount || null, [Validators.required, Validators.min(0.01)]],
    frequency: [this.data.budget?.frequency || 'Monthly', Validators.required],
    effectiveDate: [this.data.budget?.effectiveDate ? new Date(this.data.budget.effectiveDate) : new Date(), Validators.required],
    notes: [this.data.budget?.notes || '']
  });

  save(): void {
    if (this.form.invalid) return;

    this.saving = true;
    const formValue = this.form.value;

    // Format date to ISO string (date only)
    const effectiveDate = formValue.effectiveDate instanceof Date
      ? formValue.effectiveDate.toISOString().split('T')[0]
      : formValue.effectiveDate;

    if (this.data.mode === 'create') {
      const request: CreateBudgetRequest = {
        categoryId: formValue.categoryId,
        amount: formValue.amount,
        frequency: formValue.frequency,
        effectiveDate: effectiveDate,
        notes: formValue.notes || undefined
      };

      this.apiService.createBudget(request).subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Error creating budget:', error);
          this.saving = false;
        }
      });
    } else {
      const request: UpdateBudgetRequest = {
        categoryId: formValue.categoryId,
        amount: formValue.amount,
        frequency: formValue.frequency,
        effectiveDate: effectiveDate,
        notes: formValue.notes
      };

      this.apiService.updateBudget(this.data.budget!.id, request).subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Error updating budget:', error);
          this.saving = false;
        }
      });
    }
  }
}
