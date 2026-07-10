import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

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
          <div id="greeks-container">Loading Greeks from Python engine...</div>
        </div>
        
        <div class="border rounded shadow p-4">
          <h2 class="text-xl font-semibold mb-2">Payoff Chart</h2>
          <div id="payoff-chart-container">Loading visualization...</div>
        </div>
      </div>
    </div>
  `
})
export class OptionsPortfolioComponent {
}
