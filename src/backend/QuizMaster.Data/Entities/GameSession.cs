namespace QuizMaster.Data;

/// <summary>
/// State for the current global game session.
/// </summary>
public class GameSession
{
    public int Id { get; set; }
    public string Game { get; set; } = "none";
    public string Phase { get; set; } = "lobby";
    public int Round { get; set; }
    public bool HelperWords { get; set; } = true;

    /// <summary>Prevents the host from obtaining privileged game secrets when playing.</summary>
    public bool HostPlaysBlind { get; set; } = true;
    public int ImposterCount { get; set; } = 1;
    public int? QuestionId { get; set; }
    public int? WordId { get; set; }
    public int Version { get; set; } = 1;
    public int QuestionIndex { get; set; }
    public bool Scored { get; set; }
    public int SpeakerIndex { get; set; }

    /// <summary>The currently opened Jeopardy cell, if any.</summary>
    public int? JeopardyQuestionId { get; set; }

    /// <summary>UTC deadline selected by the host; null means no timer is active.</summary>
    public DateTime? TimerEndsUtc { get; set; }

    /// <summary>Active Jeopardy board round selected by the host.</summary>
    public int JeopardyRound { get; set; } = 1;

    /// <summary>JSON array of the six host-selected source categories in left-to-right order.</summary>
    public string JeopardyCategoriesJson { get; set; } = "[]";
}
