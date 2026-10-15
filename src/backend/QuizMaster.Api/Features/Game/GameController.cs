using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Features.Jeopardy;
using QuizMaster.Api.Features.Storage;
using QuizMaster.Api.Infrastructure.Realtime;
using QuizMaster.Data;

namespace QuizMaster.Api.Features.Game;

/// <summary>HTTP routes for player identity, content, and host-only game controls.</summary>
public static class GameController
{
    /// <summary>Register all QuizMaster API and realtime endpoints.</summary>
    public static void MapGameController(this WebApplication app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "QuizMaster" }));
        app.MapGet(
            "/api/auth/login",
            (HttpContext http, IConfiguration configuration) =>
            {
                var configuredOrigin = configuration["Hosting:FrontendOrigin"]?.TrimEnd('/');
                var returnUri =
                    Uri.TryCreate(configuredOrigin, UriKind.Absolute, out var configured)
                    && configured.Scheme is "http" or "https"
                        ? $"{configuredOrigin}/"
                        : $"{http.Request.Scheme}://{http.Request.Host}/";
                return Results.Challenge(
                    new AuthenticationProperties { RedirectUri = returnUri },
                    new[] { "Discord" }
                );
            }
        );
        app.MapPost(
                "/api/auth/logout",
                async (HttpContext http) =>
                {
                    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Ok();
                }
            )
            .RequireAuthorization();
        app.MapGet(
            "/api/auth/me",
            async (ClaimsPrincipal user, GameDbContext db) =>
            {
                if (user.Identity?.IsAuthenticated != true)
                    return Results.Unauthorized();
                var id = int.Parse(user.FindFirstValue("player_id")!);
                var p = await db.Players.FindAsync(id);
                return p == null
                    ? Results.Unauthorized()
                    : Results.Ok(
                        new
                        {
                            p.Id,
                            p.Name,
                            p.AvatarUrl,
                            p.IsHost,
                            p.Points,
                            p.Team,
                        }
                    );
            }
        );
        app.MapGet(
                "/api/lobby",
                async (GameDbContext db) =>
                    Results.Ok(
                        new
                        {
                            players = await db
                                .Players.Where(x => x.LastSeenUtc > DateTime.UtcNow.AddMinutes(-5))
                                .Select(x => new
                                {
                                    x.Id,
                                    x.Name,
                                    x.AvatarUrl,
                                    x.IsHost,
                                    x.Points,
                                    x.Team,
                                })
                                .ToListAsync(),
                            session = PublicSession(await db.Sessions.FirstAsync()),
                        }
                    )
            )
            .RequireAuthorization();
        app.MapPost(
                "/api/lobby/heartbeat",
                async (ClaimsPrincipal user, GameDbContext db) =>
                {
                    var p = await db.Players.FindAsync(
                        int.Parse(user.FindFirstValue("player_id")!)
                    );
                    if (p == null)
                        return Results.Unauthorized();
                    p.LastSeenUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }
            )
            .RequireAuthorization();
        app.MapGet(
                "/api/games/words",
                async (GameDbContext db) =>
                    Results.Ok(await db.Words.Select(w => new { w.Id, w.Category }).ToListAsync())
            )
            .RequireAuthorization();
        app.MapGet(
                "/api/games/questions",
                async (GameDbContext db) =>
                    Results.Ok(
                        await db
                            .Questions.Select(q => new
                            {
                                q.Id,
                                q.Category,
                                q.Difficulty,
                                q.Text,
                            })
                            .ToListAsync()
                    )
            )
            .RequireAuthorization();
        app.MapGet(
                "/api/host/questions",
                async (GameDbContext db) => Results.Ok(await db.Questions.ToListAsync())
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);
        app.MapGet(
                "/api/host/words",
                async (GameDbContext db) =>
                    await StorageService.IsBlindWordAccessBlockedAsync(db)
                        ? Results.Conflict("Word storage is locked while the host plays blind.")
                        : Results.Ok(await db.Words.ToListAsync())
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/questions",
                async (Question input, GameDbContext db) =>
                {
                    input.Id = 0;
                    db.Questions.Add(input);
                    await db.SaveChangesAsync();
                    return Results.Ok(input);
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/words",
                async (SecretWord input, GameDbContext db) =>
                {
                    input.Id = 0;
                    db.Words.Add(input);
                    await db.SaveChangesAsync();
                    return Results.Ok(input);
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/game",
                async (
                    SetGame request,
                    GameDbContext db,
                    Microsoft.AspNetCore.SignalR.IHubContext<GameHub> hub
                ) =>
                {
                    if (request.Game is not ("imposter" or "wwm" or "jeopardy" or "none"))
                        return Results.BadRequest();
                    var s = await db.Sessions.FirstAsync();
                    if (s.Game != request.Game && s.Game != "none" && !request.Confirm)
                        return Results.Conflict(
                            "Confirm ending the active game before changing modes."
                        );
                    if (s.Game == request.Game)
                    {
                        if (request.Game == "jeopardy" && s.Phase == "lobby")
                        {
                            s.Phase = "board";
                            s.Version++;
                            await db.SaveChangesAsync();
                            await hub.Clients.All.SendAsync("changed");
                        }
                        return Results.Ok(PublicSession(s));
                    }
                    s.Game = request.Game;
                    db.Answers.RemoveRange(db.Answers);
                    db.Roles.RemoveRange(db.Roles);
                    db.Votes.RemoveRange(db.Votes);
                    db.ImposterClues.RemoveRange(db.ImposterClues);
                    db.QuestionUses.RemoveRange(db.QuestionUses.Where(x => x.SessionId == s.Id));
                    db.RoundParticipants.RemoveRange(
                        db.RoundParticipants.Where(x => x.SessionId == s.Id)
                    );
                    s.Phase = request.Game == "jeopardy" ? "board" : "lobby";
                    s.Round = 0;
                    s.QuestionIndex = 0;
                    s.Scored = false;
                    s.QuestionId = null;
                    s.WordId = null;
                    s.JeopardyQuestionId = null;
                    s.TimerEndsUtc = null;
                    s.JeopardyRound = 1;
                    s.JeopardyCategoriesJson = "[]";
                    s.Version++;
                    db.JokerUses.RemoveRange(db.JokerUses);
                    db.JeopardyPlays.RemoveRange(db.JeopardyPlays.Where(x => x.SessionId == s.Id));
                    foreach (var player in await db.Players.ToListAsync())
                        player.Points = 0;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(PublicSession(s));
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/phase",
                async (
                    SetPhase request,
                    GameDbContext db,
                    Microsoft.AspNetCore.SignalR.IHubContext<GameHub> hub
                ) =>
                {
                    if (
                        request.Phase
                        is not (
                            "lobby"
                            or "clues"
                            or "vote"
                            or "reveal"
                            or "question"
                            or "answers"
                            or "results"
                        )
                    )
                        return Results.BadRequest();
                    var s = await db.Sessions.FirstAsync();
                    if (request.Phase == "lobby" && s.Phase != "lobby" && !request.Confirm)
                        return Results.Conflict("Confirm returning everyone to the lobby.");
                    if (s.Game == "wwm" && (request.Phase == "results" || request.AdvanceRound))
                        return Results.BadRequest("Use start-question / resolve-question");
                    if (s.Game == "imposter" && request.AdvanceRound)
                        return Results.BadRequest("Use start-imposter");
                    var clueOrder =
                        s.Game == "imposter"
                            ? await db
                                .Roles.Where(r => r.SessionId == s.Id && r.Round == s.Round)
                                .OrderBy(r => r.TurnOrder)
                                .Select(r => r.PlayerId)
                                .ToListAsync()
                            : [];
                    var clueCount =
                        s.Game == "imposter"
                            ? await db.ImposterClues.CountAsync(c =>
                                c.SessionId == s.Id && c.Round == s.Round
                            )
                            : 0;
                    if (s.Game == "imposter" && request.Phase == "vote" && clueOrder.Count == 0)
                        return Results.Conflict("Start an Imposter round before voting.");
                    if (
                        !GameService.CanChangePhase(
                            s.Game,
                            s.Phase,
                            request.Phase,
                            GameService.ImposterCurrentSpeaker(clueOrder, clueCount)
                        )
                    )
                        return Results.Conflict(
                            "This phase transition is not allowed. Complete both clue passes before voting."
                        );
                    s.Phase = request.Phase;
                    s.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(PublicSession(s));
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/config",
                async (
                    GameConfig request,
                    GameDbContext db,
                    Microsoft.AspNetCore.SignalR.IHubContext<GameHub> hub
                ) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Phase != "lobby" && s.Phase != "reveal" && s.Phase != "results")
                        return Results.Conflict("Settings cannot change during an active round.");
                    s.ImposterCount = Math.Clamp(request.ImposterCount, 1, 3);
                    s.HelperWords = request.HelperWords;
                    s.HostPlaysBlind = request.HostPlaysBlind;
                    s.WordId = request.HostPlaysBlind ? null : request.WordId;
                    s.QuestionId = request.QuestionId;
                    s.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(PublicSession(s));
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/points",
                async (
                    PointChange request,
                    GameDbContext db,
                    Microsoft.AspNetCore.SignalR.IHubContext<GameHub> hub
                ) =>
                {
                    var p = await db.Players.FindAsync(request.PlayerId);
                    if (p == null)
                        return Results.NotFound();
                    if (p.IsHost)
                        return Results.BadRequest("Der Host erhält keine Spielerpunkte.");
                    if (request.Delta == 0)
                        return Results.BadRequest("Der Punktebetrag muss größer als null sein.");
                    var delta = Math.Clamp(request.Delta, -10000, 10000);
                    var reason = string.IsNullOrWhiteSpace(request.Reason)
                        ? "Host correction"
                        : $"Host correction: {request.Reason.Trim()[..Math.Min(request.Reason.Trim().Length, 80)]}";
                    p.Points += delta;
                    db.ScoreEvents.Add(
                        new ScoreEvent
                        {
                            Round = (await db.Sessions.FirstAsync()).Round,
                            PlayerId = p.Id,
                            Delta = delta,
                            Reason = reason,
                        }
                    );
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/team",
                async (
                    TeamChange request,
                    GameDbContext db,
                    Microsoft.AspNetCore.SignalR.IHubContext<GameHub> hub
                ) =>
                {
                    var p = await db.Players.FindAsync(request.PlayerId);
                    if (p == null)
                        return Results.NotFound();
                    if (p.IsHost)
                        return Results.BadRequest("Der Host wird keinem Team zugeordnet.");
                    var team = (request.Team ?? string.Empty).Trim();
                    var activePlayerCount = await db.Players.CountAsync(x =>
                        !x.IsHost && x.LastSeenUtc > DateTime.UtcNow.AddMinutes(-5)
                    );
                    var allowedTeams = TeamPalette
                        .Take(Math.Clamp(activePlayerCount, 2, TeamPalette.Length))
                        .ToHashSet(StringComparer.Ordinal);
                    if (team.Length > 32 || (team.Length > 0 && !allowedTeams.Contains(team)))
                        return Results.BadRequest(
                            "Dieses Team steht bei der aktuellen Spielerzahl nicht zur Auswahl."
                        );
                    p.Team = team;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/answers",
                async (
                    AnswerInput input,
                    ClaimsPrincipal user,
                    GameDbContext db,
                    Microsoft.AspNetCore.SignalR.IHubContext<GameHub> hub
                ) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (
                        await db.Players.AnyAsync(p =>
                            p.Id == int.Parse(user.FindFirstValue("player_id")!) && p.IsHost
                        )
                    )
                        return Results.Forbid();
                    if (
                        s.Game != "wwm"
                        || s.Phase != "question"
                        || input.Answer is not ("A" or "B" or "C" or "D")
                    )
                        return Results.BadRequest();
                    if (input.SessionVersion != s.Version || input.QuestionId != s.QuestionId)
                        return Results.Conflict(
                            "The question changed. Please use the current question."
                        );
                    var id = int.Parse(user.FindFirstValue("player_id")!);
                    if (
                        !await db.RoundParticipants.AnyAsync(p =>
                            p.SessionId == s.Id
                            && p.Game == "wwm"
                            && p.Round == s.Round
                            && p.PlayerId == id
                        )
                    )
                        return Results.Forbid();
                    if (
                        await db.Answers.AnyAsync(a =>
                            a.SessionId == s.Id && a.PlayerId == id && a.Round == s.Round
                        )
                    )
                        return Results.Conflict("Already answered");
                    var question = await db.Questions.FindAsync(s.QuestionId);
                    if (question == null)
                        return Results.Conflict("The current question is unavailable.");
                    if (
                        await db.JokerUses.AnyAsync(j =>
                            j.SessionId == s.Id
                            && j.PlayerId == id
                            && j.Round == s.Round
                            && j.Kind == "fifty"
                        )
                        && GameService
                            .RemovedAnswers(s.Round, id, question.Correct)
                            .Contains(input.Answer)
                    )
                        return Results.BadRequest("This option was removed by 50:50.");
                    db.Answers.Add(
                        new GameAnswer
                        {
                            SessionId = s.Id,
                            PlayerId = id,
                            Round = s.Round,
                            Answer = input.Answer,
                            Joker = await db.JokerUses.AnyAsync(j =>
                                j.SessionId == s.Id
                                && j.PlayerId == id
                                && j.Kind == "risk"
                                && j.Round == s.Round
                            )
                                ? "risk"
                                : "",
                        }
                    );
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange);
        app.MapGet(
                "/api/game/state",
                async (ClaimsPrincipal user, GameDbContext db) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    var player = await db.Players.FindAsync(
                        int.Parse(user.FindFirstValue("player_id")!)
                    );
                    var host = player!.IsHost;
                    var q =
                        s.Game == "wwm" && s.QuestionId != null
                            ? await db.Questions.FindAsync(s.QuestionId)
                            : null;
                    var w =
                        s.Game == "imposter" && s.WordId != null
                            ? await db.Words.FindAsync(s.WordId)
                            : null;
                    var jeopardyQuestion =
                        s.Game == "jeopardy" && s.JeopardyQuestionId != null
                            ? await db.JeopardyQuestions.FindAsync(s.JeopardyQuestionId)
                            : null;
                    var jeopardyPlay =
                        jeopardyQuestion == null
                            ? null
                            : await db.JeopardyPlays.SingleOrDefaultAsync(x =>
                                x.SessionId == s.Id && x.QuestionId == jeopardyQuestion.Id
                            );
                    var imposterRoles =
                        s.Game == "imposter"
                            ? await db
                                .Roles.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                .OrderBy(x => x.TurnOrder)
                                .ToListAsync()
                            : [];
                    var imposterClues =
                        s.Game == "imposter"
                            ? await db
                                .ImposterClues.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                .OrderBy(x => x.TurnIndex)
                                .ToListAsync()
                            : [];
                    var speakerOrder = imposterRoles.Select(x => x.PlayerId).ToArray();
                    return Results.Ok(
                        new
                        {
                            session = PublicSession(s),
                            clues = s.Game == "imposter"
                                ? imposterClues
                                    .Select(c => new
                                    {
                                        playerId = c.PlayerId,
                                        clue = c.Clue,
                                        pass = c.Pass,
                                    })
                                    .ToArray()
                                : null,
                            currentSpeakerId = s.Game == "imposter"
                                ? GameService.ImposterCurrentSpeaker(
                                    speakerOrder,
                                    imposterClues.Count
                                )
                                : null,
                            cluePass = s.Game == "imposter"
                                ? GameService.ImposterPass(speakerOrder, imposterClues.Count)
                                : 0,
                            speakerOrder,
                            participants = s.Game == "imposter" ? speakerOrder
                            : s.Game == "wwm"
                                ? await db
                                    .RoundParticipants.Where(x =>
                                        x.SessionId == s.Id && x.Game == "wwm" && x.Round == s.Round
                                    )
                                    .Select(x => x.PlayerId)
                                    .ToArrayAsync()
                            : Array.Empty<int>(),
                            revealedRoles = s.Game == "imposter" && s.Phase == "reveal"
                                ? await db
                                    .Roles.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                    .Select(x => new { x.PlayerId, x.IsImposter })
                                    .ToArrayAsync()
                                : null,
                            revealedWord = s.Game == "imposter" && s.Phase == "reveal"
                                ? w?.Word
                                : null,
                            question = q == null
                                ? null
                                : new
                                {
                                    q.Id,
                                    q.Text,
                                    q.AnswerA,
                                    q.AnswerB,
                                    q.AnswerC,
                                    q.AnswerD,
                                    correct = host || s.Phase == "results" ? q.Correct : null,
                                },
                            jeopardy = jeopardyQuestion == null
                                ? null
                                : new
                                {
                                    jeopardyQuestion.Id,
                                    jeopardyQuestion.Board,
                                    jeopardyQuestion.BoardRound,
                                    jeopardyQuestion.Category,
                                    Value = jeopardyQuestion.Value * jeopardyQuestion.BoardRound,
                                    jeopardyQuestion.Clue,
                                    jeopardyQuestion.ImageUrl,
                                    answer = host ? jeopardyQuestion.Answer : null,
                                    acceptedAnswers = host
                                        ? jeopardyQuestion.AcceptedAnswers
                                        : null,
                                    hostNotes = host ? jeopardyQuestion.HostNotes : null,
                                    state = jeopardyPlay?.State == "open"
                                        ? "question"
                                        : jeopardyPlay?.State,
                                    contestantId = jeopardyPlay?.ContestantId,
                                    stealContestantId = jeopardyPlay?.StealContestantId,
                                    scoreDelta = host ? jeopardyPlay?.ScoreDelta : null,
                                    stealScoreDelta = host ? jeopardyPlay?.StealScoreDelta : null,
                                    timerEndsUtc = s.TimerEndsUtc,
                                },
                            word = host && !s.HostPlaysBlind && w != null ? w.Word : null,
                            role = s.Game == "imposter"
                                ? await db
                                    .Roles.Where(x =>
                                        x.SessionId == s.Id
                                        && x.Round == s.Round
                                        && x.PlayerId == player.Id
                                    )
                                    .Select(x => x.IsImposter ? "imposter" : "crew")
                                    .FirstOrDefaultAsync()
                                : null,
                            privateWord = s.Game == "imposter"
                            && await db.Roles.AnyAsync(x =>
                                x.SessionId == s.Id && x.Round == s.Round && x.PlayerId == player.Id
                            )
                                ? (
                                    (
                                        await db.Roles.AnyAsync(x =>
                                            x.SessionId == s.Id
                                            && x.Round == s.Round
                                            && x.PlayerId == player.Id
                                            && !x.IsImposter
                                        )
                                    )
                                        ? w?.Word
                                        : (
                                            s.Phase == "reveal"
                                                ? w?.Word
                                                : (s.HelperWords ? w?.Hint : null)
                                        )
                                )
                                : null,
                            answers = host || s.Phase == "results"
                                ? await db
                                    .Answers.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                    .ToListAsync()
                                : null,
                            myAnswer = await db
                                .Answers.Where(x =>
                                    x.SessionId == s.Id
                                    && x.Round == s.Round
                                    && x.PlayerId == player.Id
                                )
                                .Select(x => x.Answer)
                                .FirstOrDefaultAsync(),
                            votes = s.Phase == "reveal" || (host && !s.HostPlaysBlind)
                                ? await db
                                    .Votes.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                    .ToListAsync()
                                : null,
                            myVote = await db
                                .Votes.Where(x =>
                                    x.SessionId == s.Id
                                    && x.Round == s.Round
                                    && x.VoterId == player.Id
                                )
                                .Select(x => (int?)x.SuspectId)
                                .FirstOrDefaultAsync(),
                            usedJokers = await db
                                .JokerUses.Where(x =>
                                    x.SessionId == s.Id && x.PlayerId == player.Id
                                )
                                .Select(x => x.Kind)
                                .ToListAsync(),
                            roundJoker = await db
                                .JokerUses.Where(x =>
                                    x.SessionId == s.Id
                                    && x.PlayerId == player.Id
                                    && x.Round == s.Round
                                )
                                .Select(x => x.Kind)
                                .FirstOrDefaultAsync(),
                            removedAnswers = await db.JokerUses.AnyAsync(x =>
                                x.SessionId == s.Id
                                && x.PlayerId == player.Id
                                && x.Round == s.Round
                                && x.Kind == "fifty"
                            )
                            && q != null
                                ? GameService.RemovedAnswers(s.Round, player.Id, q.Correct)
                                : Array.Empty<string>(),
                            spy = await db.JokerUses.AnyAsync(x =>
                                x.SessionId == s.Id
                                && x.PlayerId == player.Id
                                && x.Kind == "spy"
                                && x.Round == s.Round
                            )
                                ? await db
                                    .Answers.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                    .Select(x => new { x.PlayerId, x.Answer })
                                    .ToListAsync()
                                : null,
                        }
                    );
                }
            )
            .RequireAuthorization();
        // Authoritative game transitions. All rule decisions are computed on the server.
        app.MapPost(
                "/api/host/start-imposter",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Game != "imposter")
                        return Results.BadRequest("Select Imposter first");
                    if (s.Phase is not ("lobby" or "reveal"))
                        return Results.Conflict("Finish the current Imposter round first.");
                    var players = await db
                        .Players.Where(p =>
                            p.LastSeenUtc > DateTime.UtcNow.AddMinutes(-5)
                            && (!p.IsHost || s.HostPlaysBlind)
                        )
                        .OrderBy(p => p.Id)
                        .ToListAsync();
                    if (players.Count < (app.Environment.IsDevelopment() ? 2 : 3))
                        return Results.BadRequest("Not enough active players for Imposter");
                    var words = await db.Words.ToListAsync();
                    if (words.Count == 0)
                        return Results.BadRequest("Add words first");
                    var word =
                        !s.HostPlaysBlind && s.WordId.HasValue
                            ? words.FirstOrDefault(x => x.Id == s.WordId)
                            : words[Random.Shared.Next(words.Count)];
                    if (word == null)
                        return Results.BadRequest("Invalid word");
                    s.Round++;
                    s.WordId = word.Id;
                    s.Phase = "clues";
                    s.SpeakerIndex = 0;
                    s.Scored = false;
                    s.Version++;
                    var imposters = Math.Min(s.ImposterCount, players.Count - 1);
                    var imposterIds = players
                        .OrderBy(_ => Random.Shared.Next())
                        .Take(imposters)
                        .Select(p => p.Id)
                        .ToHashSet();
                    var speakerIds = players
                        .OrderBy(_ => Random.Shared.Next())
                        .Select(p => p.Id)
                        .ToArray();
                    foreach (
                        var (playerId, turnOrder) in speakerIds.Select(
                            (playerId, index) => (playerId, index)
                        )
                    )
                        db.Roles.Add(
                            new RoundRole
                            {
                                SessionId = s.Id,
                                Round = s.Round,
                                PlayerId = playerId,
                                IsImposter = imposterIds.Contains(playerId),
                                TurnOrder = turnOrder,
                            }
                        );
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(new
                    {
                        s.Round,
                        s.Phase
                    });
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        // Spoken clues are entered by the host and displayed under their owners.
        app.MapPost(
                "/api/host/clue",
                async (ClueRequest input, GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var session = await db.Sessions.FirstAsync();
                    if (session.Game != "imposter" || session.Phase != "clues")
                        return Results.Conflict("Clues may only be entered in the clue phase.");
                    var roles = await db
                        .Roles.Where(r => r.SessionId == session.Id && r.Round == session.Round)
                        .OrderBy(r => r.TurnOrder)
                        .ToListAsync();
                    var clueCount = await db.ImposterClues.CountAsync(c =>
                        c.SessionId == session.Id && c.Round == session.Round
                    );
                    var current = GameService.ImposterCurrentSpeaker(
                        roles.Select(r => r.PlayerId).ToArray(),
                        clueCount
                    );
                    if (current is null)
                        return Results.Conflict("Both clue passes are already complete.");
                    if (input.PlayerId != current.Value)
                        return Results.BadRequest("It is another player's turn.");
                    if (string.IsNullOrWhiteSpace(input.Clue) || input.Clue.Length > 60)
                        return Results.BadRequest("A clue must contain 1 to 60 characters.");
                    db.ImposterClues.Add(
                        new ImposterClue
                        {
                            SessionId = session.Id,
                            Round = session.Round,
                            PlayerId = input.PlayerId,
                            Pass = GameService.ImposterPass(
                                roles.Select(r => r.PlayerId).ToArray(),
                                clueCount
                            ),
                            TurnIndex = clueCount,
                            Clue = input.Clue.Trim(),
                        }
                    );
                    session.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/vote",
                async (
                    VoteRequest input,
                    ClaimsPrincipal user,
                    GameDbContext db,
                    IHubContext<GameHub> hub
                ) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Game != "imposter" || s.Phase != "vote")
                        return Results.BadRequest("Voting closed");
                    var id = int.Parse(user.FindFirstValue("player_id")!);
                    if (
                        !await db.Roles.AnyAsync(x =>
                            x.SessionId == s.Id && x.Round == s.Round && x.PlayerId == id
                        )
                    )
                        return Results.Forbid();
                    if (
                        !await db.Roles.AnyAsync(x =>
                            x.SessionId == s.Id
                            && x.Round == s.Round
                            && x.PlayerId == input.SuspectId
                        )
                    )
                        return Results.BadRequest("Unknown target");
                    if (id == input.SuspectId)
                        return Results.BadRequest("Du kannst nicht für dich selbst stimmen.");
                    var v = await db.Votes.SingleOrDefaultAsync(x =>
                        x.SessionId == s.Id && x.Round == s.Round && x.VoterId == id
                    );
                    if (v == null)
                        db.Votes.Add(
                            new ImposterVote
                            {
                                SessionId = s.Id,
                                Round = s.Round,
                                VoterId = id,
                                SuspectId = input.SuspectId,
                            }
                        );
                    else
                        v.SuspectId = input.SuspectId;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange);
        app.MapPost(
                "/api/host/reveal-imposter",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Game != "imposter" || s.Phase != "vote")
                        return Results.Conflict("Start voting before revealing roles.");
                    s.Phase = "reveal";
                    s.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(
                        new
                        {
                            roles = await db
                                .Roles.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                .ToListAsync(),
                            votes = await db
                                .Votes.Where(x => x.SessionId == s.Id && x.Round == s.Round)
                                .ToListAsync(),
                        }
                    );
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapGet(
                "/api/host/wwm/readiness",
                async (GameDbContext db) =>
                {
                    var session = await db.Sessions.FirstAsync();
                    var usedIds = (
                        await db.QuestionUses.Where(use => use.SessionId == session.Id)
                            .Select(use => use.QuestionId)
                            .ToListAsync()
                    ).ToHashSet();
                    var questions = await db.Questions.AsNoTracking().ToListAsync();
                    return Results.Ok(
                        Enumerable.Range(1, 30).Select(difficulty => new
                        {
                            difficulty,
                            total = questions.Count(question => question.Difficulty == difficulty),
                            unused = questions.Count(question =>
                                question.Difficulty == difficulty && !usedIds.Contains(question.Id)
                            ),
                        })
                    );
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/start-question",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Game != "wwm")
                        return Results.BadRequest("Select WWM first");
                    // Randomize among previously unused questions nearest the current difficulty.
                    var questions = await db.Questions.AsNoTracking().ToListAsync();
                    if (questions.Count == 0)
                        return Results.BadRequest("Add questions first");
                    if (s.Round > 0 && !s.Scored)
                        return Results.Conflict("Resolve the current question first");
                    if (s.QuestionIndex >= Math.Min(30, questions.Count))
                        return Results.Conflict("No questions remaining");
                    var usedIds = (
                        await db
                            .QuestionUses.Where(x => x.SessionId == s.Id)
                            .Select(x => x.QuestionId)
                            .ToListAsync()
                    ).ToHashSet();
                    var selection = GameService.ChooseQuestionWithDiagnostics(
                        questions,
                        usedIds,
                        s.Round + 1
                    );
                    if (selection is null)
                        return Results.Conflict("No unused questions remain.");
                    var next = selection.Question;
                    s.QuestionId = next.Id;
                    s.QuestionIndex++;
                    s.Round++;
                    s.Phase = "question";
                    s.Scored = false;
                    s.Version++;
                    db.QuestionUses.Add(
                        new QuestionUse
                        {
                            SessionId = s.Id,
                            QuestionId = next.Id,
                            Round = s.Round,
                        }
                    );
                    var participants = await db
                        .Players.Where(p =>
                            !p.IsHost && p.LastSeenUtc > DateTime.UtcNow.AddMinutes(-5)
                        )
                        .Select(p => p.Id)
                        .ToListAsync();
                    foreach (var playerId in participants)
                        db.RoundParticipants.Add(
                            new RoundParticipant
                            {
                                SessionId = s.Id,
                                Game = "wwm",
                                Round = s.Round,
                                PlayerId = playerId,
                            }
                        );
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(
                        new
                        {
                            session = PublicSession(s),
                            warning = selection.Distance > 2
                                ? $"No close question exists for difficulty {selection.TargetDifficulty}. Using difficulty {next.Difficulty}."
                                : null,
                        }
                    );
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        // Skip a broken question without scoring or consuming additional jokers.
        app.MapPost(
                "/api/host/skip-question",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var session = await db.Sessions.FirstAsync();
                    if (session.Game != "wwm" || session.Phase != "question")
                        return Results.Conflict("There is no active question to replace.");

                    var questions = await db.Questions.AsNoTracking().ToListAsync();
                    var usedIds = (
                        await db
                            .QuestionUses.Where(x => x.SessionId == session.Id)
                            .Select(x => x.QuestionId)
                            .ToListAsync()
                    ).ToHashSet();
                    var replacement = GameService.ChooseQuestion(questions, usedIds, session.Round);
                    if (replacement is null)
                        return Results.Conflict("No alternative questions available.");
                    session.QuestionId = replacement.Id;
                    db.QuestionUses.Add(
                        new QuestionUse
                        {
                            SessionId = session.Id,
                            QuestionId = replacement.Id,
                            Round = session.Round,
                        }
                    );
                    db.Answers.RemoveRange(
                        db.Answers.Where(a => a.SessionId == session.Id && a.Round == session.Round)
                    );
                    // A replacement stays in the same round: preserve both joker consumption and its effect.
                    session.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(new
                    {
                        questionId = replacement.Id
                    });
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);

        app.MapPost(
                "/api/host/resolve-question",
                async (GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Game != "wwm" || s.QuestionId == null)
                        return Results.BadRequest();
                    if (s.Scored)
                        return Results.Conflict("Already scored");
                    var q = await db.Questions.FindAsync(s.QuestionId);
                    if (q == null)
                        return Results.BadRequest();
                    var answers = await db
                        .Answers.Where(a => a.SessionId == s.Id && a.Round == s.Round)
                        .ToListAsync();
                    foreach (var ans in answers)
                    {
                        var p = await db.Players.FindAsync(ans.PlayerId);
                        if (p == null || p.IsHost)
                            continue;
                        var correct = ans.CorrectOverride ?? (ans.Answer == q.Correct);
                        var risk = await db.JokerUses.AnyAsync(j =>
                            j.SessionId == s.Id
                            && j.PlayerId == p.Id
                            && j.Kind == "risk"
                            && j.Round == s.Round
                        );
                        var delta = GameService.WwmScoreDelta(s.Round, correct, risk);
                        p.Points += delta;
                        db.ScoreEvents.Add(
                            new ScoreEvent
                            {
                                Round = s.Round,
                                PlayerId = p.Id,
                                Delta = delta,
                                Reason = risk ? "WWM risk" : "WWM answer",
                            }
                        );
                    }
                    s.Phase = "results";
                    s.Scored = true;
                    s.Version++;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok(new
                    {
                        answers = answers.Count
                    });
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/host/override-answer",
                async (OverrideAnswer input, GameDbContext db, IHubContext<GameHub> hub) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Scored)
                        return Results.Conflict(
                            "Resolve corrections before scoring. Use point adjustments afterward."
                        );
                    var a = await db.Answers.SingleOrDefaultAsync(x =>
                        x.SessionId == s.Id && x.Round == s.Round && x.PlayerId == input.PlayerId
                    );
                    if (a == null)
                        return Results.NotFound();
                    a.CorrectOverride = input.Correct;
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    return Results.Ok();
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange)
            .AddEndpointFilter(HostOnly);
        app.MapPost(
                "/api/joker",
                async (
                    JokerRequest input,
                    ClaimsPrincipal user,
                    GameDbContext db,
                    IHubContext<GameHub> hub
                ) =>
                {
                    var s = await db.Sessions.FirstAsync();
                    if (s.Game != "wwm" || s.Phase != "question")
                        return Results.BadRequest("Jokers only during questions");
                    if (input.SessionVersion != s.Version || input.QuestionId != s.QuestionId)
                        return Results.Conflict(
                            "The question changed. Please use the current question."
                        );
                    if (input.Kind is not ("fifty" or "spy" or "risk"))
                        return Results.BadRequest();
                    var id = int.Parse(user.FindFirstValue("player_id")!);
                    if (await db.Players.AnyAsync(p => p.Id == id && p.IsHost))
                        return Results.Forbid();
                    if (
                        !await db.RoundParticipants.AnyAsync(p =>
                            p.SessionId == s.Id
                            && p.Game == "wwm"
                            && p.Round == s.Round
                            && p.PlayerId == id
                        )
                    )
                        return Results.Forbid();
                    if (
                        await db.JokerUses.AnyAsync(j =>
                            j.SessionId == s.Id && j.PlayerId == id && j.Kind == input.Kind
                        )
                    )
                        return Results.Conflict("This joker has already been consumed.");
                    if (
                        await db.JokerUses.AnyAsync(j =>
                            j.SessionId == s.Id && j.PlayerId == id && j.Round == s.Round
                        )
                    )
                        return Results.Conflict("Only one joker may be used in each question.");
                    if (
                        await db.Answers.AnyAsync(a =>
                            a.SessionId == s.Id && a.Round == s.Round && a.PlayerId == id
                        )
                    )
                        return Results.Conflict("Already answered");
                    var q = await db.Questions.FindAsync(s.QuestionId);
                    if (q == null)
                        return Results.Conflict("The current question is unavailable.");
                    db.JokerUses.Add(
                        new JokerUse
                        {
                            SessionId = s.Id,
                            Round = s.Round,
                            PlayerId = id,
                            Kind = input.Kind,
                        }
                    );
                    await db.SaveChangesAsync();
                    await hub.Clients.All.SendAsync("changed");
                    if (input.Kind != "fifty")
                        return Results.Ok(new
                        {
                            kind = input.Kind
                        });
                    return Results.Ok(
                        new
                        {
                            kind = input.Kind,
                            removed = GameService.RemovedAnswers(s.Round, id, q.Correct),
                        }
                    );
                }
            )
            .RequireAuthorization()
            .AddEndpointFilter(SerializeGameChange);
        app.MapGet(
                "/api/host/score-log",
                async (GameDbContext db) =>
                    Results.Ok(
                        await db.ScoreEvents.OrderByDescending(e => e.Id).Take(100).ToListAsync()
                    )
            )
            .RequireAuthorization()
            .AddEndpointFilter(HostOnly);
        app.MapHub<GameHub>("/hub/game").RequireAuthorization();
    }

    // The content ID itself can reveal the secret through a previously loaded word catalog.
    // Single-instance game transitions must not race (double scoring / two jokers in one round).
    private static readonly SemaphoreSlim GameChangeGate = new(1, 1);

    /// <summary>Stable palette used by the host UI and enforced server-side for optional teams.</summary>
    internal static readonly string[] TeamPalette =
    [
        "Rot",
        "Blau",
        "Grün",
        "Gelb",
        "Lila",
        "Orange",
        "Türkis",
        "Pink",
    ];

    private static async ValueTask<object?> SerializeGameChange(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next
    )
    {
        await GameChangeGate.WaitAsync(ctx.HttpContext.RequestAborted);
        try
        {
            return await next(ctx);
        }
        finally
        {
            GameChangeGate.Release();
        }
    }

    private static object PublicSession(GameSession s) =>
        new
        {
            s.Id,
            s.Game,
            s.Phase,
            s.Round,
            s.HelperWords,
            s.HostPlaysBlind,
            s.ImposterCount,
            s.QuestionId,
            WordId = (int?)null,
            s.JeopardyQuestionId,
            s.TimerEndsUtc,
            s.JeopardyRound,
            s.JeopardyCategoriesJson,
            s.Version,
            s.QuestionIndex,
            s.Scored,
            s.SpeakerIndex,
        };

    private static ValueTask<object?> HostOnly(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next
    ) =>
        ctx.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
        == ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>()[
            "Discord:HostUserId"
        ]
            ? next(ctx)
            : ValueTask.FromResult<object?>(Results.Forbid());
}
