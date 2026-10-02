namespace DartsTournament.Api.Models;

/// <summary>
/// Effectif déclaré : joueur autorisé à jouer pour un club dans un championnat.
/// Clé composite (ChampionshipId, PlayerId) : un joueur ne joue que pour un seul
/// club par championnat.
/// </summary>
public class ChampionshipRosterEntry
{
    public int ChampionshipId { get; set; }
    public InterclubChampionship Championship { get; set; } = null!;

    public int ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
}
