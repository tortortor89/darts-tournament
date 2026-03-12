using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

public record CreateTournamentRequest(
    string Name,
    TournamentFormat Format,
    DateTime? StartDate,
    int? NumberOfGroups = null,
    int? PlayersPerGroup = null,
    int? QualifiersPerGroup = null,
    bool HasKnockoutPhase = true
);
public record UpdateTournamentRequest(string Name, DateTime? StartDate);
public record AddPlayerToTournamentRequest(int PlayerId, int? Seed);
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
    bool HasKnockoutPhase
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
