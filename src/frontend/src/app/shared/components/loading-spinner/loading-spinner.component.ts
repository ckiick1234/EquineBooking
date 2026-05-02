import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="spinner"
      role="status"
      [attr.aria-label]="label()"
      [style.--spinner-size.px]="size()"
    >
      <div class="ring"></div>
      @if (label()) {
        <span class="label">{{ label() }}</span>
      }
    </div>
  `,
  styles: [
    `
      .spinner {
        --spinner-size: 32px;
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
      }
      .ring {
        width: var(--spinner-size);
        height: var(--spinner-size);
        border: 3px solid rgba(0, 0, 0, 0.1);
        border-top-color: currentColor;
        border-radius: 50%;
        animation: spin 0.9s linear infinite;
      }
      .label {
        font-size: 0.875rem;
      }
      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class LoadingSpinnerComponent {
  readonly size = input<number>(32);
  readonly label = input<string>('');
}
