namespace QuizMaster.Data;

/// <summary>
/// A word and optional helper hint for Imposter.
/// </summary>
public class SecretWord
{
    public int Id { get; set; }
    public string Category { get; set; } = "General";
    public string Word { get; set; } = "";
    public string Hint { get; set; } = "";
}
