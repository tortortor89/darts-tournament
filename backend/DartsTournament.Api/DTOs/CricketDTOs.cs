using System.ComponentModel.DataAnnotations;

namespace DartsTournament.Api.DTOs;

/// <summary>
/// Requête pour enregistrer un throw Cricket
/// </summary>
public record RecordCricketThrowRequest(
    [Required]
    int Target,  // 15, 16, 17, 18, 19, 20, 25 (Bull)

    [Range(1, 3, ErrorMessage = "Le nombre de hits doit être entre 1 et 3")]
    int Hits  // 1 = simple, 2 = double, 3 = triple
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
/// Réponse après un throw Cricket
/// </summary>
public record CricketThrowResponse(
    int PlayerId,
    string PlayerName,
    int Target,
    int Hits,
    int PointsScored,
    bool ClosedTarget,
    CricketDisplayState CurrentState
);
