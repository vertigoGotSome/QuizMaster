namespace QuizMaster.Api.Features.Game;

/// <summary>Voting target supplied by an authenticated Imposter participant.</summary>
public sealed record VoteRequest(int SuspectId);

/// <summary>Joker requested by a WWM participant for the current question version.</summary>
public sealed record JokerRequest(string Kind, int? QuestionId, int SessionVersion);

/// <summary>Host-recorded spoken clue for the active Imposter participant.</summary>
public sealed record ClueRequest(int PlayerId, string Clue);

/// <summary>Host correction for an already submitted WWM answer.</summary>
public sealed record OverrideAnswer(int PlayerId, bool? Correct);

/// <summary>Selected game mode; switching an active game requires confirmation.</summary>
public sealed record SetGame(string Game, bool Confirm = false);

/// <summary>Requested phase transition under the current game's server-side rules.</summary>
public sealed record SetPhase(string Phase, bool AdvanceRound = false, bool Confirm = false);

/// <summary>Host-managed settings used when the next game round is created.</summary>
public sealed record GameConfig(
    int ImposterCount,
    bool HelperWords,
    int? WordId,
    int? QuestionId,
    bool HostPlaysBlind = true
);

/// <summary>Manual score correction entered by the host.</summary>
public sealed record PointChange(int PlayerId, int Delta, string? Reason = null);

/// <summary>Optional team assignment for a non-host participant.</summary>
public sealed record TeamChange(int PlayerId, string? Team);

/// <summary>Multiple-choice answer submitted against one immutable WWM question version.</summary>
public sealed record AnswerInput(string Answer, int? QuestionId, int SessionVersion);
