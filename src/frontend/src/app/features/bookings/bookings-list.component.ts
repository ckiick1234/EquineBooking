import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

import { getAccountRole } from '../../core/auth/auth.guard';
import { Booking } from '../../core/models/booking.model';
import { BookingService } from '../../core/services/booking.service';
import { NotificationService } from '../../core/services/notification.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-bookings-list',
  imports: [
    DatePipe,
    RouterLink,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      <header>
        <h1>{{ isAdmin() ? 'All bookings' : 'My bookings' }}</h1>
      </header>

      @if (loading()) {
        <app-loading-spinner label="Loading bookings..." />
      } @else if (bookings().length === 0) {
        <app-empty-state
          title="No bookings yet"
          message="Bookings will show up here once created."
          icon="📋"
        />
      } @else {
        <ul class="list">
          @for (b of bookings(); track b.id) {
            <li>
              <a [routerLink]="['/bookings', b.spaceId, b.id]" class="row">
                <span class="space">{{ b.spaceName }}</span>
                <span class="when">
                  {{ b.startTime | date: 'short' }} →
                  {{ b.endTime | date: 'short' }}
                </span>
                @if (isAdmin()) {
                  <span class="user">{{ b.userName }}</span>
                }
                <app-status-badge [status]="b.status" />
              </a>
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [
    `
      .page {
        padding: 1.5rem;
      }
      .list {
        list-style: none;
        padding: 0;
        margin: 1rem 0 0;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }
      .row {
        display: grid;
        grid-template-columns: 1fr 2fr 1fr auto;
        align-items: center;
        gap: 1rem;
        padding: 0.75rem 1rem;
        border: 1px solid #e5e7eb;
        border-radius: 6px;
        text-decoration: none;
        color: inherit;
        background: #fff;
      }
      .row:hover {
        background: #f9fafb;
      }
      .space {
        font-weight: 600;
      }
      .when {
        color: #4b5563;
      }
    `,
  ],
})
export class BookingsListComponent implements OnInit {
  private readonly bookingService = inject(BookingService);
  private readonly notifications = inject(NotificationService);
  private readonly msal = inject(MsalService);

  readonly loading = signal(true);
  readonly bookings = signal<Booking[]>([]);
  readonly isAdmin = computed(
    () => getAccountRole(this.msal.instance.getActiveAccount()) === 'Admin',
  );

  ngOnInit(): void {
    this.bookingService.list().subscribe({
      next: (items) => {
        this.bookings.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.notifications.error('Failed to load bookings');
        this.loading.set(false);
      },
    });
  }
}
