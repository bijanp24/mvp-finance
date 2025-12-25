import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { ApiService } from '../../core/services/api.service';
import { UpdateSettingsRequest, RecurringContribution, Account } from '../../core/models/api.models';
import { RecurringContributionDialogComponent } from './recurring-contribution-dialog.component';

@Component({
  selector: 'app-settings',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatDialogModule,
    MatChipsModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="settings-container">
      <header class="page-header">
        <h1>Settings</h1>
      </header>

      <div class="app-card settings-card">
        <div class="section-header">
          <h2>Income & Buffer</h2>
          <p class="section-description">Configure your recurring income and safety threshold for cash flow projections.</p>
        </div>

        @if (loading()) {
          <div class="skeleton-form">
            <div class="skeleton-line"></div>
            <div class="skeleton-line"></div>
            <div class="skeleton-line"></div>
          </div>
        } @else {
          <form [formGroup]="form">
            <div class="form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Pay Frequency</mat-label>
                <mat-select formControlName="payFrequency" required>
                  <mat-option value="Weekly">Weekly (Every 7 days)</mat-option>
                  <mat-option value="BiWeekly">Bi-Weekly (Every 14 days)</mat-option>
                  <mat-option value="SemiMonthly">Semi-Monthly (Twice per month)</mat-option>
                  <mat-option value="Monthly">Monthly (Once per month)</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Next Paycheck Date</mat-label>
                <input matInput type="date" formControlName="nextPaycheckDate">
                <mat-hint>Base date for future paycheck calculations</mat-hint>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Paycheck Amount</mat-label>
                <input matInput type="number" formControlName="paycheckAmount" placeholder="0.00" required>
                <span matTextPrefix>$&nbsp;</span>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Safety Buffer</mat-label>
                <input matInput type="number" formControlName="safetyBuffer" placeholder="0.00" required>
                <span matTextPrefix>$&nbsp;</span>
                <mat-hint>Minimum cash cushion to maintain</mat-hint>
              </mat-form-field>
            </div>

            <div class="form-actions">
              <button mat-flat-button color="primary" (click)="onSave()" [disabled]="!form.valid || saving()">
                <mat-icon>{{ saving() ? 'sync' : 'save' }}</mat-icon>
                {{ saving() ? 'Saving...' : 'Save Settings' }}
              </button>
            </div>
          </form>
        }
      </div>

      <div class="app-card settings-card">
        <div class="section-header">
          <h2>Recurring Contributions</h2>
          <p class="section-description">Manage scheduled investment and savings contributions.</p>
        </div>

        @if (loadingContributions()) {
          <div class="skeleton-form">
            <div class="skeleton-line"></div>
            <div class="skeleton-line"></div>
          </div>
        } @else {
          <div class="contributions-section">
            <button mat-flat-button color="primary" (click)="openCreateContributionDialog()">
              <mat-icon>add</mat-icon>
              Add Contribution
            </button>

            @if (contributions().length === 0) {
              <div class="empty-state">
                <p>No recurring contributions configured yet.</p>
              </div>
            } @else {
              <div class="contributions-list">
                @for (contribution of contributions(); track contribution.id) {
                  <div class="contribution-card">
                    <div class="contribution-header">
                      <h3>{{ contribution.name }}</h3>
                      <mat-chip [class.active]="contribution.isActive" [class.inactive]="!contribution.isActive">
                        {{ contribution.isActive ? 'Active' : 'Inactive' }}
                      </mat-chip>
                    </div>
                    <div class="contribution-details">
                      <p class="contribution-amount">\${{ contribution.amount | number:'1.2-2' }} {{ contribution.frequency }}</p>
                      <p class="contribution-accounts">
                        {{ contribution.sourceAccountName || 'Unknown' }} → {{ contribution.targetAccountName || 'Unknown' }}
                      </p>
                      <p class="contribution-next">Next: {{ formatDate(contribution.nextContributionDate) }}</p>
                    </div>
                    <div class="contribution-actions">
                      <button mat-button (click)="openEditContributionDialog(contribution)">
                        <mat-icon>edit</mat-icon>
                        Edit
                      </button>
                      <button mat-button (click)="toggleContribution(contribution)">
                        <mat-icon>{{ contribution.isActive ? 'pause' : 'play_arrow' }}</mat-icon>
                        {{ contribution.isActive ? 'Deactivate' : 'Activate' }}
                      </button>
                      <button mat-button color="warn" (click)="deleteContribution(contribution)">
                        <mat-icon>delete</mat-icon>
                        Delete
                      </button>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>

      <div class="app-card danger-zone">
        <div class="section-header">
          <h2>Data Management</h2>
          <p class="section-description">Manage your account data and persistence.</p>
        </div>
        <p class="info-text">Automatic cloud sync is currently enabled for your account.</p>
        <button mat-stroked-button color="warn" disabled>
          <mat-icon>delete_forever</mat-icon>
          Reset All Data
        </button>
      </div>
    </div>
  `,
  styles: [`
    .settings-container {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-xxl);
      max-width: 900px;
    }

    .page-header h1 {
      font-size: 2.5rem;
      margin: 0;
      color: var(--color-primary);
    }

    .section-header {
      margin-bottom: var(--spacing-xl);

      h2 {
        font-size: 1.25rem;
        margin: 0 0 4px 0;
        font-family: 'Fraunces', serif;
      }

      .section-description {
        margin: 0;
        font-size: 0.875rem;
        color: var(--color-text-muted);
      }
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: var(--spacing-lg);
      margin-bottom: var(--spacing-xl);
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      padding-top: var(--spacing-lg);
      border-top: 1px solid var(--color-divider);

      button {
        padding: 0 var(--spacing-xl);
      }
    }

    .contributions-section {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-lg);
    }

    .contributions-list {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-md);
    }

    .contribution-card {
      padding: var(--spacing-lg);
      border: 1px solid var(--color-divider);
      border-radius: var(--radius-md);
      background: var(--color-surface);

      .contribution-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: var(--spacing-sm);

        h3 {
          margin: 0;
          font-size: 1rem;
          font-weight: 600;
        }

        mat-chip {
          font-size: 0.75rem;
          min-height: 24px;
          padding: 0 8px;

          &.active {
            background-color: var(--color-success);
            color: white;
          }

          &.inactive {
            background-color: var(--color-text-muted);
            color: white;
          }
        }
      }

      .contribution-details {
        margin-bottom: var(--spacing-md);

        p {
          margin: 4px 0;
          font-size: 0.875rem;
        }

        .contribution-amount {
          font-weight: 600;
          color: var(--color-primary);
        }

        .contribution-accounts {
          color: var(--color-text-main);
        }

        .contribution-next {
          color: var(--color-text-muted);
          font-size: 0.8125rem;
        }
      }

      .contribution-actions {
        display: flex;
        gap: var(--spacing-sm);
        flex-wrap: wrap;

        button {
          font-size: 0.875rem;
        }
      }
    }

    .empty-state {
      padding: var(--spacing-xl);
      text-align: center;
      color: var(--color-text-muted);

      p {
        margin: 0;
      }
    }

    .danger-zone {
      border-left: 4px solid var(--color-warn);
      
      .info-text {
        font-size: 0.875rem;
        color: var(--color-text-main);
        margin-bottom: var(--spacing-lg);
      }
    }

    .skeleton-form {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-md);

      .skeleton-line {
        height: 56px;
        background: var(--color-border);
        border-radius: var(--radius-sm);
        animation: pulse 1.5s infinite ease-in-out;
      }
    }

    @keyframes pulse {
      0% { opacity: 0.6; }
      50% { opacity: 0.3; }
      100% { opacity: 0.6; }
    }
  `]
})
export class SettingsPage implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly loadingContributions = signal(true);
  
  readonly contributions = signal<RecurringContribution[]>([]);
  readonly accounts = signal<Account[]>([]);

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
    this.loadContributions();
    this.loadAccounts();
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

  loadContributions(): void {
    this.loadingContributions.set(true);

    this.apiService.getRecurringContributions().subscribe({
      next: (contributions) => {
        this.contributions.set(contributions);
        this.loadingContributions.set(false);
      },
      error: (error) => {
        console.error('Error loading contributions:', error);
        this.snackBar.open('Failed to load recurring contributions', 'Close', { duration: 3000 });
        this.loadingContributions.set(false);
      }
    });
  }

  loadAccounts(): void {
    this.apiService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
      },
      error: (error) => {
        console.error('Error loading accounts:', error);
      }
    });
  }

  openCreateContributionDialog(): void {
    const dialogRef = this.dialog.open(RecurringContributionDialogComponent, {
      width: '600px',
      data: { mode: 'create', accounts: this.accounts() }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadContributions();
        this.snackBar.open('Contribution created successfully', 'Close', { duration: 3000 });
      }
    });
  }

  openEditContributionDialog(contribution: RecurringContribution): void {
    const dialogRef = this.dialog.open(RecurringContributionDialogComponent, {
      width: '600px',
      data: { mode: 'edit', contribution, accounts: this.accounts() }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadContributions();
        this.snackBar.open('Contribution updated successfully', 'Close', { duration: 3000 });
      }
    });
  }

  toggleContribution(contribution: RecurringContribution): void {
    this.apiService.toggleRecurringContribution(contribution.id).subscribe({
      next: () => {
        this.loadContributions();
        const status = contribution.isActive ? 'deactivated' : 'activated';
        this.snackBar.open(`Contribution ${status} successfully`, 'Close', { duration: 3000 });
      },
      error: (error) => {
        console.error('Error toggling contribution:', error);
        this.snackBar.open('Failed to toggle contribution', 'Close', { duration: 3000 });
      }
    });
  }

  deleteContribution(contribution: RecurringContribution): void {
    if (confirm(`Are you sure you want to delete "${contribution.name}"?`)) {
      this.apiService.deleteRecurringContribution(contribution.id).subscribe({
        next: () => {
          this.loadContributions();
          this.snackBar.open('Contribution deleted successfully', 'Close', { duration: 3000 });
        },
        error: (error) => {
          console.error('Error deleting contribution:', error);
          this.snackBar.open('Failed to delete contribution', 'Close', { duration: 3000 });
        }
      });
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric' 
    });
  }
}