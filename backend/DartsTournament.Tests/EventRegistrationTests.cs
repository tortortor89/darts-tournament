using DartsTournament.Api.Models;
using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class EventRegistrationTests
{
    [Fact]
    public void RegistrationOpen_WithoutDeadline()
    {
        var evt = new Event { Name = "Open" };

        Assert.True(evt.IsRegistrationOpen(DateTime.UtcNow));
    }

    [Fact]
    public void RegistrationOpen_UntilDeadline()
    {
        var deadline = new DateTime(2026, 12, 1, 20, 0, 0, DateTimeKind.Utc);
        var evt = new Event { Name = "Open", RegistrationDeadline = deadline };

        Assert.True(evt.IsRegistrationOpen(deadline.AddMinutes(-1)));
        Assert.True(evt.IsRegistrationOpen(deadline));
        Assert.False(evt.IsRegistrationOpen(deadline.AddMinutes(1)));
    }

    [Fact]
    public void NewTeam_IsApprovedByDefault()
    {
        // Paires créées par l'admin (et interclubs) : validées d'office
        Assert.Equal(RegistrationStatus.Approved, new TournamentTeam().Status);
    }

    private static Tournament DoublesWithPendingTeam()
    {
        var t = new Tournament { Id = 1, Name = "Double", Format = TournamentFormat.SingleElimination, TeamSize = 2 };
        var players = Enumerable.Range(1, 6)
            .Select(i => new Player { Id = i, FirstName = $"J{i}", LastName = "Test" })
            .ToList();

        t.Teams.Add(new TournamentTeam { Id = 10, TournamentId = 1, Player1Id = 1, Player1 = players[0], Player2Id = 2, Player2 = players[1] });
        t.Teams.Add(new TournamentTeam { Id = 11, TournamentId = 1, Player1Id = 3, Player1 = players[2], Player2Id = 4, Player2 = players[3] });
        t.Teams.Add(new TournamentTeam
        {
            Id = 12, TournamentId = 1, Player1Id = 5, Player1 = players[4], Player2Id = 6, Player2 = players[5],
            Status = RegistrationStatus.Pending
        });
        return t;
    }

    [Fact]
    public void GetSides_ExcludesPendingTeams()
    {
        var sides = MatchSideAccessor.GetSides(DoublesWithPendingTeam());

        Assert.Equal(new[] { 10, 11 }, sides.Select(s => s.Id));
    }

    [Fact]
    public void SideMembers_ExcludesPendingTeams()
    {
        var members = FinalPlacementCalculator.GetSideMembers(DoublesWithPendingTeam());

        Assert.False(members.ContainsKey(12));
    }
}
