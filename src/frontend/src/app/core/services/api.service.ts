import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export type QueryParams = Record<
  string,
  string | number | boolean | null | undefined
>;

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  get<T>(path: string, params?: QueryParams): Observable<T> {
    return this.http.get<T>(this.url(path), { params: this.params(params) });
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(this.url(path), body);
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(this.url(path), body);
  }

  delete<T>(path: string): Observable<T> {
    return this.http.delete<T>(this.url(path));
  }

  private url(path: string): string {
    const trimmed = path.startsWith('/') ? path.slice(1) : path;
    return `${this.baseUrl}/${trimmed}`;
  }

  private params(params?: QueryParams): HttpParams | undefined {
    if (!params) return undefined;
    let p = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value !== null && value !== undefined) {
        p = p.set(key, String(value));
      }
    }
    return p;
  }
}
