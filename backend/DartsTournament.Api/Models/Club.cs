namespace DartsTournament.Api.Models;

public class Club
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<ChampionshipClub> ChampionshipClubs { get; set; } = new List<ChampionshipClub>();
}
