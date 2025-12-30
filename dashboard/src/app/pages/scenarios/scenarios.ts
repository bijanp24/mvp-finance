import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatSliderModule } from '@angular/material/slider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { ApiService } from '../../core/services/api.service';
import {
  ScenarioRequest,
  ScenarioResponse,
  ScenarioDefaultsResponse,
  ScenarioNetWorthSnapshot
} from '../../core/models/api.models';

@Component({
  selector: 'app-scenarios',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatSliderModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './scenarios.html',
  styleUrl: './scenarios.scss'
})
export class ScenariosPage {
  private readonly apiService = inject(ApiService);

  // Loading states
  readonly loading = signal(true);
  readonly calculating = signal(false);

  // Slider defaults and ranges
  readonly defaults = signal<ScenarioDefaultsResponse | null>(null);

  // Slider values
  readonly monthlyDiscretionary = signal(0);
  readonly extraDebtPayment = signal(0);
  readonly extraInvestmentContribution = signal(0);

  // Scenario result
  readonly result = signal<ScenarioResponse | null>(null);

  // Subject for debounced recalculation
  private readonly recalculateSubject = new Subject<ScenarioRequest>();

  // Computed values for display
  readonly discretionaryChange = computed(() => {
    const base = this.defaults()?.baseDiscretionary ?? 0;
    return this.monthlyDiscretionary() - base;
  });

  readonly totalMonthlyChange = computed(() => {
    return this.discretionaryChange() + this.extraDebtPayment() + this.extraInvestmentContribution();
  });

  readonly hasChanges = computed(() => {
    const d = this.defaults();
    if (!d) return false;
    return this.monthlyDiscretionary() !== d.baseDiscretionary ||
           this.extraDebtPayment() !== 0 ||
           this.extraInvestmentContribution() !== 0;
  });

  constructor() {
    this.loadDefaults();

    // Debounced recalculation
    this.recalculateSubject.pipe(
      debounceTime(300),
      distinctUntilChanged((prev, curr) =>
        prev.monthlyDiscretionary === curr.monthlyDiscretionary &&
        prev.extraDebtPayment === curr.extraDebtPayment &&
        prev.extraInvestmentContribution === curr.extraInvestmentContribution
      ),
      switchMap(request => {
        this.calculating.set(true);
        return this.apiService.calculateScenario(request);
      })
    ).subscribe({
      next: result => {
        this.result.set(result);
        this.calculating.set(false);
      },
      error: () => {
        this.calculating.set(false);
      }
    });
  }

  loadDefaults(): void {
    this.loading.set(true);
    this.apiService.getScenarioDefaults().subscribe({
      next: defaults => {
        this.defaults.set(defaults);
        this.monthlyDiscretionary.set(defaults.baseDiscretionary);
        this.extraDebtPayment.set(0);
        this.extraInvestmentContribution.set(0);
        this.loading.set(false);
        // Initial calculation
        this.triggerRecalculation();
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  onSliderChange(): void {
    this.triggerRecalculation();
  }

  resetToDefaults(): void {
    const d = this.defaults();
    if (d) {
      this.monthlyDiscretionary.set(d.baseDiscretionary);
      this.extraDebtPayment.set(0);
      this.extraInvestmentContribution.set(0);
      this.triggerRecalculation();
    }
  }

  private triggerRecalculation(): void {
    this.recalculateSubject.next({
      monthlyDiscretionary: this.monthlyDiscretionary(),
      extraDebtPayment: this.extraDebtPayment(),
      extraInvestmentContribution: this.extraInvestmentContribution()
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

  formatSignedCurrency(value: number): string {
    const formatted = this.formatCurrency(Math.abs(value));
    if (value > 0) return `+${formatted}`;
    if (value < 0) return `-${formatted}`;
    return formatted;
  }

  formatDate(dateString?: string | null): string {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short'
    });
  }

  formatMonths(months: number | null | undefined): string {
    if (months == null) return 'N/A';
    if (months === 0) return 'Already paid off';
    const years = Math.floor(months / 12);
    const remainingMonths = months % 12;
    if (years === 0) return `${remainingMonths} mo`;
    if (remainingMonths === 0) return `${years} yr`;
    return `${years} yr ${remainingMonths} mo`;
  }

  getStatusClass(surplus: number): string {
    if (surplus >= 100) return 'healthy';
    if (surplus >= 0) return 'tight';
    return 'deficit';
  }

  getComparisonClass(value: number): string {
    if (value > 0) return 'positive';
    if (value < 0) return 'negative';
    return 'neutral';
  }

  // Expose Math for template
  readonly Math = Math;

  // Timeline chart helpers
  getBarHeight(netWorth: number, snapshots: ScenarioNetWorthSnapshot[]): number {
    const maxNetWorth = Math.max(...snapshots.map(s => Math.abs(s.netWorth)));
    if (maxNetWorth === 0) return 10;
    return Math.max(10, (Math.abs(netWorth) / maxNetWorth) * 100);
  }

  getDebtRatio(snapshot: ScenarioNetWorthSnapshot, snapshots: ScenarioNetWorthSnapshot[]): number {
    const total = snapshot.investments + snapshot.debt;
    if (total === 0) return 0;
    return (snapshot.debt / total) * 100;
  }

  getInvestmentRatio(snapshot: ScenarioNetWorthSnapshot, snapshots: ScenarioNetWorthSnapshot[]): number {
    const total = snapshot.investments + snapshot.debt;
    if (total === 0) return 100;
    return (snapshot.investments / total) * 100;
  }
}
