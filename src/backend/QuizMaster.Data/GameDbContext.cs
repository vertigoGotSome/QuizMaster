using Microsoft.EntityFrameworkCore;

namespace QuizMaster.Data;

/// <summary>EF Core persistence for identity, game sessions, and scoring.</summary>
public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<RoundRole> Roles => Set<RoundRole>();
    public DbSet<ImposterVote> Votes => Set<ImposterVote>();
    public DbSet<ScoreEvent> ScoreEvents => Set<ScoreEvent>();
    public DbSet<JokerUse> JokerUses => Set<JokerUse>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<SecretWord> Words => Set<SecretWord>();
    public DbSet<GameSession> Sessions => Set<GameSession>();
    public DbSet<GameAnswer> Answers => Set<GameAnswer>();
    public DbSet<JeopardyQuestion> JeopardyQuestions => Set<JeopardyQuestion>();
    public DbSet<JeopardyPlay> JeopardyPlays => Set<JeopardyPlay>();
    public DbSet<ImposterClue> ImposterClues => Set<ImposterClue>();
    public DbSet<QuestionUse> QuestionUses => Set<QuestionUse>();
    public DbSet<RoundParticipant> RoundParticipants => Set<RoundParticipant>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Player>().HasIndex(x => x.DiscordId).IsUnique();
        b.Entity<GameAnswer>()
            .HasIndex(x => new
            {
                x.SessionId,
                x.PlayerId,
                x.Round,
            })
            .IsUnique();
        b.Entity<GameSession>().HasData(new GameSession { Id = 1 });
        b.Entity<ImposterVote>()
            .HasIndex(x => new
            {
                x.SessionId,
                x.Round,
                x.VoterId,
            })
            .IsUnique();
        b.Entity<RoundRole>()
            .HasIndex(x => new
            {
                x.SessionId,
                x.Round,
                x.PlayerId,
            })
            .IsUnique();
        b.Entity<JokerUse>()
            .HasIndex(x => new
            {
                x.SessionId,
                x.PlayerId,
                x.Kind,
            })
            .IsUnique();
        b.Entity<JeopardyQuestion>()
            .HasIndex(x => new
            {
                x.Board,
                x.BoardRound,
                x.Category,
                x.Value,
            })
            .IsUnique();
        b.Entity<JeopardyPlay>().HasIndex(x => new { x.SessionId, x.QuestionId }).IsUnique();
        b.Entity<ImposterClue>()
            .HasIndex(x => new
            {
                x.SessionId,
                x.Round,
                x.TurnIndex,
            })
            .IsUnique();
        b.Entity<QuestionUse>().HasIndex(x => new { x.SessionId, x.QuestionId }).IsUnique();
        b.Entity<RoundParticipant>()
            .HasIndex(x => new
            {
                x.SessionId,
                x.Game,
                x.Round,
                x.PlayerId,
            })
            .IsUnique();
    }
}
