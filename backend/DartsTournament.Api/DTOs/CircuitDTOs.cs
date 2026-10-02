using System.ComponentModel.DataAnnotations;

namespace DartsTournament.Api.DTOs;

public record PointsRuleDto(
    [Range(1, 1000, ErrorMessage = "Le rang minimum doit être entre 1 et 1000")]
    int MinRank,

    [Range(1, 1000, ErrorMessage = "Le rang maximum doit être entre 1 et 1000")]
    int MaxRank,

    [Range(0, 10000, ErrorMessage = "Les points doivent être entre 0 et 10000")]
    int Points
);

public record CreateCircuitRequest(
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    string Name,

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    string? Description = null,

    [Range(0, 10000, ErrorMessage = "Les points de participation doivent être entre 0 et 10000")]
    int ParticipationPoints = 10,

    // null => barème par défaut
    List<PointsRuleDto>? PointsRules = null
);

public record UpdateCircuitRequest(
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    string Name,

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    string? Description,

    [Range(0, 10000, ErrorMessage = "Les points de participation doivent être entre 0 et 10000")]
    int ParticipationPoints,

    // Remplacement complet du barème
    List<PointsRuleDto> PointsRules
);

public record AttachTournamentRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Identifiant de tournoi invalide")]
    int TournamentId
);

public record CircuitResponse(
    int Id,
    string Name,
    string? Description,
    int ParticipationPoints,
    DateTime CreatedAt,
    int TournamentCount,
    int CompletedTournamentCount,
    List<PointsRuleDto> PointsRules
);

public record CircuitDetailResponse(
    int Id,
    string Name,
    string? Description,
    int ParticipationPoints,
    DateTime CreatedAt,
    List<PointsRuleDto> PointsRules,
    List<TournamentResponse> Tournaments
);

public record CircuitTournamentPointsResponse(
    int TournamentId,
    string TournamentName,
    int FinalRank,
    int Points
);

public record CircuitStandingResponse(
    int PlayerId,
    string PlayerName,
    int TournamentsPlayed,
    int TotalPoints,
    int Rank,
    List<CircuitTournamentPointsResponse> Details
);
