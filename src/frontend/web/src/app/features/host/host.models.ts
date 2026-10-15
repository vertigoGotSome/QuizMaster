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

/** Mode-specific readiness report calculated from host-owned content. */
export interface ContentReadiness {
  imposter: ModeReadiness;
  wwm: ModeReadiness;
  jeopardy: ModeReadiness;
}

/** A concise status and actionable content gaps for one mode. */
export interface ModeReadiness {
  ready: boolean;
  summary: string;
  warnings: string[];
}
