import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { AuthService } from '../../core/auth.service';
import { demoAccount } from '../../core/models';
import { applyFieldErrors, controlMessage, readProblem } from '../../core/problem';

@Component({
  selector: 'app-sign-in',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './sign-in.html',
})
export class SignIn {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly demo = demoAccount;
  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  constructor() {
    inject(Title).setTitle('Sign in · Tasks');
  }

  protected message(name: 'email' | 'password', label: string): string | null {
    return controlMessage(this.form.controls[name], label);
  }

  protected useDemo(): void {
    this.form.setValue({ email: this.demo.email, password: this.demo.password });
    this.formError.set(null);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl('/tasks');
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        const problem = readProblem(error);
        applyFieldErrors(this.form, problem.errors);
        if (!problem.errors) {
          this.formError.set(problem.title ?? 'Could not sign in.');
        }
      },
    });
  }
}
