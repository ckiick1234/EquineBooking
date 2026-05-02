import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Dialog } from '@angular/cdk/dialog';
import { forkJoin } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';

import { Booking } from '../../core/models/booking.model';
import { UserProfile } from '../../core/models/user.model';
import { BookingService } from '../../core/services/booking.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserService } from '../../core/services/user.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

interface UserRow extends UserProfile {
  bookingCount: number;
}

@Component({
  selector: 'app-admin-user-list',
  imports: [
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatSortModule,
    MatTableModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      <header>
        <h1>Users</h1>
      </header>

      <div class="toolbar">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Search name or email</mat-label>
          <input matInput [(ngModel)]="search" (ngModelChange)="applyFilter()" />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>
      </div>

      <div class="table-wrap">
        <table mat-table [dataSource]="dataSource" matSort matSortActive="displayName" matSortDirection="asc">
          <ng-container matColumnDef="displayName">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
            <td mat-cell *matCellDef="let row">{{ row.displayName }}</td>
          </ng-container>

          <ng-container matColumnDef="email">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Email</th>
            <td mat-cell *matCellDef="let row">{{ row.email }}</td>
          </ng-container>

          <ng-container matColumnDef="phone">
            <th mat-header-cell *matHeaderCellDef>Phone</th>
            <td mat-cell *matCellDef="let row">{{ row.phone || '—' }}</td>
          </ng-container>

          <ng-container matColumnDef="createdAt">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Registered</th>
            <td mat-cell *matCellDef="let row">
              {{ row.createdAt | date: 'mediumDate' }}
            </td>
          </ng-container>

          <ng-container matColumnDef="bookingCount">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Bookings</th>
            <td mat-cell *matCellDef="let row">{{ row.bookingCount }}</td>
          </ng-container>

          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
            <td mat-cell *matCellDef="let row">
              <span class="status" [class.inactive]="!row.isActive">
                {{ row.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let row">
              <button mat-icon-button [matMenuTriggerFor]="menu" (click)="$event.stopPropagation()">
                <mat-icon>more_vert</mat-icon>
              </button>
              <mat-menu #menu="matMenu">
                <button mat-menu-item (click)="open(row)">
                  <mat-icon>open_in_new</mat-icon>
                  <span>View detail</span>
                </button>
                @if (row.isActive) {
                  <button mat-menu-item (click)="confirmDeactivate(row)">
                    <mat-icon>block</mat-icon>
                    <span>Deactivate</span>
                  </button>
                } @else {
                  <button mat-menu-item (click)="confirmReactivate(row)">
                    <mat-icon>check_circle</mat-icon>
                    <span>Reactivate</span>
                  </button>
                }
              </mat-menu>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr
            mat-row
            *matRowDef="let row; columns: displayedColumns"
            class="row"
            (click)="open(row)"
          ></tr>

          <tr class="empty-row" *matNoDataRow>
            <td [attr.colspan]="displayedColumns.length">No users found.</td>
          </tr>
        </table>
        <mat-paginator
          [pageSizeOptions]="[10, 25, 50]"
          [pageSize]="25"
          showFirstLastButtons
        />
      </div>
    </section>
  `,
  styles: [
    `
      .page {
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      header h1 {
        margin: 0;
      }
      .toolbar mat-form-field {
        min-width: 280px;
      }
      .table-wrap {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        overflow: hidden;
      }
      table {
        width: 100%;
      }
      .row {
        cursor: pointer;
      }
      .row:hover {
        background: #f9fafb;
      }
      .status {
        display: inline-block;
        padding: 0.125rem 0.5rem;
        border-radius: 999px;
        background: #d1fae5;
        color: #065f46;
        font-size: 0.75rem;
        font-weight: 600;
      }
      .status.inactive {
        background: #fee2e2;
        color: #991b1b;
      }
      .empty-row td {
        padding: 1.5rem;
        text-align: center;
        color: #6b7280;
      }
    `,
  ],
})
export class AdminUserListComponent implements OnInit, AfterViewInit {
  private readonly userService = inject(UserService);
  private readonly bookingService = inject(BookingService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(Dialog);
  private readonly router = inject(Router);

  readonly dataSource = new MatTableDataSource<UserRow>([]);
  readonly displayedColumns = [
    'displayName',
    'email',
    'phone',
    'createdAt',
    'bookingCount',
    'status',
    'actions',
  ];
  search = '';
  readonly loading = signal(true);

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  ngOnInit(): void {
    this.dataSource.filterPredicate = (row, raw) => {
      const q = raw.toLowerCase();
      if (!q) return true;
      return (
        row.displayName.toLowerCase().includes(q) ||
        row.email.toLowerCase().includes(q)
      );
    };
    this.dataSource.sortingDataAccessor = (row, prop) => {
      switch (prop) {
        case 'displayName':
          return row.displayName.toLowerCase();
        case 'email':
          return row.email.toLowerCase();
        case 'createdAt':
          return new Date(row.createdAt).getTime();
        case 'bookingCount':
          return row.bookingCount;
        case 'status':
          return row.isActive ? 1 : 0;
        default:
          return '';
      }
    };

    forkJoin({
      users: this.userService.list(),
      bookings: this.bookingService.list(),
    }).subscribe({
      next: ({ users, bookings }) => {
        const counts = countByUser(bookings);
        this.dataSource.data = users.map((u) => ({
          ...u,
          bookingCount: counts.get(u.id) ?? 0,
        }));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notifications.error('Failed to load users');
      },
    });
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  applyFilter(): void {
    this.dataSource.filter = this.search.trim();
  }

  open(row: UserProfile): void {
    this.router.navigate(['/admin/users', row.id]);
  }

  confirmDeactivate(row: UserRow): void {
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: `Deactivate ${row.displayName}?`,
      message:
        'They will no longer be able to sign in or place bookings. Existing bookings are not affected.',
      confirmLabel: 'Deactivate',
      destructive: true,
    });
    ref.closed.subscribe((ok) => {
      if (!ok) return;
      this.userService.setActive(row.id, false).subscribe({
        next: (updated) => {
          this.replace(updated);
          this.notifications.success('User deactivated');
        },
        error: () => this.notifications.error('Failed to deactivate user'),
      });
    });
  }

  confirmReactivate(row: UserRow): void {
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: `Reactivate ${row.displayName}?`,
      message: 'They will be able to sign in and book spaces again.',
      confirmLabel: 'Reactivate',
    });
    ref.closed.subscribe((ok) => {
      if (!ok) return;
      this.userService.setActive(row.id, true).subscribe({
        next: (updated) => {
          this.replace(updated);
          this.notifications.success('User reactivated');
        },
        error: () => this.notifications.error('Failed to reactivate user'),
      });
    });
  }

  private replace(updated: UserProfile): void {
    this.dataSource.data = this.dataSource.data.map((r) =>
      r.id === updated.id ? { ...r, ...updated } : r,
    );
  }
}

function countByUser(bookings: Booking[]): Map<string, number> {
  const m = new Map<string, number>();
  for (const b of bookings) {
    m.set(b.userId, (m.get(b.userId) ?? 0) + 1);
  }
  return m;
}
