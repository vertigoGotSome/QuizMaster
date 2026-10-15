namespace QuizMaster.Data;

/// <summary>
/// The usage of a specific joker by a player.
/// </summary>
public class JokerUse
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public int PlayerId { get; set; }
    public string Kind { get; set; } = "";
    public int Round { get; set; }
}
