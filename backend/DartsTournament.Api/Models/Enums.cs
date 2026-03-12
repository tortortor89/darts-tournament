namespace DartsTournament.Api.Models;

public enum TournamentFormat
{
    SingleElimination,
    RoundRobin,
    GroupStage
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
