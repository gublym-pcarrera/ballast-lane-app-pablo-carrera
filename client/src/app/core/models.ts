export type TaskStatus = 'Todo' | 'InProgress' | 'Done';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserProfile;
}

export interface TaskItem {
  id: string;
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface TaskDraft {
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string;
}

export interface Credentials {
  email: string;
  password: string;
}

export interface Registration extends Credentials {
  displayName: string;
}

export const demoAccount = {
  email: 'demo@ballast.local',
  password: 'Demo1234!',
};

export const statusLabels: Record<TaskStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  Done: 'Done',
};

export function todayIso(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

export function isOverdue(task: TaskItem): boolean {
  return task.status !== 'Done' && task.dueDate < todayIso();
}

export function formatDue(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  const options: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
  if (year !== new Date().getFullYear()) {
    options.year = 'numeric';
  }
  return new Intl.DateTimeFormat(undefined, options).format(date);
}
