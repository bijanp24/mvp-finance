import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="home-container">
      <h1>Welcome to MVP Finance</h1>
      <p>Your personal finance management dashboard</p>
    </div>
  `,
  styles: [`
    .home-container {
      padding: 24px;
    }

    h1 {
      margin: 0 0 16px 0;
      font-size: 32px;
      font-weight: 400;
    }

    p {
      margin: 0;
      font-size: 16px;
      color: rgba(255, 255, 255, 0.7);
    }
  `]
})
export class HomePage {}
