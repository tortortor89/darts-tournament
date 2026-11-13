using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class GroupRankingCalculatorTests
{
    private static readonly TieBreaker[] Fsgt = { TieBreaker.Wins, TieBreaker.LegDifference, TieBreaker.HeadToHead };

    // Match gagné par le côté qui a le plus de manches
    private static GroupMatchResult M(int side1, int side2, int legs1, int legs2) =>
        new(side1, side2, legs1, legs2, legs1 > legs2 ? side1 : side2);

    private static List<GroupRankEntry> RankFsgt(int[] sides, List<GroupMatchResult> matches, int[]? manual = null) =>
        GroupRankingCalculator.Rank(sides, matches, Fsgt, manual, shareUnresolvedRanks: true);

    private static int RankOf(List<GroupRankEntry> ranking, int side) => ranking.First(r => r.SideId == side).Rank;

    [Fact]
    public void Wins_FirstCriterion()
    {
        // 1 bat 2 et 3, 2 bat 3
        var ranking = RankFsgt(new[] { 3, 2, 1 }, new() { M(1, 2, 3, 2), M(1, 3, 3, 0), M(2, 3, 3, 1) });

        Assert.Equal(new[] { 1, 2, 3 }, ranking.Select(r => r.SideId));
        Assert.Equal(new[] { 1, 2, 3 }, ranking.Select(r => r.Rank));
        Assert.All(ranking, r => Assert.False(r.TieUnresolved));
    }

    [Fact]
    public void LegDifference_BreaksEqualWins()
    {
        // Cercle 1 > 2 > 3 > 1, une victoire chacun ; 1-2 : 3-0, 2-3 : 3-2, 3-1 : 3-2
        // => différences 1 : +2, 3 : 0, 2 : -2
        var ranking = RankFsgt(new[] { 1, 2, 3 }, new() { M(1, 2, 3, 0), M(2, 3, 3, 2), M(3, 1, 3, 2) });

        Assert.Equal(new[] { 1, 3, 2 }, ranking.Select(r => r.SideId));
        Assert.All(ranking, r => Assert.False(r.TieUnresolved));
    }

    [Fact]
    public void HeadToHead_BreaksEqualWinsAndDiff()
    {
        // 4 joueurs : 1 et 2 à 2 victoires et même différence, 1 a battu 2
        var matches = new List<GroupMatchResult>
        {
            M(1, 2, 3, 2), // 1 bat 2 (+1 / -1)
            M(1, 3, 2, 3), // 3 bat 1 (-1 / +1)
            M(1, 4, 3, 1), // 1 bat 4 (+2)
            M(2, 3, 3, 1), // 2 bat 3 (+2)
            M(2, 4, 3, 2), // 2 bat 4 (+1)
            M(3, 4, 1, 3), // 4 bat 3
        };
        // Diff : 1 = +1-1+2 = +2 ; 2 = -1+2+1 = +2 ; 3 = +1-2-2 = -3 ; 4 = -2-1+2 = -1

        var ranking = RankFsgt(new[] { 2, 1, 3, 4 }, matches);

        Assert.Equal(1, RankOf(ranking, 1));
        Assert.Equal(2, RankOf(ranking, 2));
        Assert.All(ranking, r => Assert.False(r.TieUnresolved));
    }

    [Fact]
    public void PerfectCircularTie_SharesRankAndIsFlagged()
    {
        // Cercle parfait : 1 victoire chacun, 3-2 partout, confrontation directe 1-1-1
        var ranking = RankFsgt(new[] { 1, 2, 3 }, new() { M(1, 2, 3, 2), M(2, 3, 3, 2), M(3, 1, 3, 2) });

        Assert.All(ranking, r => Assert.Equal(1, r.Rank));
        Assert.All(ranking, r => Assert.True(r.TieUnresolved));
    }

    [Fact]
    public void ManualOrder_ResolvesPerfectTie()
    {
        var ranking = RankFsgt(new[] { 1, 2, 3 },
            new List<GroupMatchResult> { M(1, 2, 3, 2), M(2, 3, 3, 2), M(3, 1, 3, 2) },
            manual: new[] { 3, 1, 2 });

        Assert.Equal(new[] { 3, 1, 2 }, ranking.Select(r => r.SideId));
        Assert.Equal(new[] { 1, 2, 3 }, ranking.Select(r => r.Rank));
        Assert.All(ranking, r => Assert.False(r.TieUnresolved));
    }

    [Fact]
    public void ManualOrder_Partial_LeavesTieUnresolved()
    {
        var ranking = RankFsgt(new[] { 1, 2, 3 },
            new List<GroupMatchResult> { M(1, 2, 3, 2), M(2, 3, 3, 2), M(3, 1, 3, 2) },
            manual: new[] { 3, 1 });

        Assert.All(ranking, r => Assert.True(r.TieUnresolved));
    }

    [Fact]
    public void HeadToHead_Recursive_SplitsRemainingSubBlocks()
    {
        // 6 joueurs, 1-2-3-4 à 3 victoires. Entre eux : 1 bat 2 et 3, 2 bat 3 et 4,
        // 3 bat 4, 4 bat 1 => mini-championnat 2-2-1-1, puis 1 > 2 et 3 > 4 par leur match
        var matches = new List<GroupMatchResult>
        {
            M(1, 2, 3, 0), M(1, 3, 3, 0), M(2, 3, 3, 0), M(2, 4, 3, 0), M(3, 4, 3, 0), M(4, 1, 3, 0),
            M(1, 5, 3, 0), M(6, 1, 3, 0),
            M(2, 5, 3, 0), M(6, 2, 3, 0),
            M(3, 5, 3, 0), M(3, 6, 3, 0),
            M(4, 5, 3, 0), M(4, 6, 3, 0),
            M(5, 6, 3, 0),
        };

        var ranking = GroupRankingCalculator.Rank(new[] { 4, 3, 2, 1, 5, 6 }, matches,
            new[] { TieBreaker.Wins, TieBreaker.HeadToHead }, null, shareUnresolvedRanks: true);

        Assert.Equal(new[] { 1, 2, 3, 4, 6, 5 }, ranking.Select(r => r.SideId));
        Assert.All(ranking, r => Assert.False(r.TieUnresolved));
    }

    [Fact]
    public void LegsWon_CanBeAddedAsCriterion()
    {
        // 1 et 2 : même victoires et même différence, 2 a gagné plus de manches
        // 1-3 : 3-0 (+3), 2-3 : 4-1 (+3), 1-2 : sans match entre eux
        var matches = new List<GroupMatchResult> { M(1, 3, 3, 0), M(2, 3, 4, 1) };

        var withoutLegsWon = RankFsgt(new[] { 1, 2, 3 }, matches);
        Assert.True(withoutLegsWon.First(r => r.SideId == 1).TieUnresolved);

        var withLegsWon = GroupRankingCalculator.Rank(new[] { 1, 2, 3 }, matches,
            new[] { TieBreaker.Wins, TieBreaker.LegDifference, TieBreaker.HeadToHead, TieBreaker.LegsWon },
            null, shareUnresolvedRanks: true);
        Assert.Equal(new[] { 2, 1, 3 }, withLegsWon.Select(r => r.SideId));
    }

    [Fact]
    public void LegacyMode_PerfectTie_KeepsInputOrderWithDistinctRanks()
    {
        var ranking = GroupRankingCalculator.Rank(new[] { 2, 3, 1 },
            new List<GroupMatchResult> { M(1, 2, 3, 2), M(2, 3, 3, 2), M(3, 1, 3, 2) },
            new[] { TieBreaker.Wins, TieBreaker.LegDifference, TieBreaker.LegsWon },
            null, shareUnresolvedRanks: false);

        Assert.Equal(new[] { 2, 3, 1 }, ranking.Select(r => r.SideId));
        Assert.Equal(new[] { 1, 2, 3 }, ranking.Select(r => r.Rank));
        Assert.All(ranking, r => Assert.False(r.TieUnresolved));
    }

    [Fact]
    public void TieOnlyBelowQualifiers_RankReflectsPosition()
    {
        // 1 bat tout le monde ; 2, 3, 4 en cercle parfait derrière
        var matches = new List<GroupMatchResult>
        {
            M(1, 2, 3, 0), M(1, 3, 3, 0), M(1, 4, 3, 0),
            M(2, 3, 3, 2), M(3, 4, 3, 2), M(4, 2, 3, 2),
        };

        var ranking = RankFsgt(new[] { 1, 2, 3, 4 }, matches);

        Assert.Equal(1, RankOf(ranking, 1));
        Assert.False(ranking.First(r => r.SideId == 1).TieUnresolved);
        Assert.All(ranking.Where(r => r.SideId != 1), r => Assert.Equal(2, r.Rank));
        Assert.All(ranking.Where(r => r.SideId != 1), r => Assert.True(r.TieUnresolved));
    }
}
