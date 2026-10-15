import { Component, inject } from '@angular/core';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'qm-toast-stack',
  standalone: true,
  template: `<div class="toast-stack" aria-live="polite">
    @for (t of toast.toasts(); track t.id) {
      <button class="toast {{ t.tone }}" (click)="toast.dismiss(t.id)">{{ t.message }}</button>
    }
  </div>`,
})
export class ToastStackComponent {
  readonly toast = inject(ToastService);
}
