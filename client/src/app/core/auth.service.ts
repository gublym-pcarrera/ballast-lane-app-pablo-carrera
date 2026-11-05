import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthResponse, Credentials, Registration, UserProfile } from './models';
import { SessionStore } from './session.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(SessionStore);

  constructor() {
    if (!this.session.token()) {
      return;
    }

    this.http.get<UserProfile>('/api/auth/me').subscribe({
      next: (user) => this.session.setUser(user),
      error: () => this.session.clear(),
    });
  }

  register(body: Registration): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/register', body).pipe(tap((response) => this.session.save(response)));
  }

  login(body: Credentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', body).pipe(tap((response) => this.session.save(response)));
  }
}
