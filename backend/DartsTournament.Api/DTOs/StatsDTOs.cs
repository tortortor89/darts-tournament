namespace DartsTournament.Api.DTOs;

/// <summary>
/// Statistiques complètes d'un match.
/// En double : Player1Stats/Player2Stats sont les stats agrégées de chaque ÉQUIPE
/// (PlayerId = id d'équipe) et les MemberStats détaillent chaque lanceur.
/// </summary>
public record MatchStatsResponse(
    PlayerStatsInfo Player1Stats,
    PlayerStatsInfo Player2Stats,
    List<PlayerStatsInfo>? Player1MemberStats = null,
    List<PlayerStatsInfo>? Player2MemberStats = null
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
    int OneEighties,
    double? DoublesHitRate,
    double? MarksPerRound = null  // Cricket uniquement (null en 501)
);
