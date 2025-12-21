import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ProjectionService } from '../../core/services/projection.service';
import { DebtProjectionChartComponent } from '../../features/charts/debt-projection-chart.component';
import { InvestmentProjectionChartComponent } from '../../features/charts/investment-projection-chart.component';
import { Account, UserSettings } from '../../core/models/api.models';

@Component({
  selector: 'app-projections',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonToggleModule,
    DebtProjectionChartComponent,
    InvestmentProjectionChartComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './projections.html',
  styleUrl: './projections.scss'
})
export class ProjectionsPage {
  private readonly apiService = inject(ApiService);
  private readonly projectionService = inject(ProjectionService);

  readonly timeRangeMonths = signal(12); // 1 year default
  readonly accounts = signal<Account[]>([]);
  readonly settings = signal<UserSettings | null>(null);

  readonly debtChartData = this.projectionService.debtChartData;
  readonly investmentChartData = this.projectionService.investmentChartData;
  readonly loading = this.projectionService.loading;

  readonly debtProjection = this.projectionService.debtProjection;
  readonly investmentProjection = this.projectionService.investmentProjection;

  constructor() {
    this.loadData();
  }

  loadData(): void {
    forkJoin({
      accounts: this.apiService.getAccounts(),
      settings: this.apiService.getSettings()
    }).subscribe(({ accounts, settings }) => {
      this.accounts.set(accounts);
      this.settings.set(settings);
      this.calculateProjections();
    });
  }

  onTimeRangeChange(months: number): void {
    this.timeRangeMonths.set(months);
    this.calculateProjections();
  }

  private calculateProjections(): void {
    this.projectionService.calculateProjections(
      this.accounts(),
      this.timeRangeMonths(),
      this.settings() || undefined
    ).subscribe();
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }

  formatDate(dateString?: string): string {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long'
    });
  }
}
