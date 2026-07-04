using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class TurnRotationCalculatorTests
{
    // Ids : côté 1 = A1(1), A2(2) ; côté 2 = B1(11), B2(12)
    private static readonly int[] Side1 = { 1, 2 };
    private static readonly int[] Side2 = { 11, 12 };
    private static readonly int[] Solo1 = { 1 };
    private static readonly int[] Solo2 = { 11 };

    // ----- BuildLegRotation -----

    [Fact]
    public void Rotation_Double_AlterneStrictementLesQuatreLanceurs()
    {
        var rotation = TurnRotationCalculator.BuildLegRotation(Side1, Side2, side1Starts: true);
        Assert.Equal(new[] { 1, 11, 2, 12 }, rotation);
    }

    [Fact]
    public void Rotation_Double_Cote2Commence()
    {
        var rotation = TurnRotationCalculator.BuildLegRotation(Side1, Side2, side1Starts: false);
        Assert.Equal(new[] { 11, 1, 12, 2 }, rotation);
    }

    [Fact]
    public void Rotation_Simple_DegenereEnAlternanceClassique()
    {
        var rotation = TurnRotationCalculator.BuildLegRotation(Solo1, Solo2, side1Starts: true);
        Assert.Equal(new[] { 1, 11 }, rotation);
    }

    // ----- NextThrower -----

    [Theory]
    [InlineData(0, 1)]   // première volée du leg
    [InlineData(1, 11)]
    [InlineData(2, 2)]
    [InlineData(3, 12)]
    [InlineData(4, 1)]   // la rotation boucle
    [InlineData(7, 12)]
    public void NextThrower_Double_SuitLaRotation(int throwsInLeg, int expectedThrower)
    {
        var rotation = TurnRotationCalculator.BuildLegRotation(Side1, Side2, side1Starts: true);
        Assert.Equal(expectedThrower, TurnRotationCalculator.NextThrower(rotation, throwsInLeg));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 11)]
    [InlineData(2, 1)]
    public void NextThrower_Simple_AlterneLesDeuxJoueurs(int throwsInLeg, int expectedThrower)
    {
        var rotation = TurnRotationCalculator.BuildLegRotation(Solo1, Solo2, side1Starts: true);
        Assert.Equal(expectedThrower, TurnRotationCalculator.NextThrower(rotation, throwsInLeg));
    }

    [Fact]
    public void NextThrower_ScenarioUndo_RecalculRedonneLeLanceurDeLaVoleeRetiree()
    {
        // 5 volées jouées (1, 11, 2, 12, 1) ; on annule la 5e :
        // le prochain lanceur recalculé avec 4 volées doit être le lanceur retiré
        var rotation = TurnRotationCalculator.BuildLegRotation(Side1, Side2, side1Starts: true);
        int removedThrower = TurnRotationCalculator.NextThrower(rotation, 4);
        Assert.Equal(1, removedThrower);
        Assert.Equal(removedThrower, TurnRotationCalculator.NextThrower(rotation, 5 - 1));
    }

    // ----- Side1StartsLeg -----

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(2, true, false)]
    [InlineData(3, true, true)]
    [InlineData(1, false, false)]
    [InlineData(2, false, true)]
    [InlineData(3, false, false)]
    [InlineData(4, false, true)]
    public void Side1StartsLeg_AlterneParParite(int legNumber, bool side1StartedLeg1, bool expected)
    {
        Assert.Equal(expected, TurnRotationCalculator.Side1StartsLeg(legNumber, side1StartedLeg1));
    }

    // ----- SideOfThrower -----

    [Fact]
    public void SideOfThrower_TrouveLeBonCote()
    {
        Assert.Equal(1, TurnRotationCalculator.SideOfThrower(2, Side1, Side2));
        Assert.Equal(2, TurnRotationCalculator.SideOfThrower(12, Side1, Side2));
    }

    [Fact]
    public void SideOfThrower_LanceurInconnu_Exception()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TurnRotationCalculator.SideOfThrower(99, Side1, Side2));
    }
}
