import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Dialog } from '@angular/cdk/dialog';
import { interval, startWith, switchMap } from 'rxjs';

import { Booking } from '../../core/models/booking.model';
import { BookingService } from '../../core/services/booking.service';
import { NotificationService } from '../../core/services/notification.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { DeclineModalComponent } from './decline-modal.component';

const POLL_MS = 30_000;

interface SpaceSchedule {
  spaceId: string;
  spaceName: string;
  bookings: Booking[];
}

@Component({
  selector: 'app-admin-dashboard',
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
        <h1>Admin Dashboard</h1>
        <p class="subtle">
          @if (lastUpdated(); as t) {
            Updated {{ t | date: 'shortTime' }} · auto-refreshing every 30s
          }
        </p>
      </header>

      <div class="cards">
        <div class="card">
          <div class="label">Pending requests</div>
          <div class="value">{{ pendingCount() }}</div>
        </div>
        <div class="card">
          <div class="label">Today's bookings</div>
          <div class="value">{{ todayCount() }}</div>
        </div>
        <div class="card">
          <div class="label">This week</div>
          <div class="value">{{ weekCount() }}</div>
        </div>
      </div>

      <div class="grid">
        <div class="panel">
          <div class="panel-header">
            <h2>Pending approvals</h2>
            <a routerLink="/admin/bookings">View all</a>
          </div>
          @if (loading()) {
            <app-loading-spinner label="Loading..." />
          } @else if (pending().length === 0) {
            <app-empty-state
              title="Nothing to review"
              message="No pending booking requests right now."
              icon="✅"
            />
          } @else {
            <ul class="pending-list">
              @for (b of pending(); track b.id) {
                <li>
                  <div class="row-main">
                    <div class="row-title">
                      {{ b.userName }} · {{ b.spaceName }}
                    </div>
                    <div class="row-meta">
                      {{ b.startTime | date: 'short' }} →
                      {{ b.endTime | date: 'shortTime' }}
                    </div>
                    @if (b.notes) {
                      <div class="row-notes">{{ b.notes }}</div>
                    }
                  </div>
                  <div class="row-actions">
                    <button
                      type="button"
                      class="btn approve"
                      [disabled]="busyId() === b.id"
                      (click)="confirmApprove(b)"
                    >
                      Approve
                    </button>
                    <button
                      type="button"
                      class="btn decline"
                      [disabled]="busyId() === b.id"
                      (click)="openDecline(b)"
                    >
                      Decline
                    </button>
                  </div>
                </li>
              }
            </ul>
          }
        </div>

        <div class="panel">
          <div class="panel-header">
            <h2>Today's schedule</h2>
          </div>
          @if (loading()) {
            <app-loading-spinner label="Loading..." />
          } @else if (todaySchedule().length === 0) {
            <app-empty-state
              title="Nothing scheduled today"
              icon="🗓️"
            />
          } @else {
            @for (s of todaySchedule(); track s.spaceId) {
              <div class="schedule">
                <h3>{{ s.spaceName }}</h3>
                @if (s.bookings.length === 0) {
                  <div class="empty-row">No bookings</div>
                } @else {
                  <ol class="timeline">
                    @for (b of s.bookings; track b.id) {
                      <li>
                        <span class="time">
                          {{ b.startTime | date: 'shortTime' }}–{{
                            b.endTime | date: 'shortTime'
                          }}
                        </span>
                        <span class="who">{{ b.userName }}</span>
                        <app-status-badge [status]="b.status" />
                      </li>
                    }
                  </ol>
                }
              </div>
            }
          }
        </div>

        <div class="panel full">
          <div class="panel-header">
            <h2>Recent activity</h2>
          </div>
          @if (loading()) {
            <app-loading-spinner label="Loading..." />
          } @else if (recent().length === 0) {
            <app-empty-state title="No activity yet" icon="📭" />
          } @else {
            <ul class="activity">
              @for (b of recent(); track b.id) {
                <li>
                  <span class="when">{{ b.updatedAt | date: 'short' }}</span>
                  <span class="what">
                    {{ b.userName }} · {{ b.spaceName }}
                  </span>
                  <app-status-badge [status]="b.status" />
                </li>
              }
            </ul>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .page {
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1.25rem;
      }
      header h1 {
        margin: 0;
      }
      .subtle {
        margin: 0.25rem 0 0;
        color: #6b7280;
        font-size: 0.875rem;
      }
      .cards {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1rem;
      }
      .card {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        padding: 1rem;
      }
      .card .label {
        font-size: 0.8125rem;
        color: #6b7280;
      }
      .card .value {
        font-size: 1.875rem;
        font-weight: 700;
        margin-top: 0.25rem;
      }
      .grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
      }
      .panel {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        padding: 1rem;
      }
      .panel.full {
        grid-column: 1 / -1;
      }
      .panel-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.75rem;
      }
      .panel-header h2 {
        margin: 0;
        font-size: 1rem;
      }
      .panel-header a {
        font-size: 0.8125rem;
        color: #2563eb;
        text-decoration: none;
      }
      .pending-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }
      .pending-list li {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.625rem 0.75rem;
        border: 1px solid #e5e7eb;
        border-radius: 6px;
      }
      .row-title {
        font-weight: 600;
      }
      .row-meta,
      .row-notes {
        font-size: 0.8125rem;
        color: #4b5563;
      }
      .row-notes {
        margin-top: 0.25rem;
      }
      .row-actions {
        display: flex;
        gap: 0.375rem;
        align-items: flex-start;
      }
      .btn {
        padding: 0.375rem 0.75rem;
        border-radius: 4px;
        border: 1px solid transparent;
        cursor: pointer;
        font: inherit;
        font-size: 0.8125rem;
      }
      .btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .approve {
        background: #16a34a;
        color: #fff;
      }
      .decline {
        background: #fff;
        border-color: #d1d5db;
        color: #374151;
      }
      .schedule + .schedule {
        margin-top: 0.75rem;
      }
      .schedule h3 {
        margin: 0 0 0.25rem;
        font-size: 0.875rem;
        color: #374151;
      }
      .timeline {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
      }
      .timeline li {
        display: grid;
        grid-template-columns: 9rem 1fr auto;
        gap: 0.5rem;
        align-items: center;
        font-size: 0.875rem;
      }
      .time {
        color: #6b7280;
        font-variant-numeric: tabular-nums;
      }
      .empty-row {
        color: #6b7280;
        font-size: 0.8125rem;
      }
      .activity {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.375rem;
      }
      .activity li {
        display: grid;
        grid-template-columns: 10rem 1fr auto;
        gap: 0.75rem;
        align-items: center;
        font-size: 0.875rem;
      }
      .when {
        color: #6b7280;
        font-variant-numeric: tabular-nums;
      }
      @media (max-width: 900px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class AdminDashboardComponent implements OnInit {
  private readonly bookingService = inject(BookingService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(Dialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly bookings = signal<Booking[]>([]);
  readonly busyId = signal<string | null>(null);
  readonly lastUpdated = signal<Date | null>(null);

  readonly pending = computed(() =>
    this.bookings()
      .filter((b) => b.status === 'Pending')
      .sort(
        (a, b) =>
          new Date(a.startTime).getTime() - new Date(b.startTime).getTime(),
      ),
  );

  readonly pendingCount = computed(() => this.pending().length);

  readonly todayCount = computed(() => {
    const { start, end } = todayRange();
    return this.bookings().filter((b) => inRange(b.startTime, start, end))
      .length;
  });

  readonly weekCount = computed(() => {
    const { start, end } = weekRange();
    return this.bookings().filter((b) => inRange(b.startTime, start, end))
      .length;
  });

  readonly todaySchedule = computed<SpaceSchedule[]>(() => {
    const { start, end } = todayRange();
    const todays = this.bookings()
      .filter(
        (b) =>
          inRange(b.startTime, start, end) &&
          b.status !== 'Cancelled' &&
          b.status !== 'Declined',
      )
      .sort(
        (a, b) =>
          new Date(a.startTime).getTime() - new Date(b.startTime).getTime(),
      );
    const grouped = new Map<string, SpaceSchedule>();
    for (const b of todays) {
      const existing = grouped.get(b.spaceId);
      if (existing) {
        existing.bookings.push(b);
      } else {
        grouped.set(b.spaceId, {
          spaceId: b.spaceId,
          spaceName: b.spaceName,
          bookings: [b],
        });
      }
    }
    return [...grouped.values()].sort((a, b) =>
      a.spaceName.localeCompare(b.spaceName),
    );
  });

  readonly recent = computed(() =>
    [...this.bookings()]
      .sort(
        (a, b) =>
          new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime(),
      )
      .slice(0, 10),
  );

  ngOnInit(): void {
    interval(POLL_MS)
      .pipe(
        startWith(0),
        switchMap(() => this.bookingService.list()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (items) => {
          this.bookings.set(items);
          this.loading.set(false);
          this.lastUpdated.set(new Date());
        },
        error: () => {
          this.loading.set(false);
          this.notifications.error('Failed to load bookings');
        },
      });
  }

  confirmApprove(b: Booking): void {
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: 'Approve booking?',
      message: `Approve ${b.userName}'s booking for ${b.spaceName}?`,
      confirmLabel: 'Approve',
    });
    ref.closed.subscribe((confirmed) => {
      if (!confirmed) return;
      this.busyId.set(b.id);
      this.bookingService
        .decide(b.spaceId, b.id, { action: 'Approve', adminNotes: null })
        .subscribe({
          next: (updated) => {
            this.replaceLocal(updated);
            this.notifications.success('Booking approved');
            this.busyId.set(null);
          },
          error: () => {
            this.busyId.set(null);
            this.notifications.error('Failed to approve booking');
          },
        });
    });
  }

  openDecline(b: Booking): void {
    const ref = DeclineModalComponent.open(this.dialog, b);
    ref.closed.subscribe((result) => {
      if (!result) return;
      this.busyId.set(b.id);
      this.bookingService
        .decide(b.spaceId, b.id, {
          action: 'Decline',
          adminNotes: result.reason,
        })
        .subscribe({
          next: (updated) => {
            this.replaceLocal(updated);
            this.notifications.success('Booking declined');
            this.busyId.set(null);
          },
          error: () => {
            this.busyId.set(null);
            this.notifications.error('Failed to decline booking');
          },
        });
    });
  }

  private replaceLocal(updated: Booking): void {
    this.bookings.update((items) =>
      items.map((b) => (b.id === updated.id ? updated : b)),
    );
  }
}

function todayRange(): { start: Date; end: Date } {
  const start = new Date();
  start.setHours(0, 0, 0, 0);
  const end = new Date(start);
  end.setDate(end.getDate() + 1);
  return { start, end };
}

function weekRange(): { start: Date; end: Date } {
  const start = new Date();
  start.setHours(0, 0, 0, 0);
  const end = new Date(start);
  end.setDate(end.getDate() + 7);
  return { start, end };
}

function inRange(value: string, start: Date, end: Date): boolean {
  const t = new Date(value).getTime();
  return t >= start.getTime() && t < end.getTime();
}
