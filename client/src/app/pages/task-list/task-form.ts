import { Component, effect, inject, input, output } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { TaskDraft, TaskStatus, todayIso } from '../../core/models';

@Component({
  selector: 'app-task-form',
  imports: [ReactiveFormsModule],
  templateUrl: './task-form.html',
})
export class TaskForm {
  readonly heading = input('New task');
  readonly allowStatus = input(false);
  readonly initial = input<TaskDraft | null>(null);
  readonly saving = input(false);
  readonly serverErrors = input<Record<string, string[]> | null>(null);
  readonly save = output<TaskDraft>();
  readonly cancel = output<void>();

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(120)]],
    description: ['', [Validators.maxLength(2000)]],
    status: ['Todo' as TaskStatus, Validators.required],
    dueDate: [todayIso(), [Validators.required, notBeforeToday]],
  });

  constructor() {
    effect(() => {
      const value = this.initial();
      this.form.reset({
        title: value?.title ?? '',
        description: value?.description ?? '',
        status: this.allowStatus() ? (value?.status ?? 'Todo') : 'Todo',
        dueDate: value?.dueDate ?? todayIso(),
      });
    });

    effect(() => {
      const errors = this.serverErrors();
      if (!errors) {
        return;
      }

      for (const [key, messages] of Object.entries(errors)) {
        this.form.get(key)?.setErrors({ server: messages.join(' ') });
      }
    });
  }

  protected field(name: 'title' | 'description' | 'status' | 'dueDate'): string | null {
    const control = this.form.controls[name];
    const server = control.errors?.['server'];
    if (typeof server === 'string' && server.length > 0) {
      return server;
    }

    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'This field is required.';
    }

    if (control.hasError('maxlength')) {
      return 'This is too long.';
    }

    if (control.hasError('past')) {
      return 'Due date cannot be before today.';
    }

    return null;
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.save.emit({
      title: value.title,
      description: value.description,
      status: this.allowStatus() ? value.status : 'Todo',
      dueDate: value.dueDate,
    });
  }

  protected close(): void {
    this.cancel.emit();
  }

  protected earliestDueDate(): string {
    return todayIso();
  }
}

function notBeforeToday(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  if (value.length === 0 || value >= todayIso()) {
    return null;
  }

  return { past: true };
}
