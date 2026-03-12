namespace DartsTournament.Api.Models;

public class TournamentPlayer
{
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int? Seed { get; set; }
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
}
