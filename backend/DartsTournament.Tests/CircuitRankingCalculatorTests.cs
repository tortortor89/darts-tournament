using DartsTournament.Api.Models;
using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class CircuitRankingCalculatorTests
{
    private const int ParticipationPoints = 10;

    // Barème par défaut : 1er=100, 2e=60, 3e-4e=40, 5e-8e=20
    private static List<CircuitPointsRule> DefaultRules() => new()
    {
        new CircuitPointsRule { MinRank = 1, MaxRank = 1, Points = 100 },
        new CircuitPointsRule { MinRank = 2, MaxRank = 2, Points = 60 },
        new CircuitPointsRule { MinRank = 3, MaxRank = 4, Points = 40 },
        new CircuitPointsRule { MinRank = 5, MaxRank = 8, Points = 20 }
    };

    private static Tournament MakeTournament(int id, string name) =>
        new() { Id = id, Name = name };

    // ----- PointsForRank -----

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 60)]
    [InlineData(3, 40)]
    [InlineData(4, 40)]
    [InlineData(5, 20)]
    [InlineData(8, 20)]
    [InlineData(9, ParticipationPoints)]  // hors barème -> participation
    [InlineData(15, ParticipationPoints)]
    public void PointsForRank_BaremeParDefaut(int rank, int expectedPoints)
    {
        Assert.Equal(expectedPoints,
            CircuitRankingCalculator.PointsForRank(rank, DefaultRules(), ParticipationPoints));
    }

    [Fact]
    public void PointsForRank_BaremeVide_ToutLeMondeAuxPointsDeParticipation()
    {
        Assert.Equal(ParticipationPoints,
            CircuitRankingCalculator.PointsForRank(1, new List<CircuitPointsRule>(), ParticipationPoints));
    }

    // ----- Aggregate -----

    [Fact]
    public void Aggregate_EgaliteTroisiemes_MemesPoints()
    {
        var t1 = MakeTournament(1, "Tournoi 1");
        var placements = new List<FinalPlacement>
        {
            new(1, "Alice", 1),
            new(2, "Bob", 2),
            new(3, "Carol", 3),
            new(4, "David", 3)
        };

        var standings = CircuitRankingCalculator.Aggregate(
            new[] { (t1, placements) }, DefaultRules(), ParticipationPoints);

        Assert.Equal(40, standings.First(s => s.PlayerId == 3).TotalPoints);
        Assert.Equal(40, standings.First(s => s.PlayerId == 4).TotalPoints);
    }

    [Fact]
    public void Aggregate_SommeSurDeuxTournois()
    {
        var t1 = MakeTournament(1, "Tournoi 1");
        var t2 = MakeTournament(2, "Tournoi 2");
        var tournaments = new[]
        {
            (t1, new List<FinalPlacement> { new(1, "Alice", 1), new(2, "Bob", 2) }),
            (t2, new List<FinalPlacement> { new(1, "Alice", 2), new(2, "Bob", 1) })
        };

        var standings = CircuitRankingCalculator.Aggregate(
            tournaments, DefaultRules(), ParticipationPoints);

        // Alice : 100 + 60 = 160, Bob : 60 + 100 = 160 -> rang partagé
        Assert.All(standings, s => Assert.Equal(160, s.TotalPoints));
        Assert.All(standings, s => Assert.Equal(1, s.Rank));
        Assert.All(standings, s => Assert.Equal(2, s.TournamentsPlayed));
    }

    [Fact]
    public void Aggregate_AbsentDUnTournoi_PasDePointsDessus()
    {
        var t1 = MakeTournament(1, "Tournoi 1");
        var t2 = MakeTournament(2, "Tournoi 2");
        var tournaments = new[]
        {
            (t1, new List<FinalPlacement> { new(1, "Alice", 1), new(2, "Bob", 2) }),
            (t2, new List<FinalPlacement> { new(1, "Alice", 1) })  // Bob absent
        };

        var standings = CircuitRankingCalculator.Aggregate(
            tournaments, DefaultRules(), ParticipationPoints);

        var alice = standings.First(s => s.PlayerId == 1);
        var bob = standings.First(s => s.PlayerId == 2);

        Assert.Equal(200, alice.TotalPoints);
        Assert.Equal(2, alice.TournamentsPlayed);
        Assert.Equal(1, alice.Rank);
        Assert.Equal(60, bob.TotalPoints);
        Assert.Equal(1, bob.TournamentsPlayed);
        Assert.Equal(2, bob.Rank);
        Assert.Single(bob.Details);
    }

    [Fact]
    public void Aggregate_DetailsContiennentTournoiPlaceEtPoints()
    {
        var t1 = MakeTournament(1, "Open de janvier");
        var tournaments = new[]
        {
            (t1, new List<FinalPlacement> { new(1, "Alice", 3) })
        };

        var standings = CircuitRankingCalculator.Aggregate(
            tournaments, DefaultRules(), ParticipationPoints);

        var detail = Assert.Single(standings[0].Details);
        Assert.Equal(1, detail.TournamentId);
        Assert.Equal("Open de janvier", detail.TournamentName);
        Assert.Equal(3, detail.FinalRank);
        Assert.Equal(40, detail.Points);
    }

    [Fact]
    public void Aggregate_TriParTotalDecroissant()
    {
        var t1 = MakeTournament(1, "Tournoi 1");
        var tournaments = new[]
        {
            (t1, new List<FinalPlacement> { new(1, "Alice", 3), new(2, "Bob", 1), new(3, "Carol", 2) })
        };

        var standings = CircuitRankingCalculator.Aggregate(
            tournaments, DefaultRules(), ParticipationPoints);

        Assert.Equal(new[] { 2, 3, 1 }, standings.Select(s => s.PlayerId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, standings.Select(s => s.Rank).ToArray());
    }
}
