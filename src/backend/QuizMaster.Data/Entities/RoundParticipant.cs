namespace QuizMaster.Data;

/// <summary>Snapshot of eligible players when a WWM question starts.</summary>
public class RoundParticipant
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public string Game { get; set; } = "";
    public int Round { get; set; }
    public int PlayerId { get; set; }
}
