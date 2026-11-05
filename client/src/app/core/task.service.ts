import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TaskDraft, TaskItem, TaskStatus } from './models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  list(status?: TaskStatus): Observable<TaskItem[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<TaskItem[]>('/api/tasks', { params });
  }

  create(draft: TaskDraft): Observable<TaskItem> {
    return this.http.post<TaskItem>('/api/tasks', draft);
  }

  update(id: string, draft: TaskDraft): Observable<TaskItem> {
    return this.http.put<TaskItem>(`/api/tasks/${id}`, draft);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/tasks/${id}`);
  }
}
