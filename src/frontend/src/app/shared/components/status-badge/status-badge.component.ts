import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { BookingStatus } from '../../../core/models/booking.model';

interface BadgeStyle {
  bg: string;
  fg: string;
  label: string;
}

const STYLES: Record<BookingStatus, BadgeStyle> = {
  Pending: { bg: '#fef3c7', fg: '#92400e', label: 'Pending' },
  Approved: { bg: '#d1fae5', fg: '#065f46', label: 'Approved' },
  Declined: { bg: '#fee2e2', fg: '#991b1b', label: 'Declined' },
  Cancelled: { bg: '#e5e7eb', fg: '#374151', label: 'Cancelled' },
  ModificationRequested: {
    bg: '#dbeafe',
    fg: '#1e40af',
    label: 'Change Requested',
  },
};

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="badge"
      [style.background]="style().bg"
      [style.color]="style().fg"
    >
      {{ style().label }}
    </span>
  `,
  styles: [
    `
      .badge {
        display: inline-block;
        padding: 0.125rem 0.5rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 600;
        line-height: 1.4;
      }
    `,
  ],
})
export class StatusBadgeComponent {
  readonly status = input.required<BookingStatus>();
  readonly style = computed(() => STYLES[this.status()]);
}
