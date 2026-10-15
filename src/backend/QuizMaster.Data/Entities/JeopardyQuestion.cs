namespace QuizMaster.Data;

/// <summary>A single host-moderated Jeopardy board cell.</summary>
public class JeopardyQuestion
{
    public int Id { get; set; }
    public string Board { get; set; } = "Standard";
    public int BoardRound { get; set; } = 1;
    public string Category { get; set; } = "Allgemein";
    public int Value { get; set; } = 100;

    /// <summary>Author-assigned challenge level from 1 (introductory) through 10 (expert).</summary>
    public int Difficulty { get; set; }

    public string Clue { get; set; } = "";
    public string Answer { get; set; } = "";
    public string AcceptedAnswers { get; set; } = "";
    public string HostNotes { get; set; } = "";
    public string ImageUrl { get; set; } = "";
}
