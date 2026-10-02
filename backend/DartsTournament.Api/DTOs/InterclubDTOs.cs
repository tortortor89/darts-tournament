using System.ComponentModel.DataAnnotations;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

// ----- Clubs -----

public record CreateClubRequest(
    [Required(ErrorMessage = "Le nom du club est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom du club doit contenir entre 2 et 100 caractères")]
    string Name
);

public record ClubResponse(
    int Id,
    string Name,
    DateTime CreatedAt,
    int PlayerCount
);

public record ClubDetailResponse(
    int Id,
    string Name,
    DateTime CreatedAt,
    List<ClubPlayerResponse> Players
);

public record ClubPlayerResponse(int PlayerId, string Name, string? Nickname);

public record AssignPlayerToClubRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Identifiant de joueur invalide")]
    int PlayerId
);

// ----- Championnats -----

public record CreateChampionshipRequest(
    [Required(ErrorMessage = "Le nom du championnat est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    string Name,

    [Range(0, 20, ErrorMessage = "Le nombre de simples doit être entre 0 et 20")]
    int SinglesPerEncounter = 4,

    [Range(0, 10, ErrorMessage = "Le nombre de doubles doit être entre 0 et 10")]
    int DoublesPerEncounter = 2,

    [Range(1, 10, ErrorMessage = "Le nombre de legs à gagner doit être entre 1 et 10")]
    int LegsToWin = 3,

    GameMode GameMode = GameMode.FiveOhOne,
    bool DoubleOut = true,

    [Range(0, 10, ErrorMessage = "Les points de victoire doivent être entre 0 et 10")]
    int PointsForWin = 2,

    [Range(0, 10, ErrorMessage = "Les points de nul doivent être entre 0 et 10")]
    int PointsForDraw = 1,

    [Range(0, 10, ErrorMessage = "Les points de défaite doivent être entre 0 et 10")]
    int PointsForLoss = 0
);

public record ChampionshipResponse(
    int Id,
    string Name,
    ChampionshipStatus Status,
    int SinglesPerEncounter,
    int DoublesPerEncounter,
    int LegsToWin,
    GameMode GameMode,
    bool DoubleOut,
    int PointsForWin,
    int PointsForDraw,
    int PointsForLoss,
    DateTime CreatedAt,
    int ClubCount
);

public record ChampionshipDetailResponse(
    int Id,
    string Name,
    ChampionshipStatus Status,
    int SinglesPerEncounter,
    int DoublesPerEncounter,
    int LegsToWin,
    GameMode GameMode,
    bool DoubleOut,
    int PointsForWin,
    int PointsForDraw,
    int PointsForLoss,
    DateTime CreatedAt,
    List<ChampionshipClubResponse> Clubs
);

public record ChampionshipClubResponse(
    int ClubId,
    string ClubName,
    List<ClubPlayerResponse> Roster
);

public record AttachClubRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Identifiant de club invalide")]
    int ClubId
);

public record SetRosterRequest(
    [Required]
    List<int> PlayerIds
);

// ----- Calendrier / rencontres -----

public record EncounterSummaryResponse(
    int Id,
    int Round,
    int HomeClubId,
    string HomeClubName,
    int AwayClubId,
    string AwayClubName,
    DateTime? ScheduledAt,
    EncounterStatus Status,
    int HomeScore,
    int AwayScore
);

public record CalendarRoundResponse(
    int Round,
    List<EncounterSummaryResponse> Encounters
);

public record EncounterDetailResponse(
    int Id,
    int ChampionshipId,
    string ChampionshipName,
    int Round,
    int HomeClubId,
    string HomeClubName,
    int AwayClubId,
    string AwayClubName,
    DateTime? ScheduledAt,
    EncounterStatus Status,
    int HomeScore,
    int AwayScore,
    int SinglesPerEncounter,
    int DoublesPerEncounter,
    List<ClubPlayerResponse> HomeRoster,
    List<ClubPlayerResponse> AwayRoster,
    List<EncounterBoardResponse> Boards
);

// Un board = un match de la rencontre (Position 1..S = simples, S+1..S+D = doubles)
public record EncounterBoardResponse(
    int Position,
    bool IsDoubles,
    MatchResponse? Match
);

public record BoardLineupDto(
    int Position,
    List<int> HomePlayerIds,
    List<int> AwayPlayerIds
);

public record SetEncounterLineupRequest(
    [Required]
    List<BoardLineupDto> Boards
);

// ----- Classement -----

public record InterclubStandingResponse(
    int ClubId,
    string ClubName,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int Points,
    int MatchesWon,
    int MatchesLost,
    int Rank
);
