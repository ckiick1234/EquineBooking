import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

import { getAccountRole } from './core/auth/auth.guard';
import { NotificationsComponent } from './shared/components/notifications/notifications.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NotificationsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="layout">
      <aside class="sidebar">
        <div class="brand">
          <a routerLink="/dashboard">Equine Booking</a>
        </div>
        <nav>
          <a routerLink="/dashboard" routerLinkActive="active">
            <span class="material-icons">dashboard</span>
            Dashboard
          </a>
          <a routerLink="/bookings" routerLinkActive="active">
            <span class="material-icons">event</span>
            Bookings
          </a>
          @if (isAdmin()) {
            <div class="section">Admin</div>
            <a routerLink="/admin" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
              <span class="material-icons">space_dashboard</span>
              Admin Home
            </a>
            <a routerLink="/admin/bookings" routerLinkActive="active">
              <span class="material-icons">fact_check</span>
              Manage Bookings
            </a>
            <a routerLink="/admin/spaces" routerLinkActive="active">
              <span class="material-icons">cottage</span>
              Spaces
            </a>
            <a routerLink="/admin/users" routerLinkActive="active">
              <span class="material-icons">group</span>
              Users
            </a>
          }
        </nav>
      </aside>
      <main>
        <router-outlet />
      </main>
    </div>
    <app-notifications />
  `,
  styles: [
    `
      :host {
        display: block;
        font-family: Roboto, system-ui, -apple-system, sans-serif;
        color: #111827;
      }
      .layout {
        display: grid;
        grid-template-columns: 240px 1fr;
        min-height: 100vh;
      }
      .sidebar {
        background: #1f2937;
        color: #e5e7eb;
        padding: 1rem 0;
      }
      .brand {
        padding: 0 1.25rem 1rem;
        border-bottom: 1px solid #374151;
      }
      .brand a {
        color: #fff;
        text-decoration: none;
        font-weight: 700;
        font-size: 1.05rem;
      }
      nav {
        display: flex;
        flex-direction: column;
        padding: 0.75rem 0;
      }
      nav a {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.625rem 1.25rem;
        color: #d1d5db;
        text-decoration: none;
        font-size: 0.9rem;
      }
      nav a:hover {
        background: #374151;
        color: #fff;
      }
      nav a.active {
        background: #2563eb;
        color: #fff;
      }
      .section {
        padding: 0.75rem 1.25rem 0.25rem;
        font-size: 0.7rem;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #9ca3af;
      }
      main {
        background: #f9fafb;
        overflow-x: auto;
      }
      .material-icons {
        font-size: 1.125rem;
      }
    `,
  ],
})
export class AppComponent {
  private readonly msal = inject(MsalService);
  readonly isAdmin = computed(
    () => getAccountRole(this.msal.instance.getActiveAccount()) === 'Admin',
  );
}
