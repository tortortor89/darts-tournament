namespace DartsTournament.Api.DTOs;

/// <summary>
/// Événement émis quand une volée est enregistrée
/// </summary>
public record ThrowRecordedEvent(
    int MatchId,
    ThrowResponse Throw,
    int Player1CurrentScore,
    int Player2CurrentScore,
    int CurrentPlayerId,
    MatchStatsResponse Stats
);

/// <summary>
/// Événement émis quand un leg est gagné
/// </summary>
public record LegWonEvent(
    int MatchId,
    int LegNumber,
    int WinnerId,
    string WinnerName,
    int Player1LegsWon,
    int Player2LegsWon,
    int NewCurrentLeg,
    LegSummary LegSummary
);

/// <summary>
/// Événement émis quand le match est terminé
/// </summary>
public record MatchFinishedEvent(
    int MatchId,
    int WinnerId,
    string WinnerName,
    int Player1LegsWon,
    int Player2LegsWon,
    MatchStatsResponse FinalStats
);

/// <summary>
/// Événement émis quand une session démarre
/// </summary>
public record SessionStartedEvent(
    int MatchId,
    MatchSessionResponse Session
);
