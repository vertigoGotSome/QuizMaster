import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { GameService } from './core/services/game.service';
import { ConfirmationDialogComponent } from './core/ui/confirmation-dialog.component';
import { ToastStackComponent } from './core/ui/toast-stack.component';

/** Root layout; connects realtime updates and keeps a polling fallback alive. */
@Component({
  selector: 'qm-root',
  standalone: true,
  imports: [RouterLink, RouterOutlet, ToastStackComponent, ConfirmationDialogComponent],
  templateUrl: './app.component.html',
})
export class AppComponent implements OnInit, OnDestroy {
  readonly g = inject(GameService);
  private readonly router = inject(Router);
  private timer: ReturnType<typeof setInterval> | undefined;

  ngOnInit(): void {
    void this.sync().then(() => this.g.connectLive());
    this.timer = setInterval(() => void this.sync(), 3500);
  }

  /** Keep normal participants on the correct game page without manual navigation. */
  private async sync(): Promise<void> {
    await this.g.refresh();
    const session = this.g.session();
    if (!this.g.me() || !session || this.g.me()?.isHost) return;
    const destination = session.game === 'none' || session.phase === 'lobby' ? '/lobby' : '/play';
    if (this.router.url !== destination) await this.router.navigateByUrl(destination);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }
}
