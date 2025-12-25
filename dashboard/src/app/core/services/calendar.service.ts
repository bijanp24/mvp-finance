import { Injectable } from '@angular/core';
import { Account, UserSettings, RecurringContribution } from '../models/api.models';

export interface ContributionEvent {
  date: Date;
  name: string;
  amount: number;
  targetAccountName?: string;
}

export interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  paychecks: number[];
  debtPayments: Array<{
    accountName: string;
    minimumPayment: number;
  }>;
  contributions: ContributionEvent[];
}

@Injectable({ providedIn: 'root' })
export class CalendarService {

  generateCalendarGrid(
    month: number,
    year: number,
    settings: UserSettings,
    accounts: Account[],
    contributions: RecurringContribution[] = []
  ): CalendarDay[][] {
    // Create calendar grid for the month
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay()); // Start on Sunday

    const weeks: CalendarDay[][] = [];
    let currentWeek: CalendarDay[] = [];

    // Calculate paychecks and debt payments for this month
    const paycheckDates = settings.nextPaycheckDate
      ? this.calculatePaycheckDates(
          new Date(settings.nextPaycheckDate),
          settings.payFrequency,
          month,
          year
        )
      : [];

    const debtPayments = this.calculateDebtDueDates(accounts, month, year);

    // Calculate contribution dates for this month
    const contributionEvents = this.calculateContributionDates(
      contributions.filter(c => c.isActive),
      month,
      year
    );

    // Build calendar grid (6 weeks max)
    for (let week = 0; week < 6; week++) {
      currentWeek = [];
      for (let day = 0; day < 7; day++) {
        const currentDate = new Date(startDate);
        const isCurrentMonth = currentDate.getMonth() === month;

        // Find paychecks for this date
        const dayPaychecks = paycheckDates
          .filter(pd => this.isSameDay(pd, currentDate))
          .map(() => settings.paycheckAmount);

        // Find debt payments for this date
        const dayDebtPayments = debtPayments
          .filter(dp => this.isSameDay(dp.date, currentDate))
          .map(dp => ({
            accountName: dp.accountName,
            minimumPayment: dp.amount
          }));

        // Find contributions for this date
        const dayContributions = contributionEvents
          .filter(ce => this.isSameDay(ce.date, currentDate));

        currentWeek.push({
          date: new Date(currentDate),
          isCurrentMonth,
          paychecks: dayPaychecks,
          debtPayments: dayDebtPayments,
          contributions: dayContributions
        });

        startDate.setDate(startDate.getDate() + 1);
      }
      weeks.push(currentWeek);

      // Stop if we've passed the last day of the month
      if (startDate > lastDay && currentWeek[currentWeek.length - 1].date.getMonth() !== month) {
        break;
      }
    }

    return weeks;
  }

  calculatePaycheckDates(
    anchorDate: Date,
    payFrequency: string,
    month: number,
    year: number
  ): Date[] {
    const paychecks: Date[] = [];
    const intervalDays = this.getPayFrequencyDays(payFrequency);
    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);

    // Calculate how many intervals ago the anchor date was from the first day of the month
    const daysDiff = Math.floor((firstDayOfMonth.getTime() - anchorDate.getTime()) / (1000 * 60 * 60 * 24));
    const intervalsPassed = Math.floor(daysDiff / intervalDays);

    // Start from a paycheck date before or at the first of the month
    let currentPaycheck = new Date(anchorDate);
    currentPaycheck.setDate(currentPaycheck.getDate() + (intervalsPassed * intervalDays));

    // If we're before the month, move forward one interval
    if (currentPaycheck < firstDayOfMonth) {
      currentPaycheck.setDate(currentPaycheck.getDate() + intervalDays);
    }

    // Collect all paychecks within the month
    while (currentPaycheck <= lastDayOfMonth) {
      if (currentPaycheck >= firstDayOfMonth) {
        paychecks.push(new Date(currentPaycheck));
      }
      currentPaycheck.setDate(currentPaycheck.getDate() + intervalDays);
    }

    return paychecks;
  }

  calculateDebtDueDates(
    accounts: Account[],
    month: number,
    year: number
  ): Array<{ date: Date; accountName: string; amount: number }> {
    const dueDates: Array<{ date: Date; accountName: string; amount: number }> = [];

    // Filter to debt accounts only
    const debtAccounts = accounts.filter(a => a.type === 'Debt');

    for (const account of debtAccounts) {
      let dueDate: Date | null = null;

      // Check for override date first
      if (account.paymentDueDateOverride) {
        const overrideDate = new Date(account.paymentDueDateOverride);
        if (overrideDate.getMonth() === month && overrideDate.getFullYear() === year) {
          dueDate = overrideDate;
        }
      }

      // If no override or override is not in this month, use recurring day
      if (!dueDate && account.paymentDueDayOfMonth) {
        const dayOfMonth = account.paymentDueDayOfMonth;
        const lastDayOfMonth = new Date(year, month + 1, 0).getDate();

        // Clamp day to valid range (handles day 31 in 30-day months)
        const validDay = Math.min(dayOfMonth, lastDayOfMonth);
        dueDate = new Date(year, month, validDay);
      }

      if (dueDate && account.minimumPayment) {
        dueDates.push({
          date: dueDate,
          accountName: account.name,
          amount: account.minimumPayment
        });
      }
    }

    return dueDates;
  }

  private getPayFrequencyDays(frequency: string): number {
    switch (frequency) {
      case 'Weekly': return 7;
      case 'BiWeekly': return 14;
      case 'SemiMonthly': return 15;
      case 'Monthly': return 30;
      default: return 14;
    }
  }

  calculateContributionDates(
    contributions: RecurringContribution[],
    month: number,
    year: number
  ): ContributionEvent[] {
    const events: ContributionEvent[] = [];
    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);

    for (const contribution of contributions) {
      const occurrences = this.expandContributionSchedule(
        contribution,
        firstDayOfMonth,
        lastDayOfMonth
      );
      events.push(...occurrences);
    }

    return events;
  }

  private expandContributionSchedule(
    contribution: RecurringContribution,
    startDate: Date,
    endDate: Date
  ): ContributionEvent[] {
    const events: ContributionEvent[] = [];
    let current = new Date(contribution.nextContributionDate);
    const frequencyDays = this.getFrequencyDays(contribution.frequency);

    // Start from the first occurrence on or after startDate
    while (current < startDate) {
      current = this.addDaysToDate(current, frequencyDays);
    }

    // Collect all occurrences within the date range
    while (current <= endDate) {
      if (current >= startDate) {
        events.push({
          date: new Date(current),
          name: contribution.name,
          amount: contribution.amount,
          targetAccountName: contribution.targetAccountName
        });
      }
      current = this.addDaysToDate(current, frequencyDays);
    }

    return events;
  }

  private getFrequencyDays(frequency: string): number {
    switch (frequency) {
      case 'Weekly': return 7;
      case 'BiWeekly': return 14;
      case 'SemiMonthly': return 15;
      case 'Monthly': return 30;
      case 'Quarterly': return 91;
      case 'Annually': return 365;
      default: return 30;
    }
  }

  private addDaysToDate(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  private isSameDay(date1: Date, date2: Date): boolean {
    return date1.getFullYear() === date2.getFullYear() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getDate() === date2.getDate();
  }
}
