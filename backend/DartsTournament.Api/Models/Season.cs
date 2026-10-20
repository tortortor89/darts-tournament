namespace DartsTournament.Api.Models;

/// <summary>
/// Saison (mode Ligue) : regroupe des tournois, circuits et championnats interclubs
/// et leur impose un règlement commun (501 double out, nombre de legs par défaut).
/// Un tournoi rattaché à un circuit hérite de la saison du circuit.
/// </summary>
public class Season
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Clôturée : plus aucun rattachement possible
    public SeasonStatus Status { get; set; } = SeasonStatus.InProgress;

    // Règlement : toujours 501 double out, legs par défaut des matchs de tournoi
    public int LegsToWin { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    public ICollection<Circuit> Circuits { get; set; } = new List<Circuit>();
    public ICollection<InterclubChampionship> Championships { get; set; } = new List<InterclubChampionship>();
}
