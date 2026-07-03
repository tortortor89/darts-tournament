using DartsTournament.Api.Models;
using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class FinalPlacementCalculatorTests
{
    // ----- Helpers de construction -----

    private static Tournament MakeTournament(TournamentFormat format, params string[] playerNames)
    {
        var tournament = new Tournament { Id = 1, Name = "Test", Format = format };
        int id = 1;
        foreach (var name in playerNames)
        {
            var player = new Player { Id = id, FirstName = name, LastName = "Test" };
            tournament.TournamentPlayers.Add(new TournamentPlayer
            {
                TournamentId = 1,
                PlayerId = id,
                Player = player,
                Status = RegistrationStatus.Approved
            });
            id++;
        }
        return tournament;
    }

    private static int PlayerId(Tournament t, string name) =>
        t.TournamentPlayers.First(tp => tp.Player.FirstName == name).PlayerId;

    private static void AddMatch(Tournament t, int round, string? p1, string? p2, string? winner,
        int? p1Score = null, int? p2Score = null, int? groupId = null,
        bool isKnockout = false, BracketType bracketType = BracketType.None)
    {
        t.Matches.Add(new Match
        {
            TournamentId = t.Id,
            Round = round,
            Position = t.Matches.Count,
            Player1Id = p1 == null ? null : PlayerId(t, p1),
            Player2Id = p2 == null ? null : PlayerId(t, p2),
            WinnerId = winner == null ? null : PlayerId(t, winner),
            Player1Score = p1Score,
            Player2Score = p2Score,
            Status = winner != null ? MatchStatus.Completed : MatchStatus.Pending,
            GroupId = groupId,
            IsKnockoutMatch = isKnockout,
            BracketType = bracketType
        });
    }

    private static int RankOf(List<FinalPlacement> placements, Tournament t, string name) =>
        placements.First(p => p.PlayerId == PlayerId(t, name)).Rank;

    // ----- Single Elimination -----

    [Fact]
    public void SingleElim_QuatreJoueurs_ChampionFinalisteEtDemiFinalistes()
    {
        var t = MakeTournament(TournamentFormat.SingleElimination, "A", "B", "C", "D");
        AddMatch(t, 1, "A", "B", "A");
        AddMatch(t, 1, "C", "D", "C");
        AddMatch(t, 2, "A", "C", "A");

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(2, RankOf(placements, t, "C"));
        Assert.Equal(3, RankOf(placements, t, "B"));
        Assert.Equal(3, RankOf(placements, t, "D"));
    }

    [Fact]
    public void SingleElim_HuitJoueurs_QuartsPerdantsRangCinq()
    {
        var t = MakeTournament(TournamentFormat.SingleElimination,
            "A", "B", "C", "D", "E", "F", "G", "H");
        // Quarts
        AddMatch(t, 1, "A", "B", "A");
        AddMatch(t, 1, "C", "D", "C");
        AddMatch(t, 1, "E", "F", "E");
        AddMatch(t, 1, "G", "H", "G");
        // Demis
        AddMatch(t, 2, "A", "C", "A");
        AddMatch(t, 2, "E", "G", "E");
        // Finale
        AddMatch(t, 3, "A", "E", "A");

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(2, RankOf(placements, t, "E"));
        Assert.Equal(3, RankOf(placements, t, "C"));
        Assert.Equal(3, RankOf(placements, t, "G"));
        foreach (var name in new[] { "B", "D", "F", "H" })
            Assert.Equal(5, RankOf(placements, t, name));
    }

    [Fact]
    public void SingleElim_AvecByes_LesByesNeComptentPasCommeElimination()
    {
        var t = MakeTournament(TournamentFormat.SingleElimination, "A", "B", "C", "D", "E", "F");
        // Round 1 (bracket de 8) : 2 byes auto-complétés
        AddMatch(t, 1, "A", null, "A");   // bye
        AddMatch(t, 1, "B", "C", "B");
        AddMatch(t, 1, "D", null, "D");   // bye
        AddMatch(t, 1, "E", "F", "E");
        // Demis
        AddMatch(t, 2, "A", "B", "A");
        AddMatch(t, 2, "D", "E", "D");
        // Finale
        AddMatch(t, 3, "A", "D", "A");

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(2, RankOf(placements, t, "D"));
        Assert.Equal(3, RankOf(placements, t, "B"));
        Assert.Equal(3, RankOf(placements, t, "E"));
        // Perdants d'un vrai match de round 1 : rang 5 (pas éliminés par un bye)
        Assert.Equal(5, RankOf(placements, t, "C"));
        Assert.Equal(5, RankOf(placements, t, "F"));
    }

    [Fact]
    public void SingleElim_JoueurPendingSansMatchExclu()
    {
        // E inscrit mais jamais approuvé ni tiré au sort : exclu du classement
        var t = MakeTournament(TournamentFormat.SingleElimination, "A", "B", "C", "D", "E");
        t.TournamentPlayers.First(tp => tp.Player.FirstName == "E").Status = RegistrationStatus.Pending;
        AddMatch(t, 1, "A", "B", "A");
        AddMatch(t, 1, "C", "D", "C");
        AddMatch(t, 2, "A", "C", "A");

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.DoesNotContain(placements, p => p.PlayerId == PlayerId(t, "E"));
        Assert.Equal(4, placements.Count);
    }

    [Fact]
    public void SingleElim_JoueurPendingAyantJoueInclus()
    {
        // Données héritées : joueurs restés Pending mais ayant réellement joué
        var t = MakeTournament(TournamentFormat.SingleElimination, "A", "B", "C", "D");
        t.TournamentPlayers.First(tp => tp.Player.FirstName == "D").Status = RegistrationStatus.Pending;
        AddMatch(t, 1, "A", "B", "A");
        AddMatch(t, 1, "C", "D", "C");
        AddMatch(t, 2, "A", "C", "A");

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(3, RankOf(placements, t, "D"));
        Assert.Equal(4, placements.Count);
    }

    // ----- Round Robin -----

    [Fact]
    public void RoundRobin_ClassementParPointsPuisDifference()
    {
        var t = MakeTournament(TournamentFormat.RoundRobin, "A", "B", "C");
        AddMatch(t, 1, "A", "B", "A", 2, 0);
        AddMatch(t, 1, "A", "C", "A", 2, 1);
        AddMatch(t, 1, "B", "C", "B", 2, 1);

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(2, RankOf(placements, t, "B"));
        Assert.Equal(3, RankOf(placements, t, "C"));
    }

    [Fact]
    public void RoundRobin_EgaliteParfaite_RangPartage()
    {
        // Triangle parfait : chacun 1 victoire 2-1, mêmes points/diff/pour
        var t = MakeTournament(TournamentFormat.RoundRobin, "A", "B", "C");
        AddMatch(t, 1, "A", "B", "A", 2, 1);
        AddMatch(t, 1, "B", "C", "B", 2, 1);
        AddMatch(t, 1, "C", "A", "C", 2, 1);

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.All(placements, p => Assert.Equal(1, p.Rank));
    }

    // ----- Double Elimination -----

    [Fact]
    public void DoubleElim_ClassementParElimination()
    {
        var t = MakeTournament(TournamentFormat.DoubleElimination, "A", "B", "C", "D");
        // Winners
        AddMatch(t, 1, "A", "B", "A", bracketType: BracketType.Winners);
        AddMatch(t, 1, "C", "D", "C", bracketType: BracketType.Winners);
        AddMatch(t, 2, "A", "C", "A", bracketType: BracketType.Winners);
        // Losers
        AddMatch(t, 1, "B", "D", "B", bracketType: BracketType.Losers);
        AddMatch(t, 2, "B", "C", "B", bracketType: BracketType.Losers);
        // Grande finale
        AddMatch(t, 3, "A", "B", "A", bracketType: BracketType.GrandFinal);

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(2, RankOf(placements, t, "B"));
        Assert.Equal(3, RankOf(placements, t, "C"));
        Assert.Equal(4, RankOf(placements, t, "D"));
    }

    [Fact]
    public void DoubleElim_BracketReset_ChampionEstVainqueurDuDernierMatch()
    {
        var t = MakeTournament(TournamentFormat.DoubleElimination, "A", "B", "C", "D");
        AddMatch(t, 1, "A", "B", "A", bracketType: BracketType.Winners);
        AddMatch(t, 1, "C", "D", "C", bracketType: BracketType.Winners);
        AddMatch(t, 2, "A", "C", "A", bracketType: BracketType.Winners);
        AddMatch(t, 1, "B", "D", "B", bracketType: BracketType.Losers);
        AddMatch(t, 2, "B", "C", "B", bracketType: BracketType.Losers);
        // B (losers) gagne la GF1 -> bracket reset -> B gagne aussi la GF2
        AddMatch(t, 3, "A", "B", "B", bracketType: BracketType.GrandFinal);
        AddMatch(t, 4, "A", "B", "B", bracketType: BracketType.GrandFinal);

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "B"));
        Assert.Equal(2, RankOf(placements, t, "A"));
    }

    [Fact]
    public void DoubleElim_EliminesMemeRoundLosers_RangPartage()
    {
        var t = MakeTournament(TournamentFormat.DoubleElimination, "A", "B", "C", "D", "E");
        AddMatch(t, 1, "A", "B", "A", bracketType: BracketType.Winners);
        AddMatch(t, 1, "C", "D", "C", bracketType: BracketType.Winners);
        AddMatch(t, 2, "A", "C", "A", bracketType: BracketType.Winners);
        // D et E éliminés au même round du losers bracket
        AddMatch(t, 1, "B", "D", "B", bracketType: BracketType.Losers);
        AddMatch(t, 1, "C", "E", "C", bracketType: BracketType.Losers);
        AddMatch(t, 2, "B", "C", "B", bracketType: BracketType.Losers);
        AddMatch(t, 3, "A", "B", "A", bracketType: BracketType.GrandFinal);

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(2, RankOf(placements, t, "B"));
        Assert.Equal(3, RankOf(placements, t, "C"));
        Assert.Equal(4, RankOf(placements, t, "D"));
        Assert.Equal(4, RankOf(placements, t, "E"));
    }

    // ----- Group Stage -----

    private static Tournament MakeGroupStageTournament(bool hasKnockout)
    {
        var t = MakeTournament(TournamentFormat.GroupStage, "A", "B", "C", "D", "E", "F");
        t.HasKnockoutPhase = hasKnockout;
        t.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "Groupe A" });
        t.Groups.Add(new Group { Id = 2, TournamentId = 1, Name = "Groupe B" });
        foreach (var name in new[] { "A", "B", "C" })
            t.TournamentPlayers.First(tp => tp.Player.FirstName == name).GroupId = 1;
        foreach (var name in new[] { "D", "E", "F" })
            t.TournamentPlayers.First(tp => tp.Player.FirstName == name).GroupId = 2;

        // Groupe A : A > B > C
        AddMatch(t, 1, "A", "B", "A", 2, 0, groupId: 1);
        AddMatch(t, 1, "A", "C", "A", 2, 0, groupId: 1);
        AddMatch(t, 1, "B", "C", "B", 2, 1, groupId: 1);
        // Groupe B : D > E > F
        AddMatch(t, 1, "D", "E", "D", 2, 0, groupId: 2);
        AddMatch(t, 1, "D", "F", "D", 2, 0, groupId: 2);
        AddMatch(t, 1, "E", "F", "E", 2, 1, groupId: 2);
        return t;
    }

    [Fact]
    public void GroupStage_AvecKnockout_QualifiesParBracketNonQualifiesDerriere()
    {
        var t = MakeGroupStageTournament(hasKnockout: true);
        // Knockout (roundOffset 1) : demi-finales croisées puis finale
        AddMatch(t, 2, "A", "E", "A", isKnockout: true);
        AddMatch(t, 2, "D", "B", "D", isKnockout: true);
        AddMatch(t, 3, "A", "D", "A", isKnockout: true);

        var placements = FinalPlacementCalculator.Compute(t);

        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(2, RankOf(placements, t, "D"));
        Assert.Equal(3, RankOf(placements, t, "B"));
        Assert.Equal(3, RankOf(placements, t, "E"));
        // Non qualifiés (3es de groupe) : après les 4 qualifiés, à égalité
        Assert.Equal(5, RankOf(placements, t, "C"));
        Assert.Equal(5, RankOf(placements, t, "F"));
    }

    [Fact]
    public void GroupStage_SansKnockout_PaliersParRangDeGroupe()
    {
        var t = MakeGroupStageTournament(hasKnockout: false);

        var placements = FinalPlacementCalculator.Compute(t);

        // Vainqueurs de groupe à égalité au rang 1, 2es au rang 3, 3es au rang 5
        Assert.Equal(1, RankOf(placements, t, "A"));
        Assert.Equal(1, RankOf(placements, t, "D"));
        Assert.Equal(3, RankOf(placements, t, "B"));
        Assert.Equal(3, RankOf(placements, t, "E"));
        Assert.Equal(5, RankOf(placements, t, "C"));
        Assert.Equal(5, RankOf(placements, t, "F"));
    }
}
