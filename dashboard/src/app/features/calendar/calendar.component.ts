import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { CalendarService } from '../../core/services/calendar.service';
import { UserSettings, Account, RecurringContribution } from '../../core/models/api.models';

@Component({
  selector: 'app-calendar',
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.scss'
})
export class CalendarComponent {
  private readonly calendarService = inject(CalendarService);
  private readonly apiService = inject(ApiService);

  // State signals
  readonly currentMonth = signal(new Date().getMonth());
  readonly currentYear = signal(new Date().getFullYear());
  readonly settings = signal<UserSettings | null>(null);
  readonly accounts = signal<Account[]>([]);
  readonly contributions = signal<RecurringContribution[]>([]);

  // Computed calendar grid
  readonly calendarGrid = computed(() => {
    const settings = this.settings();
    const accounts = this.accounts();
    const contributions = this.contributions();
    if (!settings || !accounts) return [];

    return this.calendarService.generateCalendarGrid(
      this.currentMonth(),
      this.currentYear(),
      settings,
      accounts,
      contributions
    );
  });

  readonly monthName = computed(() => {
    const date = new Date(this.currentYear(), this.currentMonth());
    return new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' })
      .format(date);
  });

  constructor() {
    this.loadData();
  }

  loadData(): void {
    forkJoin({
      settings: this.apiService.getSettings(),
      accounts: this.apiService.getAccounts(),
      contributions: this.apiService.getRecurringContributions()
    }).subscribe(({ settings, accounts, contributions }) => {
      this.settings.set(settings);
      this.accounts.set(accounts);
      this.contributions.set(contributions);
    });
  }

  previousMonth(): void {
    const newMonth = this.currentMonth() - 1;
    if (newMonth < 0) {
      this.currentMonth.set(11);
      this.currentYear.update(y => y - 1);
    } else {
      this.currentMonth.set(newMonth);
    }
  }

  nextMonth(): void {
    const newMonth = this.currentMonth() + 1;
    if (newMonth > 11) {
      this.currentMonth.set(0);
      this.currentYear.update(y => y + 1);
    } else {
      this.currentMonth.set(newMonth);
    }
  }

  today(): void {
    const now = new Date();
    this.currentMonth.set(now.getMonth());
    this.currentYear.set(now.getFullYear());
  }

  isToday(date: Date): boolean {
    const today = new Date();
    return date.getFullYear() === today.getFullYear() &&
           date.getMonth() === today.getMonth() &&
           date.getDate() === today.getDate();
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }
}
