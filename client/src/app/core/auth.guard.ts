import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionStore } from './session.store';

export const authGuard: CanActivateFn = () => {
  const session = inject(SessionStore);
  const router = inject(Router);
  return session.token() ? true : router.createUrlTree(['/sign-in']);
};

export const guestGuard: CanActivateFn = () => {
  const session = inject(SessionStore);
  const router = inject(Router);
  return session.token() ? router.createUrlTree(['/tasks']) : true;
};
