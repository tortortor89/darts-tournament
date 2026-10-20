using System.ComponentModel.DataAnnotations;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

public record CreateSeasonRequest(
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    string Name,

    DateTime? StartDate = null,
    DateTime? EndDate = null,

    [Range(1, 10, ErrorMessage = "Le nombre de legs à gagner doit être entre 1 et 10")]
    int LegsToWin = 3
);

public record UpdateSeasonRequest(
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    string Name,

    DateTime? StartDate,
    DateTime? EndDate,

    [Range(1, 10, ErrorMessage = "Le nombre de legs à gagner doit être entre 1 et 10")]
    int LegsToWin,

    SeasonStatus Status
);

public record AttachToSeasonRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Identifiant invalide")]
    int Id
);

public record SeasonResponse(
    int Id,
    string Name,
    DateTime? StartDate,
    DateTime? EndDate,
    SeasonStatus Status,
    int LegsToWin,
    DateTime CreatedAt,
    int TournamentCount,
    int CircuitCount,
    int ChampionshipCount
);

public record SeasonDetailResponse(
    int Id,
    string Name,
    DateTime? StartDate,
    DateTime? EndDate,
    SeasonStatus Status,
    int LegsToWin,
    DateTime CreatedAt,
    // Tournois rattachés directement (hors circuit)
    List<TournamentResponse> Tournaments,
    List<CircuitResponse> Circuits,
    List<ChampionshipResponse> Championships
);
