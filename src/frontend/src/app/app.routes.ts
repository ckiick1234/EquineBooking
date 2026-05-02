import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';

import { adminGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    canActivate: [MsalGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(
        (m) => m.DashboardComponent,
      ),
  },
  {
    path: 'bookings',
    canActivate: [MsalGuard],
    loadComponent: () =>
      import('./features/bookings/bookings-list.component').then(
        (m) => m.BookingsListComponent,
      ),
  },
  {
    path: 'bookings/:spaceId/:id',
    canActivate: [MsalGuard],
    loadComponent: () =>
      import('./features/bookings/booking-detail.component').then(
        (m) => m.BookingDetailComponent,
      ),
  },
  {
    path: 'bookings/:id',
    canActivate: [MsalGuard],
    loadComponent: () =>
      import('./features/bookings/booking-detail.component').then(
        (m) => m.BookingDetailComponent,
      ),
  },
  {
    path: 'admin',
    canActivate: [MsalGuard, adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-dashboard.component').then(
        (m) => m.AdminDashboardComponent,
      ),
  },
  {
    path: 'admin/bookings',
    canActivate: [MsalGuard, adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-booking-list.component').then(
        (m) => m.AdminBookingListComponent,
      ),
  },
  {
    path: 'admin/spaces',
    canActivate: [MsalGuard, adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-space-management.component').then(
        (m) => m.AdminSpaceManagementComponent,
      ),
  },
  {
    path: 'admin/users',
    canActivate: [MsalGuard, adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-user-list.component').then(
        (m) => m.AdminUserListComponent,
      ),
  },
  {
    path: 'admin/users/:id',
    canActivate: [MsalGuard, adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-user-detail.component').then(
        (m) => m.AdminUserDetailComponent,
      ),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'callback',
    loadComponent: () =>
      import('./features/auth/callback.component').then(
        (m) => m.CallbackComponent,
      ),
  },
  { path: '**', redirectTo: 'dashboard' },
];
