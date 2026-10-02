namespace DartsTournament.Api.Models;

// Règle du barème : les rangs finaux de MinRank à MaxRank inclus rapportent Points
public class CircuitPointsRule
{
    public int Id { get; set; }

    public int CircuitId { get; set; }
    public Circuit Circuit { get; set; } = null!;

    public int MinRank { get; set; }
    public int MaxRank { get; set; }
    public int Points { get; set; }
}
