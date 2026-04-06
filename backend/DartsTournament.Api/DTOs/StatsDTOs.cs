namespace DartsTournament.Api.DTOs;

/// <summary>
/// Statistiques complètes d'un match
/// </summary>
public record MatchStatsResponse(
    PlayerStatsInfo Player1Stats,
    PlayerStatsInfo Player2Stats
);

/// <summary>
/// Statistiques d'un joueur
/// </summary>
public record PlayerStatsInfo(
    int PlayerId,
    string Name,
    double ThreeDartAverage,
    double? CheckoutPercentage,
    double? First9Average,
    int? HighestCheckout,
    int TotalDartsThrown,
    int TotalScore,
    int LegsWon,
    int CheckoutAttempts,
    int CheckoutSuccesses,
    int? HighestScore,
    int OneEighties
);
