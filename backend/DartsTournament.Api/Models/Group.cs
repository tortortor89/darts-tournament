namespace DartsTournament.Api.Models;

public class Group
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Qualifiés de cette poule (format FSGT) ; null = Tournament.QualifiersPerGroup
    public int? QualifierCount { get; set; }

    // Départage manuel des égalités parfaites (ex. 701 joué hors appli) :
    // ids de côtés dans l'ordre, « 12,5,7 »
    public string? TieBreakOrder { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<TournamentTeam> Teams { get; set; } = new List<TournamentTeam>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
