import { Injectable, signal } from '@angular/core';

/** Text and visual intent for the app-owned confirmation dialog. */
export interface ConfirmationRequest {
  title: string;
  message: string;
  confirmLabel?: string;
  danger?: boolean;
}

/**
 * Replaces browser-native confirm dialogs with a styled, accessible application dialog.
 * Callers await a boolean and do not need to know how the dialog is rendered.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  readonly active = signal<ConfirmationRequest | null>(null);
  private resolvePending: ((accepted: boolean) => void) | null = null;

  ask(request: ConfirmationRequest): Promise<boolean> {
    if (this.resolvePending) {
      this.resolvePending(false);
    }

    this.active.set(request);
    return new Promise<boolean>((resolve) => {
      this.resolvePending = resolve;
    });
  }

  decide(accepted: boolean): void {
    const resolve = this.resolvePending;
    this.resolvePending = null;
    this.active.set(null);
    resolve?.(accepted);
  }
}
