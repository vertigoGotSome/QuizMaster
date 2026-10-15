namespace QuizMaster.Data;

/// <summary>
/// A WWM multiple-choice question with four choices.
/// </summary>
public class Question
{
    public int Id { get; set; }
    public string Category { get; set; } = "General";
    public int Difficulty { get; set; } = 1;
    public string Text { get; set; } = "";
    public string AnswerA { get; set; } = "";
    public string AnswerB { get; set; } = "";
    public string AnswerC { get; set; } = "";
    public string AnswerD { get; set; } = "";
    public string Correct { get; set; } = "A";
}
