namespace DartsTournament.Api.Models;

public class Match
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int Position { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public int? Player1Id { get; set; }
    public Player? Player1 { get; set; }

    public int? Player2Id { get; set; }
    public Player? Player2 { get; set; }

    public int? Player1Score { get; set; }
    public int? Player2Score { get; set; }

    public int? WinnerId { get; set; }
    public Player? Winner { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public DateTime? ScheduledAt { get; set; }
    public bool IsKnockoutMatch { get; set; } = false;
}
