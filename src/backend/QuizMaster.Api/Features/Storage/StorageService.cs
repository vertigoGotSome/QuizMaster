using Microsoft.EntityFrameworkCore;
using QuizMaster.Data;

namespace QuizMaster.Api.Features.Storage;

/// <summary>Contains content-access rules shared by storage and host configuration routes.</summary>
internal static class StorageService
{
    /// <summary>
    /// Prevents a blind Imposter host from loading the word catalogue while a round is active.
    /// </summary>
    public static Task<bool> IsBlindWordAccessBlockedAsync(GameDbContext database) =>
        database.Sessions.AnyAsync(session =>
            session.Game == "imposter"
            && session.HostPlaysBlind
            && session.Phase != "lobby"
            && session.Phase != "reveal"
        );
}
