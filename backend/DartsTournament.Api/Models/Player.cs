namespace DartsTournament.Api.Models;

public class Player
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Nickname { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // User link (nullable - a Player can exist without a linked User)
    public int? UserId { get; set; }
    public User? User { get; set; }

    // Appartenance générale à un club (préremplit les effectifs interclubs)
    public int? ClubId { get; set; }
    public Club? Club { get; set; }

    public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
}
