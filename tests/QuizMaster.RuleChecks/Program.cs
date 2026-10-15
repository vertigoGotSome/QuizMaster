using QuizMaster.Api.Features.Game;
using QuizMaster.Data;

int checks = 0;
void Check(bool value, string message)
{
    if (!value)
        throw new Exception(message);
    checks++;
}
var imposterOrder = new[] { 3, 1, 2 };
var clueCount = 0;
Check(!GameService.CanChangePhase("imposter", "clues", "vote", 3), "Voting opened early");
foreach (var id in new[] { 3, 1, 2, 3, 1, 2 })
{
    Check(
        GameService.ImposterCurrentSpeaker(imposterOrder, clueCount) == id,
        "Speaker order changed"
    );
    clueCount++;
}
Check(
    GameService.ImposterCurrentSpeaker(imposterOrder, clueCount) == null,
    "Extra clue turn offered"
);
Check(GameService.ImposterPass(imposterOrder, clueCount) == 2, "Wrong final pass");
Check(
    GameService.ImposterCurrentSpeaker(Array.Empty<int>(), 0) == null,
    "Empty Imposter round has a speaker"
);
Check(GameService.ImposterPass(Array.Empty<int>(), 0) == 0, "Empty Imposter round has a pass");
Check(GameService.ImposterCurrentSpeaker(imposterOrder, 0) == 3, "First persisted turn is wrong");
Check(
    GameService.ImposterCurrentSpeaker(imposterOrder, 3) == 3,
    "Second persisted pass did not restart order"
);
Check(GameService.ImposterPass(imposterOrder, 0) == 1, "First clue pass is wrong");
Check(GameService.ImposterPass(imposterOrder, 3) == 2, "Second clue pass is wrong");
Check(GameService.ImposterCurrentSpeaker(imposterOrder, 5) == 2, "Last speaker is wrong");
Check(
    GameService.ImposterCurrentSpeaker(imposterOrder, 6) == null,
    "Completed persisted clues still have a speaker"
);
Check(
    GameService.CanChangePhase("imposter", "clues", "vote", null),
    "Completed clues cannot advance"
);
Check(!GameService.CanChangePhase("imposter", "lobby", "reveal", null), "Reveal can bypass voting");
Check(
    !GameService.CanChangePhase("wwm", "results", "question", null),
    "WWM can bypass start-question"
);
Check(GameService.CanChangePhase("wwm", "question", "answers", null), "Host cannot close answers");
var questions = new[]
{
    new Question { Id = 1, Difficulty = 1 },
    new Question { Id = 2, Difficulty = 1 },
    new Question { Id = 3, Difficulty = 9 },
};
var used = new HashSet<int>();
var first = GameService.ChooseQuestion(questions, used, 1)!;
used.Add(first.Id);
var second = GameService.ChooseQuestion(questions, used, 1)!;
used.Add(second.Id);
Check(
    first.Id != second.Id && first.Difficulty == 1 && second.Difficulty == 1,
    "Question repeats or ignores difficulty"
);
var final = GameService.ChooseQuestion(questions, used, 1)!;
used.Add(final.Id);
Check(final.Id == 3, "Remaining question was lost");
Check(GameService.ChooseQuestion(questions, used, 1) == null, "Exhausted pool repeats");
Check(
    GameService.ChooseQuestion(questions, new HashSet<int> { 1, 2 }, 1)?.Id == 3,
    "Persisted question history was ignored"
);
Check(
    GameService.ChooseQuestion(questions, new HashSet<int> { 1, 2, 3 }, 1) == null,
    "Fully persisted question history repeats"
);
Check(GameService.AnswerRank(1, 2, "A") == 2047868925u, "50:50 ordering is not stable across runs");
foreach (var correct in new[] { "A", "B", "C", "D" })
{
    var removed = GameService.RemovedAnswers(1, 2, correct);
    Check(
        removed.Length == 2 && removed.Distinct().Count() == 2 && !removed.Contains(correct),
        "50:50 removed the solution"
    );
}
Check(GameService.JeopardyValue(200, 1) == 200, "Jeopardy round one value changed");
Check(GameService.JeopardyWrongLoss(200, 1) == -100, "Jeopardy wrong loss is not half");
Check(GameService.JeopardyStealGain(200, 1) == 100, "Jeopardy steal gain is not half");
Check(GameService.JeopardyValue(500, 2) == 1000, "Jeopardy second round did not double");
Check(GameService.JeopardyWrongLoss(500, 2) == -500, "Jeopardy doubled wrong loss is incorrect");
Check(GameService.JeopardyStealGain(500, 2) == 500, "Jeopardy doubled steal is incorrect");
Check(
    GameService.WwmRoundValue(1) == 1 && GameService.WwmRoundValue(30) == 30,
    "WWM round values are not 1 through 30"
);
Check(
    GameService.WwmScoreDelta(2, true, false) == 2,
    "WWM second question should award two points"
);
Check(GameService.WwmScoreDelta(3, true, true) == 6, "WWM risk should double correct points");
Check(
    GameService.WwmScoreDelta(3, false, true) == -6,
    "WWM risk should double the wrong-answer penalty"
);
Console.WriteLine($"{checks} rule checks passed.");
