namespace DartsTournament.Api.Models;

public class Match
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int Position { get; set; }

    // Un match appartient soit à un tournoi, soit à une rencontre interclubs
    // (exactement un des deux — check constraint en base)
    public int? TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    public int? EncounterId { get; set; }
    public InterclubEncounter? Encounter { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public int? Player1Id { get; set; }
    public Player? Player1 { get; set; }

    public int? Player2Id { get; set; }
    public Player? Player2 { get; set; }

    // Tournois en double : les côtés sont des paires (les FK joueur restent null)
    public int? Team1Id { get; set; }
    public TournamentTeam? Team1 { get; set; }

    public int? Team2Id { get; set; }
    public TournamentTeam? Team2 { get; set; }

    public int? Player1Score { get; set; }
    public int? Player2Score { get; set; }

    public int? WinnerId { get; set; }
    public Player? Winner { get; set; }

    public int? WinnerTeamId { get; set; }
    public TournamentTeam? WinnerTeam { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public DateTime? ScheduledAt { get; set; }
    public bool IsKnockoutMatch { get; set; } = false;

    // Double Elimination
    public BracketType BracketType { get; set; } = BracketType.None;
    public bool IsBracketReset { get; set; } = false;
}
