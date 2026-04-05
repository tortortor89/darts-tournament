namespace DartsTournament.Api.Models;

/// <summary>
/// Représente une session de match en cours avec sa configuration et son état
/// </summary>
public class MatchSession
{
    public int Id { get; set; }

    // Lien vers le match du tournoi
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // Configuration
    public int LegsToWin { get; set; } = 3;
    public GameMode GameMode { get; set; } = GameMode.FiveOhOne;
    public int StartingPlayerId { get; set; }

    // État de la partie
    public MatchSessionStatus Status { get; set; } = MatchSessionStatus.Configuration;
    public int Player1LegsWon { get; set; } = 0;
    public int Player2LegsWon { get; set; } = 0;
    public int Player1CurrentScore { get; set; } = 501;
    public int Player2CurrentScore { get; set; } = 501;
    public int CurrentLeg { get; set; } = 1;
    public int CurrentPlayerId { get; set; }
    public int CurrentLegStartingPlayerId { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    // Navigation
    public ICollection<Throw> Throws { get; set; } = new List<Throw>();
}
