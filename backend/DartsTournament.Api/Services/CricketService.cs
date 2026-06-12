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
    /// Valide qu'une visite Cricket est réalisable avec 3 fléchettes
    /// </summary>
    public void ValidateTurn(List<CricketHit> hits)
    {
        var validTargets = new[] { 15, 16, 17, 18, 19, 20, 25 };
        foreach (var hit in hits)
        {
            if (!validTargets.Contains(hit.Target))
                throw new InvalidOperationException($"Cible invalide: {hit.Target}");
        }

        // Faisabilité : chaque fléchette touche une seule cible et rapporte
        // au plus 3 marques (triple), ou 2 sur le Bull (pas de triple Bull)
        var minDartsNeeded = hits
            .GroupBy(h => h.Target)
            .Sum(g => (int)Math.Ceiling(g.Sum(h => h.Marks) / (double)MaxMarksPerDart(g.Key)));

        if (minDartsNeeded > 3)
            throw new InvalidOperationException(
                "Visite impossible avec 3 fléchettes (max 3 marques par fléchette, 2 sur le Bull)");
    }

    private static int MaxMarksPerDart(int target) => target == 25 ? 2 : 3;

    /// <summary>
    /// Traite une visite complète au Cricket (plusieurs hits possibles)
    /// </summary>
    public List<CricketHitResult> ProcessTurn(
        CricketGameState state,
        int playerId,
        int opponentId,
        List<CricketHit> hits)
    {
        var results = new List<CricketHitResult>();

        foreach (var hit in hits)
        {
            var result = ProcessHit(state, playerId, opponentId, hit.Target, hit.Marks);
            results.Add(new CricketHitResult(
                hit.Target,
                hit.Marks,
                result.PointsScored,
                result.ClosedTarget
            ));
        }

        return results;
    }

    /// <summary>
    /// Traite un hit Cricket sur une cible et met à jour l'état
    /// </summary>
    private CricketThrowResult ProcessHit(
        CricketGameState state,
        int playerId,
        int opponentId,
        int target,
        int marks)
    {
        var playerState = state.PlayerStates[playerId];
        var opponentState = state.PlayerStates[opponentId];

        var result = new CricketThrowResult
        {
            Target = target,
            HitsCount = marks,
            PointsScored = 0,
            ClosedTarget = false
        };

        var currentHits = playerState.TargetHits[target];
        var newHits = Math.Min(currentHits + marks, 3);  // Max 3
        var excessMarks = Math.Max((currentHits + marks) - 3, 0);  // Marques au-delà de 3

        playerState.TargetHits[target] = newHits;

        // Si fermé (3 hits)
        if (newHits >= 3 && currentHits < 3)
        {
            result.ClosedTarget = true;
        }

        // Scoring: si on a des marques en excès ET l'adversaire n'a pas fermé
        if (excessMarks > 0 && opponentState.TargetHits[target] < 3)
        {
            // On marque avec les marques en excès
            result.PointsScored = excessMarks * GetTargetValue(target);
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

        // Gagner : toutes les cibles fermées ET score supérieur ou égal à l'adversaire
        if (!playerState.AllTargetsClosed())
            return false;

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
