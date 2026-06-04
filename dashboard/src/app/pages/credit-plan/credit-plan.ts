import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/services/api.service';
import {
  CreditActionPlanDefaultsResponse,
  CreditActionPlanResponse,
  DebtStrategy
} from '../../core/models/api.models';

@Component({
  selector: 'app-credit-plan',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './credit-plan.html',
  styleUrl: './credit-plan.scss'
})
export class CreditPlanPage {
  private readonly apiService = inject(ApiService);

  readonly loading = signal(true);
  readonly calculating = signal(false);
  readonly defaults = signal<CreditActionPlanDefaultsResponse | null>(null);
  readonly result = signal<CreditActionPlanResponse | null>(null);

  // Inputs
  readonly windfall = signal(0);
  readonly emergencyFundMonths = signal(6);
  readonly monthlyEssentialExpenses = signal(0);
  readonly strategy = signal<DebtStrategy>('Avalanche');

  constructor() {
    this.loadDefaults();
  }

  loadDefaults(): void {
    this.loading.set(true);
    this.apiService.getCreditActionPlanDefaults().subscribe({
      next: defaults => {
        this.defaults.set(defaults);
        this.windfall.set(Math.round(defaults.suggestedWindfall));
        this.emergencyFundMonths.set(defaults.defaultEmergencyFundMonths);
        this.monthlyEssentialExpenses.set(Math.round(defaults.monthlyEssentialExpenses));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  generatePlan(): void {
    this.calculating.set(true);
    this.apiService.calculateCreditActionPlan({
      windfall: this.windfall(),
      emergencyFundMonths: this.emergencyFundMonths(),
      monthlyEssentialExpenses: this.monthlyEssentialExpenses(),
      strategy: this.strategy()
    }).subscribe({
      next: result => {
        this.result.set(result);
        this.calculating.set(false);
      },
      error: () => this.calculating.set(false)
    });
  }

  formatCurrency(value: number | undefined | null): string {
    if (value == null) return '$0';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  }

  formatPercent(rate: number): string {
    return (rate * 100).toLocaleString('en-US', { maximumFractionDigits: 2 }) + '%';
  }

  formatMonths(months: number | null | undefined): string {
    if (months == null) return 'Never';
    if (months === 0) return 'Paid off';
    const years = Math.floor(months / 12);
    const rem = months % 12;
    if (years === 0) return `${rem} mo`;
    if (rem === 0) return `${years} yr`;
    return `${years} yr ${rem} mo`;
  }
}
