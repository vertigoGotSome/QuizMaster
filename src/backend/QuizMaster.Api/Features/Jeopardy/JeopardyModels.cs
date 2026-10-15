namespace QuizMaster.Api.Features.Jeopardy;

/// <summary>Identifies one host-selected Jeopardy board cell.</summary>
public sealed record OpenRequest(int QuestionId);

/// <summary>Starts or clears the optional moderation timer for the active cell.</summary>
public sealed record TimerRequest(int? Seconds);

/// <summary>Closes an active cell; skipped cells are recorded distinctly from answered cells.</summary>
public sealed record CloseRequest(bool Skip = false);

/// <summary>One category selected into a board column by the host.</summary>
public sealed record JeopardyCategoryChoice(string Board, string Category);

/// <summary>Six ordered category choices for one Jeopardy board round.</summary>
public sealed record ConfigureBoardRequest(int Round, List<JeopardyCategoryChoice> Categories);
