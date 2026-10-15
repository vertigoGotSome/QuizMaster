/** Shared, strongly typed contracts for the QuizMaster HTTP API and game state. */
export interface Player {
  id: number;
  name: string;
  avatarUrl: string;
  isHost: boolean;
  points: number;
  team: string;
}
export interface Session {
  game: string;
  phase: string;
  round: number;
  imposterCount: number;
  helperWords: boolean;
  hostPlaysBlind: boolean;
  wordId: number | null;
  questionId: number | null;
  jeopardyQuestionId: number | null;
  timerEndsUtc: string | null;
  jeopardyRound: number;
  jeopardyCategoriesJson: string;
  version: number;
  questionIndex: number;
  scored: boolean;
  speakerIndex: number;
}
export interface Question {
  id: number;
  category: string;
  difficulty: number;
  text: string;
  answerA: string;
  answerB: string;
  answerC: string;
  answerD: string;
  correct?: string;
}
export interface JeopardyCell {
  id: number;
  board: string;
  boardRound: number;
  category: string;
  value: number;
  difficulty: number;
  slot: number;
  state: string;
}
export interface JeopardyState {
  id: number;
  board: string;
  boardRound: number;
  category: string;
  value: number;
  clue: string;
  imageUrl: string;
  answer?: string;
  acceptedAnswers?: string;
  hostNotes?: string;
  state: string;
  contestantId: number | null;
  stealContestantId: number | null;
  scoreDelta?: number | null;
  stealScoreDelta?: number | null;
  timerEndsUtc: string | null;
}
export interface State {
  speakerOrder: number[];
  participants: number[];
  revealedRoles: { playerId: number; isImposter: boolean }[] | null;
  revealedWord: string | null;
  session: Session;
  question: Question | null;
  jeopardy: JeopardyState | null;
  word: string | null;
  role: string | null;
  privateWord: string | null;
  myAnswer: string | null;
  myVote: number | null;
  usedJokers: string[];
  spy: { playerId: number; answer: string }[] | null;
  roundJoker: string | null;
  removedAnswers: string[];
  clues: { playerId: number; clue: string; pass: number }[] | null;
  currentSpeakerId: number | null;
  cluePass: number;
  votes: { voterId: number; suspectId: number }[] | null;
  answers: { playerId: number; answer: string; correctOverride: boolean | null }[] | null;
}
