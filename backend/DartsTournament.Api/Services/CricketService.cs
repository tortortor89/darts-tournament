using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public class CricketService
{
    /// <summary>
    /// Initialise un nouvel état Cricket pour un leg
    /// </summary>
    public CricketGameState InitializeState(int player1Id, int player2Id)
    {
        return new CricketGameState(player1Id, player2Id);
    }

    /// <summary>
    /// Traite un throw Cricket et met à jour l'état
    /// </summary>
    public CricketThrowResult ProcessThrow(
        CricketGameState state,
        int playerId,
        int opponentId,
        int target,
        int hits)
    {
        var playerState = state.PlayerStates[playerId];
        var opponentState = state.PlayerStates[opponentId];

        var result = new CricketThrowResult
        {
            Target = target,
            HitsCount = hits,
            PointsScored = 0,
            ClosedTarget = false
        };

        var currentHits = playerState.TargetHits[target];
        var newHits = Math.Min(currentHits + hits, 3);  // Max 3
        var excessHits = (currentHits + hits) - 3;      // Hits au-delà de 3

        playerState.TargetHits[target] = newHits;

        // Si fermé (3 hits)
        if (newHits >= 3 && currentHits < 3)
        {
            result.ClosedTarget = true;
        }

        // Scoring: si on a déjà fermé ET l'adversaire n'a pas fermé
        if (currentHits >= 3 && opponentState.TargetHits[target] < 3)
        {
            // On marque avec les hits en excès
            result.PointsScored = excessHits * GetTargetValue(target);
            playerState.Score += result.PointsScored;
        }

        return result;
    }

    /// <summary>
    /// Vérifie si un joueur a gagné le leg
    /// </summary>
    public bool HasPlayerWonLeg(CricketGameState state, int playerId, int opponentId)
    {
        var playerState = state.PlayerStates[playerId];
        var opponentState = state.PlayerStates[opponentId];

        // Gagner : toutes les cibles fermées ET (score >= adversaire OU adversaire n'a pas tout fermé)
        if (!playerState.AllTargetsClosed())
            return false;

        // Si adversaire n'a pas tout fermé, on gagne
        if (!opponentState.AllTargetsClosed())
            return true;

        // Si les deux ont tout fermé, celui avec le plus de points gagne
        return playerState.Score >= opponentState.Score;
    }

    /// <summary>
    /// Convertit l'état en DTO d'affichage
    /// </summary>
    public CricketDisplayState BuildDisplayState(CricketGameState state, int player1Id, int player2Id)
    {
        var p1State = state.PlayerStates[player1Id];
        var p2State = state.PlayerStates[player2Id];

        var targets = new[] { 15, 16, 17, 18, 19, 20, 25 };

        var player1Targets = targets.ToDictionary(
            t => t,
            t => new CricketTargetState(t, p1State.TargetHits[t], p1State.TargetHits[t] >= 3)
        );

        var player2Targets = targets.ToDictionary(
            t => t,
            t => new CricketTargetState(t, p2State.TargetHits[t], p2State.TargetHits[t] >= 3)
        );

        return new CricketDisplayState(
            player1Targets,
            player2Targets,
            p1State.Score,
            p2State.Score
        );
    }

    private int GetTargetValue(int target)
    {
        // 25 = Bull (on gèrera DB/SB au niveau du hit count)
        return target == 25 ? 25 : target;
    }
}
