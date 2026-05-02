import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Dialog } from '@angular/cdk/dialog';

import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

import { Space } from '../../core/models/space.model';
import { NotificationService } from '../../core/services/notification.service';
import { SpaceService } from '../../core/services/space.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

interface SpaceFormControls {
  name: FormControl<string>;
  description: FormControl<string>;
  rules: FormControl<string>;
  isActive: FormControl<boolean>;
  hourlyStart: FormControl<string>;
  hourlyEnd: FormControl<string>;
}

@Component({
  selector: 'app-admin-space-management',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSlideToggleModule,
    LoadingSpinnerComponent,
  ],
  providers: [provideNativeDateAdapter()],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      <header>
        <h1>Manage Spaces</h1>
      </header>

      @if (loading()) {
        <app-loading-spinner label="Loading spaces..." />
      } @else {
        <div class="grid">
          @for (space of spaces(); track space.id) {
            <article class="card">
              <div class="card-head">
                <div>
                  <div class="kind">{{ space.type }} · {{ space.slotType }}</div>
                  <h2>{{ space.name }}</h2>
                </div>
                <mat-slide-toggle
                  [checked]="formFor(space.id).controls.isActive.value"
                  (change)="formFor(space.id).controls.isActive.setValue($event.checked)"
                >
                  {{ formFor(space.id).controls.isActive.value ? 'Active' : 'Inactive' }}
                </mat-slide-toggle>
              </div>

              <form [formGroup]="formFor(space.id)" class="form">
                <mat-form-field appearance="outline">
                  <mat-label>Name</mat-label>
                  <input matInput formControlName="name" />
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Description</mat-label>
                  <textarea matInput formControlName="description" rows="2"></textarea>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>House rules</mat-label>
                  <textarea matInput formControlName="rules" rows="3"></textarea>
                </mat-form-field>

                @if (space.type === 'Corral') {
                  <div class="time-row">
                    <mat-form-field appearance="outline">
                      <mat-label>Open from</mat-label>
                      <input matInput type="time" formControlName="hourlyStart" />
                    </mat-form-field>
                    <mat-form-field appearance="outline">
                      <mat-label>Close at</mat-label>
                      <input matInput type="time" formControlName="hourlyEnd" />
                    </mat-form-field>
                  </div>
                }

                @if (space.type === 'Stall') {
                  <div class="block-section">
                    <label>Blocked dates (maintenance)</label>
                    <div class="block-row">
                      <mat-form-field appearance="outline" subscriptSizing="dynamic">
                        <mat-label>Add date</mat-label>
                        <input
                          matInput
                          [matDatepicker]="picker"
                          [value]="newBlockedDate()[space.id] ?? null"
                          (dateChange)="setNewBlockedDate(space.id, $event.value)"
                        />
                        <mat-datepicker-toggle matIconSuffix [for]="picker" />
                        <mat-datepicker #picker />
                      </mat-form-field>
                      <button
                        mat-stroked-button
                        type="button"
                        (click)="addBlockedDate(space.id)"
                        [disabled]="!newBlockedDate()[space.id]"
                      >
                        Add
                      </button>
                    </div>
                    <mat-chip-set>
                      @for (d of blockedDatesFor(space.id); track d) {
                        <mat-chip (removed)="removeBlockedDate(space.id, d)">
                          {{ d | date: 'mediumDate' }}
                          <button matChipRemove>
                            <mat-icon>cancel</mat-icon>
                          </button>
                        </mat-chip>
                      } @empty {
                        <span class="empty">No dates blocked</span>
                      }
                    </mat-chip-set>
                  </div>
                }

                <div class="actions">
                  <button
                    mat-flat-button
                    color="primary"
                    type="button"
                    [disabled]="formFor(space.id).pristine && !blockedDirty()[space.id] || saving() === space.id"
                    (click)="save(space)"
                  >
                    {{ saving() === space.id ? 'Saving...' : 'Save changes' }}
                  </button>
                  <button
                    mat-button
                    type="button"
                    (click)="reset(space)"
                  >
                    Discard
                  </button>
                </div>
              </form>

              <aside class="preview">
                <h3>Client preview</h3>
                <div class="preview-card" [class.dim]="!formFor(space.id).controls.isActive.value">
                  <div class="preview-name">
                    {{ formFor(space.id).controls.name.value || 'Unnamed space' }}
                  </div>
                  <div class="preview-meta">
                    {{ space.type }} · books by {{ space.slotType }}
                  </div>
                  @if (formFor(space.id).controls.description.value) {
                    <p>{{ formFor(space.id).controls.description.value }}</p>
                  }
                  @if (space.type === 'Corral') {
                    <div class="preview-meta">
                      Hours
                      {{ formFor(space.id).controls.hourlyStart.value || '—' }}
                      to
                      {{ formFor(space.id).controls.hourlyEnd.value || '—' }}
                    </div>
                  }
                  @if (formFor(space.id).controls.rules.value) {
                    <details>
                      <summary>House rules</summary>
                      <p>{{ formFor(space.id).controls.rules.value }}</p>
                    </details>
                  }
                  @if (!formFor(space.id).controls.isActive.value) {
                    <div class="preview-tag">Not currently bookable</div>
                  }
                </div>
              </aside>
            </article>
          }
        </div>
      }
    </section>
  `,
  styles: [
    `
      .page {
        padding: 1.5rem;
      }
      header h1 {
        margin: 0 0 1rem;
      }
      .grid {
        display: grid;
        grid-template-columns: 1fr;
        gap: 1rem;
      }
      .card {
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        padding: 1.25rem;
        display: grid;
        grid-template-columns: 1fr 280px;
        grid-template-areas:
          'head head'
          'form preview';
        gap: 1rem;
      }
      .card-head {
        grid-area: head;
        display: flex;
        align-items: center;
        justify-content: space-between;
      }
      .card-head h2 {
        margin: 0.125rem 0 0;
      }
      .kind {
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #6b7280;
      }
      .form {
        grid-area: form;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }
      .time-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 0.75rem;
      }
      .block-section {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }
      .block-section label {
        font-size: 0.875rem;
        font-weight: 500;
        color: #374151;
      }
      .block-row {
        display: flex;
        gap: 0.5rem;
        align-items: center;
      }
      .empty {
        color: #6b7280;
        font-size: 0.875rem;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
        margin-top: 0.5rem;
      }
      .preview {
        grid-area: preview;
        border-left: 1px solid #e5e7eb;
        padding-left: 1rem;
      }
      .preview h3 {
        margin: 0 0 0.5rem;
        font-size: 0.8125rem;
        color: #6b7280;
        text-transform: uppercase;
        letter-spacing: 0.05em;
      }
      .preview-card {
        background: #f9fafb;
        padding: 0.75rem;
        border-radius: 6px;
      }
      .preview-card.dim {
        opacity: 0.6;
      }
      .preview-name {
        font-weight: 600;
      }
      .preview-meta {
        color: #6b7280;
        font-size: 0.8125rem;
      }
      .preview p {
        margin: 0.5rem 0;
        font-size: 0.875rem;
      }
      .preview-tag {
        margin-top: 0.5rem;
        background: #fee2e2;
        color: #991b1b;
        padding: 0.125rem 0.5rem;
        border-radius: 4px;
        font-size: 0.75rem;
        display: inline-block;
      }
      @media (max-width: 800px) {
        .card {
          grid-template-columns: 1fr;
          grid-template-areas:
            'head'
            'form'
            'preview';
        }
        .preview {
          border-left: 0;
          border-top: 1px solid #e5e7eb;
          padding-left: 0;
          padding-top: 1rem;
        }
      }
    `,
  ],
})
export class AdminSpaceManagementComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly spaceService = inject(SpaceService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(Dialog);

  readonly loading = signal(true);
  readonly spaces = signal<Space[]>([]);
  readonly saving = signal<string | null>(null);
  readonly newBlockedDate = signal<Record<string, Date | null>>({});
  readonly blockedDirty = signal<Record<string, boolean>>({});

  private readonly forms = new Map<string, FormGroup<SpaceFormControls>>();
  private readonly blocked = new Map<string, string[]>();

  ngOnInit(): void {
    this.spaceService.list().subscribe({
      next: (items) => {
        this.spaces.set(items);
        for (const s of items) {
          this.forms.set(s.id, this.buildForm(s));
          this.blocked.set(s.id, [...(s.blockedDates ?? [])]);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notifications.error('Failed to load spaces');
      },
    });
  }

  formFor(id: string): FormGroup<SpaceFormControls> {
    return this.forms.get(id)!;
  }

  blockedDatesFor(id: string): string[] {
    return this.blocked.get(id) ?? [];
  }

  setNewBlockedDate(id: string, value: Date | null): void {
    this.newBlockedDate.update((m) => ({ ...m, [id]: value }));
  }

  addBlockedDate(id: string): void {
    const d = this.newBlockedDate()[id];
    if (!d) return;
    const iso = toIsoDate(d);
    const current = this.blocked.get(id) ?? [];
    if (current.includes(iso)) return;
    this.blocked.set(id, [...current, iso].sort());
    this.markBlockedDirty(id);
    this.setNewBlockedDate(id, null);
  }

  removeBlockedDate(id: string, value: string): void {
    const current = this.blocked.get(id) ?? [];
    this.blocked.set(
      id,
      current.filter((d) => d !== value),
    );
    this.markBlockedDirty(id);
  }

  save(space: Space): void {
    const form = this.formFor(space.id);
    if (!form.valid) {
      form.markAllAsTouched();
      this.notifications.error('Fix validation errors before saving');
      return;
    }
    const ref = ConfirmDialogComponent.open(this.dialog, {
      title: `Save ${space.name}?`,
      message: 'Changes take effect immediately for new bookings.',
      confirmLabel: 'Save',
    });
    ref.closed.subscribe((ok) => {
      if (!ok) return;
      const v = form.getRawValue();
      const update: Partial<Space> = {
        name: v.name,
        description: v.description,
        rules: v.rules,
        isActive: v.isActive,
        type: space.type,
        slotType: space.slotType,
        hourlyAvailability:
          space.type === 'Corral'
            ? { start: v.hourlyStart, end: v.hourlyEnd }
            : null,
        blockedDates: space.type === 'Stall' ? this.blocked.get(space.id) ?? [] : [],
      };
      this.saving.set(space.id);
      this.spaceService.update(space.id, update).subscribe({
        next: (saved) => {
          this.spaces.update((items) =>
            items.map((s) => (s.id === saved.id ? saved : s)),
          );
          this.forms.set(saved.id, this.buildForm(saved));
          this.blocked.set(saved.id, [...(saved.blockedDates ?? [])]);
          this.clearBlockedDirty(saved.id);
          this.saving.set(null);
          this.notifications.success(`${saved.name} saved`);
        },
        error: () => {
          this.saving.set(null);
          this.notifications.error('Failed to save space');
        },
      });
    });
  }

  reset(space: Space): void {
    const form = this.formFor(space.id);
    form.reset(this.formValueFor(space));
    this.blocked.set(space.id, [...(space.blockedDates ?? [])]);
    this.clearBlockedDirty(space.id);
  }

  private buildForm(s: Space): FormGroup<SpaceFormControls> {
    const v = this.formValueFor(s);
    return this.fb.nonNullable.group({
      name: [v.name, [Validators.required, Validators.maxLength(80)]],
      description: [v.description],
      rules: [v.rules],
      isActive: [v.isActive],
      hourlyStart: [v.hourlyStart],
      hourlyEnd: [v.hourlyEnd],
    });
  }

  private formValueFor(s: Space) {
    return {
      name: s.name,
      description: s.description,
      rules: s.rules,
      isActive: s.isActive,
      hourlyStart: timeOnlyToInput(s.hourlyAvailability?.start),
      hourlyEnd: timeOnlyToInput(s.hourlyAvailability?.end),
    };
  }

  private markBlockedDirty(id: string): void {
    this.blockedDirty.update((m) => ({ ...m, [id]: true }));
  }

  private clearBlockedDirty(id: string): void {
    this.blockedDirty.update((m) => ({ ...m, [id]: false }));
  }
}

function toIsoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function timeOnlyToInput(value: string | undefined): string {
  if (!value) return '';
  const m = /^(\d{2}):(\d{2})/.exec(value);
  return m ? `${m[1]}:${m[2]}` : value;
}
