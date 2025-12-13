import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="settings-container">
      <h1>Settings</h1>
      <p>Configure your preferences</p>
    </div>
  `,
  styles: [`
    .settings-container {
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
export class SettingsPage {}
