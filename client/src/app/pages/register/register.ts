import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { AuthService } from '../../core/auth.service';
import { applyFieldErrors, controlMessage, readProblem } from '../../core/problem';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(80)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
  });

  constructor() {
    inject(Title).setTitle('Create account · Tasks');
  }

  protected message(name: 'displayName' | 'email' | 'password', label: string): string | null {
    return controlMessage(this.form.controls[name], label);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);
    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl('/tasks');
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        const problem = readProblem(error);
        applyFieldErrors(this.form, problem.errors);
        this.formError.set(problem.errors ? null : (problem.title ?? 'Could not create the account.'));
      },
    });
  }
}
