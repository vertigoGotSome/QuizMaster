namespace QuizMaster.Data;

/// <summary>A single host-moderated Jeopardy board cell.</summary>
public class JeopardyQuestion
{
    public int Id { get; set; }
    public string Board { get; set; } = "Standard";
    public int BoardRound { get; set; } = 1;
    public string Category { get; set; } = "Allgemein";
    public int Value { get; set; } = 100;
    public string Clue { get; set; } = "";
    public string Answer { get; set; } = "";
    public string AcceptedAnswers { get; set; } = "";
    public string HostNotes { get; set; } = "";
    public string ImageUrl { get; set; } = "";
}
