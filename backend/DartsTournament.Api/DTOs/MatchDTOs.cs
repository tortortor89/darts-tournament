using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

public record UpdateMatchScoreRequest(int Player1Score, int Player2Score);
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
    bool IsKnockoutMatch
);
