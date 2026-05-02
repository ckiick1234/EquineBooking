import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MsalService } from '@azure/msal-angular';

import { getAccountRole } from '../../core/auth/auth.guard';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-dashboard',
  imports: [EmptyStateComponent, LoadingSpinnerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="dashboard">
      <h1>Dashboard</h1>
      @if (loading()) {
        <app-loading-spinner label="Loading..." />
      } @else if (isAdmin()) {
        <app-empty-state
          title="Admin dashboard"
          message="Calendar of all bookings across spaces will render here."
          icon="📅"
        />
      } @else {
        <app-empty-state
          title="Welcome"
          message="Browse spaces and book a slot to get started."
          icon="🐴"
        />
      }
    </section>
  `,
  styles: [
    `
      .dashboard {
        padding: 1.5rem;
      }
    `,
  ],
})
export class DashboardComponent {
  private readonly msal = inject(MsalService);
  readonly loading = signal(false);
  readonly isAdmin = computed(
    () => getAccountRole(this.msal.instance.getActiveAccount()) === 'Admin',
  );
}
