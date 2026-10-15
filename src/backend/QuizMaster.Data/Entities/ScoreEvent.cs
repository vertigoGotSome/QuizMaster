namespace QuizMaster.Data;

/// <summary>
/// An auditable points change.
/// </summary>
public class ScoreEvent
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public int Round { get; set; }
    public int PlayerId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
