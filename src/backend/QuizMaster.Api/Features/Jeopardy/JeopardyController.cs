using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Features.Game;
using QuizMaster.Api.Features.Jeopardy;
using QuizMaster.Api.Infrastructure.Realtime;
using QuizMaster.Data;

namespace QuizMaster.Api.Features.Jeopardy;

/// <summary>Server-authoritative Jeopardy board, moderation and scoring routes.</summary>
public static class JeopardyController
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static void MapJeopardyController(this WebApplication app)
    {
        app.MapPost(
                "/api/host/jeopardy/images",
                async (IFormFile image, IWebHostEnvironment environment) =>
                {
                    if (image.Length is < 1 or > 5_000_000)
                        return Results.BadRequest("Use an image up to 5 MB.");
                    var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
                    if (!allowed.Contains(image.ContentType, StringComparer.OrdinalIgnoreCase))
                        return Results.BadRequest("Use JPG, PNG, WebP or GIF.");
                    var extension = image.ContentType.ToLowerInvariant() switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/webp" => ".webp",
                        _ => ".gif",
                    };
                    var directory = Path.Combine(
                        environment.WebRootPath
                            ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
                        "uploads",
                        "jeopardy"
                    );
                    Directory.CreateDirectory(directory);
                    var name = $"{Guid.NewGuid():N}{extension}";
                    await using var stream = File.Create(Path.Combine(directory, name));
                    await image.CopyToAsync(stream);
                    return Results.Ok(new { url = $"/uploads/jeopardy/{name}" });
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);

        app.MapGet(
                "/api/jeopardy/board",
                async (GameDbContext db) =>
                {
                    var session = await db.Sessions.FirstAsync();
                    var selected = SelectedCategories(session);
                    var plays = await db
                        .JeopardyPlays.Where(x => x.SessionId == session.Id)
                        .ToDictionaryAsync(x => x.QuestionId);
                    if (session.Game != "jeopardy" || selected.Count == 0)
                        return Results.Ok(Array.Empty<object>());
                    var cells = await db
                        .JeopardyQuestions.Where(x => x.BoardRound == session.JeopardyRound)
                        .ToListAsync();
                    var ordered = selected.SelectMany(
                        (slot, index) =>
                            cells
                                .Where(x => x.Board == slot.Board && x.Category == slot.Category)
                                .OrderBy(x => x.Value)
                                .Select(x => new { Cell = x, Slot = index })
                    );
                    return Results.Ok(
                        ordered.Select(item => new
                        {
                            item.Cell.Id,
                            item.Cell.Board,
                            item.Cell.BoardRound,
                            item.Cell.Category,
                            item.Cell.Value,
                            item.Cell.Difficulty,
                            item.Slot,
                            state = plays.TryGetValue(item.Cell.Id, out var play)
                                ? play.State
                                : "available",
                        })
                    );
                }
            )
            .RequireAuthorization();

        app.MapGet(
                "/api/host/jeopardy/catalog",
                async (GameDbContext db) =>
                {
                    var cells = await db.JeopardyQuestions.AsNoTracking().ToListAsync();
                    return Results.Ok(
                        cells
                            .GroupBy(x => new { x.Board, x.Category })
                            .Where(g =>
                                g.Count(x => x.BoardRound == 1) >= 5
                                && g.Count(x => x.BoardRound == 2) >= 5
                            )
                            .OrderBy(g => g.Key.Board)
                            .ThenBy(g => g.Key.Category)
                            .Select(g => new { g.Key.Board, g.Key.Category })
                    );
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/configure",
                async (ConfigureBoardRequest request, GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    if (
                        request.Round is < 1 or > 2
                        || request.Categories is null
                        || request.Categories.Count != 6
                    )
                        return Results.BadRequest("Choose exactly six categories and a round.");
                    var selected = request
                        .Categories.Select(x => new JeopardyCategoryChoice(
                            x.Board?.Trim() ?? "",
                            x.Category?.Trim() ?? ""
                        ))
                        .ToList();
                    if (
                        selected.Any(x =>
                            string.IsNullOrWhiteSpace(x.Board)
                            || string.IsNullOrWhiteSpace(x.Category)
                        )
                        || selected.Distinct().Count() != 6
                    )
                        return Results.BadRequest("Each category must be unique.");
                    var cells = await db
                        .JeopardyQuestions.Where(x => x.BoardRound == request.Round)
                        .ToListAsync();
                    if (
                        selected.Any(slot =>
                            cells.Count(x => x.Board == slot.Board && x.Category == slot.Category)
                            < 5
                        )
                    )
                        return Results.BadRequest(
                            "Every selected category needs five fields in this round."
                        );
                    if (selected.Any(slot => !HasProgressiveDifficulty(cells, slot)))
                        return Results.BadRequest(
                            "Every selected category needs five cells with strictly increasing difficulty."
                        );
                    var session = await db.Sessions.FirstAsync();
                    if (session.Game != "jeopardy" || session.Phase is not ("board" or "setup"))
                        return Results.Conflict("Select Jeopardy before configuring its board.");
                    // Replacing a partly used board would silently strand fields and confuse every player.
                    // A fresh board is allowed before the first field and after the previous board is complete.
                    var previous = SelectedCategories(session);
                    var previousBoardFinished = false;
                    if (previous.Count > 0)
                    {
                        var previousRoundCells = await db
                            .JeopardyQuestions.Where(x => x.BoardRound == session.JeopardyRound)
                            .Select(x => new
                            {
                                x.Id,
                                x.Board,
                                x.Category,
                            })
                            .ToListAsync();
                        var previousCells = previousRoundCells
                            .Where(x =>
                                previous.Any(choice =>
                                    choice.Board == x.Board && choice.Category == x.Category
                                )
                            )
                            .Select(x => x.Id)
                            .ToList();
                        if (previousCells.Count > 0)
                        {
                            var playedIds = await db
                                .JeopardyPlays.Where(x => x.SessionId == session.Id)
                                .Select(x => x.QuestionId)
                                .ToListAsync();
                            if (previousCells.Any(id => !playedIds.Contains(id)))
                                return Results.Conflict(
                                    "Finish the current Jeopardy board before configuring the next one."
                                );
                            previousBoardFinished = true;
                        }
                    }
                    session.JeopardyRound = request.Round;
                    session.JeopardyCategoriesJson = JsonSerializer.Serialize(selected);
                    var players = await db
                        .Players.Where(x =>
                            !x.IsHost && x.LastSeenUtc > DateTime.UtcNow.AddMinutes(-5)
                        )
                        .OrderBy(x => x.Id)
                        .Select(x => new
                        {
                            x.Id,
                            x.Team,
                            x.Points,
                        })
                        .ToListAsync();
                    if (players.Count > 0)
                    {
                        if (previousBoardFinished)
                        {
                            // Board two starts with the trailing team. Members of the selected team share
                            // the opening turn, while the normal player-by-player rotation continues afterwards.
                            var teamGroups = players
                                .Where(x => !string.IsNullOrWhiteSpace(x.Team))
                                .GroupBy(x => x.Team.Trim())
                                .Select(group => new
                                {
                                    Players = group.OrderBy(x => x.Id).ToList(),
                                    Points = group.Sum(x => x.Points),
                                })
                                .OrderBy(group => group.Points)
                                .ThenBy(group => group.Players[0].Id)
                                .ToList();
                            var starters =
                                teamGroups.Count >= 2
                                    ? teamGroups[0].Players
                                    : players
                                        .Where(x =>
                                            x.Points == players.Min(player => player.Points)
                                        )
                                        .ToList();
                            session.SpeakerIndex = starters[Random.Shared.Next(starters.Count)].Id;
                        }
                        else if (session.Round == 0)
                        {
                            session.SpeakerIndex = players[Random.Shared.Next(players.Count)].Id;
                        }
                    }
                    session.Phase = "board";
                    session.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapGet(
                "/api/host/jeopardy/questions",
                async (GameDbContext db) =>
                    Results.Ok(
                        await db
                            .JeopardyQuestions.OrderBy(x => x.Board)
                            .ThenBy(x => x.BoardRound)
                            .ThenBy(x => x.Category)
                            .ThenBy(x => x.Value)
                            .ToListAsync()
                    )
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/questions",
                async (JeopardyQuestion input, GameDbContext db) =>
                {
                    var error = Validate(input);
                    if (error is not null)
                        return Results.BadRequest(error);
                    input.Id = 0;
                    Normalize(input);
                    db.JeopardyQuestions.Add(input);
                    await db.SaveChangesAsync();
                    return Results.Ok(input);
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/questions/import",
                async (List<JeopardyQuestion> items, GameDbContext db) =>
                {
                    if (items is null || items.Count is < 1 or > 1000)
                        return Results.BadRequest("Provide 1â€“1000 Jeopardy cells.");
                    var existing = (await db.JeopardyQuestions.AsNoTracking().ToListAsync())
                        .Select(x =>
                            $"{x.Board.Trim().ToUpperInvariant()}|{x.BoardRound}|{x.Category.Trim().ToUpperInvariant()}|{x.Value}"
                        )
                        .ToHashSet();
                    var inserted = 0;
                    var skipped = 0;
                    var warnings = new HashSet<string>();
                    foreach (var item in items)
                    {
                        if (Validate(item) is not null)
                        {
                            skipped++;
                            continue;
                        }
                        var expectedDifficulty = ExpectedDifficulty(item.Value, item.BoardRound);
                        if (item.Difficulty != 0 && item.Difficulty != expectedDifficulty)
                            warnings.Add(
                                $"{item.Category} · {item.Value}: declared difficulty {item.Difficulty}, expected {expectedDifficulty}."
                            );
                        Normalize(item);
                        var key =
                            $"{item.Board.ToUpperInvariant()}|{item.BoardRound}|{item.Category.ToUpperInvariant()}|{item.Value}";
                        if (!existing.Add(key))
                        {
                            skipped++;
                            continue;
                        }
                        item.Id = 0;
                        db.JeopardyQuestions.Add(item);
                        inserted++;
                    }
                    await db.SaveChangesAsync();
                    return Results.Ok(new { inserted, skipped, warnings = warnings.Order().ToArray() });
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapPut(
                "/api/host/jeopardy/questions/{id:int}",
                async (int id, JeopardyQuestion input, GameDbContext db) =>
                {
                    var stored = await db.JeopardyQuestions.FindAsync(id);
                    if (stored is null)
                        return Results.NotFound();
                    if (await db.JeopardyPlays.AnyAsync(x => x.QuestionId == id))
                        return Results.Conflict("A played Jeopardy cell cannot be edited.");
                    var error = Validate(input);
                    if (error is not null)
                        return Results.BadRequest(error);
                    input.Id = id;
                    Normalize(input);
                    db.Entry(stored).CurrentValues.SetValues(input);
                    await db.SaveChangesAsync();
                    return Results.Ok(stored);
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);

        app.MapDelete(
                "/api/host/jeopardy/questions/{id:int}",
                async (int id, GameDbContext db) =>
                {
                    var cell = await db.JeopardyQuestions.FindAsync(id);
                    if (cell is null)
                        return Results.NotFound();
                    if (await db.JeopardyPlays.AnyAsync(x => x.QuestionId == id))
                        return Results.Conflict("A played Jeopardy cell cannot be deleted.");
                    db.JeopardyQuestions.Remove(cell);
                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);

        app.MapDelete(
                "/api/host/jeopardy/questions",
                async (GameDbContext db) =>
                {
                    var session = await db.Sessions.FirstAsync();
                    if (session.Game == "jeopardy" && session.JeopardyQuestionId is not null)
                        return Results.Conflict("Close the active Jeopardy question first.");
                    db.JeopardyQuestions.RemoveRange(db.JeopardyQuestions);
                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/open",
                async (OpenRequest request, GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var session = await db.Sessions.FirstAsync();
                    if (session.Game != "jeopardy" || session.Phase != "board")
                        return Results.Conflict("Start Jeopardy from its board.");
                    var cell = await db.JeopardyQuestions.FindAsync(request.QuestionId);
                    if (cell is null)
                        return Results.NotFound();
                    if (
                        cell.BoardRound != session.JeopardyRound
                        || !SelectedCategories(session)
                            .Any(x => x.Board == cell.Board && x.Category == cell.Category)
                    )
                        return Results.Conflict("This cell is not on the configured board.");
                    if (
                        await db.JeopardyPlays.AnyAsync(x =>
                            x.SessionId == session.Id && x.QuestionId == cell.Id
                        )
                    )
                        return Results.Conflict("This cell was already played.");
                    var activePlayers = await db
                        .Players.Where(x =>
                            !x.IsHost && x.LastSeenUtc > DateTime.UtcNow.AddMinutes(-5)
                        )
                        .OrderBy(x => x.Id)
                        .Select(x => x.Id)
                        .ToListAsync();
                    if (activePlayers.Count == 0)
                        return Results.BadRequest("No active player.");
                    if (!activePlayers.Contains(session.SpeakerIndex))
                        session.SpeakerIndex = activePlayers[
                            Random.Shared.Next(activePlayers.Count)
                        ];
                    var contestantId = session.SpeakerIndex;
                    session.SpeakerIndex = activePlayers[
                        (activePlayers.IndexOf(contestantId) + 1) % activePlayers.Count
                    ];
                    session.JeopardyQuestionId = cell.Id;
                    session.Phase = "question";
                    session.TimerEndsUtc = null;
                    session.Round++;
                    session.Version++;
                    db.JeopardyPlays.Add(
                        new JeopardyPlay
                        {
                            SessionId = session.Id,
                            QuestionId = cell.Id,
                            ContestantId = contestantId,
                            State = "question",
                        }
                    );
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/timer",
                async (TimerRequest request, GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var session = await db.Sessions.FirstAsync();
                    if (
                        session.Game != "jeopardy"
                        || session.JeopardyQuestionId is null
                        || session.Phase is not ("question" or "steal")
                    )
                        return Results.Conflict("There is no Jeopardy answer phase to time.");
                    session.TimerEndsUtc = request.Seconds is null
                        ? null
                        : DateTime.UtcNow.AddSeconds(Math.Clamp(request.Seconds.Value, 5, 120));
                    session.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(new { session.TimerEndsUtc });
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/correct",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var state = await CurrentPlay(db);
                    if (state.Error is not null)
                        return state.Error;
                    var (session, cell, play) = state.Value!.Value;
                    if (session.Phase != "question" || play.ContestantId is null)
                        return Results.Conflict("The initial answer is not open.");
                    var gain = GameService.JeopardyValue(cell.Value, cell.BoardRound);
                    await ChangePoints(play.ContestantId.Value, gain, "Jeopardy correct", db);
                    play.ScoreDelta = gain;
                    Close(session, play);
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/wrong",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var state = await CurrentPlay(db);
                    if (state.Error is not null)
                        return state.Error;
                    var (session, cell, play) = state.Value!.Value;
                    if (session.Phase != "question" || play.ContestantId is null)
                        return Results.Conflict("The initial answer is not open.");
                    var loss = GameService.JeopardyWrongLoss(cell.Value, cell.BoardRound);
                    await ChangePoints(play.ContestantId.Value, loss, "Jeopardy wrong", db);
                    play.ScoreDelta = loss;
                    play.State = "steal";
                    session.Phase = "steal";
                    session.TimerEndsUtc = null;
                    session.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/jeopardy/buzz",
                async (ClaimsPrincipal user, GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var state = await CurrentPlay(db);
                    if (state.Error is not null)
                        return state.Error;
                    var (session, cell, play) = state.Value!.Value;
                    var playerId = int.Parse(user.FindFirstValue("player_id")!);
                    if (session.Phase != "steal" || play.ContestantId == playerId)
                        return Results.Conflict("Buzzer is not open for you.");
                    if (play.StealContestantId is not null)
                        return Results.Conflict("Another player already buzzed first.");
                    if (!await IsActiveNonHostPlayer(playerId, db))
                        return Results.Forbid();
                    play.StealContestantId = playerId;
                    session.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized);

        app.MapPost(
                "/api/host/jeopardy/steal-correct",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var state = await CurrentPlay(db);
                    if (state.Error is not null)
                        return state.Error;
                    var (session, cell, play) = state.Value!.Value;
                    if (session.Phase != "steal" || play.StealContestantId is null)
                        return Results.Conflict("Wait for a player to buzz.");
                    var gain = GameService.JeopardyStealGain(cell.Value, cell.BoardRound);
                    await ChangePoints(play.StealContestantId.Value, gain, "Jeopardy steal", db);
                    play.StealScoreDelta = gain;
                    Close(session, play);
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/jeopardy/steal-wrong",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var state = await CurrentPlay(db);
                    if (state.Error is not null)
                        return state.Error;
                    var (session, cell, play) = state.Value!.Value;
                    if (session.Phase != "steal" || play.StealContestantId is null)
                        return Results.Conflict("Wait for a player to buzz.");
                    var loss = GameService.JeopardyWrongLoss(cell.Value, cell.BoardRound);
                    await ChangePoints(
                        play.StealContestantId.Value,
                        loss,
                        "Jeopardy steal wrong",
                        db
                    );
                    play.StealScoreDelta = loss;
                    Close(session, play);
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);

        // A steal is optional: the host can close immediately, including before a timer expires.
        app.MapPost(
                "/api/host/jeopardy/close",
                async (CloseRequest request, GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var state = await CurrentPlay(db);
                    if (state.Error is not null)
                        return state.Error;
                    var (session, _, play) = state.Value!.Value;
                    if (session.Phase is not ("question" or "steal"))
                        return Results.Conflict("This cell is already closed.");
                    play.State = request.Skip ? "skipped" : "closed";
                    Close(session, play);
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(Serialized)
            .AddEndpointFilter(HostOnly);
    }

    private static async Task<bool> IsActiveNonHostPlayer(int id, GameDbContext db) =>
        await db.Players.AnyAsync(x =>
            x.Id == id && !x.IsHost && x.LastSeenUtc > DateTime.UtcNow.AddMinutes(-5)
        );

    private static async Task ChangePoints(int playerId, int delta, string reason, GameDbContext db)
    {
        var player = await db.Players.FindAsync(playerId);
        if (player is null)
            return;
        player.Points += delta;
        var session = await db.Sessions.FirstAsync();
        db.ScoreEvents.Add(
            new ScoreEvent
            {
                SessionId = session.Id,
                Round = session.Round,
                PlayerId = playerId,
                Delta = delta,
                Reason = reason,
            }
        );
    }

    private static void Close(GameSession session, JeopardyPlay play)
    {
        play.State = play.State == "skipped" ? "skipped" : "closed";
        play.ClosedUtc = DateTime.UtcNow;
        session.JeopardyQuestionId = null;
        session.TimerEndsUtc = null;
        session.Phase = "board";
        session.Version++;
    }

    private static async Task<(
        IResult? Error,
        (GameSession, JeopardyQuestion, JeopardyPlay)? Value
    )> CurrentPlay(GameDbContext db)
    {
        var session = await db.Sessions.FirstAsync();
        if (session.Game != "jeopardy" || session.JeopardyQuestionId is null)
            return (Results.Conflict("There is no active Jeopardy cell."), null);
        var cell = await db.JeopardyQuestions.FindAsync(session.JeopardyQuestionId);
        var play = cell is null
            ? null
            : await db.JeopardyPlays.SingleOrDefaultAsync(x =>
                x.SessionId == session.Id && x.QuestionId == cell.Id
            );
        return cell is null || play is null
            ? (Results.NotFound(), null)
            : (null, (session, cell, play));
    }

    private static List<JeopardyCategoryChoice> SelectedCategories(GameSession session)
    {
        try
        {
            return JsonSerializer.Deserialize<List<JeopardyCategoryChoice>>(
                    session.JeopardyCategoriesJson
                ) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>Maps the visible board position to the 1–10 authoring difficulty scale.</summary>
    private static int ExpectedDifficulty(int value, int boardRound)
    {
        var valueTier = Math.Clamp((int)Math.Ceiling(value / 100d), 1, 5);
        return Math.Clamp(valueTier + (boardRound - 1) * 5, 1, 10);
    }

    /// <summary>Requires a category to become strictly more challenging down its visible column.</summary>
    private static bool HasProgressiveDifficulty(
        IEnumerable<JeopardyQuestion> cells,
        JeopardyCategoryChoice slot
    )
    {
        var ordered = cells
            .Where(cell => cell.Board == slot.Board && cell.Category == slot.Category)
            .OrderBy(cell => cell.Value)
            .Take(5)
            .ToArray();
        return ordered.Length == 5
            && ordered.Zip(ordered.Skip(1), (left, right) => left.Difficulty < right.Difficulty).All(x => x);
    }

    private static string? Validate(JeopardyQuestion input)
    {
        if (
            string.IsNullOrWhiteSpace(input.Board)
            || input.Board.Length > 80
            || string.IsNullOrWhiteSpace(input.Category)
            || input.Category.Length > 80
        )
            return "Board and category are required and must be at most 80 characters.";
        if (
            input.BoardRound is < 1 or > 2
            || input.Value is < 50 or > 5000
            || input.Difficulty is < 0 or > 10
            || string.IsNullOrWhiteSpace(input.Clue)
            || input.Clue.Length > 1600
            || string.IsNullOrWhiteSpace(input.Answer)
            || input.Answer.Length > 800
        )
            return "Check round, value, difficulty, clue and answer.";
        if (
            input.ImageUrl.Length > 500
            || input.AcceptedAnswers.Length > 1000
            || input.HostNotes.Length > 1600
        )
            return "One field is too long.";
        return null;
    }

    private static void Normalize(JeopardyQuestion input)
    {
        input.Board = input.Board.Trim();
        input.Category = input.Category.Trim();
        if (input.Difficulty == 0)
            input.Difficulty = ExpectedDifficulty(input.Value, input.BoardRound);
        input.Clue = input.Clue.Trim();
        input.Answer = input.Answer.Trim();
        input.AcceptedAnswers = input.AcceptedAnswers.Trim();
        input.HostNotes = input.HostNotes.Trim();
        input.ImageUrl = input.ImageUrl.Trim();
    }

    private static async ValueTask<object?> Serialized(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next
    )
    {
        await Gate.WaitAsync(ctx.HttpContext.RequestAborted);
        try
        {
            return await next(ctx);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static ValueTask<object?> HostOnly(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next
    )
    {
        var hostId = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>()[
            "Discord:HostUserId"
        ];
        return
            !string.IsNullOrWhiteSpace(hostId)
            && ctx.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) == hostId
            ? next(ctx)
            : ValueTask.FromResult<object?>(Results.Forbid());
    }
}
