namespace DartsTournament.Api.Models;

public class Circuit
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Points attribués quand le rang final ne correspond à aucune règle du barème
    public int ParticipationPoints { get; set; } = 10;

    // Barème FSGT « Point Tournois » (tour atteint selon le nombre d'inscrits) au lieu
    // du barème personnalisable par rang (PointsRules + ParticipationPoints)
    public bool UseFsgtPoints { get; set; }

    // Saison (optionnelle) : s'applique aussi aux tournois du circuit
    public int? SeasonId { get; set; }
    public Season? Season { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    public ICollection<CircuitPointsRule> PointsRules { get; set; } = new List<CircuitPointsRule>();
}
