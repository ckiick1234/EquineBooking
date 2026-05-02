import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-notifications',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="stack" aria-live="polite">
      @for (n of notifications.notifications(); track n.id) {
        <div class="toast" [class]="n.kind">
          <span>{{ n.message }}</span>
          <button type="button" (click)="notifications.dismiss(n.id)" aria-label="Dismiss">
            ×
          </button>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .stack {
        position: fixed;
        top: 1rem;
        right: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        z-index: 1000;
      }
      .toast {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        min-width: 240px;
        padding: 0.5rem 0.75rem;
        border-radius: 6px;
        color: #fff;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      }
      .toast button {
        background: transparent;
        border: 0;
        color: inherit;
        font-size: 1.25rem;
        cursor: pointer;
        line-height: 1;
      }
      .success {
        background: #16a34a;
      }
      .error {
        background: #dc2626;
      }
      .info {
        background: #2563eb;
      }
    `,
  ],
})
export class NotificationsComponent {
  readonly notifications = inject(NotificationService);
}
