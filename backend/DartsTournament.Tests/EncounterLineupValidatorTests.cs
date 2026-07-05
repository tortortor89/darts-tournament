using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class EncounterLineupValidatorTests
{
    // Championnat 2 simples + 1 double ; effectifs : domicile {1,2,3}, extérieur {11,12,13}
    private static readonly IReadOnlySet<int> HomeRoster = new HashSet<int> { 1, 2, 3 };
    private static readonly IReadOnlySet<int> AwayRoster = new HashSet<int> { 11, 12, 13 };

    private static List<string> Validate(params BoardLineupInput[] boards) =>
        EncounterLineupValidator.Validate(boards, singlesCount: 2, doublesCount: 1, HomeRoster, AwayRoster);

    [Fact]
    public void CompositionValide_AucuneErreur()
    {
        var errors = Validate(
            new BoardLineupInput(1, new[] { 1 }, new[] { 11 }),
            new BoardLineupInput(2, new[] { 2 }, new[] { 12 }),
            new BoardLineupInput(3, new[] { 1, 3 }, new[] { 11, 13 }));  // le joueur 1 rejoue en double : permis

        Assert.Empty(errors);
    }

    [Fact]
    public void SimpleAvecDeuxJoueurs_Erreur()
    {
        var errors = Validate(new BoardLineupInput(1, new[] { 1, 2 }, new[] { 11 }));
        Assert.Contains(errors, e => e.Contains("Board 1") && e.Contains("domicile"));
    }

    [Fact]
    public void DoubleAvecUnSeulJoueur_Erreur()
    {
        var errors = Validate(new BoardLineupInput(3, new[] { 1, 2 }, new[] { 11 }));
        Assert.Contains(errors, e => e.Contains("Board 3") && e.Contains("extérieur"));
    }

    [Fact]
    public void PaireAvecDoublon_Erreur()
    {
        var errors = Validate(new BoardLineupInput(3, new[] { 1, 1 }, new[] { 11, 12 }));
        Assert.Contains(errors, e => e.Contains("deux joueurs différents"));
    }

    [Fact]
    public void JoueurHorsEffectif_Erreur()
    {
        var errors = Validate(new BoardLineupInput(1, new[] { 99 }, new[] { 11 }));
        Assert.Contains(errors, e => e.Contains("effectif du club domicile"));
    }

    [Fact]
    public void JoueurDeLAutreClub_Erreur()
    {
        // Joueur 11 (extérieur) aligné côté domicile
        var errors = Validate(new BoardLineupInput(1, new[] { 11 }, new[] { 1 }));
        Assert.Contains(errors, e => e.Contains("effectif du club domicile"));
        Assert.Contains(errors, e => e.Contains("effectif du club extérieur"));
    }

    [Fact]
    public void PositionHorsLimites_Erreur()
    {
        var errors = Validate(new BoardLineupInput(4, new[] { 1 }, new[] { 11 }));
        Assert.Contains(errors, e => e.Contains("Board 4 invalide"));
    }

    [Fact]
    public void PositionDupliquee_Erreur()
    {
        var errors = Validate(
            new BoardLineupInput(1, new[] { 1 }, new[] { 11 }),
            new BoardLineupInput(1, new[] { 2 }, new[] { 12 }));
        Assert.Contains(errors, e => e.Contains("plusieurs fois"));
    }
}
