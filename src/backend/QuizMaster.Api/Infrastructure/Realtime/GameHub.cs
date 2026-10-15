using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace QuizMaster.Api.Infrastructure.Realtime;

/// <summary>
/// Routes WebRTC offers, answers and ICE candidates between authenticated players.
/// Actual video stays peer-to-peer and is never transmitted through SignalR.
/// </summary>
[Authorize]
public sealed class GameHub : Hub
{
    private static readonly ConcurrentDictionary<int, string> Connections = new();

    private int CurrentPlayerId => int.Parse(Context.User!.FindFirstValue("player_id")!);

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    /// <summary>Only camera channels register; general game subscriptions cannot replace them.</summary>
    public async Task JoinCamera()
    {
        var id = CurrentPlayerId;
        var peers = Connections.Keys.Where(peer => peer != id).ToArray();
        Connections[id] = Context.ConnectionId;
        await Clients.Caller.SendAsync("camera-peers", peers, id);
        await Clients.Others.SendAsync("camera-joined", id);
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var id = CurrentPlayerId;
        if (Connections.TryGetValue(id, out var connection) && connection == Context.ConnectionId)
        {
            Connections.TryRemove(id, out _);
            await Clients.Others.SendAsync("camera-left", id);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Deliver bounded WebRTC signaling data to an authenticated peer.</summary>
    public Task SignalCamera(int destinationPlayerId, JsonElement payload)
    {
        if (
            !Connections.TryGetValue(CurrentPlayerId, out var source)
            || source != Context.ConnectionId
        )
            throw new HubException("Register the camera channel first.");
        if (payload.GetRawText().Length > 20_000)
            throw new HubException("Signaling payload exceeds the size limit.");

        return Connections.TryGetValue(destinationPlayerId, out var connection)
            ? Clients.Client(connection).SendAsync("camera-signal", CurrentPlayerId, payload)
            : Task.CompletedTask;
    }
}
