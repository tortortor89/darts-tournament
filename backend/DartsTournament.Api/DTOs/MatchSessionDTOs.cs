using System.ComponentModel.DataAnnotations;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

/// <summary>
/// Requête pour démarrer une session de match
/// </summary>
public record StartMatchSessionRequest(
    [Range(1, 10, ErrorMessage = "Le nombre de legs à gagner doit être entre 1 et 10")]
    int LegsToWin,

    // Simple : joueur qui commence. Double : ignoré (utiliser StartingTeamId + ordres)
    int StartingPlayerId = 0,

    bool TrackDoubles = false,  // Active le tracking avancé des doubles tentés

    GameMode GameMode = GameMode.FiveOhOne,  // Mode de jeu (501 par défaut)

    bool DoubleOut = true,  // x01 : finir sur un double (ignoré en Cricket)

    // Double uniquement : équipe qui commence + ordre de passage de chaque paire
    // (fixé pour tout le match)
    int? StartingTeamId = null,
    List<int>? Side1PlayerOrder = null,
    List<int>? Side2PlayerOrder = null
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

    [Range(0, 3, ErrorMessage = "Le nombre de doubles tentés doit être entre 0 et 3")]
    int? DoublesAttempted
);

/// <summary>
/// Réponse d'état d'une session de match
/// </summary>
// En double, Player1/Player2 représentent les deux ÉQUIPES (PlayerId = id d'équipe,
// Name = label de paire, Members renseigné). CurrentPlayerId reste le lanceur
// individuel courant ; CurrentSideId identifie le côté au trait.
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
    CricketDisplayState? CricketState,  // État Cricket (null si mode x01)
    bool DoubleOut,
    bool IsDoubles = false,
    int CurrentSideId = 0,
    string? CurrentThrowerName = null
);

/// <summary>
/// Informations d'un côté (joueur ou paire) dans une session
/// </summary>
public record PlayerSessionInfo(
    int PlayerId,
    string Name,
    int LegsWon,
    int CurrentScore,
    bool IsStarting,
    List<TeamMemberInfo>? Members = null
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
    CricketDisplayState? CricketState,
    bool IsDoubles = false,
    int CurrentSideId = 0,
    string? CurrentThrowerName = null
);

/// <summary>
/// Informations d'un côté (joueur ou paire) pour spectateur
/// </summary>
public record PlayerSpectatorInfo(
    int PlayerId,
    string Name,
    int LegsWon,
    int CurrentScore,
    List<TeamMemberInfo>? Members = null
);

/// <summary>
/// Résumé d'une session active pour l'écran TV
/// </summary>
public record ActiveSessionSummaryResponse(
    int MatchId,
    string TournamentName,
    string Player1Name,
    string Player2Name,
    int Player1LegsWon,
    int Player2LegsWon,
    int LegsToWin,
    GameMode GameMode,
    int CurrentLeg,
    DateTime? StartedAt
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
