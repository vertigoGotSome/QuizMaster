namespace QuizMaster.Data;

/// <summary>Records a WWM question once it has been shown, preventing repeats in the same game.</summary>
public class QuestionUse
{
    public int Id { get; set; }
    public int SessionId { get; set; } = 1;
    public int QuestionId { get; set; }
    public int Round { get; set; }
}
