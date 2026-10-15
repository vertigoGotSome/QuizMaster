import { Injectable, signal } from '@angular/core';

export type ToastTone = 'success' | 'error' | 'info';
export interface Toast {
  id: number;
  message: string;
  tone: ToastTone;
}

/** Small global feedback queue for game actions without blocking the flow. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private next = 1;
  show(message: string, tone: ToastTone = 'info'): void {
    const toast = { id: this.next++, message, tone };
    this.toasts.update((items) => [...items, toast]);
    setTimeout(() => this.dismiss(toast.id), 3200);
  }
  dismiss(id: number): void {
    this.toasts.update((items) => items.filter((item) => item.id !== id));
  }
}
