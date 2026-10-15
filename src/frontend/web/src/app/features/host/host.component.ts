import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { GameService, describeApiError } from '../../core/services/game.service';
import type { Player, Question } from '../../core/models/game.models';
import { JeopardyHostComponent } from '../jeopardy/jeopardy-host.component';
import { CameraService } from '../../core/services/camera.service';
import { CameraStreamDirective } from '../../core/services/camera-stream.directive';
import { ConfirmationService } from '../../core/services/confirmation.service';
import type { SecretWord, TeamScore } from './host.models';

/** Provides game-specific host controls without exposing the host API to players. */
@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, JeopardyHostComponent, CameraStreamDirective],
  templateUrl: './host.component.html',
})
export class HostComponent {
  readonly g = inject(GameService);
  readonly camera = inject(CameraService);
  readonly confirmation = inject(ConfirmationService);

  readonly questions = signal<Question[]>([]);
  readonly words = signal<SecretWord[]>([]);
  readonly message = signal('');

  clue = '';
  imposters = 1;
  helper = true;
  hostPlaysBlind = true;
  selectedQuestion: number | null = null;
  selectedWord: number | null = null;
  manualPoints = 50;
  manualScoreReason = '';

  /** Non-host accounts are the only participants shown in the moderation list. */
  players(): Player[] {
    return this.g.players().filter((player) => !player.isHost);
  }

  /** Team choices expand with the active player count. Team play remains optional. */
  teamOptions(): string[] {
    const playerCount = this.g.players().filter((player) => !player.isHost).length;
    return ['Rot', 'Blau', 'Grün', 'Gelb', 'Lila', 'Orange', 'Türkis', 'Pink'].slice(
      0,
      Math.max(2, playerCount),
    );
  }

  teamStandings(): TeamScore[] {
    const teams = new Map<string, TeamScore>();
    for (const player of this.players()) {
      if (!player.team) continue;
      const score = teams.get(player.team) ?? { name: player.team, points: 0, members: 0 };
      score.points += player.points;
      score.members++;
      teams.set(player.team, score);
    }
    return [...teams.values()].sort(
      (left, right) => right.points - left.points || left.name.localeCompare(right.name),
    );
  }

  wwmPrimaryLabel(): string {
    switch (this.g.session()?.phase) {
      case 'question':
        return 'Antworten sperren';
      case 'answers':
        return 'Auswerten & Lösung zeigen';
      default:
        return 'Nächste Frage starten';
    }
  }

  /** One primary WWM action keeps the moderator flow in a single place. */
  wwmPrimary(): void {
    switch (this.g.session()?.phase) {
      case 'question':
        this.phase('answers');
        break;
      case 'answers':
        void this.command('host/resolve-question', {});
        break;
      default:
        void this.command('host/start-question', {});
    }
  }

  ngOnInit(): void {
    void this.load();
    void this.camera.connect();
  }

  remoteCamera(id: number): MediaStream | null {
    return this.camera.feeds().find((feed) => feed.playerId === id)?.stream ?? null;
  }

  speakerName(): string {
    return (
      this.g.players().find((p) => p.id === this.g.state()?.currentSpeakerId)?.name ??
      'Alle Hinweise abgegeben'
    );
  }

  /** Loads host-only content. Called after login and on demand. */
  async load(): Promise<void> {
    if (!this.g.me()?.isHost) return;

    try {
      this.questions.set(await this.g.get<Question[]>('host/questions'));
      const session = this.g.session();
      if (session) {
        this.imposters = session.imposterCount;
        this.helper = session.helperWords;
        this.hostPlaysBlind = session.hostPlaysBlind;
      }
      if (
        this.g.session()?.game !== 'imposter' ||
        !this.g.session()?.hostPlaysBlind ||
        ['lobby', 'reveal'].includes(this.g.session()?.phase ?? '')
      )
        this.words.set(await this.g.get<SecretWord[]>('host/words'));
      else this.words.set([]);
    } catch (error) {
      this.message.set(`Content could not be loaded: ${describeApiError(error)}`);
    }
  }

  /** Executes an authorized host operation and refreshes the host dashboard. */
  async command(path: string, body: object): Promise<boolean> {
    try {
      await this.g.post(path, body);
      this.message.set('Gespeichert.');
      return true;
    } catch (error) {
      this.message.set(`Aktion fehlgeschlagen: ${describeApiError(error)}`);
      return false;
    }
  }

  /** Record the spoken word for the player whose turn is active. */
  async recordClue(): Promise<void> {
    const playerId = this.g.state()?.currentSpeakerId;
    if (!playerId || !this.clue.trim()) return;
    if (await this.command('host/clue', { playerId, clue: this.clue.trim() })) this.clue = '';
  }

  /** Offers values derived from the active mode while retaining free-form host corrections. */
  scorePresets(): { label: string; amount: number }[] {
    const session = this.g.session();
    if (!session) return [];

    if (session.game === 'wwm') {
      const roundValue = Math.max(1, Math.min(30, session.round || 1));
      return [
        { label: `WWM +${roundValue}`, amount: roundValue },
        { label: `Risk �${roundValue * 2}`, amount: roundValue * 2 },
      ];
    }

    if (session.game === 'jeopardy') {
      const cell = this.g.state()?.jeopardy;
      if (!cell) return [];
      const value = cell.value * cell.boardRound;
      return [
        { label: `Cell ${value}`, amount: value },
        { label: `Steal ${value / 2}`, amount: value / 2 },
      ];
    }

    return [];
  }

  /** Applies a manual points correction to the server-maintained score history. */
  points(player: Player, direction: 1 | -1): void {
    const amount = Math.floor(Number(this.manualPoints));
    if (!Number.isFinite(amount) || amount < 1) {
      this.message.set('Bitte einen Punktebetrag ab 1 eingeben.');
      return;
    }

    void this.command('host/points', {
      playerId: player.id,
      delta: Math.min(amount, 10000) * direction,
      reason: this.manualScoreReason.trim() || undefined,
    });
  }

  /** Transitions to another phase; game rules are verified by the backend. */
  async phase(phase: string, advanceRound = false): Promise<void> {
    if (
      phase === 'lobby' &&
      !(await this.confirmation.ask({
        title: 'Zur Lobby zurückkehren?',
        message: 'Alle Spieler verlassen die laufende Partie. Die Punkte bleiben erhalten.',
        confirmLabel: 'Zur Lobby',
      }))
    )
      return;

    await this.command('host/phase', { phase, advanceRound, confirm: phase === 'lobby' });
  }

  async chooseGame(game: 'imposter' | 'wwm' | 'jeopardy' | 'none'): Promise<void> {
    const session = this.g.session();
    const active = !!session && session.game !== 'none' && session.game !== game;
    if (
      active &&
      !(await this.confirmation.ask({
        title: 'Spiel wirklich wechseln?',
        message: `Die aktuelle Partie endet. Für ${game === 'jeopardy' ? 'Jeopardy' : game} beginnen alle Punktestände wieder bei null.`,
        confirmLabel: 'Spiel wechseln',
        danger: true,
      }))
    )
      return;

    await this.command('host/game', { game, confirm: active });
  }

  /** Saves the game configuration without assigning secret roles client-side. */
  saveConfig(): void {
    void this.command('host/config', {
      imposterCount: this.imposters,
      helperWords: this.helper,
      hostPlaysBlind: this.hostPlaysBlind,
      wordId: this.selectedWord,
      questionId: this.selectedQuestion,
    });
  }
}
