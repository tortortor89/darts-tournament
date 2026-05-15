using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

// Stats complètes de carrière
public record PlayerCareerStatsResponse(
    int PlayerId,
    string PlayerName,

    // Win/Loss Record (toujours disponible)
    int TotalMatches,
    int MatchesWon,
    int MatchesLost,
    double WinPercentage,

    // Stats de jeu (nullable - seulement si Throws disponibles)
    PlayerStatsAggregated? DetailedStats,

    // Tournois
    int TournamentsPlayed,
    int TournamentsWon,

    // Dates
    DateTime? FirstMatchDate,
    DateTime? LastMatchDate
);

// Stats agrégées de jeu
public record PlayerStatsAggregated(
    double ThreeDartAverage,
    double? CheckoutPercentage,
    double? First9Average,
    int? HighestCheckout,
    int TotalDartsThrown,
    int TotalScore,
    int TotalLegsWon,
    int TotalCheckoutAttempts,
    int TotalCheckoutSuccesses,
    int? HighestScore,
    int TotalOneEighties,
    int MatchesWithStats  // Combien de matchs avec Throws
);

// Historique tournoi
public record PlayerTournamentHistoryItem(
    int TournamentId,
    string TournamentName,
    TournamentFormat Format,
    TournamentStatus Status,
    DateTime? StartDate,

    int MatchesPlayed,
    int MatchesWon,
    int MatchesLost,
    string Result,  // "Winner", "Finalist", "Eliminated Round X", etc.

    int? GroupId,
    string? GroupName,
    int? GroupRank
);

// Head-to-head
public record HeadToHeadRecord(
    int OpponentId,
    string OpponentName,
    int MatchesPlayed,
    int MatchesWon,
    int MatchesLost,
    double WinPercentage,
    int TotalLegsWon,
    int TotalLegsLost,
    DateTime? LastMatchDate,
    string? LastMatchTournament
);
