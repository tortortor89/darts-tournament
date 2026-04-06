using Microsoft.AspNetCore.SignalR;
using DartsTournament.Api.DTOs;

namespace DartsTournament.Api.Hubs;

/// <summary>
/// Interface définissant les méthodes client pour les événements de match
/// </summary>
public interface IMatchHubClient
{
    Task SessionStarted(SessionStartedEvent sessionEvent);
    Task ThrowRecorded(ThrowRecordedEvent throwEvent);
    Task LegWon(LegWonEvent legEvent);
    Task MatchFinished(MatchFinishedEvent finishedEvent);
    Task SessionCancelled(int matchId);
}

/// <summary>
/// Hub SignalR pour les mises à jour en temps réel des matchs
/// </summary>
public class MatchHub : Hub<IMatchHubClient>
{
    /// <summary>
    /// Rejoint le groupe d'un match pour recevoir les mises à jour
    /// </summary>
    public async Task JoinMatch(int matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match-{matchId}");
    }

    /// <summary>
    /// Quitte le groupe d'un match
    /// </summary>
    public async Task LeaveMatch(int matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match-{matchId}");
    }
}
