import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { JeopardyCell, JeopardyState, Player } from '../../core/models/game.models';
import { describeApiError, GameService } from '../../core/services/game.service';
import { JeopardyHostApiService } from './jeopardy-api.service';
import type { CatalogCategory } from './jeopardy.models';

/**
 * View controller for a voice-moderated Jeopardy board.
 *
 * It owns only host UI state. The API service owns HTTP paths and the server
 * remains responsible for turns, scoring, steals, and validation.
 */
@Component({
  selector: 'qm-jeopardy',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './jeopardy.component.html',
})
export class JeopardyHostComponent {
  readonly g = inject(GameService);
  readonly api = inject(JeopardyHostApiService);
  readonly catalog = signal<CatalogCategory[]>([]);
  readonly notice = signal('');
  readonly busy = signal(false);

  setupRound = 1;
  slots: (CatalogCategory | null)[] = [null, null, null, null, null, null];

  ngOnInit(): void {
    void this.loadCatalog();
  }

  /** Loads only categories complete enough to fill a full board column. */
  async loadCatalog(): Promise<void> {
    try {
      this.catalog.set(await this.api.listCatalog());
    } catch (error) {
      this.notice.set(describeApiError(error));
    }
  }

  configured(): boolean {
    return this.g.jeopardyCells().length > 0;
  }

  boardFinished(): boolean {
    const cells = this.g.jeopardyCells();
    return cells.length > 0 && cells.every((cell) => cell.state !== 'available');
  }

  categoryKey(item: CatalogCategory): string {
    return `${item.board}::${item.category}`;
  }

  slotKey(index: number): string {
    const item = this.slots[index];
    return item ? this.categoryKey(item) : '';
  }

  setSlot(index: number, key: string): void {
    this.slots[index] = this.catalog().find((item) => this.categoryKey(item) === key) ?? null;
  }

  unavailable(item: CatalogCategory, index: number): boolean {
    return this.slots.some(
      (slot, slotIndex) =>
        slotIndex !== index && slot && this.categoryKey(slot) === this.categoryKey(item),
    );
  }

  /** Sends the six selected categories to the server in their visible order. */
  async configure(): Promise<void> {
    if (this.slots.some((slot) => !slot)) {
      this.notice.set('Wähle für jede der sechs Spalten eine Kategorie.');
      return;
    }

    this.busy.set(true);
    try {
      const categories = this.slots.filter((slot): slot is CatalogCategory => !!slot);
      await this.api.configure(this.setupRound, categories);
      this.notice.set('Tafel bereit.');
      await this.g.refresh();
    } catch (error) {
      this.notice.set(describeApiError(error));
    } finally {
      this.busy.set(false);
    }
  }

  categories(): string[] {
    return [
      ...new Set(
        this.g
          .jeopardyCells()
          .sort((left, right) => left.slot - right.slot)
          .map((cell) => cell.category),
      ),
    ];
  }

  values(category: string): JeopardyCell[] {
    return this.g
      .jeopardyCells()
      .filter((cell) => cell.category === category)
      .sort((left, right) => left.value - right.value);
  }

  players(): Player[] {
    return this.g.players().filter((player) => !player.isHost);
  }

  active(): JeopardyState | null {
    return this.g.state()?.jeopardy ?? null;
  }

  playerName(id: number | null): string {
    return this.players().find((player) => player.id === id)?.name ?? '—';
  }

  playerTeam(id: number | null): string {
    return this.players().find((player) => player.id === id)?.team || '';
  }

  nextPlayerName(): string {
    return this.playerName(this.g.session()?.speakerIndex ?? null);
  }

  nextPlayerTeam(): string {
    return this.playerTeam(this.g.session()?.speakerIndex ?? null);
  }

  isPlayed(id: number): boolean {
    return this.g.jeopardyCells().some((cell) => cell.id === id && cell.state !== 'available');
  }

  shownValue(question: JeopardyCell): number {
    return question.value * question.boardRound;
  }

  open(question: JeopardyCell): Promise<void> {
    return this.command('host/jeopardy/open', { questionId: question.id });
  }

  correct(): Promise<void> {
    return this.command('host/jeopardy/correct');
  }

  wrong(): Promise<void> {
    return this.command('host/jeopardy/wrong');
  }

  stealCorrect(): Promise<void> {
    return this.command('host/jeopardy/steal-correct');
  }

  stealWrong(): Promise<void> {
    return this.command('host/jeopardy/steal-wrong');
  }

  close(skip = false): Promise<void> {
    return this.command('host/jeopardy/close', { skip });
  }

  timer(seconds: number | null): Promise<void> {
    return this.command('host/jeopardy/timer', { seconds });
  }

  private async command(path: string, body: object = {}): Promise<void> {
    this.busy.set(true);
    try {
      await this.api.command(path, body);
      this.notice.set('Gespeichert.');
    } catch (error) {
      this.notice.set(describeApiError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
