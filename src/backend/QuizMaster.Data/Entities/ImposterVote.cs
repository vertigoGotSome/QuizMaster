namespace QuizMaster.Data;

/// <summary>
/// A vote for a suspected Imposter.
/// </summary>
public class ImposterVote
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public int Round { get; set; }
    public int VoterId { get; set; }
    public int SuspectId { get; set; }
}
