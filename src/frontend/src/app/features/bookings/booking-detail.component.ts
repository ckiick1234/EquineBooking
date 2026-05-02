import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Dialog } from '@angular/cdk/dialog';

import { Booking } from '../../core/models/booking.model';
import { BookingService } from '../../core/services/booking.service';
import { NotificationService } from '../../core/services/notification.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-booking-detail',
  imports: [DatePipe, LoadingSpinnerComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      @if (loading()) {
        <app-loading-spinner label="Loading booking..." />
      } @else {
        @if (booking(); as b) {
          <header>
            <h1>{{ b.spaceName }}</h1>
            <app-status-badge [status]="b.status" />
          </header>
          <dl class="meta">
            <dt>When</dt>
            <dd>
              {{ b.startTime | date: 'medium' }} →
              {{ b.endTime | date: 'medium' }}
            </dd>
            <dt>Booked by</dt>
            <dd>{{ b.userName }} ({{ b.userEmail }})</dd>
            @if (b.notes) {
              <dt>Notes</dt>
              <dd>{{ b.notes }}</dd>
            }
            @if (b.adminNotes) {
              <dt>Admin notes</dt>
              <dd>{{ b.adminNotes }}</dd>
            }
          </dl>
          <div class="actions">
            <button type="button" class="danger" (click)="confirmCancel(b)">
              Cancel booking
            </button>
          </div>
        } @else {
          <p>Booking not found.</p>
        }
      }
    </section>
  `,
  styles: [
    `
      .page {
        padding: 1.5rem;
        max-width: 720px;
      }
      header {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1rem;
      }
      .meta {
        display: grid;
        grid-template-columns: 8rem 1fr;
        row-gap: 0.5rem;
        column-gap: 1rem;
      }
      dt {
        font-weight: 600;
        color: #374151;
      }
      dd {
        margin: 0;
      }
      .actions {
        margin-top: 1.5rem;
      }
      .danger {
        background: #dc2626;
        color: #fff;
        border: 0;
        padding: 0.5rem 1rem;
        border-radius: 4px;
        cursor: pointer;
      }
    `,
  ],
})
export class BookingDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly bookingService = inject(BookingService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(Dialog);

  readonly loading = signal(true);
  readonly booking = signal<Booking | null>(null);

  ngOnInit(): void {
    const params = this.route.snapshot.paramMap;
    const id = params.get('id');
    const spaceId = params.get('spaceId');
    if (!id || !spaceId) {
      this.loading.set(false);
      this.notifications.error('Missing booking identifiers');
      return;
    }
    this.bookingService.get(spaceId, id).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.loading.set(false);
      },
      error: () => {
        this.notifications.error('Failed to load booking');
        this.loading.set(false);
      },
    });
  }

  confirmCancel(b: Booking): void {
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: 'Cancel booking?',
      message: 'This will release the slot. The action cannot be undone.',
      confirmLabel: 'Cancel booking',
      destructive: true,
    });
    ref.closed.subscribe((confirmed) => {
      if (!confirmed) return;
      this.bookingService.delete(b.spaceId, b.id).subscribe({
        next: () => {
          this.notifications.success('Booking cancelled');
          this.router.navigate(['/bookings']);
        },
        error: () => this.notifications.error('Failed to cancel booking'),
      });
    });
  }
}
