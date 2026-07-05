namespace DartsTournament.Api.Models;

// Club engagé dans un championnat (clé composite ChampionshipId + ClubId)
public class ChampionshipClub
{
    public int ChampionshipId { get; set; }
    public InterclubChampionship Championship { get; set; } = null!;

    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;
}
