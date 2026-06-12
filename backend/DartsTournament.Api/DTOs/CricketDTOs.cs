using System.ComponentModel.DataAnnotations;

namespace DartsTournament.Api.DTOs;

/// <summary>
/// Requête pour enregistrer une visite complète au Cricket (3 fléchettes max)
/// </summary>
public record RecordCricketTurnRequest(
    [Required]
    List<CricketHit> Hits  // Liste des hits de la visite (peut être vide pour 3 ratés)
);

/// <summary>
/// Un hit Cricket : cible touchée et nombre de marques
/// </summary>
public record CricketHit(
    [Required]
    int Target,  // 15, 16, 17, 18, 19, 20, 25 (Bull)

    [Range(1, 9, ErrorMessage = "Le nombre de marques doit être entre 1 et 9")]
    int Marks  // Nombre de marques sur cette cible dans la visite
);

/// <summary>
/// État Cricket pour l'affichage
/// </summary>
public record CricketDisplayState(
    Dictionary<int, CricketTargetState> Player1Targets,
    Dictionary<int, CricketTargetState> Player2Targets,
    int Player1Score,
    int Player2Score
);

/// <summary>
/// État d'une cible pour un joueur
/// </summary>
public record CricketTargetState(
    int Target,
    int Hits,      // 0-3
    bool Closed    // Hits >= 3
);

/// <summary>
/// Réponse après une visite Cricket
/// </summary>
public record CricketTurnResponse(
    int PlayerId,
    string PlayerName,
    List<CricketHitResult> HitResults,  // Résultats pour chaque cible touchée
    int TotalPointsScored,
    CricketDisplayState CurrentState
);

/// <summary>
/// Résultat d'un hit sur une cible lors d'une visite
/// </summary>
public record CricketHitResult(
    int Target,
    int Marks,
    int PointsScored,
    bool ClosedTarget
);

/// <summary>
/// Forme stockée dans Throw.CricketDataJson ({ "hits": [...], "results": [...] })
/// </summary>
public class CricketThrowData
{
    [System.Text.Json.Serialization.JsonPropertyName("hits")]
    public List<CricketHit>? Hits { get; set; }
}
