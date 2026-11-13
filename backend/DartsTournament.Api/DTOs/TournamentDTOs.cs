using System.ComponentModel.DataAnnotations;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

public record CreateTournamentRequest(
    [Required(ErrorMessage = "Le nom du tournoi est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom du tournoi doit contenir entre 2 et 100 caractères")]
    string Name,

    [Required(ErrorMessage = "Le format du tournoi est requis")]
    TournamentFormat Format,

    DateTime? StartDate,

    [Range(1, 20, ErrorMessage = "Le nombre de groupes doit être entre 1 et 20")]
    int? NumberOfGroups = null,

    [Range(2, 50, ErrorMessage = "Le nombre de joueurs par groupe doit être entre 2 et 50")]
    int? PlayersPerGroup = null,

    [Range(1, 10, ErrorMessage = "Le nombre de qualifiés par groupe doit être entre 1 et 10")]
    int? QualifiersPerGroup = null,

    bool HasKnockoutPhase = true,
    bool AllowBracketReset = true,

    int? CircuitId = null,

    // Tournoi en double : les inscriptions se font par paires (TeamSize = 2)
    bool IsDoubles = false,

    // Saison (hors circuit : un tournoi de circuit prend la saison du circuit)
    int? SeasonId = null,

    // Format FSGT (poules selon le nombre d'inscrits) ; null = activé par défaut
    // pour un tournoi en poules rattaché à une saison
    bool? UseFsgtFormat = null,

    // Tableau d'un événement (prend la saison de l'événement, sauf circuit)
    int? EventId = null
);

public record UpdateTournamentRequest(
    [Required(ErrorMessage = "Le nom du tournoi est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom du tournoi doit contenir entre 2 et 100 caractères")]
    string Name,

    DateTime? StartDate
);

public record AddPlayerToTournamentRequest(
    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du joueur est invalide")]
    int PlayerId,

    [Range(1, 1000, ErrorMessage = "Le seed doit être entre 1 et 1000")]
    int? Seed
);

public record TournamentResponse(
    int Id,
    string Name,
    TournamentFormat Format,
    TournamentStatus Status,
    DateTime? StartDate,
    DateTime CreatedAt,
    int PlayerCount,
    int? NumberOfGroups,
    int? PlayersPerGroup,
    int? QualifiersPerGroup,
    bool HasKnockoutPhase,
    bool AllowBracketReset,
    int? CircuitId = null,
    string? CircuitName = null,
    bool IsDoubles = false,
    // Saison effective (propre ou héritée du circuit)
    int? SeasonId = null,
    string? SeasonName = null,
    bool UseFsgtFormat = false,
    int? EventId = null,
    string? EventName = null
);

public record TournamentDetailResponse(
    int Id,
    string Name,
    TournamentFormat Format,
    TournamentStatus Status,
    DateTime? StartDate,
    DateTime CreatedAt,
    int? NumberOfGroups,
    int? PlayersPerGroup,
    int? QualifiersPerGroup,
    bool HasKnockoutPhase,
    bool AllowBracketReset,
    List<TournamentPlayerResponse> Players,
    List<GroupResponse> Groups,
    List<MatchResponse> Matches,
    int? CircuitId = null,
    string? CircuitName = null,
    bool IsDoubles = false,
    List<TournamentTeamResponse>? Teams = null,
    int? SeasonId = null,
    string? SeasonName = null,
    bool UseFsgtFormat = false,
    int? EventId = null,
    string? EventName = null
);

public record TournamentPlayerResponse(int PlayerId, string FirstName, string LastName, string? Nickname, int? Seed, int? GroupId, RegistrationStatus Status);
public record GroupResponse(int Id, string Name, List<TournamentPlayerResponse> Players, List<TournamentTeamResponse>? Teams = null, int? QualifierCount = null);

public record CreateTournamentTeamRequest(
    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du joueur 1 est invalide")]
    int Player1Id,

    [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du joueur 2 est invalide")]
    int Player2Id,

    [Range(1, 1000, ErrorMessage = "Le seed doit être entre 1 et 1000")]
    int? Seed = null
);

public record TournamentTeamResponse(
    int Id,
    int Player1Id,
    string Player1Name,
    int Player2Id,
    string Player2Name,
    string Name,
    int? Seed,
    int? GroupId,
    RegistrationStatus Status = RegistrationStatus.Approved
);

public record GroupStandingResponse(
    int GroupId,
    string GroupName,
    List<PlayerStandingResponse> Standings
);

public record PlayerStandingResponse(
    int PlayerId,
    string PlayerName,
    int Played,
    int Won,
    int Lost,
    int PointsFor,
    int PointsAgainst,
    int PointsDiff,
    int Points,
    int Rank,
    // Égalité parfaite à départager à la main (rang partagé)
    bool TieUnresolved = false
);

public record SetGroupTieBreakRequest(
    [Required(ErrorMessage = "L'ordre de départage est requis")]
    [MinLength(2, ErrorMessage = "Au moins 2 côtés à départager")]
    List<int> SideIds
);
