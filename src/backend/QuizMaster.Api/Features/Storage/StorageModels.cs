namespace QuizMaster.Api.Features.Storage;

/// <summary>Validated word payload used by the host JSON import endpoint.</summary>
public sealed record WordInput(string Category, string Word, string? Hint);

/// <summary>Validated multiple-choice payload used by the host JSON import endpoint.</summary>
public sealed record QuestionInput(
    string Category,
    int Difficulty,
    string Text,
    string AnswerA,
    string AnswerB,
    string AnswerC,
    string AnswerD,
    string Correct
);
