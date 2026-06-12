using System.Text.Json;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public class MatchStatsService
{
    /// <summary>
    /// Calcule les statistiques complètes d'un match
    /// </summary>
    public MatchStatsResponse CalculateStats(MatchSession session)
    {
        var player1Id = session.Match.Player1Id!.Value;
        var player2Id = session.Match.Player2Id!.Value;

        return new MatchStatsResponse(
            CalculatePlayerStats(session, player1Id, session.Match.Player1!, session.Player1LegsWon),
            CalculatePlayerStats(session, player2Id, session.Match.Player2!, session.Player2LegsWon)
        );
    }

    private PlayerStatsInfo CalculatePlayerStats(MatchSession session, int playerId, Player player, int legsWon)
    {
        var playerThrows = session.Throws.Where(t => t.PlayerId == playerId).ToList();

        if (session.GameMode == GameMode.Cricket)
            return CalculateCricketStats(playerThrows, playerId, player, legsWon);

        // Moyenne 3 fléchettes
        var totalScore = playerThrows.Sum(t => t.Score);

        // Calculer le nombre de fléchettes lancées
        // Utiliser DartsUsed quand disponible, sinon fallback sur 3 par volée
        var dartsThrown = playerThrows
            .Sum(t => t.DartsUsed ?? 3);

        var average = dartsThrown > 0 ? (double)totalScore / dartsThrown * 3 : 0;

        // Checkout %
        var checkoutSuccesses = playerThrows.Count(t => t.IsCheckout);
        int checkoutAttempts;
        double? checkoutPercentage;
        double? doublesHitRate = null;

        if (session.TrackDoubles)
        {
            // Tracking précis : une tentative = une fléchette lancée sur un double
            checkoutAttempts = playerThrows.Sum(t => t.DoublesAttempted ?? 0);
            checkoutPercentage = checkoutAttempts > 0
                ? (double)checkoutSuccesses / checkoutAttempts * 100
                : null;
            doublesHitRate = checkoutPercentage;
        }
        else
        {
            // Heuristique grossière : toute volée commencée à 170 ou moins compte comme une tentative
            checkoutAttempts = playerThrows.Count(t =>
                (t.RemainingScore + t.Score) <= 170 && !t.IsBust);
            checkoutPercentage = checkoutAttempts > 0
                ? (double)checkoutSuccesses / checkoutAttempts * 100
                : null;
        }

        // Moyenne 9 premières fléchettes (3 premiers throws de chaque leg)
        var first9Throws = playerThrows
            .Where(t => t.ThrowNumber <= 3)
            .ToList();
        var first9Score = first9Throws.Sum(t => t.Score);
        var first9Darts = first9Throws.Count * 3;
        double? first9Average = first9Darts > 0
            ? (double)first9Score / first9Darts * 3
            : null;

        // Plus haut checkout
        var highestCheckout = playerThrows
            .Where(t => t.IsCheckout)
            .Select(t => t.Score)
            .DefaultIfEmpty(0)
            .Max();

        // Plus haut score sur une volée
        var highestScore = playerThrows
            .Select(t => t.Score)
            .DefaultIfEmpty(0)
            .Max();

        // Nombre de 180
        var oneEighties = playerThrows.Count(t => t.Score == 180);

        return new PlayerStatsInfo(
            playerId,
            $"{player.FirstName} {player.LastName}",
            Math.Round(average, 2),
            checkoutPercentage.HasValue ? Math.Round(checkoutPercentage.Value, 1) : null,
            first9Average.HasValue ? Math.Round(first9Average.Value, 2) : null,
            highestCheckout > 0 ? highestCheckout : null,
            dartsThrown,
            totalScore,
            legsWon,
            checkoutAttempts,
            checkoutSuccesses,
            highestScore > 0 ? highestScore : null,
            oneEighties,
            doublesHitRate.HasValue ? Math.Round(doublesHitRate.Value, 1) : null
        );
    }

    /// <summary>
    /// Statistiques Cricket : MPR (marks per round), points marqués
    /// </summary>
    private PlayerStatsInfo CalculateCricketStats(List<Throw> playerThrows, int playerId, Player player, int legsWon)
    {
        var rounds = playerThrows.Count;
        var totalMarks = playerThrows.Sum(CountMarks);
        var marksPerRound = rounds > 0 ? (double)totalMarks / rounds : 0;

        // En Cricket, Throw.Score contient les points marqués dans la visite
        var totalPoints = playerThrows.Sum(t => t.Score);
        var highestScore = playerThrows
            .Select(t => t.Score)
            .DefaultIfEmpty(0)
            .Max();

        return new PlayerStatsInfo(
            playerId,
            $"{player.FirstName} {player.LastName}",
            0,      // ThreeDartAverage : sans objet en Cricket
            null,   // CheckoutPercentage
            null,   // First9Average
            null,   // HighestCheckout
            rounds * 3,  // Approximation : 3 fléchettes par visite
            totalPoints,
            legsWon,
            0,
            0,
            highestScore > 0 ? highestScore : null,
            0,      // OneEighties
            null,   // DoublesHitRate
            Math.Round(marksPerRound, 2)
        );
    }

    private static int CountMarks(Throw cricketThrow)
    {
        if (string.IsNullOrEmpty(cricketThrow.CricketDataJson))
            return 0;

        var data = JsonSerializer.Deserialize<CricketThrowData>(cricketThrow.CricketDataJson);
        return data?.Hits?.Sum(h => h.Marks) ?? 0;
    }
}
