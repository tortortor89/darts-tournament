using System.ComponentModel.DataAnnotations;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

/// <summary>
/// Requête pour démarrer une session de match
/// </summary>
public record StartMatchSessionRequest(
    [Range(1, 10, ErrorMessage = "Le nombre de legs à gagner doit être entre 1 et 10")]
    int LegsToWin,

    [Required(ErrorMessage = "L'ID du joueur qui commence est requis")]
    int StartingPlayerId,

    bool TrackDoubles = false,  // Active le tracking avancé des doubles tentés

    GameMode GameMode = GameMode.FiveOhOne  // Mode de jeu (501 par défaut)
);

/// <summary>
/// Requête pour enregistrer une volée
/// </summary>
public record RecordThrowRequest(
    [Range(0, 180, ErrorMessage = "Le score doit être entre 0 et 180")]
    int Score,

    string? Dart1,
    string? Dart2,
    string? Dart3,

    [Range(1, 3, ErrorMessage = "Le nombre de fléchettes utilisées doit être entre 1 et 3")]
    int? DartsUsed,

    [Range(1, 3, ErrorMessage = "Le nombre de doubles tentés doit être entre 1 et 3")]
    int? DoublesAttempted
);

/// <summary>
/// Réponse d'état d'une session de match
/// </summary>
public record MatchSessionResponse(
    int Id,
    int MatchId,
    int LegsToWin,
    GameMode GameMode,
    MatchSessionStatus Status,
    PlayerSessionInfo Player1,
    PlayerSessionInfo Player2,
    int CurrentPlayerId,
    int CurrentLeg,
    List<ThrowResponse> CurrentLegThrows,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    bool TrackDoubles,
    CricketDisplayState? CricketState  // État Cricket (null si mode 501)
);

/// <summary>
/// Informations d'un joueur dans une session
/// </summary>
public record PlayerSessionInfo(
    int PlayerId,
    string Name,
    int LegsWon,
    int CurrentScore,
    bool IsStarting
);

/// <summary>
/// Réponse pour une volée
/// </summary>
public record ThrowResponse(
    int Id,
    int PlayerId,
    string PlayerName,
    int LegNumber,
    int ThrowNumber,
    int Score,
    string? Dart1,
    string? Dart2,
    string? Dart3,
    int RemainingScore,
    bool IsCheckout,
    bool IsBust,
    int? DartsUsed,
    int? DoublesAttempted,
    DateTime CreatedAt
);

/// <summary>
/// Réponse simplifiée pour le mode spectateur
/// </summary>
public record MatchSessionSpectatorResponse(
    int MatchId,
    string TournamentName,
    int LegsToWin,
    GameMode GameMode,
    MatchSessionStatus Status,
    PlayerSpectatorInfo Player1,
    PlayerSpectatorInfo Player2,
    int CurrentPlayerId,
    int CurrentLeg,
    List<LegSummary> LegsHistory,
    CricketDisplayState? CricketState
);

/// <summary>
/// Informations joueur pour spectateur
/// </summary>
public record PlayerSpectatorInfo(
    int PlayerId,
    string Name,
    int LegsWon,
    int CurrentScore
);

/// <summary>
/// Résumé d'un leg terminé
/// </summary>
public record LegSummary(
    int LegNumber,
    int WinnerId,
    string WinnerName,
    int WinnerDartsThrown,
    double? WinnerAverage
);
