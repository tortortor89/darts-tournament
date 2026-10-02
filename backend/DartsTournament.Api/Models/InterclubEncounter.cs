namespace DartsTournament.Api.Models;

/// <summary>
/// Rencontre interclubs : club domicile contre club extérieur lors d'une journée.
/// Convention : côté 1 des matchs = club domicile, côté 2 = club extérieur.
/// HomeScore/AwayScore (matchs gagnés) sont dénormalisés et recalculés
/// intégralement à chaque (re)complétion de match — idempotent.
/// </summary>
public class InterclubEncounter
{
    public int Id { get; set; }

    public int ChampionshipId { get; set; }
    public InterclubChampionship Championship { get; set; } = null!;

    // Journée du calendrier (1..N)
    public int Round { get; set; }

    public int HomeClubId { get; set; }
    public Club HomeClub { get; set; } = null!;

    public int AwayClubId { get; set; }
    public Club AwayClub { get; set; } = null!;

    public DateTime? ScheduledAt { get; set; }
    public EncounterStatus Status { get; set; } = EncounterStatus.Pending;

    public int HomeScore { get; set; } = 0;
    public int AwayScore { get; set; } = 0;

    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
