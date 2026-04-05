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
    FiveOhOne = 501
}

public enum MatchSessionStatus
{
    Configuration,
    InProgress,
    Finished,
    Cancelled
}
