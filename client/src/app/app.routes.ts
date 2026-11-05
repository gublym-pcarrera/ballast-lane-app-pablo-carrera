import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';
import { Register } from './pages/register/register';
import { SignIn } from './pages/sign-in/sign-in';
import { TaskList } from './pages/task-list/task-list';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: 'sign-in', component: SignIn, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'tasks', component: TaskList, canActivate: [authGuard] },
  { path: '**', redirectTo: 'tasks' },
];
