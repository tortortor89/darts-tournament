using DartsTournament.Api.Models;
using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class FsgtPointsCalculatorTests
{
    private static readonly FsgtPoints Points = FsgtFormatCatalog
        .Load(Path.Combine(AppContext.BaseDirectory, "Data", FsgtFormatCatalog.DefaultFileName))
        .Points;

    // ----- Barème seul -----

    [Theory]
    // 6-10 inscrits : sortie en demies (6 / 8 / 10)
    [InlineData(10, 1, 10)]
    [InlineData(10, 2, 8)]
    [InlineData(10, 3, 6)]
    // 11-16 : quarts 8, demi 10, finale 12, vainqueur 14
    [InlineData(16, 5, 8)]
    [InlineData(16, 3, 10)]
    [InlineData(16, 1, 14)]
    // 21-40 : 8es 10 ... vainqueur 18
    [InlineData(24, 9, 10)]
    [InlineData(24, 5, 12)]
    // 41-80 : 16es 10 ... vainqueur 20
    [InlineData(48, 17, 10)]
    [InlineData(48, 1, 20)]
    public void PointsFor_Qualified_RoundReached(int sides, int rank, int expected)
    {
        Assert.Equal(expected, FsgtPointsCalculator.PointsFor(sides, inBracket: true, rank, poolWins: 1, Points));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 4)]
    [InlineData(3, 8)]
    public void PointsFor_EliminatedInPools_ParticipationPlusWins(int poolWins, int expected)
    {
        Assert.Equal(expected, FsgtPointsCalculator.PointsFor(24, inBracket: false, rank: 17, poolWins, Points));
    }

    [Fact]
    public void PointsFor_RoundMissingFromRow_FallsBackToPoolPoints()
    {
        // 12 inscrits mais tableau de 16 (format FSGT contourné) : 8es absents de la ligne 11-16
        Assert.Equal(2 + 2 * 2, FsgtPointsCalculator.PointsFor(12, inBracket: true, rank: 9, poolWins: 2, Points));
    }

    [Fact]
    public void PointsFor_BelowGrid_UsesSmallestRow()
    {
        // Élimination directe à 4 : ligne 6-10
        Assert.Equal(10, FsgtPointsCalculator.PointsFor(4, inBracket: true, rank: 1, poolWins: 0, Points));
        Assert.Equal(6, FsgtPointsCalculator.PointsFor(4, inBracket: true, rank: 3, poolWins: 0, Points));
    }

    // ----- Tournoi complet -----

    private static Tournament MakeTournament(TournamentFormat format, params string[] names)
    {
        var t = new Tournament { Id = 1, Name = "Test", Format = format };
        int id = 1;
        foreach (var name in names)
        {
            t.TournamentPlayers.Add(new TournamentPlayer
            {
                TournamentId = 1,
                PlayerId = id,
                Player = new Player { Id = id, FirstName = name, LastName = "Test" },
                Status = RegistrationStatus.Approved
            });
            id++;
        }
        return t;
    }

    private static int Id(Tournament t, string name) =>
        t.TournamentPlayers.First(tp => tp.Player.FirstName == name).PlayerId;

    private static void Play(Tournament t, string p1, string p2, string winner, int? groupId, bool knockout = false, int round = 1)
    {
        t.Matches.Add(new Match
        {
            TournamentId = 1,
            Round = round,
            Position = t.Matches.Count,
            Player1Id = Id(t, p1),
            Player2Id = Id(t, p2),
            WinnerId = Id(t, winner),
            Player1Score = winner == p1 ? 3 : 1,
            Player2Score = winner == p2 ? 3 : 1,
            Status = MatchStatus.Completed,
            GroupId = groupId,
            IsKnockoutMatch = knockout
        });
    }

    [Fact]
    public void Compute_SixPlayers_TwoPoolsThenSemis()
    {
        // Poule 1 : A, B, C ; poule 2 : D, E, F ; 2 qualifiés par poule
        var t = MakeTournament(TournamentFormat.GroupStage, "A", "B", "C", "D", "E", "F");
        t.Groups.Add(new Group { Id = 1, Name = "Groupe A", TournamentId = 1 });
        t.Groups.Add(new Group { Id = 2, Name = "Groupe B", TournamentId = 1 });
        foreach (var tp in t.TournamentPlayers)
            tp.GroupId = tp.PlayerId <= 3 ? 1 : 2;

        Play(t, "A", "B", "A", 1); Play(t, "A", "C", "A", 1); Play(t, "B", "C", "B", 1);
        Play(t, "D", "E", "D", 2); Play(t, "D", "F", "D", 2); Play(t, "E", "F", "F", 2);

        // Demies 1p1-2p2 (A-F), 1p2-2p1 (D-B) ; finale A-D
        Play(t, "A", "F", "A", null, knockout: true, round: 2);
        Play(t, "D", "B", "D", null, knockout: true, round: 2);
        Play(t, "A", "D", "D", null, knockout: true, round: 3);

        var points = FsgtPointsCalculator.Compute(t, Points).ToDictionary(p => p.PlayerName.Split(' ')[0], p => p.Points);

        Assert.Equal(10, points["D"]); // vainqueur
        Assert.Equal(8, points["A"]);  // finaliste
        Assert.Equal(6, points["B"]);  // demi
        Assert.Equal(6, points["F"]);  // demi
        Assert.Equal(2, points["C"]);  // éliminé en poule, 0 victoire
        Assert.Equal(2, points["E"]);  // éliminé en poule, 0 victoire
    }

    [Fact]
    public void Compute_SinglePool_ParticipationPlusWinsForAll()
    {
        var t = MakeTournament(TournamentFormat.GroupStage, "A", "B", "C");
        t.HasKnockoutPhase = false;
        t.Groups.Add(new Group { Id = 1, Name = "Groupe A", TournamentId = 1 });
        foreach (var tp in t.TournamentPlayers)
            tp.GroupId = 1;

        Play(t, "A", "B", "A", 1); Play(t, "A", "C", "A", 1); Play(t, "B", "C", "B", 1);

        var points = FsgtPointsCalculator.Compute(t, Points).ToDictionary(p => p.PlayerName.Split(' ')[0], p => p.Points);

        Assert.Equal(6, points["A"]);
        Assert.Equal(4, points["B"]);
        Assert.Equal(2, points["C"]);
    }

    [Fact]
    public void Aggregate_SumsFsgtPointsAcrossTournaments()
    {
        var t1 = new Tournament { Id = 1, Name = "T1", Format = TournamentFormat.GroupStage };
        var t2 = new Tournament { Id = 2, Name = "T2", Format = TournamentFormat.GroupStage };

        var standings = CircuitRankingCalculator.Aggregate(new[]
        {
            (t1, new List<PlayerTournamentPoints> { new(1, "A", 1, 10), new(2, "B", 2, 8) }),
            (t2, new List<PlayerTournamentPoints> { new(2, "B", 1, 14), new(1, "A", 9, 2) }),
        });

        Assert.Equal(new[] { 2, 1 }, standings.Select(s => s.PlayerId));
        Assert.Equal(22, standings[0].TotalPoints);
        Assert.Equal(12, standings[1].TotalPoints);
    }
}
