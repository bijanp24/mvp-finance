import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { UpdateSettingsRequest } from '../../core/models/api.models';

@Component({
  selector: 'app-settings',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="settings-container">
      <h1>Settings</h1>

      <mat-card>
        <mat-card-header>
          <mat-card-title>Income & Safety Buffer</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          @if (loading()) {
            <p>Loading settings...</p>
          } @else {
            <form [formGroup]="form">
              <mat-form-field appearance="outline">
                <mat-label>Pay Frequency</mat-label>
                <mat-select formControlName="payFrequency" required>
                  <mat-option value="Weekly">Weekly (Every 7 days)</mat-option>
                  <mat-option value="BiWeekly">Bi-Weekly (Every 14 days)</mat-option>
                  <mat-option value="SemiMonthly">Semi-Monthly (Twice per month)</mat-option>
                  <mat-option value="Monthly">Monthly (Once per month)</mat-option>
                </mat-select>
                @if (form.get('payFrequency')?.hasError('required') && form.get('payFrequency')?.touched) {
                  <mat-error>Pay frequency is required</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Next Paycheck Date</mat-label>
                <input
                  matInput
                  type="date"
                  formControlName="nextPaycheckDate">
                <mat-hint>When is your next paycheck? We'll calculate future dates from this.</mat-hint>
                @if (form.get('nextPaycheckDate')?.hasError('tooOld') && form.get('nextPaycheckDate')?.touched) {
                  <mat-error>Date cannot be more than 90 days in the past</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Paycheck Amount</mat-label>
                <input
                  matInput
                  type="number"
                  formControlName="paycheckAmount"
                  placeholder="0.00"
                  required>
                <span matTextPrefix>$&nbsp;</span>
                @if (form.get('paycheckAmount')?.hasError('required') && form.get('paycheckAmount')?.touched) {
                  <mat-error>Paycheck amount is required</mat-error>
                }
                @if (form.get('paycheckAmount')?.hasError('min')) {
                  <mat-error>Paycheck amount must be greater than 0</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Safety Buffer</mat-label>
                <input
                  matInput
                  type="number"
                  formControlName="safetyBuffer"
                  placeholder="0.00"
                  required>
                <span matTextPrefix>$&nbsp;</span>
                <mat-hint>Minimum cash cushion to maintain</mat-hint>
                @if (form.get('safetyBuffer')?.hasError('required') && form.get('safetyBuffer')?.touched) {
                  <mat-error>Safety buffer is required</mat-error>
                }
                @if (form.get('safetyBuffer')?.hasError('min')) {
                  <mat-error>Safety buffer cannot be negative</mat-error>
                }
              </mat-form-field>
            </form>
          }
        </mat-card-content>

        <mat-card-actions align="end">
          <button
            mat-raised-button
            color="primary"
            (click)="onSave()"
            [disabled]="!form.valid || saving()">
            {{ saving() ? 'Saving...' : 'Save Settings' }}
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .settings-container {
      padding: 24px;
      max-width: 800px;
    }

    h1 {
      margin: 0 0 24px 0;
      font-size: 32px;
      font-weight: 400;
    }

    mat-card {
      margin-bottom: 24px;
    }

    mat-card-content {
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

    mat-card-actions {
      padding: 16px 24px;
    }
  `]
})
export class SettingsPage implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly saving = signal(false);

  readonly form: FormGroup;

  constructor() {
    this.form = this.fb.group({
      payFrequency: ['BiWeekly', Validators.required],
      paycheckAmount: [2500, [Validators.required, Validators.min(0.01)]],
      safetyBuffer: [100, [Validators.required, Validators.min(0)]],
      nextPaycheckDate: [null, this.validateFutureDate.bind(this)]
    });
  }

  private validateFutureDate(control: any): { [key: string]: boolean } | null {
    if (!control.value) {
      return null; // Allow empty date
    }

    const selectedDate = new Date(control.value);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    // Date should not be more than 90 days in the past
    const ninetyDaysAgo = new Date(today);
    ninetyDaysAgo.setDate(ninetyDaysAgo.getDate() - 90);
    
    if (selectedDate < ninetyDaysAgo) {
      return { tooOld: true };
    }
    
    return null;
  }

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading.set(true);

    this.apiService.getSettings().subscribe({
      next: (settings) => {
        // Convert date to YYYY-MM-DD format for the date input
        let formattedDate: string | null = null;
        if (settings.nextPaycheckDate) {
          const date = new Date(settings.nextPaycheckDate);
          formattedDate = date.toISOString().split('T')[0];
        }

        this.form.patchValue({
          payFrequency: settings.payFrequency,
          paycheckAmount: settings.paycheckAmount,
          safetyBuffer: settings.safetyBuffer,
          nextPaycheckDate: formattedDate
        });
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading settings:', error);
        this.snackBar.open('Failed to load settings', 'Close', { duration: 3000 });
        this.loading.set(false);
      }
    });
  }

  onSave(): void {
    if (this.form.valid) {
      this.saving.set(true);

      // Convert date string to ISO DateTime for the API
      const formValue = this.form.value;
      let nextPaycheckDate: string | undefined = undefined;
      if (formValue.nextPaycheckDate) {
        // Create date at noon UTC to avoid timezone issues
        const date = new Date(formValue.nextPaycheckDate + 'T12:00:00Z');
        nextPaycheckDate = date.toISOString();
      }

      const request: UpdateSettingsRequest = {
        payFrequency: formValue.payFrequency,
        paycheckAmount: formValue.paycheckAmount,
        safetyBuffer: formValue.safetyBuffer,
        nextPaycheckDate
      };

      this.apiService.updateSettings(request).subscribe({
        next: () => {
          this.saving.set(false);
          this.snackBar.open('Settings saved successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error saving settings:', error);
          this.snackBar.open('Failed to save settings', 'Close', { duration: 3000 });
          this.saving.set(false);
        }
      });
    }
  }
}
