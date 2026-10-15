/** A secret Imposter word available to the host before a round starts. */
export interface SecretWord {
  id: number;
  word: string;
  hint: string;
  category: string;
}

/** Aggregated display value for an optional Jeopardy team. */
export interface TeamScore {
  name: string;
  points: number;
  members: number;
}
