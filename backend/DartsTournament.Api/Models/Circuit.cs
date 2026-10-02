namespace DartsTournament.Api.Models;

public class Circuit
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Points attribués quand le rang final ne correspond à aucune règle du barème
    public int ParticipationPoints { get; set; } = 10;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    public ICollection<CircuitPointsRule> PointsRules { get; set; } = new List<CircuitPointsRule>();
}
