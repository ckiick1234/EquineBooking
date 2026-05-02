import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { AccountInfo } from '@azure/msal-browser';

import { UserRole } from '../models/user.model';

const ROLE_CLAIMS = ['extension_Role', 'role', 'roles'];

export function getAccountRole(account: AccountInfo | null): UserRole | null {
  if (!account?.idTokenClaims) {
    return null;
  }
  const claims = account.idTokenClaims as Record<string, unknown>;
  for (const key of ROLE_CLAIMS) {
    const value = claims[key];
    if (typeof value === 'string' && (value === 'Admin' || value === 'Client')) {
      return value;
    }
    if (Array.isArray(value)) {
      if (value.includes('Admin')) return 'Admin';
      if (value.includes('Client')) return 'Client';
    }
  }
  return null;
}

function getActiveAccount(msal: MsalService): AccountInfo | null {
  const active = msal.instance.getActiveAccount();
  if (active) return active;
  const all = msal.instance.getAllAccounts();
  return all.length > 0 ? all[0] : null;
}

export function roleGuard(required: UserRole): CanActivateFn {
  return (): boolean | UrlTree => {
    const msal = inject(MsalService);
    const router = inject(Router);
    const account = getActiveAccount(msal);
    if (!account) {
      return router.createUrlTree(['/login']);
    }
    const role = getAccountRole(account);
    if (role === required) {
      return true;
    }
    return router.createUrlTree(['/dashboard']);
  };
}

export const adminGuard: CanActivateFn = roleGuard('Admin');
export const clientGuard: CanActivateFn = roleGuard('Client');
