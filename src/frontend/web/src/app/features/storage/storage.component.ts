import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GameService, describeApiError } from '../../core/services/game.service';
import { ConfirmationService } from '../../core/services/confirmation.service';

type ContentKind = 'words' | 'questions' | 'jeopardy';

/** Host-only JSON content manager. All writes are validated again on the server. */
@Component({
  standalone: true,
  imports: [FormsModule],
  templateUrl: './storage.component.html',
})
export class StorageComponent implements OnInit {
  readonly game = inject(GameService);
  readonly confirmation = inject(ConfirmationService);
  readonly message = signal('');
  readonly items = signal<Record<string, unknown>[]>([]);
  kind: ContentKind = 'words';
  text = '[\n  { "category": "Gaming", "word": "Minecraft", "hint": "Terraria" }\n]';

  /** Load the initially selected collection as soon as the host opens Storage. */
  ngOnInit(): void {
    void this.load();
  }

  /** Switch collection and immediately replace, rather than append to, the visible list. */
  onKindChange(kind: ContentKind): void {
    this.kind = kind;
    this.items.set([]);
    this.message.set('');
    void this.load();
  }

  /** Load a JSON file locally without submitting it to the backend. */
  async onFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.text = await file.text();
    this.message.set(`Loaded ${file.name}. Review and import when ready.`);
  }

  /** Accept files dropped onto the JSON editor. */
  async onDrop(event: DragEvent): Promise<void> {
    event.preventDefault();
    const file = event.dataTransfer?.files[0];
    if (file) this.text = await file.text();
  }

  /** Fetch the current content for inspection and export. */
  async load(): Promise<void> {
    try {
      this.items.set(await this.game.get<Record<string, unknown>[]>(this.path()));
      this.message.set(`${this.items().length} entries loaded.`);
    } catch (error) {
      this.message.set(describeApiError(error));
    }
  }

  /** Validate a JSON array before sending the bulk request. */
  async import(): Promise<void> {
    try {
      const parsed: unknown = JSON.parse(this.text);
      if (!Array.isArray(parsed) || parsed.length === 0) {
        throw new Error('Expected a non-empty JSON array.');
      }
      const result = await this.game.postResult<{
        inserted: number;
        skipped: number;
        warnings?: string[];
      }>(`${this.path()}/import`, parsed);
      const warnings = result.warnings?.length
        ? ` ${result.warnings.length} difficulty warning(s): ${result.warnings.join(' ')}`
        : '';
      this.message.set(
        `Imported ${result.inserted}, skipped ${result.skipped} invalid or duplicate entries.${warnings}`,
      );
      await this.load();
    } catch (error) {
      this.message.set(describeApiError(error));
    }
  }

  /** Download the current records as a JSON export. */
  export(): void {
    const blob = new Blob([JSON.stringify(this.items(), null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `quizmaster-${this.kind}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  async remove(id: number): Promise<void> {
    if (
      !(await this.confirmation.ask({
        title: 'Eintrag löschen?',
        message: 'Dieser Eintrag wird dauerhaft aus dem aktuellen Inhalt entfernt.',
        confirmLabel: 'Eintrag löschen',
        danger: true,
      }))
    )
      return;
    try {
      await this.game.delete(`${this.path()}/${id}`);
      await this.load();
    } catch (error) {
      this.message.set(describeApiError(error));
    }
  }

  async clearAll(): Promise<void> {
    const label =
      this.kind === 'words'
        ? 'Imposter-Wörter'
        : this.kind === 'jeopardy'
          ? 'Jeopardy-Felder'
          : 'WWM-Fragen';
    if (
      !(await this.confirmation.ask({
        title: `Alle ${label} löschen?`,
        message: 'Dieser Vorgang kann nicht rückgängig gemacht werden.',
        confirmLabel: 'Alles löschen',
        danger: true,
      }))
    )
      return;
    try {
      await this.game.delete(this.path());
      this.items.set([]);
      this.message.set(`${label} wurden gelöscht.`);
    } catch (error) {
      this.message.set(describeApiError(error));
    }
  }

  private path(): string {
    return this.kind === 'jeopardy' ? 'host/jeopardy/questions' : `host/storage/${this.kind}`;
  }
}
