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
import { RouterLink } from '@angular/router';
import { Dialog } from '@angular/cdk/dialog';
import { SelectionModel } from '@angular/cdk/collections';

import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';

import { Booking, BookingStatus } from '../../core/models/booking.model';
import { Space } from '../../core/models/space.model';
import { BookingService } from '../../core/services/booking.service';
import { NotificationService } from '../../core/services/notification.service';
import { SpaceService } from '../../core/services/space.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { DeclineModalComponent } from './decline-modal.component';

const STATUS_OPTIONS: BookingStatus[] = [
  'Pending',
  'Approved',
  'Declined',
  'Cancelled',
  'ModificationRequested',
];

@Component({
  selector: 'app-admin-booking-list',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
    StatusBadgeComponent,
  ],
  providers: [provideNativeDateAdapter()],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      <header>
        <h1>All Bookings</h1>
      </header>

      <div class="toolbar">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Search name or email</mat-label>
          <input matInput [(ngModel)]="search" (ngModelChange)="applyFilter()" />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Status</mat-label>
          <mat-select
            [(value)]="statusFilter"
            (selectionChange)="applyFilter()"
            multiple
          >
            @for (s of statusOptions; track s) {
              <mat-option [value]="s">{{ s }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Space</mat-label>
          <mat-select
            [(value)]="spaceFilter"
            (selectionChange)="applyFilter()"
            multiple
          >
            @for (s of spaces(); track s.id) {
              <mat-option [value]="s.id">{{ s.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>From</mat-label>
          <input
            matInput
            [matDatepicker]="fromPicker"
            [(ngModel)]="fromDate"
            (ngModelChange)="applyFilter()"
          />
          <mat-datepicker-toggle matIconSuffix [for]="fromPicker" />
          <mat-datepicker #fromPicker />
        </mat-form-field>

        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>To</mat-label>
          <input
            matInput
            [matDatepicker]="toPicker"
            [(ngModel)]="toDate"
            (ngModelChange)="applyFilter()"
          />
          <mat-datepicker-toggle matIconSuffix [for]="toPicker" />
          <mat-datepicker #toPicker />
        </mat-form-field>

        <button mat-stroked-button type="button" (click)="resetFilters()">
          Reset
        </button>
      </div>

      @if (selection.hasValue()) {
        <div class="bulk-bar">
          <span>{{ selection.selected.length }} selected</span>
          <button mat-flat-button color="primary" (click)="bulkApprove()">
            Approve selected
          </button>
          <button mat-flat-button color="warn" (click)="bulkDecline()">
            Decline selected
          </button>
          <button mat-button (click)="selection.clear()">Clear</button>
        </div>
      }

      <div class="table-wrap">
        <table mat-table [dataSource]="dataSource" matSort matSortActive="startTime" matSortDirection="asc">
          <ng-container matColumnDef="select">
            <th mat-header-cell *matHeaderCellDef>
              <mat-checkbox
                [checked]="allSelected()"
                [indeterminate]="someSelected()"
                (change)="toggleAll($event.checked)"
              />
            </th>
            <td mat-cell *matCellDef="let row">
              <mat-checkbox
                [checked]="selection.isSelected(row)"
                (click)="$event.stopPropagation()"
                (change)="selection.toggle(row)"
              />
            </td>
          </ng-container>

          <ng-container matColumnDef="startTime">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Date</th>
            <td mat-cell *matCellDef="let row">
              {{ row.startTime | date: 'short' }}
            </td>
          </ng-container>

          <ng-container matColumnDef="spaceName">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Space</th>
            <td mat-cell *matCellDef="let row">{{ row.spaceName }}</td>
          </ng-container>

          <ng-container matColumnDef="userName">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Client</th>
            <td mat-cell *matCellDef="let row">
              <div>{{ row.userName }}</div>
              <div class="muted">{{ row.userEmail }}</div>
            </td>
          </ng-container>

          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Status</th>
            <td mat-cell *matCellDef="let row">
              <app-status-badge [status]="row.status" />
            </td>
          </ng-container>

          <ng-container matColumnDef="createdAt">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Created</th>
            <td mat-cell *matCellDef="let row">
              {{ row.createdAt | date: 'short' }}
            </td>
          </ng-container>

          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let row">
              <button
                mat-icon-button
                [matMenuTriggerFor]="menu"
                aria-label="Actions"
              >
                <mat-icon>more_vert</mat-icon>
              </button>
              <mat-menu #menu="matMenu">
                @if (row.status === 'Pending' || row.status === 'ModificationRequested') {
                  <button mat-menu-item (click)="confirmApprove(row)">
                    <mat-icon>check</mat-icon>
                    <span>Approve</span>
                  </button>
                  <button mat-menu-item (click)="openDecline(row)">
                    <mat-icon>close</mat-icon>
                    <span>Decline</span>
                  </button>
                }
                <a
                  mat-menu-item
                  [routerLink]="['/bookings', row.spaceId, row.id]"
                >
                  <mat-icon>open_in_new</mat-icon>
                  <span>View detail</span>
                </a>
              </mat-menu>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>

          <tr class="empty-row" *matNoDataRow>
            <td [attr.colspan]="displayedColumns.length">
              No bookings match the current filters.
            </td>
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
      .toolbar {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: center;
      }
      .toolbar mat-form-field {
        min-width: 180px;
      }
      .bulk-bar {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.5rem 0.75rem;
        background: #eff6ff;
        border: 1px solid #bfdbfe;
        border-radius: 6px;
      }
      .bulk-bar span {
        font-weight: 600;
        color: #1e3a8a;
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
      .muted {
        color: #6b7280;
        font-size: 0.8125rem;
      }
      .empty-row td {
        padding: 1.5rem;
        text-align: center;
        color: #6b7280;
      }
    `,
  ],
})
export class AdminBookingListComponent implements OnInit, AfterViewInit {
  private readonly bookingService = inject(BookingService);
  private readonly spaceService = inject(SpaceService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(Dialog);

  readonly statusOptions = STATUS_OPTIONS;
  readonly spaces = signal<Space[]>([]);
  readonly dataSource = new MatTableDataSource<Booking>([]);
  readonly selection = new SelectionModel<Booking>(true, []);
  readonly displayedColumns = [
    'select',
    'startTime',
    'spaceName',
    'userName',
    'status',
    'createdAt',
    'actions',
  ];

  search = '';
  statusFilter: BookingStatus[] = [];
  spaceFilter: string[] = [];
  fromDate: Date | null = null;
  toDate: Date | null = null;

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  ngOnInit(): void {
    this.dataSource.filterPredicate = (booking, raw) => {
      const f = JSON.parse(raw) as {
        search: string;
        status: BookingStatus[];
        space: string[];
        from: number | null;
        to: number | null;
      };
      if (f.search) {
        const q = f.search.toLowerCase();
        if (
          !booking.userName.toLowerCase().includes(q) &&
          !booking.userEmail.toLowerCase().includes(q)
        ) {
          return false;
        }
      }
      if (f.status.length && !f.status.includes(booking.status)) return false;
      if (f.space.length && !f.space.includes(booking.spaceId)) return false;
      const t = new Date(booking.startTime).getTime();
      if (f.from !== null && t < f.from) return false;
      if (f.to !== null && t >= f.to) return false;
      return true;
    };

    this.dataSource.sortingDataAccessor = (b, prop) => {
      switch (prop) {
        case 'startTime':
          return new Date(b.startTime).getTime();
        case 'createdAt':
          return new Date(b.createdAt).getTime();
        case 'spaceName':
          return b.spaceName.toLowerCase();
        case 'userName':
          return b.userName.toLowerCase();
        case 'status':
          return b.status;
        default:
          return '';
      }
    };

    this.spaceService.list().subscribe({
      next: (s) => this.spaces.set(s),
      error: () => this.notifications.error('Failed to load spaces'),
    });

    this.reload();
  }

  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  reload(): void {
    this.bookingService.list().subscribe({
      next: (items) => {
        this.dataSource.data = items;
        this.applyFilter();
      },
      error: () => this.notifications.error('Failed to load bookings'),
    });
  }

  applyFilter(): void {
    const filter = {
      search: this.search.trim(),
      status: this.statusFilter,
      space: this.spaceFilter,
      from: this.fromDate ? startOfDay(this.fromDate).getTime() : null,
      to: this.toDate ? startOfDay(addDays(this.toDate, 1)).getTime() : null,
    };
    this.dataSource.filter = JSON.stringify(filter);
    this.selection.clear();
  }

  resetFilters(): void {
    this.search = '';
    this.statusFilter = [];
    this.spaceFilter = [];
    this.fromDate = null;
    this.toDate = null;
    this.applyFilter();
  }

  allSelected(): boolean {
    const visible = this.dataSource.filteredData;
    return visible.length > 0 && visible.every((b) => this.selection.isSelected(b));
  }

  someSelected(): boolean {
    const visible = this.dataSource.filteredData;
    const sel = visible.filter((b) => this.selection.isSelected(b)).length;
    return sel > 0 && sel < visible.length;
  }

  toggleAll(checked: boolean): void {
    if (checked) {
      this.selection.select(...this.dataSource.filteredData);
    } else {
      this.selection.clear();
    }
  }

  confirmApprove(b: Booking): void {
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: 'Approve booking?',
      message: `Approve ${b.userName}'s booking for ${b.spaceName}?`,
      confirmLabel: 'Approve',
    });
    ref.closed.subscribe((ok) => {
      if (!ok) return;
      this.bookingService
        .decide(b.spaceId, b.id, { action: 'Approve', adminNotes: null })
        .subscribe({
          next: (updated) => {
            this.replaceLocal(updated);
            this.notifications.success('Booking approved');
          },
          error: () => this.notifications.error('Failed to approve'),
        });
    });
  }

  openDecline(b: Booking): void {
    const ref = DeclineModalComponent.open(this.dialog, b);
    ref.closed.subscribe((res) => {
      if (!res) return;
      this.bookingService
        .decide(b.spaceId, b.id, {
          action: 'Decline',
          adminNotes: res.reason,
        })
        .subscribe({
          next: (updated) => {
            this.replaceLocal(updated);
            this.notifications.success('Booking declined');
          },
          error: () => this.notifications.error('Failed to decline'),
        });
    });
  }

  bulkApprove(): void {
    const items = this.selection.selected.filter(
      (b) => b.status === 'Pending' || b.status === 'ModificationRequested',
    );
    if (items.length === 0) {
      this.notifications.info('No pending bookings in selection');
      return;
    }
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: `Approve ${items.length} bookings?`,
      message: 'This will approve all pending bookings in your selection.',
      confirmLabel: 'Approve all',
    });
    ref.closed.subscribe((ok) => {
      if (!ok) return;
      let done = 0;
      let failed = 0;
      items.forEach((b) =>
        this.bookingService
          .decide(b.spaceId, b.id, { action: 'Approve', adminNotes: null })
          .subscribe({
            next: (updated) => {
              this.replaceLocal(updated);
              done++;
              if (done + failed === items.length) this.reportBulk(done, failed);
            },
            error: () => {
              failed++;
              if (done + failed === items.length) this.reportBulk(done, failed);
            },
          }),
      );
    });
  }

  bulkDecline(): void {
    const items = this.selection.selected.filter(
      (b) => b.status === 'Pending' || b.status === 'ModificationRequested',
    );
    if (items.length === 0) {
      this.notifications.info('No pending bookings in selection');
      return;
    }
    const ref = DeclineModalComponent.open(this.dialog, items[0]);
    ref.closed.subscribe((res) => {
      if (!res) return;
      let done = 0;
      let failed = 0;
      items.forEach((b) =>
        this.bookingService
          .decide(b.spaceId, b.id, {
            action: 'Decline',
            adminNotes: res.reason,
          })
          .subscribe({
            next: (updated) => {
              this.replaceLocal(updated);
              done++;
              if (done + failed === items.length) this.reportBulk(done, failed);
            },
            error: () => {
              failed++;
              if (done + failed === items.length) this.reportBulk(done, failed);
            },
          }),
      );
    });
  }

  private replaceLocal(updated: Booking): void {
    this.dataSource.data = this.dataSource.data.map((b) =>
      b.id === updated.id ? updated : b,
    );
    this.selection.deselect(
      ...this.selection.selected.filter((b) => b.id === updated.id),
    );
  }

  private reportBulk(done: number, failed: number): void {
    if (failed === 0) {
      this.notifications.success(`Updated ${done} bookings`);
    } else {
      this.notifications.error(
        `Updated ${done}, ${failed} failed`,
      );
    }
    this.selection.clear();
  }
}

function startOfDay(d: Date): Date {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function addDays(d: Date, n: number): Date {
  const x = new Date(d);
  x.setDate(x.getDate() + n);
  return x;
}
