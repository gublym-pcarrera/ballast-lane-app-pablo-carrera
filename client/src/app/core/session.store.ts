import { Injectable, signal } from '@angular/core';
import { AuthResponse, UserProfile } from './models';

const tokenKey = 'tasks.token';
const userKey = 'tasks.user';

@Injectable({ providedIn: 'root' })
export class SessionStore {
  private readonly userState = signal<UserProfile | null>(readUser());
  readonly user = this.userState.asReadonly();

  token(): string | null {
    return localStorage.getItem(tokenKey);
  }

  save(response: AuthResponse): void {
    localStorage.setItem(tokenKey, response.accessToken);
    this.setUser(response.user);
  }

  setUser(user: UserProfile): void {
    localStorage.setItem(userKey, JSON.stringify(user));
    this.userState.set(user);
  }

  clear(): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(userKey);
    this.userState.set(null);
  }
}

function readUser(): UserProfile | null {
  const raw = localStorage.getItem(userKey);
  if (!raw) {
    return null;
  }

  try {
    const parsed: unknown = JSON.parse(raw);
    if (!parsed || typeof parsed !== 'object') {
      return null;
    }

    const record = parsed as Partial<UserProfile>;
    if (!record.id || !record.email || !record.displayName) {
      return null;
    }

    return { id: record.id, email: record.email, displayName: record.displayName };
  } catch {
    return null;
  }
}
