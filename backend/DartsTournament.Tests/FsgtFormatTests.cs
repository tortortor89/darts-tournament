using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class FsgtFormatTests
{
    private static FsgtFormatCatalog LoadRealCatalog() =>
        FsgtFormatCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Data", FsgtFormatCatalog.DefaultFileName));

    // ----- Grille réelle (Data/fsgt-formats.json) -----

    [Fact]
    public void RealGrid_LoadsAndValidates_From2To80()
    {
        // Load valide chaque variante : tailles de poules, qualifiés = 2 x affiches, slots uniques
        var catalog = LoadRealCatalog();

        Assert.Equal(2, catalog.MinPlayers);
        Assert.Equal(80, catalog.MaxPlayers);
        for (int n = 2; n <= 80; n++)
            Assert.NotNull(catalog.GetDefault(n));
        Assert.Null(catalog.GetDefault(81));
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(6, 2)]
    [InlineData(10, 2)]
    [InlineData(11, 4)]
    [InlineData(20, 4)]
    [InlineData(21, 4)]
    [InlineData(22, 8)]
    [InlineData(42, 8)]
    [InlineData(43, 16)]
    [InlineData(80, 16)]
    public void RealGrid_FirstRoundMatchCount(int players, int expectedPairings)
    {
        var variant = LoadRealCatalog().GetDefault(players)!;

        Assert.Equal(expectedPairings, variant.Bracket.Count);
        Assert.Equal(expectedPairings > 0, variant.HasKnockout);
    }

    [Fact]
    public void RealGrid_EveryDefaultVariant_ResolvesToDistinctSides()
    {
        var catalog = LoadRealCatalog();
        for (int n = 6; n <= 80; n++)
        {
            var variant = catalog.GetDefault(n)!;
            var groupOfSeed = FsgtDrawCalculator.AssignGroups(n, variant.Groups.Select(g => g.Size).ToList());

            // Côté = seed ; classement simulé = ordre des seeds dans la poule
            var qualified = variant.Groups
                .Select((g, gi) => (IReadOnlyList<int>)Enumerable.Range(0, n)
                    .Where(seed => groupOfSeed[seed] == gi)
                    .Take(g.Qualifiers)
                    .ToList())
                .ToList();

            var firstRound = FsgtDrawCalculator.ResolveFirstRound(
                FsgtDrawCalculator.SerializePairings(variant.Bracket), qualified);

            Assert.Equal(variant.Bracket.Count * 2, firstRound.Count);
            Assert.Equal(firstRound.Count, firstRound.Distinct().Count());
        }
    }

    // ----- Répartition en poules -----

    [Fact]
    public void AssignGroups_EqualSizes_IsSnakeDraft()
    {
        var result = FsgtDrawCalculator.AssignGroups(8, new[] { 4, 4 });

        Assert.Equal(new[] { 0, 1, 1, 0, 0, 1, 1, 0 }, result);
    }

    [Fact]
    public void AssignGroups_MixedSizes_SkipsFullGroups()
    {
        // 11 joueurs : poules de 3, 4, 4
        var result = FsgtDrawCalculator.AssignGroups(11, new[] { 3, 4, 4 });

        Assert.Equal(new[] { 0, 1, 2, 2, 1, 0, 0, 1, 2, 2, 1 }, result);
        Assert.Equal(3, result.Count(g => g == 0));
        Assert.Equal(4, result.Count(g => g == 1));
        Assert.Equal(4, result.Count(g => g == 2));
    }

    [Fact]
    public void AssignGroups_SizeMismatch_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => FsgtDrawCalculator.AssignGroups(10, new[] { 4, 4 }));
    }

    // ----- Affiches -----

    [Theory]
    [InlineData("1p1", 1, 1)]
    [InlineData("2p14", 2, 14)]
    [InlineData("3 p 7", 3, 7)]
    [InlineData("1P2", 1, 2)]
    public void ParseSlot_ReadsRankAndGroup(string token, int rank, int group)
    {
        Assert.Equal((rank, group), FsgtDrawCalculator.ParseSlot(token));
    }

    [Fact]
    public void ParseSlot_Invalid_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => FsgtDrawCalculator.ParseSlot("A1"));
    }

    [Fact]
    public void SerializePairings_RoundTrips()
    {
        var bracket = new List<List<string>> { new() { "1p1", "2p2" }, new() { "1p2", "2p1" } };

        var serialized = FsgtDrawCalculator.SerializePairings(bracket);

        Assert.Equal("1p1-2p2,1p2-2p1", serialized);
        Assert.Equal(
            new[] { ((1, 1), (2, 2)), ((1, 2), (2, 1)) },
            FsgtDrawCalculator.ParsePairings(serialized));
    }

    [Fact]
    public void ResolveFirstRound_PlacesQualifiersInPairingOrder()
    {
        // Poule 1 : 10 puis 11 ; poule 2 : 20 puis 21
        var qualified = new List<IReadOnlyList<int>> { new[] { 10, 11 }, new[] { 20, 21 } };

        var firstRound = FsgtDrawCalculator.ResolveFirstRound("1p1-2p2,1p2-2p1", qualified);

        Assert.Equal(new[] { 10, 21, 20, 11 }, firstRound);
    }

    [Fact]
    public void ResolveFirstRound_MissingQualifier_Throws()
    {
        var qualified = new List<IReadOnlyList<int>> { new[] { 10 }, new[] { 20, 21 } };

        Assert.Throws<InvalidOperationException>(() =>
            FsgtDrawCalculator.ResolveFirstRound("1p1-2p2,1p2-2p1", qualified));
    }

    // ----- Validation de la grille -----

    private static FsgtFormatFile OneFormat(int players, List<FsgtGroupSpec> groups, params string[][] bracket) =>
        new(new List<FsgtFormatEntry>
        {
            new(players, new List<FsgtFormatVariant>
            {
                new(true, "SemiFinal", groups, bracket.Select(p => p.ToList()).ToList())
            })
        });

    [Fact]
    public void Catalog_GroupSizesNotMatchingPlayers_Throws()
    {
        var file = OneFormat(9, new() { new(4, 2), new(4, 2) }, new[] { "1p1", "2p2" }, new[] { "1p2", "2p1" });

        Assert.Throws<InvalidOperationException>(() => new FsgtFormatCatalog(file));
    }

    [Fact]
    public void Catalog_DuplicateSlot_Throws()
    {
        var file = OneFormat(8, new() { new(4, 2), new(4, 2) }, new[] { "1p1", "2p2" }, new[] { "1p1", "2p1" });

        Assert.Throws<InvalidOperationException>(() => new FsgtFormatCatalog(file));
    }

    [Fact]
    public void Catalog_SlotBeyondQualifiers_Throws()
    {
        var file = OneFormat(8, new() { new(4, 2), new(4, 2) }, new[] { "1p1", "3p2" }, new[] { "1p2", "2p1" });

        Assert.Throws<InvalidOperationException>(() => new FsgtFormatCatalog(file));
    }
}
