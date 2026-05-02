import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AdminActionRequest,
  Booking,
  CreateBookingRequest,
  UpdateBookingRequest,
} from '../models/booking.model';
import { ApiService } from './api.service';

export interface BookingListFilters {
  [key: string]: string | undefined;
  spaceId?: string;
  status?: string;
  from?: string;
  to?: string;
}

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly api = inject(ApiService);

  list(filters: BookingListFilters = {}): Observable<Booking[]> {
    return this.api.get<Booking[]>('bookings', filters);
  }

  get(spaceId: string, id: string): Observable<Booking> {
    return this.api.get<Booking>(`spaces/${spaceId}/bookings/${id}`);
  }

  create(request: CreateBookingRequest): Observable<Booking> {
    return this.api.post<Booking>('bookings', request);
  }

  update(
    spaceId: string,
    id: string,
    request: UpdateBookingRequest,
  ): Observable<Booking> {
    return this.api.put<Booking>(`spaces/${spaceId}/bookings/${id}`, request);
  }

  delete(spaceId: string, id: string): Observable<void> {
    return this.api.delete<void>(`spaces/${spaceId}/bookings/${id}`);
  }

  decide(
    spaceId: string,
    id: string,
    request: AdminActionRequest,
  ): Observable<Booking> {
    return this.api.post<Booking>(
      `spaces/${spaceId}/bookings/${id}/decision`,
      request,
    );
  }
}
