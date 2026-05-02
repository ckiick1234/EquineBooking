import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AvailabilityResponse } from '../models/availability.model';
import { Space } from '../models/space.model';
import { ApiService } from './api.service';

export interface AvailabilityQuery {
  from: string;
  to: string;
}

@Injectable({ providedIn: 'root' })
export class SpaceService {
  private readonly api = inject(ApiService);

  list(): Observable<Space[]> {
    return this.api.get<Space[]>('spaces');
  }

  get(id: string): Observable<Space> {
    return this.api.get<Space>(`spaces/${id}`);
  }

  create(space: Partial<Space>): Observable<Space> {
    return this.api.post<Space>('spaces', space);
  }

  update(id: string, space: Partial<Space>): Observable<Space> {
    return this.api.put<Space>(`spaces/${id}`, space);
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`spaces/${id}`);
  }

  availability(
    spaceId: string,
    query: AvailabilityQuery,
  ): Observable<AvailabilityResponse[]> {
    return this.api.get<AvailabilityResponse[]>(
      `spaces/${spaceId}/availability`,
      { from: query.from, to: query.to },
    );
  }
}
