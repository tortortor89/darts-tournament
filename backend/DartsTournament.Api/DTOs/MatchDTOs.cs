using System.ComponentModel.DataAnnotations;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

public record UpdateMatchScoreRequest(
    [Range(0, 1000, ErrorMessage = "Le score du joueur 1 doit être entre 0 et 1000")]
    int Player1Score,

    [Range(0, 1000, ErrorMessage = "Le score du joueur 2 doit être entre 0 et 1000")]
    int Player2Score
);

public record TeamMemberInfo(int PlayerId, string Name);

// En double, Player1Id/Player2Id/WinnerId portent des ids de TournamentTeam et les
// noms le label de la paire (« Alice D. / Bob M. »). Ne pas utiliser ces ids pour
// naviguer vers un profil joueur quand IsDoubles est vrai.
public record MatchResponse(
    int Id,
    int TournamentId,
    int? GroupId,
    int Round,
    int Position,
    int? Player1Id,
    string? Player1Name,
    int? Player2Id,
    string? Player2Name,
    int? Player1Score,
    int? Player2Score,
    int? WinnerId,
    MatchStatus Status,
    DateTime? ScheduledAt,
    bool IsKnockoutMatch,
    BracketType BracketType,
    bool IsBracketReset,
    bool IsDoubles = false,
    List<TeamMemberInfo>? Team1Members = null,
    List<TeamMemberInfo>? Team2Members = null
);
