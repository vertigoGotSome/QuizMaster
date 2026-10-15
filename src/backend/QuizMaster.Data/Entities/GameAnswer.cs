namespace QuizMaster.Data;

/// <summary>
/// An answer submitted by a player in a quiz round.
/// </summary>
public class GameAnswer
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int Round { get; set; }
    public string Answer { get; set; } = "";
    public bool? CorrectOverride { get; set; }
    public string Joker { get; set; } = "";
    public int SessionId { get; set; } = 1;
}
