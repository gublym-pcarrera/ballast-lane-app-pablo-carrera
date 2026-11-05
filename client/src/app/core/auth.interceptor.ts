import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SessionStore } from './session.store';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionStore);
  const router = inject(Router);
  const token = session.token();
  const authed = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authed).pipe(
    catchError((error: unknown) => {
      const isAuthAttempt = request.url.includes('/api/auth/login') || request.url.includes('/api/auth/register');
      if (error instanceof HttpErrorResponse && error.status === 401 && !isAuthAttempt) {
        session.clear();
        void router.navigateByUrl('/sign-in');
      }

      return throwError(() => error);
    }),
  );
};
