using System.Text.Json;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;
using DartsTournament.Api.Services;

namespace DartsTournament.Tests;

public class MatchStatsServiceTests
{
    private readonly MatchStatsService _service = new();

    private static MatchSession BuildSession(GameMode mode = GameMode.FiveOhOne, bool trackDoubles = false)
    {
        var player1 = new Player { Id = 1, FirstName = "Alice", LastName = "Martin" };
        var player2 = new Player { Id = 2, FirstName = "Bob", LastName = "Durand" };

        return new MatchSession
        {
            GameMode = mode,
            TrackDoubles = trackDoubles,
            Match = new Match
            {
                Tournament = new Tournament { Name = "Test", TeamSize = 1 },
                Player1Id = player1.Id,
                Player1 = player1,
                Player2Id = player2.Id,
                Player2 = player2
            }
        };
    }

    private static Throw NewThrow(int playerId, int score, int remaining, int throwNumber, int legNumber = 1) =>
        new()
        {
            PlayerId = playerId,
            LegNumber = legNumber,
            ThrowNumber = throwNumber,
            Score = score,
            RemainingScore = remaining
        };

    private static string CricketJson(params CricketHit[] hits) =>
        JsonSerializer.Serialize(new { hits = hits.ToList(), results = new List<CricketHitResult>() });

    // Session de double : paires 101 (joueurs 1, 2) et 102 (joueurs 11, 12)
    private static MatchSession BuildDoublesSession(GameMode mode = GameMode.FiveOhOne)
    {
        var a1 = new Player { Id = 1, FirstName = "Alice", LastName = "Martin" };
        var a2 = new Player { Id = 2, FirstName = "Anna", LastName = "Morel" };
        var b1 = new Player { Id = 11, FirstName = "Bob", LastName = "Durand" };
        var b2 = new Player { Id = 12, FirstName = "Bill", LastName = "Dupont" };

        return new MatchSession
        {
            GameMode = mode,
            Match = new Match
            {
                Tournament = new Tournament { Name = "Test double", TeamSize = 2 },
                Team1Id = 101,
                Team1 = new TournamentTeam { Id = 101, Player1Id = 1, Player1 = a1, Player2Id = 2, Player2 = a2 },
                Team2Id = 102,
                Team2 = new TournamentTeam { Id = 102, Player1Id = 11, Player1 = b1, Player2Id = 12, Player2 = b2 }
            },
            Side1Player1Id = 1,
            Side1Player2Id = 2,
            Side2Player1Id = 11,
            Side2Player2Id = 12
        };
    }

    // --- 501 ---

    [Fact]
    public void Moyenne_UtiliseDartsUsedSurLeCheckout()
    {
        var session = BuildSession();
        session.Throws.Add(NewThrow(1, 60, 441, 1));
        session.Throws.Add(NewThrow(1, 60, 381, 2));
        var checkout = NewThrow(1, 60, 321, 3);
        checkout.IsCheckout = true;
        checkout.DartsUsed = 1;
        session.Throws.Add(checkout);

        var stats = _service.CalculateStats(session);

        // 180 points en 7 fléchettes (3 + 3 + 1) => 77.14
        Assert.Equal(7, stats.Player1Stats.TotalDartsThrown);
        Assert.Equal(Math.Round(180.0 / 7 * 3, 2), stats.Player1Stats.ThreeDartAverage);
    }

    [Fact]
    public void Moyenne_VoleeBust_CompteZeroPointEtTroisFlechettes()
    {
        var session = BuildSession();
        session.Throws.Add(NewThrow(1, 100, 401, 1));
        var bust = NewThrow(1, 0, 401, 2);
        bust.IsBust = true;
        session.Throws.Add(bust);

        var stats = _service.CalculateStats(session);

        // 100 points en 6 fléchettes => 50.0
        Assert.Equal(50.0, stats.Player1Stats.ThreeDartAverage);
    }

    [Fact]
    public void CheckoutPourcentage_AvecTracking_UtiliseLesDoublesTentes()
    {
        var session = BuildSession(trackDoubles: true);
        var missedDouble = NewThrow(1, 8, 32, 1);
        missedDouble.DoublesAttempted = 2;
        session.Throws.Add(missedDouble);

        var checkout = NewThrow(1, 32, 0, 2);
        checkout.IsCheckout = true;
        checkout.DartsUsed = 1;
        checkout.DoublesAttempted = 2;
        session.Throws.Add(checkout);

        var stats = _service.CalculateStats(session);

        // 1 checkout sur 4 fléchettes tentées sur un double => 25 %
        Assert.Equal(4, stats.Player1Stats.CheckoutAttempts);
        Assert.Equal(25.0, stats.Player1Stats.CheckoutPercentage);
        Assert.Equal(25.0, stats.Player1Stats.DoublesHitRate);
    }

    [Fact]
    public void CheckoutPourcentage_SansTracking_GardeLHeuristique()
    {
        var session = BuildSession(trackDoubles: false);
        // Volée commencée à 100 (<= 170) sans checkout : 1 tentative, 0 réussite
        session.Throws.Add(NewThrow(1, 40, 60, 5));

        var stats = _service.CalculateStats(session);

        Assert.Equal(1, stats.Player1Stats.CheckoutAttempts);
        Assert.Equal(0.0, stats.Player1Stats.CheckoutPercentage);
        Assert.Null(stats.Player1Stats.DoublesHitRate);
    }

