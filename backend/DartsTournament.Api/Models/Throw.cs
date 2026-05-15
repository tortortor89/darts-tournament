namespace DartsTournament.Api.Models;

/// <summary>
/// Représente une volée (3 fléchettes) dans un match
/// </summary>
public class Throw
{
    public int Id { get; set; }

    public int MatchSessionId { get; set; }
    public MatchSession MatchSession { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int LegNumber { get; set; }
    public int ThrowNumber { get; set; }

    // Score total de la volée
    public int Score { get; set; }

    // Détail des fléchettes (optionnel, pour le mode fléchette par fléchette)
    public string? Dart1 { get; set; }  // Ex: "T20", "D16", "S5", "BULL", "DB"
    public string? Dart2 { get; set; }
    public string? Dart3 { get; set; }

    // Score restant après cette volée
    public int RemainingScore { get; set; }

    // Statistiques détaillées (optionnel)
    public int? DartsUsed { get; set; }        // Nombre de fléchettes utilisées (1, 2 ou 3) - surtout pour les checkouts
    public int? DoublesAttempted { get; set; } // Nombre de doubles tentés dans cette volée - pour stats précises

    // Métadonnées
    public bool IsCheckout { get; set; } = false;
    public bool IsBust { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Cricket data (optionnel)
    public string? CricketDataJson { get; set; }  // Optionnel: { "target": 20, "hits": 3, "pointsScored": 60 }
}
