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

        // Moyenne 3 fléchettes
        var totalScore = playerThrows.Sum(t => t.Score);
        var dartsThrown = playerThrows.Count * 3;
        var average = dartsThrown > 0 ? (double)totalScore / dartsThrown * 3 : 0;

        // Checkout %
        // Une tentative de checkout = quand le score restant avant la volée <= 170
        var checkoutAttempts = playerThrows.Count(t =>
            (t.RemainingScore + t.Score) <= 170 && !t.IsBust);
        var checkoutSuccesses = playerThrows.Count(t => t.IsCheckout);
        double? checkoutPercentage = checkoutAttempts > 0
            ? (double)checkoutSuccesses / checkoutAttempts * 100
            : null;

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
            oneEighties
        );
    }
}
