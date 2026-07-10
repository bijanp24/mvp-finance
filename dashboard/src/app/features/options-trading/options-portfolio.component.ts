import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OptionsService, OptionParams, Greeks, PayoffPoint } from './options.service';

@Component({
  selector: 'app-options-portfolio',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="p-4">
      <h1 class="text-2xl font-bold mb-4">Options Trading</h1>
      <p>Options portfolio and risk metrics will be displayed here.</p>
      
      <div class="mt-8 grid grid-cols-1 md:grid-cols-2 gap-4">
        <div class="border rounded shadow p-4">
          <h2 class="text-xl font-semibold mb-2">Greeks Widget</h2>
          <div id="greeks-container" *ngIf="greeks()">
            <p>Delta: {{ greeks()?.delta | number:'1.4-4' }}</p>
            <p>Gamma: {{ greeks()?.gamma | number:'1.4-4' }}</p>
            <p>Vega: {{ greeks()?.vega | number:'1.4-4' }}</p>
          </div>
          <div *ngIf="!greeks() && !error()">Loading Greeks from Python engine...</div>
          <div *ngIf="error()" class="text-red-500">{{ error() }}</div>
        </div>
        
        <div class="border rounded shadow p-4">
          <h2 class="text-xl font-semibold mb-2">Payoff Chart</h2>
          <div id="payoff-chart-container" *ngIf="payoffData().length > 0">
            <p class="text-sm text-gray-500">Data points generated: {{ payoffData().length }}</p>
            <!-- Chart implementation goes here -->
          </div>
          <div *ngIf="payoffData().length === 0 && !error()">Loading visualization...</div>
        </div>
      </div>
    </div>
  `
})
export class OptionsPortfolioComponent implements OnInit {
  private optionsService = inject(OptionsService);
  
  greeks = signal<Greeks | null>(null);
  payoffData = signal<PayoffPoint[]>([]);
  error = signal<string | null>(null);
  
  defaultParams: OptionParams = {
    S: 100,
    K: 105,
    T: 1,
    r: 0.05,
    sigma: 0.2,
    option_type: 'call'
  };

  ngOnInit() {
    this.optionsService.getGreeks(this.defaultParams).subscribe({
      next: (data) => this.greeks.set(data),
      error: (err) => this.error.set('Failed to connect to Python Engine')
    });
    
    this.optionsService.getPayoff(this.defaultParams).subscribe({
      next: (data) => this.payoffData.set(data),
      error: (err) => console.error(err)
    });
  }
}
