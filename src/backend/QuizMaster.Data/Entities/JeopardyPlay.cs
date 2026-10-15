namespace QuizMaster.Data;

/// <summary>Persistent state and scoring outcome for one opened Jeopardy cell.</summary>
public class JeopardyPlay
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public int QuestionId { get; set; }
    public int? ContestantId { get; set; }
    public int? StealContestantId { get; set; }
    public string State { get; set; } = "question"; // question, steal, closed, skipped
    public int ScoreDelta { get; set; }
    public int StealScoreDelta { get; set; }
    public DateTime OpenedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedUtc { get; set; }
}
