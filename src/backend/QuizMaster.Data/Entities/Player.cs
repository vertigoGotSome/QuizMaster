namespace QuizMaster.Data;

/// <summary>
/// Discord identity, lobby presence, and persistent score.
/// </summary>
public class Player
{
    public int Id { get; set; }
    public string DiscordId { get; set; } = "";
    public string Name { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public bool IsHost { get; set; }
    public int Points { get; set; }
    public string Team { get; set; } = "";
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}
