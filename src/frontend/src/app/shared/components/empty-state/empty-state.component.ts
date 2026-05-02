import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty">
      @if (icon()) {
        <div class="icon" aria-hidden="true">{{ icon() }}</div>
      }
      <h3 class="title">{{ title() }}</h3>
      @if (message()) {
        <p class="message">{{ message() }}</p>
      }
      <ng-content></ng-content>
    </div>
  `,
  styles: [
    `
      .empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        padding: 2rem 1rem;
        color: #4b5563;
      }
      .icon {
        font-size: 2.5rem;
        margin-bottom: 0.5rem;
        opacity: 0.6;
      }
      .title {
        margin: 0 0 0.25rem;
        font-size: 1.125rem;
        color: #111827;
      }
      .message {
        margin: 0 0 1rem;
        font-size: 0.875rem;
      }
    `,
  ],
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly message = input<string>('');
  readonly icon = input<string>('');
}
