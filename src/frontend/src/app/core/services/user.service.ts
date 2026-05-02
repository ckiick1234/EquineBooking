import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { UpdateUserProfileRequest, UserProfile } from '../models/user.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = inject(ApiService);

  me(): Observable<UserProfile> {
    return this.api.get<UserProfile>('users/me');
  }

  updateMe(request: UpdateUserProfileRequest): Observable<UserProfile> {
    return this.api.put<UserProfile>('users/me', request);
  }

  list(): Observable<UserProfile[]> {
    return this.api.get<UserProfile[]>('users');
  }

  setActive(userId: string, isActive: boolean): Observable<UserProfile> {
    return this.api.put<UserProfile>(`users/${userId}`, { isActive });
  }
}
