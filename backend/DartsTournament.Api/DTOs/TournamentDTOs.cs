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
    bool AllowBracketReset = true
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
    bool AllowBracketReset
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
    List<MatchResponse> Matches
);

public record TournamentPlayerResponse(int PlayerId, string FirstName, string LastName, string? Nickname, int? Seed, int? GroupId);
public record GroupResponse(int Id, string Name, List<TournamentPlayerResponse> Players);

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
    int Rank
);
