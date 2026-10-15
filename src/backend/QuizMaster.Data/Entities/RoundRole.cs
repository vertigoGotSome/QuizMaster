namespace QuizMaster.Data;

/// <summary>
/// The private role of a player in an Imposter round.
/// </summary>
public class RoundRole
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public int Round { get; set; }
    public int PlayerId { get; set; }
    public bool IsImposter { get; set; }

    /// <summary>Randomized speaking position, retained for both clue passes.</summary>
    public int TurnOrder { get; set; }
}
