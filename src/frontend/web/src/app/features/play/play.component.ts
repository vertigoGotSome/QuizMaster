import { Component, effect, HostListener, inject, OnDestroy, signal } from '@angular/core';
import { GameService, describeApiError } from '../../core/services/game.service';
import { CameraService } from '../../core/services/camera.service';
import { CameraStreamDirective } from '../../core/services/camera-stream.directive';
import type { Player, Question } from '../../core/models/game.models';

/** Presents player actions for the currently active game and phase. */
@Component({
  standalone: true,
  imports: [CameraStreamDirective],
  templateUrl: './play.component.html',
})
export class PlayComponent implements OnDestroy {
  readonly g = inject(GameService);
  readonly camera = inject(CameraService);

  constructor() {
    void this.camera.connect();
    let previousQuestion: number | undefined;
    effect(() => {
      const id = this.g.state()?.question?.id;
      if (id !== previousQuestion) {
        this.selected.set('');
        previousQuestion = id;
      }
    });
  }

  playerName(id: number | null): string {
    if (id === null) return '—';
    return this.g.players().find((p) => p.id === id)?.name ?? `Spieler #${id}`;
  }
  playerTeam(id: number | null): string {
    if (id === null) return '';
    return this.g.players().find((player) => player.id === id)?.team || '';
  }

  private readonly clock = signal(Date.now());
  private readonly clockTimer = setInterval(() => this.clock.set(Date.now()), 500);

  ngOnDestroy(): void {
    clearInterval(this.clockTimer);
    void this.camera.disconnect();
  }

  /** Select the remote stream for the requested participant, if available. */
  remoteCamera(id: number): MediaStream | null {
    return this.camera.feeds().find((feed) => feed.playerId === id)?.stream ?? null;
  }

  /** The host camera is deliberately rendered outside the participant card row. */
  hostPlayer(): Player | null {
    return this.g.players().find((player) => player.isHost) ?? null;
  }

  /** Keeps the shared game area focused on participants rather than moderation. */
  gamePlayers(): Player[] {
    return this.g.players().filter((player) => !player.isHost);
  }
  readonly selected = signal('');
  readonly removed = signal<string[]>([]);
  readonly voteChoice = signal<number | null>(null);
  readonly error = signal('');
  readonly buzzing = signal(false);

  /** Resolve the speaking player from the public lobby state. */
  speakerName(): string {
    return (
      this.g.players().find((player) => player.id === this.g.state()?.currentSpeakerId)?.name ??
      'Alle Hinweise abgegeben'
    );
  }

  /** Converts a question to the four stable options expected by the template. */
  answers(question: Question): { key: string; text: string }[] {
    return [
      { key: 'A', text: question.answerA },
      { key: 'B', text: question.answerB },
      { key: 'C', text: question.answerC },
      { key: 'D', text: question.answerD },
    ];
  }

  /** Checks whether the current player has already consumed a given joker. */
  used(kind: string): boolean {
    return this.g.state()?.usedJokers?.includes(kind) ?? false;
  }

  timerLabel(endsUtc: string | null | undefined): string {
    this.clock();
    if (!endsUtc) return '';
    return `${Math.max(0, Math.ceil((new Date(endsUtc).getTime() - Date.now()) / 1000))}s`;
  }

  jeopardyCategories(): string[] {
    return [...new Set(this.g.jeopardyCells().map((cell) => cell.category))];
  }
  jeopardyCells(category: string) {
    return this.g
      .jeopardyCells()
      .filter((cell) => cell.category === category)
      .sort((a, b) => a.value - b.value);
  }
  jeopardyBoardFinished(): boolean {
    const cells = this.g.jeopardyCells();
    return cells.length > 0 && cells.every((cell) => cell.state !== 'available');
  }
  isJeopardySelector(playerId: number): boolean {
    return (
      this.g.session()?.game === 'jeopardy' &&
      !this.g.state()?.jeopardy &&
      this.g.session()?.speakerIndex === playerId
    );
  }
  canBuzz(): boolean {
    const clue = this.g.state()?.jeopardy;
    return (
      !!clue &&
      clue.state === 'steal' &&
      !clue.stealContestantId &&
      !this.g.me()?.isHost &&
      clue.contestantId !== this.g.me()?.id
    );
  }
  @HostListener('window:keydown.space', ['$event'])
  onSpace(event: Event): void {
    if (this.canBuzz()) {
      event.preventDefault();
      void this.buzz();
    }
  }
  async buzz(): Promise<void> {
    if (!this.canBuzz() || this.buzzing()) return;
    this.buzzing.set(true);
    try {
      await this.g.post('jeopardy/buzz');
    } catch (error) {
      this.error.set(`Buzzer: ${describeApiError(error)}`);
    } finally {
      this.buzzing.set(false);
    }
  }

  /** Submits a locked-in answer; the server is responsible for scoring it. */
  async sendAnswer(): Promise<void> {
    try {
      await this.g.post('answers', {
        answer: this.selected(),
        questionId: this.g.session()?.questionId,
        sessionVersion: this.g.session()?.version,
      });
    } catch (error) {
      this.error.set(`Antwort konnte nicht gespeichert werden: ${describeApiError(error)}`);
    }
  }

  /** Activates a server-side joker and stores only its permitted public result. */
  async joker(kind: string): Promise<void> {
    try {
      const result = await this.g.postResult<{ removed?: string[] }>('joker', {
        kind,
        questionId: this.g.session()?.questionId,
        sessionVersion: this.g.session()?.version,
      });
      if (kind === 'fifty') this.removed.set(result.removed ?? []);
    } catch (error) {
      this.error.set(`Joker fehlgeschlagen: ${describeApiError(error)}`);
    }
  }

  /** Sends a vote without exposing another player's secret role. */
  async vote(): Promise<void> {
    if (this.voteChoice() === null) return;

    try {
      await this.g.post('vote', { suspectId: this.voteChoice() });
    } catch (error) {
      this.error.set(`Abstimmung fehlgeschlagen: ${describeApiError(error)}`);
    }
  }
}
