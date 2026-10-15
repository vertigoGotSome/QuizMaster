import { Component, HostListener, inject } from '@angular/core';
import { ConfirmationService } from '../services/confirmation.service';

/** Global overlay that renders every destructive action in QuizMaster's own UI. */
@Component({
  selector: 'qm-confirmation-dialog',
  standalone: true,
  template: `
    @if (confirmation.active(); as dialog) {
      <div class="dialog-backdrop" role="presentation" (click)="confirmation.decide(false)">
        <section
          class="confirmation-dialog"
          role="alertdialog"
          aria-modal="true"
          aria-labelledby="confirmation-title"
          aria-describedby="confirmation-message"
          (click)="$event.stopPropagation()"
        >
          <p class="eyebrow">BESTÄTIGUNG ERFORDERLICH</p>
          <h2 id="confirmation-title">{{ dialog.title }}</h2>
          <p id="confirmation-message">{{ dialog.message }}</p>
          <div class="dialog-actions">
            <button class="muted" type="button" (click)="confirmation.decide(false)">
              Abbrechen
            </button>
            <button
              type="button"
              [class.danger]="dialog.danger"
              (click)="confirmation.decide(true)"
            >
              {{ dialog.confirmLabel || 'Bestätigen' }}
            </button>
          </div>
        </section>
      </div>
    }
  `,
})
export class ConfirmationDialogComponent {
  readonly confirmation = inject(ConfirmationService);

  @HostListener('window:keydown.escape')
  onEscape(): void {
    if (this.confirmation.active()) {
      this.confirmation.decide(false);
    }
  }
}
