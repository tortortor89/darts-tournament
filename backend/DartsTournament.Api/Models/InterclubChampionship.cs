namespace DartsTournament.Api.Models;

/// <summary>
/// Championnat interclubs : des clubs s'affrontent en rencontres (soirées club
/// contre club) composées de X simples + Y doubles, avec calendrier aller-retour
/// et classement de saison.
/// </summary>
public class InterclubChampionship
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ChampionshipStatus Status { get; set; } = ChampionshipStatus.Draft;

    // Composition d'une rencontre
    public int SinglesPerEncounter { get; set; } = 4;
    public int DoublesPerEncounter { get; set; } = 2;
    public int LegsToWin { get; set; } = 3;
    public GameMode GameMode { get; set; } = GameMode.FiveOhOne;
    public bool DoubleOut { get; set; } = true;

    // Barème du classement de saison
    public int PointsForWin { get; set; } = 2;
    public int PointsForDraw { get; set; } = 1;
    public int PointsForLoss { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChampionshipClub> Clubs { get; set; } = new List<ChampionshipClub>();
    public ICollection<ChampionshipRosterEntry> Roster { get; set; } = new List<ChampionshipRosterEntry>();
    public ICollection<InterclubEncounter> Encounters { get; set; } = new List<InterclubEncounter>();
}
