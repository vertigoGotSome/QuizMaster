import { Injectable, inject } from '@angular/core';
import { GameService } from '../../core/services/game.service';
import type { CatalogCategory, JeopardyQuestion, JeopardyQuestionDraft } from './jeopardy.models';

/**
 * API boundary for the Jeopardy host feature.
 *
 * The component controls UI state; this service owns endpoint paths and request shapes.
 */
@Injectable({ providedIn: 'root' })
export class JeopardyHostApiService {
  private readonly game = inject(GameService);

  listQuestions(): Promise<JeopardyQuestion[]> {
    return this.game.get<JeopardyQuestion[]>('host/jeopardy/questions');
  }

  listCatalog(): Promise<CatalogCategory[]> {
    return this.game.get<CatalogCategory[]>('host/jeopardy/catalog');
  }

  configure(round: number, categories: CatalogCategory[]): Promise<void> {
    return this.game.post('host/jeopardy/configure', { round, categories });
  }

  save(question: JeopardyQuestionDraft, id: number | null): Promise<JeopardyQuestion> {
    return id
      ? this.game.postResult<JeopardyQuestion>(`host/jeopardy/questions/${id}`, question)
      : this.game.postResult<JeopardyQuestion>('host/jeopardy/questions', question);
  }

  remove(questionId: number): Promise<void> {
    return this.game.delete(`host/jeopardy/questions/${questionId}`);
  }

  uploadImage(data: FormData): Promise<{ url: string }> {
    return this.game.postForm<{ url: string }>('host/jeopardy/images', data);
  }

  command(path: string, body: object = {}): Promise<void> {
    return this.game.post(path, body);
  }
}
