namespace QuizMaster.Data;

/// <summary>A host-entered spoken clue in its authoritative round order.</summary>
public class ImposterClue
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public int Round { get; set; }
    public int PlayerId { get; set; }
    public int Pass { get; set; }
    public int TurnIndex { get; set; }
    public string Clue { get; set; } = "";
}
