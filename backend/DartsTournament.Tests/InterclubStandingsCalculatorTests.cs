using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class InterclubStandingsCalculatorTests
{
    private static readonly (int, string)[] Clubs =
    {
        (1, "Les Bulls"), (2, "Hacien'Darts"), (3, "Double Out"), (4, "Le 180")
    };

    [Fact]
    public void Bareme_VictoireNulDefaite()
    {
        var encounters = new[]
        {
            new EncounterResult(1, 2, 5, 3),  // 1 bat 2
            new EncounterResult(3, 4, 4, 4),  // nul
        };

        var standings = InterclubStandingsCalculator.Compute(encounters, Clubs);

        Assert.Equal(2, standings.First(s => s.ClubId == 1).Points);
        Assert.Equal(0, standings.First(s => s.ClubId == 2).Points);
        Assert.Equal(1, standings.First(s => s.ClubId == 3).Points);
        Assert.Equal(1, standings.First(s => s.ClubId == 4).Points);
        Assert.Equal(1, standings.First(s => s.ClubId == 3).Draws);
    }

    [Fact]
    public void Departage_AuNombreDeMatchsGagnes()
    {
        var encounters = new[]
        {
            new EncounterResult(1, 3, 6, 2),  // 1 gagne large
            new EncounterResult(2, 4, 5, 3),  // 2 gagne petit
        };

        var standings = InterclubStandingsCalculator.Compute(encounters, Clubs);

        // 1 et 2 ont 2 points, mais 1 a gagné plus de matchs individuels
        Assert.Equal(1, standings[0].ClubId);
        Assert.Equal(1, standings[0].Rank);
        Assert.Equal(2, standings[1].ClubId);
        Assert.Equal(2, standings[1].Rank);
    }

    [Fact]
    public void EgaliteParfaite_RangPartage()
    {
        var encounters = new[]
        {
            new EncounterResult(1, 3, 5, 3),
            new EncounterResult(2, 4, 5, 3),
        };

        var standings = InterclubStandingsCalculator.Compute(encounters, Clubs);

        // 1 et 2 : mêmes points, mêmes matchs gagnés => rang 1 partagé
        Assert.Equal(1, standings.First(s => s.ClubId == 1).Rank);
        Assert.Equal(1, standings.First(s => s.ClubId == 2).Rank);
        // 3 et 4 : rang 3 partagé (le rang 2 est sauté)
        Assert.Equal(3, standings.First(s => s.ClubId == 3).Rank);
        Assert.Equal(3, standings.First(s => s.ClubId == 4).Rank);
    }

    [Fact]
    public void ClubSansRencontreJouee_ApparaitAZero()
    {
        var encounters = new[] { new EncounterResult(1, 2, 5, 3) };

        var standings = InterclubStandingsCalculator.Compute(encounters, Clubs);

        var idle = standings.First(s => s.ClubId == 3);
        Assert.Equal(0, idle.Played);
        Assert.Equal(0, idle.Points);
        Assert.Equal(4, standings.Count);
    }

    [Fact]
    public void BaremePersonnalise()
    {
        var encounters = new[] { new EncounterResult(1, 2, 5, 3) };

        var standings = InterclubStandingsCalculator.Compute(
            encounters, Clubs, pointsForWin: 3, pointsForDraw: 1, pointsForLoss: 0);

        Assert.Equal(3, standings.First(s => s.ClubId == 1).Points);
    }

    [Fact]
    public void CumulSurPlusieursRencontres()
    {
        var encounters = new[]
        {
            new EncounterResult(1, 2, 5, 3),
            new EncounterResult(2, 1, 6, 2),  // revanche gagnée par 2
        };

        var standings = InterclubStandingsCalculator.Compute(encounters, Clubs);

        var club1 = standings.First(s => s.ClubId == 1);
        var club2 = standings.First(s => s.ClubId == 2);
        Assert.Equal(2, club1.Played);
        Assert.Equal(2, club1.Points);
        Assert.Equal(2, club2.Points);
        Assert.Equal(7, club1.MatchesWon);  // 5 + 2
        Assert.Equal(9, club2.MatchesWon);  // 3 + 6
        // Égalité de points, 2 devant au départage
        Assert.Equal(1, club2.Rank);
        Assert.Equal(2, club1.Rank);
    }
}
