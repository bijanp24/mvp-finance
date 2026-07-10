import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.DashboardPage)
  },
  {
    path: 'settings',
    loadComponent: () => import('./pages/settings/settings').then(m => m.SettingsPage)
  },
  {
    path: 'accounts',
    loadComponent: () => import('./pages/accounts/accounts').then(m => m.AccountsPage)
  },
  {
    path: 'transactions',
    loadComponent: () => import('./pages/transactions/transactions').then(m => m.TransactionsPage)
  },
  {
    path: 'budgets',
    loadComponent: () => import('./pages/budgets/budgets').then(m => m.BudgetsPage)
  },
  {
    path: 'goals',
    loadComponent: () => import('./pages/goals/goals').then(m => m.GoalsPage)
  },
  {
    path: 'calendar',
    loadComponent: () => import('./features/calendar/calendar.component').then(m => m.CalendarComponent)
  },
  {
    path: 'projections',
    loadComponent: () => import('./pages/projections/projections').then(m => m.ProjectionsPage)
  },
  {
    path: 'scenarios',
    loadComponent: () => import('./pages/scenarios/scenarios').then(m => m.ScenariosPage)
  },
  {
    path: 'credit-plan',
    loadComponent: () => import('./pages/credit-plan/credit-plan').then(m => m.CreditPlanPage)
  },
  {
    path: 'options',
    loadChildren: () => import('./features/options-trading/options-trading.routes').then(m => m.OPTIONS_TRADING_ROUTES)
  }
];
