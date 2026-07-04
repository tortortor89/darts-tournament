namespace DartsTournament.Api.Models;

public class Tournament
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TournamentFormat Format { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.Draft;
    public DateTime? StartDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Configuration GroupStage
    public int? NumberOfGroups { get; set; }
    public int? PlayersPerGroup { get; set; }
    public int? QualifiersPerGroup { get; set; }
    public bool HasKnockoutPhase { get; set; } = true;

    // Configuration Double Elimination
    public bool AllowBracketReset { get; set; } = true;

    // Circuit (optionnel)
    public int? CircuitId { get; set; }
    public Circuit? Circuit { get; set; }

    // Taille d'un côté : 1 = simple, 2 = double (paires)
    public int TeamSize { get; set; } = 1;

    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
