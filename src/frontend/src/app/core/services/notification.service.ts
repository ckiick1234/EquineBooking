import { Injectable, signal } from '@angular/core';

export type NotificationKind = 'success' | 'error' | 'info';

export interface Notification {
  id: number;
  kind: NotificationKind;
  message: string;
}

const DEFAULT_TTL_MS = 4000;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 1;
  readonly notifications = signal<Notification[]>([]);

  success(message: string, ttlMs = DEFAULT_TTL_MS): void {
    this.push('success', message, ttlMs);
  }

  error(message: string, ttlMs = DEFAULT_TTL_MS): void {
    this.push('error', message, ttlMs);
  }

  info(message: string, ttlMs = DEFAULT_TTL_MS): void {
    this.push('info', message, ttlMs);
  }

  dismiss(id: number): void {
    this.notifications.update((items) => items.filter((n) => n.id !== id));
  }

  private push(kind: NotificationKind, message: string, ttlMs: number): void {
    const id = this.nextId++;
    this.notifications.update((items) => [...items, { id, kind, message }]);
    if (ttlMs > 0) {
      setTimeout(() => this.dismiss(id), ttlMs);
    }
  }
}
