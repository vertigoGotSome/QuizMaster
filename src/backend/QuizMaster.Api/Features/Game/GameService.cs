using QuizMaster.Data;

namespace QuizMaster.Api.Features.Game;

/// <summary>
/// Pure, server-authoritative scoring and phase rules shared by game endpoint modules.
/// These methods have no HTTP or database dependencies, which keeps their behavior testable.
/// </summary>
internal static class GameService
{
    /// <summary>Returns the visible Jeopardy cell value for the selected board round.</summary>
    public static int JeopardyValue(int baseValue, int boardRound) =>
        Math.Clamp(baseValue, 50, 5000) * Math.Clamp(boardRound, 1, 2);

    /// <summary>Returns the initial-answer penalty: half of the cell value.</summary>
    public static int JeopardyWrongLoss(int baseValue, int boardRound) =>
        -JeopardyValue(baseValue, boardRound) / 2;

    /// <summary>Returns the optional steal reward: half of the cell value.</summary>
    public static int JeopardyStealGain(int baseValue, int boardRound) =>
        JeopardyValue(baseValue, boardRound) / 2;

    /// <summary>WWM rises one point per question, from one through thirty.</summary>
    public static int WwmRoundValue(int round) => Math.Clamp(round, 1, 30);

    /// <summary>Risk doubles either the awarded points or the penalty.</summary>
    public static int WwmScoreDelta(int round, bool correct, bool risk)
    {
        var value = WwmRoundValue(round);
        return correct ? value * (risk ? 2 : 1)
            : risk ? -value * 2
            : 0;
    }

    /// <summary>Returns the next Imposter clue giver across two complete passes.</summary>
    public static int? ImposterCurrentSpeaker(IReadOnlyList<int> orderedPlayerIds, int clueCount) =>
        orderedPlayerIds.Count == 0 || clueCount >= orderedPlayerIds.Count * 2
            ? null
            : orderedPlayerIds[clueCount % orderedPlayerIds.Count];

    /// <summary>Returns the one-based Imposter clue pass number, capped at two.</summary>
    public static int ImposterPass(IReadOnlyList<int> orderedPlayerIds, int clueCount) =>
        orderedPlayerIds.Count == 0 ? 0 : Math.Min(2, clueCount / orderedPlayerIds.Count + 1);

    /// <summary>Captures the selected WWM question and how far it deviates from the target level.</summary>
    public sealed record WwmQuestionSelection(Question Question, int TargetDifficulty, int Distance);

    /// <summary>
    /// Selects an unused question nearest to the requested difficulty and records the fallback distance.
    /// </summary>
    public static WwmQuestionSelection? ChooseQuestionWithDiagnostics(
        IReadOnlyCollection<Question> questions,
        IReadOnlySet<int> usedQuestionIds,
        int difficulty
    )
    {
        var selected = ChooseQuestion(questions, usedQuestionIds, difficulty);
        return selected is null
            ? null
            : new WwmQuestionSelection(selected, difficulty, Math.Abs(selected.Difficulty - difficulty));
    }

    /// <summary>
    /// Selects an unused question nearest to the requested difficulty and randomizes only among equals.
    /// </summary>
    public static Question? ChooseQuestion(
        IReadOnlyCollection<Question> questions,
        IReadOnlySet<int> usedQuestionIds,
        int difficulty
    )
    {
        var available = questions.Where(q => !usedQuestionIds.Contains(q.Id)).ToArray();
        if (available.Length == 0)
        {
            return null;
        }

        var distance = available.Min(q => Math.Abs(q.Difficulty - difficulty));
        var matches = available
            .Where(q => Math.Abs(q.Difficulty - difficulty) == distance)
            .ToArray();
        return matches[Random.Shared.Next(matches.Length)];
    }

    /// <summary>Returns two deterministic incorrect WWM choices for the 50:50 joker.</summary>
    public static string[] RemovedAnswers(int round, int playerId, string correct) =>
        new[] { "A", "B", "C", "D" }
            .Where(answer => answer != correct)
            .OrderBy(answer => AnswerRank(round, playerId, answer))
            .Take(2)
            .ToArray();

    /// <summary>Checks the small set of host phase transitions that are legal outside dedicated commands.</summary>
    public static bool CanChangePhase(string game, string current, string target, int? speaker) =>
        target == "lobby"
        || (game == "imposter" && current == "clues" && target == "vote" && speaker is null)
        || (game == "wwm" && current == "question" && target == "answers");

    /// <summary>
    /// Calculates a stable answer ordering across process restarts.
    /// HashCode.Combine is intentionally avoided because its string hash is randomized per process.
    /// </summary>
    public static uint AnswerRank(int round, int playerId, string answer)
    {
        uint hash = 2166136261;
        foreach (char c in $"{round}:{playerId}:{answer}")
        {
            hash = unchecked((hash ^ c) * 16777619);
        }

        return hash;
    }
}
