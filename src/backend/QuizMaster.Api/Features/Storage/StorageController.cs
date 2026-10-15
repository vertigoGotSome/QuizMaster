using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Data;

namespace QuizMaster.Api.Features.Storage;

/// <summary>Host-only CRUD and JSON bulk import/export for the current and future games.</summary>
public static class StorageController
{
    /// <summary>Registers content storage endpoints and enforces authorization server-side.</summary>
    public static void MapStorageController(this WebApplication app)
    {
        var storage = app.MapGroup("/api/host/storage")
            .RequireAuthorization()
            .AddEndpointFilter(OnlyHost);

        storage.MapGet(
            "/words",
            async (GameDbContext db) =>
                await StorageService.IsBlindWordAccessBlockedAsync(db)
                    ? Results.Conflict("Word storage is locked while the host plays blind.")
                    : Results.Ok(
                        await db.Words.OrderBy(w => w.Category).ThenBy(w => w.Word).ToListAsync()
                    )
        );

        storage.MapGet(
            "/questions",
            async (GameDbContext db) =>
                Results.Ok(
                    await db.Questions.OrderBy(q => q.Difficulty).ThenBy(q => q.Id).ToListAsync()
                )
        );

        storage.MapPost(
            "/words/import",
            async (List<WordInput> items, GameDbContext db) =>
            {
                if (items is null || items.Count is < 1 or > 1000)
                    return Results.BadRequest(new { message = "Provide 1–1000 words." });

                var existing = (await db.Words.AsNoTracking().ToListAsync())
                    .Select(w => Normalize(w.Word))
                    .ToHashSet();
                var inserted = 0;
                var skipped = 0;

                foreach (var item in items)
                {
                    var key = Normalize(item.Word);
                    if (key.Length == 0 || key.Length > 160 || existing.Contains(key))
                    {
                        skipped++;
                        continue;
                    }

                    db.Words.Add(
                        new SecretWord
                        {
                            Category = CleanRequired(item.Category, 80),
                            Word = item.Word.Trim(),
                            Hint = CleanOptional(item.Hint, 160),
                        }
                    );
                    existing.Add(key);
                    inserted++;
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { inserted, skipped });
            }
        );

        storage.MapPost(
            "/questions/import",
            async (List<QuestionInput> items, GameDbContext db) =>
            {
                if (items is null || items.Count is < 1 or > 1000)
                    return Results.BadRequest(new { message = "Provide 1–1000 questions." });

                var existing = (await db.Questions.AsNoTracking().ToListAsync())
                    .Select(q => Normalize(q.Text))
                    .ToHashSet();
                var inserted = 0;
                var skipped = 0;

                foreach (var item in items)
                {
                    var key = Normalize(item.Text);
                    if (
                        key.Length < 8
                        || key.Length > 700
                        || existing.Contains(key)
                        || !new[] { "A", "B", "C", "D" }.Contains(item.Correct?.ToUpperInvariant())
                        || new[] { item.AnswerA, item.AnswerB, item.AnswerC, item.AnswerD }.Any(a =>
                            string.IsNullOrWhiteSpace(a)
                        )
                    )
                    {
                        skipped++;
                        continue;
                    }

                    db.Questions.Add(
                        new Question
                        {
                            Category = CleanRequired(item.Category, 80),
                            Difficulty = Math.Clamp(item.Difficulty, 1, 30),
                            Text = item.Text.Trim(),
                            AnswerA = item.AnswerA.Trim(),
                            AnswerB = item.AnswerB.Trim(),
                            AnswerC = item.AnswerC.Trim(),
                            AnswerD = item.AnswerD.Trim(),
                            Correct = item.Correct!.ToUpperInvariant(),
                        }
                    );
                    existing.Add(key);
                    inserted++;
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { inserted, skipped });
            }
        );

        storage.MapDelete(
            "/words/{id:int}",
            async (int id, GameDbContext db) =>
            {
                var word = await db.Words.FindAsync(id);
                if (word is null)
                    return Results.NotFound();
                if (await db.Sessions.AnyAsync(s => s.WordId == id))
                    return Results.Conflict(
                        new { message = "Word is selected in the current session." }
                    );
                db.Words.Remove(word);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
        );

        // Bulk deletion is intentionally blocked while a word is actively used.
        // This prevents an Impostor round from losing its secret word mid-game.
        storage.MapDelete(
            "/words",
            async (GameDbContext db) =>
            {
                if (
                    await db.Sessions.AnyAsync(session =>
                        session.Game == "imposter"
                        && session.WordId != null
                        && session.Phase != "lobby"
                        && session.Phase != "reveal"
                    )
                )
                {
                    return Results.Conflict(
                        new { message = "Finish or reveal the active Impostor round first." }
                    );
                }

                db.Words.RemoveRange(db.Words);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
        );

        storage.MapDelete(
            "/questions/{id:int}",
            async (int id, GameDbContext db) =>
            {
                var question = await db.Questions.FindAsync(id);
                if (question is null)
                    return Results.NotFound();
                if (await db.Sessions.AnyAsync(s => s.QuestionId == id))
                    return Results.Conflict(
                        new { message = "Question is selected in the current session." }
                    );
                db.Questions.Remove(question);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
        );

        storage.MapDelete(
            "/questions",
            async (GameDbContext db) =>
            {
                if (
                    await db.Sessions.AnyAsync(s =>
                        s.Game == "wwm" && s.QuestionId != null && !s.Scored
                    )
                )
                    return Results.Conflict(
                        new { message = "Finish or replace the active WWM question first." }
                    );
                db.Questions.RemoveRange(db.Questions);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
        );
    }

    private static string Normalize(string? value) =>
        Regex.Replace((value ?? "").Trim().ToUpperInvariant(), @"\s+", " ");

    private static string CleanRequired(string? value, int length) =>
        string.IsNullOrWhiteSpace(value)
            ? "General"
            : value.Trim()[..Math.Min(value.Trim().Length, length)];

    private static string CleanOptional(string? value, int length) =>
        string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim()[..Math.Min(value.Trim().Length, length)];

    private static ValueTask<object?> OnlyHost(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var hostId = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()[
            "Discord:HostUserId"
        ];
        return
            !string.IsNullOrWhiteSpace(hostId)
            && context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) == hostId
            ? next(context)
            : ValueTask.FromResult<object?>(Results.Forbid());
    }
}
