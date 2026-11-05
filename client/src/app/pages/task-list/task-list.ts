import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { EMPTY, Subject, catchError, switchMap } from 'rxjs';
import { SessionStore } from '../../core/session.store';
import { TaskService } from '../../core/task.service';
import { TaskDraft, TaskItem, TaskStatus, formatDue, isOverdue, statusLabels } from '../../core/models';
import { readProblem } from '../../core/problem';
import { TaskForm } from './task-form';

type StatusFilter = 'all' | TaskStatus;

@Component({
  selector: 'app-task-list',
  imports: [TaskForm],
  templateUrl: './task-list.html',
})
export class TaskList {
  private readonly tasksApi = inject(TaskService);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly load = new Subject<void>();

  protected readonly user = this.session.user;
  protected readonly tasks = signal<TaskItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly banner = signal<string | null>(null);
  protected readonly filter = signal<StatusFilter>('all');
  protected readonly creating = signal(false);
  protected readonly editing = signal<TaskItem | null>(null);
  protected readonly saving = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]> | null>(null);
  protected readonly pendingDeleteId = signal<string | null>(null);
  protected readonly draft = computed<TaskDraft | null>(() => {
    const task = this.editing();
    if (!task) {
      return null;
    }

    return {
      title: task.title,
      description: task.description,
      status: task.status,
      dueDate: task.dueDate,
    };
  });

  protected readonly filters: { value: StatusFilter; label: string }[] = [
    { value: 'all', label: 'All' },
    { value: 'Todo', label: 'To do' },
    { value: 'InProgress', label: 'In progress' },
    { value: 'Done', label: 'Done' },
  ];

  constructor() {
    inject(Title).setTitle('Tasks');
    this.load
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          const status = this.filter();
          return this.tasksApi.list(status === 'all' ? undefined : status).pipe(
            catchError((error: unknown) => {
              this.loading.set(false);
              this.banner.set(readProblem(error).title ?? 'Could not load tasks.');
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((items) => {
        this.tasks.set(items);
        this.loading.set(false);
        this.banner.set(null);
      });
    this.reload();
  }

  protected reload(): void {
    this.load.next();
  }

  protected setFilter(value: StatusFilter): void {
    this.filter.set(value);
    this.reload();
  }

  protected startCreate(): void {
    this.editing.set(null);
    this.creating.set(true);
    this.serverErrors.set(null);
    this.banner.set(null);
  }

  protected startEdit(task: TaskItem): void {
    this.creating.set(false);
    this.editing.set(task);
    this.pendingDeleteId.set(null);
    this.serverErrors.set(null);
    this.banner.set(null);
  }

  protected closeEditor(): void {
    this.creating.set(false);
    this.editing.set(null);
    this.serverErrors.set(null);
  }

  protected save(draft: TaskDraft): void {
    const current = this.editing();
    this.saving.set(true);
    this.serverErrors.set(null);
    const request = current ? this.tasksApi.update(current.id, draft) : this.tasksApi.create(draft);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEditor();
        this.reload();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        const problem = readProblem(error);
        this.serverErrors.set(problem.errors ?? null);
        if (!problem.errors) {
          this.banner.set(problem.title ?? 'Could not save the task.');
        }
      },
    });
  }

  protected askDelete(id: string): void {
    this.pendingDeleteId.set(id);
  }

  protected cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  protected confirmDelete(id: string): void {
    this.tasksApi.delete(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.pendingDeleteId.set(null);
        if (this.editing()?.id === id) {
          this.closeEditor();
        }
        this.reload();
      },
      error: (error: unknown) => {
        this.banner.set(readProblem(error).title ?? 'Could not delete the task.');
      },
    });
  }

  protected signOut(): void {
    this.session.clear();
    void this.router.navigateByUrl('/sign-in');
  }

  protected label(status: TaskStatus): string {
    return statusLabels[status];
  }

  protected due(iso: string): string {
    return formatDue(iso);
  }

  protected overdue(task: TaskItem): boolean {
    return isOverdue(task);
  }
}
