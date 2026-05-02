import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Dialog, DialogRef, DIALOG_DATA, DialogModule } from '@angular/cdk/dialog';

import { Booking } from '../../core/models/booking.model';

export interface DeclineModalData {
  booking: Booking;
}

export type DeclineModalResult = { reason: string } | null;

@Component({
  selector: 'app-decline-modal',
  imports: [FormsModule, DialogModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="modal">
      <h2>Decline booking</h2>
      <p class="subtitle">
        {{ data.booking.userName }} · {{ data.booking.spaceName }}
      </p>
      <label for="reason">Reason for decline (sent to client)</label>
      <textarea
        id="reason"
        rows="4"
        [(ngModel)]="reason"
        (ngModelChange)="onChange($event)"
        placeholder="e.g. Space unavailable due to maintenance on this date."
      ></textarea>
      @if (showError()) {
        <div class="error">A reason is required.</div>
      }
      <div class="actions">
        <button type="button" class="btn cancel" (click)="dialogRef.close(null)">
          Cancel
        </button>
        <button type="button" class="btn destructive" (click)="confirm()">
          Decline booking
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      .modal {
        background: #fff;
        padding: 1.25rem 1.5rem;
        border-radius: 8px;
        min-width: 420px;
        max-width: 520px;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
      }
      h2 {
        margin: 0 0 0.25rem;
        font-size: 1.125rem;
      }
      .subtitle {
        margin: 0 0 1rem;
        color: #6b7280;
        font-size: 0.875rem;
      }
      label {
        display: block;
        font-size: 0.875rem;
        font-weight: 600;
        margin-bottom: 0.25rem;
      }
      textarea {
        width: 100%;
        padding: 0.5rem;
        border: 1px solid #d1d5db;
        border-radius: 4px;
        font: inherit;
        resize: vertical;
        box-sizing: border-box;
      }
      .error {
        color: #dc2626;
        font-size: 0.8125rem;
        margin-top: 0.25rem;
      }
      .actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.5rem;
        margin-top: 1rem;
      }
      .btn {
        padding: 0.5rem 1rem;
        border-radius: 4px;
        border: 1px solid transparent;
        cursor: pointer;
        font: inherit;
      }
      .cancel {
        background: transparent;
        border-color: #ccc;
      }
      .destructive {
        background: #dc2626;
        color: #fff;
      }
    `,
  ],
})
export class DeclineModalComponent {
  readonly dialogRef = inject<DialogRef<DeclineModalResult>>(DialogRef);
  readonly data = inject<DeclineModalData>(DIALOG_DATA);

  reason = '';
  readonly showError = signal(false);

  onChange(value: string): void {
    if (value.trim().length > 0) {
      this.showError.set(false);
    }
  }

  confirm(): void {
    const trimmed = this.reason.trim();
    if (trimmed.length === 0) {
      this.showError.set(true);
      return;
    }
    this.dialogRef.close({ reason: trimmed });
  }

  static open(dialog: Dialog, booking: Booking) {
    return dialog.open<DeclineModalResult, DeclineModalData, DeclineModalComponent>(
      DeclineModalComponent,
      { data: { booking } },
    );
  }
}
