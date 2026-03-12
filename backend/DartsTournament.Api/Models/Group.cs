namespace DartsTournament.Api.Models;

public class Group
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public ICollection<TournamentPlayer> Players { get; set; } = new List<TournamentPlayer>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
