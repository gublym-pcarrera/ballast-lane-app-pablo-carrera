import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormGroup } from '@angular/forms';

export interface ProblemBody {
  title?: string;
  errors?: Record<string, string[]>;
}

export function readProblem(error: unknown): ProblemBody {
  if (!(error instanceof HttpErrorResponse)) {
    return { title: 'Something went wrong. Try again.' };
  }

  const body: unknown = error.error;
  if (body && typeof body === 'object') {
    const record = body as { title?: unknown; errors?: unknown };
    const title = typeof record.title === 'string' ? record.title : fallback(error.status);
    return { title, errors: isErrorMap(record.errors) ? record.errors : undefined };
  }

  return { title: fallback(error.status) };
}

export function applyFieldErrors(form: FormGroup, errors: Record<string, string[]> | undefined): void {
  if (!errors) {
    return;
  }

  for (const [key, messages] of Object.entries(errors)) {
    form.get(key)?.setErrors({ server: messages.join(' ') });
  }
}

export function controlMessage(control: AbstractControl | null, label: string): string | null {
  if (!control) {
    return null;
  }

  const server = control.errors?.['server'];
  if (typeof server === 'string' && server.length > 0) {
    return server;
  }

  if (!control.touched) {
    return null;
  }

  if (control.hasError('required')) {
    return `${label} is required.`;
  }

  if (control.hasError('email')) {
    return 'Enter a valid email address.';
  }

  if (control.hasError('minlength') || control.hasError('maxlength')) {
    return `${label} is the wrong length.`;
  }

  return null;
}

function isErrorMap(value: unknown): value is Record<string, string[]> {
  if (!value || typeof value !== 'object') {
    return false;
  }

  return Object.values(value).every(
    (entry) => Array.isArray(entry) && entry.every((item) => typeof item === 'string'),
  );
}

function fallback(status: number): string {
  if (status === 0) {
    return 'The API is not reachable.';
  }

  if (status === 401) {
    return 'Sign in again.';
  }

  return 'Something went wrong. Try again.';
}
