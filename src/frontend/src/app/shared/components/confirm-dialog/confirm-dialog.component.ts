import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DIALOG_DATA, Dialog, DialogModule, DialogRef } from '@angular/cdk/dialog';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [DialogModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="confirm-dialog">
      <h2 class="title">{{ data.title }}</h2>
      <p class="message">{{ data.message }}</p>
      <div class="actions">
        <button type="button" class="btn cancel" (click)="dialogRef.close(false)">
          {{ data.cancelLabel ?? 'Cancel' }}
        </button>
        <button
          type="button"
          class="btn confirm"
          [class.destructive]="data.destructive"
          (click)="dialogRef.close(true)"
        >
          {{ data.confirmLabel ?? 'Confirm' }}
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      .confirm-dialog {
        background: #fff;
        padding: 1.25rem 1.5rem;
        border-radius: 8px;
        min-width: 320px;
        max-width: 480px;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
      }
      .title {
        margin: 0 0 0.5rem;
        font-size: 1.125rem;
      }
      .message {
        margin: 0 0 1rem;
        color: #444;
      }
      .actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.5rem;
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
      .confirm {
        background: #2563eb;
        color: #fff;
      }
      .confirm.destructive {
        background: #dc2626;
      }
    `,
  ],
})
export class ConfirmDialogComponent {
  readonly dialogRef = inject<DialogRef<boolean>>(DialogRef);
  readonly data = inject<ConfirmDialogData>(DIALOG_DATA);

  static open(dialog: Dialog, data: ConfirmDialogData) {
    return dialog.open<boolean, ConfirmDialogData, ConfirmDialogComponent>(
      ConfirmDialogComponent,
      { data },
    );
  }
}