    [Fact]
    public void OneEighties_CompteLesVoleesA180()
    {
        var session = BuildSession();
        session.Throws.Add(NewThrow(1, 180, 321, 1));
        session.Throws.Add(NewThrow(1, 140, 181, 2));
        session.Throws.Add(NewThrow(1, 180, 1, 3));

        var stats = _service.CalculateStats(session);

        Assert.Equal(2, stats.Player1Stats.OneEighties);
        Assert.Null(stats.Player1Stats.MarksPerRound);
    }

    // --- Cricket ---

    [Fact]
    public void Cricket_CalculeLeMarksPerRound()
    {
        var session = BuildSession(GameMode.Cricket);

        var turn1 = NewThrow(1, 0, 0, 1);
        turn1.CricketDataJson = CricketJson(new CricketHit(20, 3), new CricketHit(19, 2));
        session.Throws.Add(turn1);

        var turn2 = NewThrow(1, 40, 0, 2);
        turn2.CricketDataJson = CricketJson(new CricketHit(20, 2), new CricketHit(25, 2));
        session.Throws.Add(turn2);

        var stats = _service.CalculateStats(session);

        // 9 marques en 2 visites => MPR 4.5, 40 points marqués
        Assert.Equal(4.5, stats.Player1Stats.MarksPerRound);
        Assert.Equal(40, stats.Player1Stats.TotalScore);
        Assert.Equal(40, stats.Player1Stats.HighestScore);
    }

    [Fact]
    public void Cricket_SansVisites_StatsAZero()
    {
        var session = BuildSession(GameMode.Cricket);

        var stats = _service.CalculateStats(session);

        Assert.Equal(0.0, stats.Player1Stats.MarksPerRound);
        Assert.Equal(0, stats.Player1Stats.TotalScore);
        Assert.Null(stats.Player1Stats.CheckoutPercentage);
    }

    [Fact]
    public void Cricket_DeserialiseLeFormatProduitParLeService()
    {
        // Reproduit exactement la sérialisation anonyme de MatchSessionService
        var json = JsonSerializer.Serialize(new
        {
            hits = new List<CricketHit> { new(20, 3) },
            results = new List<CricketHitResult> { new(20, 3, 0, true) }
        });

        var session = BuildSession(GameMode.Cricket);
        var turn = NewThrow(1, 0, 0, 1);
        turn.CricketDataJson = json;
        session.Throws.Add(turn);

        var stats = _service.CalculateStats(session);

        Assert.Equal(3.0, stats.Player1Stats.MarksPerRound);
    }

    // --- Doubles ---

    [Fact]
    public void Doubles_StatsDeCoteAgregentLesDeuxMembres()
    {
        var session = BuildDoublesSession();
        // Alice (1) et Anna (2) lancent pour la paire 101
        session.Throws.Add(NewThrow(1, 100, 401, 1));
        session.Throws.Add(NewThrow(2, 60, 341, 1));
        // Bob (11) lance pour la paire 102
        session.Throws.Add(NewThrow(11, 45, 456, 1));

        var stats = _service.CalculateStats(session);

        // Sujet côté 1 = équipe 101 : 160 points en 6 fléchettes => 80.0
        Assert.Equal(101, stats.Player1Stats.PlayerId);
        Assert.Equal("Alice Martin / Anna Morel", stats.Player1Stats.Name);
        Assert.Equal(160, stats.Player1Stats.TotalScore);
        Assert.Equal(80.0, stats.Player1Stats.ThreeDartAverage);
        Assert.Equal(102, stats.Player2Stats.PlayerId);
        Assert.Equal(45, stats.Player2Stats.TotalScore);
    }

    [Fact]
    public void Doubles_StatsIndividuellesParMembreExactes()
    {
        var session = BuildDoublesSession();
        session.Throws.Add(NewThrow(1, 100, 401, 1));
        session.Throws.Add(NewThrow(2, 60, 341, 1));
        session.Throws.Add(NewThrow(1, 140, 201, 2));

        var stats = _service.CalculateStats(session);

        Assert.NotNull(stats.Player1MemberStats);
        var alice = stats.Player1MemberStats!.First(m => m.PlayerId == 1);
        var anna = stats.Player1MemberStats!.First(m => m.PlayerId == 2);
        // Alice : 240 points en 6 fléchettes => 120.0 ; Anna : 60 en 3 => 60.0
        Assert.Equal(120.0, alice.ThreeDartAverage);
        Assert.Equal(60.0, anna.ThreeDartAverage);
    }

    [Fact]
    public void Simple_PasDeStatsMembres()
    {
        var session = BuildSession();
        session.Throws.Add(NewThrow(1, 60, 441, 1));

        var stats = _service.CalculateStats(session);

        Assert.Null(stats.Player1MemberStats);
        Assert.Null(stats.Player2MemberStats);
    }
}
