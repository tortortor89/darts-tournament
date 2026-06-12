namespace DartsTournament.Api.Models;

public enum TournamentFormat
{
    SingleElimination,
    RoundRobin,
    GroupStage,
    DoubleElimination
}

public enum BracketType
{
    None = 0,
    Winners = 1,
    Losers = 2,
    GrandFinal = 3
}

public enum TournamentStatus
{
    Draft,
    InProgress,
    Completed
}

public enum MatchStatus
{
    Pending,
    InProgress,
    Completed
}

public enum UserRole
{
    User,
    Admin
}

public enum GameMode
{
    // Pour les modes x01, la valeur correspond au score de départ
    Cricket = 1,
    ThreeOhOne = 301,
    FiveOhOne = 501
}

public static class GameModeExtensions
{
    /// <summary>
    /// Vrai pour les modes de type x01 (501, 301)
    /// </summary>
    public static bool IsX01(this GameMode mode) =>
        mode == GameMode.FiveOhOne || mode == GameMode.ThreeOhOne;

    /// <summary>
    /// Score de départ d'un leg pour les modes x01
    /// </summary>
    public static int StartingScore(this GameMode mode) => (int)mode;
}

public enum MatchSessionStatus
{
    Configuration,
    InProgress,
    Finished,
    Cancelled
}

public enum RegistrationStatus
{
    Pending,
    Approved,
    Rejected
}
