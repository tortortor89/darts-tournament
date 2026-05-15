namespace DartsTournament.Api.Models;

/// <summary>
/// État du jeu Cricket pour un leg
/// </summary>
public class CricketGameState
{
    public Dictionary<int, CricketPlayerState> PlayerStates { get; set; } = new();

    public CricketGameState()
    {
    }

    public CricketGameState(int player1Id, int player2Id)
    {
        PlayerStates[player1Id] = new CricketPlayerState();
        PlayerStates[player2Id] = new CricketPlayerState();
    }
}

/// <summary>
/// État Cricket d'un joueur (cibles + points)
/// </summary>
public class CricketPlayerState
{
    // Nombre de hits par cible (0-3, 3 = fermé)
    public Dictionary<int, int> TargetHits { get; set; } = new()
    {
        { 15, 0 }, { 16, 0 }, { 17, 0 }, { 18, 0 }, { 19, 0 }, { 20, 0 }, { 25, 0 }  // 25 = Bull
    };

    // Points accumulés
    public int Score { get; set; } = 0;

    // Helper: toutes les cibles fermées ?
    public bool AllTargetsClosed() => TargetHits.Values.All(h => h >= 3);
}

/// <summary>
/// Résultat d'un throw Cricket
/// </summary>
public class CricketThrowResult
{
    public int Target { get; set; }           // 15-20 ou 25 (Bull)
    public int HitsCount { get; set; }        // 1 (simple), 2 (double), 3 (triple)
    public int PointsScored { get; set; }     // Points marqués sur cette volée
    public bool ClosedTarget { get; set; }    // Vrai si la cible vient d'être fermée
}
