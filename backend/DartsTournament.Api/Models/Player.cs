namespace DartsTournament.Api.Models;

public class Player
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Nickname { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}
