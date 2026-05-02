import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Dialog } from '@angular/cdk/dialog';
import { forkJoin } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { Booking } from '../../core/models/booking.model';
import { UserProfile } from '../../core/models/user.model';
import { BookingService } from '../../core/services/booking.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserService } from '../../core/services/user.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-admin-user-detail',
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    LoadingSpinnerComponent,
    StatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      <a routerLink="/admin/users" class="back">
        <mat-icon>arrow_back</mat-icon>
        All users
      </a>

      @if (loading()) {
        <app-loading-spinner label="Loading user..." />
      } @else {
        @if (user(); as u) {
        <header class="head">
          <div>
            <h1>{{ u.displayName }}</h1>
            <p class="meta">
              {{ u.email }}
              @if (u.phone) {
                · {{ u.phone }}
              }
            </p>
            <p class="meta">
              Registered {{ u.createdAt | date: 'mediumDate' }} · Role
              {{ u.role }}
            </p>
          </div>
          <div class="head-actions">
            <span class="status" [class.inactive]="!u.isActive">
              {{ u.isActive ? 'Active' : 'Inactive' }}
            </span>
            @if (u.isActive) {
              <button mat-stroked-button color="warn" (click)="confirmDeactivate(u)">
                Deactivate
              </button>
            } @else {
              <button mat-stroked-button color="primary" (click)="confirmReactivate(u)">
                Reactivate
              </button>
            }
          </div>
        </header>

        <div class="cards">
          <div class="card">
            <div class="label">Total bookings</div>
            <div class="value">{{ bookings().length }}</div>
          </div>
          <div class="card">
            <div class="label">Approved</div>
            <div class="value">{{ counts().approved }}</div>
          </div>
          <div class="card">
            <div class="label">Pending</div>
            <div class="value">{{ counts().pending }}</div>
          </div>
          <div class="card">
            <div class="label">Cancelled / declined</div>
            <div class="value">{{ counts().cancelled }}</div>
          </div>
        </div>

        <div class="panel">
          <h2>Booking history</h2>
          @if (bookings().length === 0) {
            <p class="muted">No bookings yet.</p>
          } @else {
            <ul class="list">
              @for (b of sortedBookings(); track b.id) {
                <li>
                  <a [routerLink]="['/bookings', b.spaceId, b.id]" class="row">
                    <span class="when">{{ b.startTime | date: 'short' }}</span>
                    <span class="space">{{ b.spaceName }}</span>
                    <app-status-badge [status]="b.status" />
                  </a>
                </li>
              }
            </ul>
          }
        </div>
        } @else {
          <p>User not found.</p>
        }
      }
    </section>
  `,
  styles: [
    `
      .page {
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
        max-width: 960px;
      }
      .back {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        color: #2563eb;
        text-decoration: none;
        font-size: 0.875rem;
      }
      .head {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 1rem;
      }
      .head h1 {
        margin: 0;
      }
      .meta {
        margin: 0.25rem 0 0;
        color: #6b7280;
        font-size: 0.875rem;
      }
      .head-actions {
        display: flex;
        align-items: center;
        gap: 0.75rem;
      }
      .status {
        padding: 0.25rem 0.625rem;
        border-radius: 999px;
        background: #d1fae5;
        color: #065f46;
        font-size: 0.8125rem;
        font-weight: 600;
      }
      .status.inactive {
        background: #fee2e2;
        color: #991b1b;
      }
      .cards {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 0.75rem;
      }
      .card {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        padding: 0.75rem 1rem;
      }
      .card .label {
        font-size: 0.8125rem;
        color: #6b7280;
      }
      .card .value {
        font-size: 1.5rem;
        font-weight: 700;
      }
      .panel {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        padding: 1rem;
      }
      .panel h2 {
        margin: 0 0 0.75rem;
        font-size: 1rem;
      }
      .muted {
        color: #6b7280;
      }
      .list {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: 0.375rem;
      }
      .row {
        display: grid;
        grid-template-columns: 12rem 1fr auto;
        gap: 0.75rem;
        align-items: center;
        padding: 0.5rem 0.75rem;
        border: 1px solid #e5e7eb;
        border-radius: 6px;
        text-decoration: none;
        color: inherit;
      }
      .row:hover {
        background: #f9fafb;
      }
      .when {
        color: #6b7280;
        font-variant-numeric: tabular-nums;
      }
      .space {
        font-weight: 600;
      }
    `,
  ],
})
export class AdminUserDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(UserService);
  private readonly bookingService = inject(BookingService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(Dialog);

  readonly loading = signal(true);
  readonly user = signal<UserProfile | null>(null);
  readonly bookings = signal<Booking[]>([]);

  readonly sortedBookings = computed(() =>
    [...this.bookings()].sort(
      (a, b) =>
        new Date(b.startTime).getTime() - new Date(a.startTime).getTime(),
    ),
  );

  readonly counts = computed(() => {
    const all = this.bookings();
    return {
      approved: all.filter((b) => b.status === 'Approved').length,
      pending: all.filter(
        (b) => b.status === 'Pending' || b.status === 'ModificationRequested',
      ).length,
      cancelled: all.filter(
        (b) => b.status === 'Cancelled' || b.status === 'Declined',
      ).length,
    };
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }
    forkJoin({
      users: this.userService.list(),
      bookings: this.bookingService.list(),
    }).subscribe({
      next: ({ users, bookings }) => {
        const u = users.find((x) => x.id === id) ?? null;
        this.user.set(u);
        this.bookings.set(bookings.filter((b) => b.userId === id));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notifications.error('Failed to load user');
      },
    });
  }

  confirmDeactivate(u: UserProfile): void {
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: `Deactivate ${u.displayName}?`,
      message: 'They will no longer be able to sign in or place bookings.',
      confirmLabel: 'Deactivate',
      destructive: true,
    });
    ref.closed.subscribe((ok) => {
      if (!ok) return;
      this.userService.setActive(u.id, false).subscribe({
        next: (updated) => {
          this.user.set(updated);
          this.notifications.success('User deactivated');
        },
        error: () => this.notifications.error('Failed to deactivate user'),
      });
    });
  }

  confirmReactivate(u: UserProfile): void {
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: `Reactivate ${u.displayName}?`,
      message: 'They will be able to sign in and book again.',
      confirmLabel: 'Reactivate',
    });
    ref.closed.subscribe((ok) => {
      if (!ok) return;
      this.userService.setActive(u.id, true).subscribe({
        next: (updated) => {
          this.user.set(updated);
          this.notifications.success('User reactivated');
        },
        error: () => this.notifications.error('Failed to reactivate user'),
      });
    });
  }
}
