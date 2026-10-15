import type { JeopardyCell } from '../../core/models/game.models';

/** Full host-only Jeopardy content including the answer and moderation notes. */
export interface JeopardyQuestion extends JeopardyCell {
  clue: string;
  answer: string;
  acceptedAnswers: string;
  hostNotes: string;
  imageUrl: string;
}

/** A reusable board/category pair offered in a Jeopardy setup slot. */
export interface CatalogCategory {
  board: string;
  category: string;
}

/** Editable part of a Jeopardy question, excluding server-assigned display state. */
export type JeopardyQuestionDraft = Omit<JeopardyQuestion, 'id' | 'state' | 'slot'>;
