using DartsTournament.Api.Services;
using Xunit;

namespace DartsTournament.Tests;

public class InterclubCalendarCalculatorTests
{
    [Fact]
    public void NombrePair_BonNombreDeJourneesEtDeRencontres()
    {
        var clubs = new[] { 1, 2, 3, 4 };
        var slots = InterclubCalendarCalculator.Generate(clubs);

        // 4 clubs : 3 journées aller + 3 retour, 2 rencontres par journée
        Assert.Equal(6, slots.Max(s => s.Round));
        Assert.Equal(12, slots.Count);
        for (int round = 1; round <= 6; round++)
            Assert.Equal(2, slots.Count(s => s.Round == round));
    }

    [Fact]
    public void ChaqueClubJoueUneFoisParJournee()
    {
        var clubs = new[] { 1, 2, 3, 4, 5, 6 };
        var slots = InterclubCalendarCalculator.Generate(clubs);

        foreach (var round in slots.GroupBy(s => s.Round))
        {
            var participants = round.SelectMany(s => new[] { s.HomeClubId, s.AwayClubId }).ToList();
            Assert.Equal(participants.Count, participants.Distinct().Count());
            Assert.Equal(clubs.Length, participants.Count);
        }
    }

    [Fact]
    public void ChaquePaireSeRencontreDeuxFoisAvecDomicileInverse()
    {
        var clubs = new[] { 1, 2, 3, 4 };
        var slots = InterclubCalendarCalculator.Generate(clubs);

        foreach (var a in clubs)
        {
            foreach (var b in clubs.Where(c => c > a))
            {
                var meetings = slots
                    .Where(s => (s.HomeClubId == a && s.AwayClubId == b)
                        || (s.HomeClubId == b && s.AwayClubId == a))
                    .ToList();
                Assert.Equal(2, meetings.Count);
                // Une fois chez l'un, une fois chez l'autre
                Assert.Single(meetings, m => m.HomeClubId == a);
                Assert.Single(meetings, m => m.HomeClubId == b);
            }
        }
    }

    [Fact]
    public void EquilibreDomicileExterieurSurLaSaison()
    {
        var clubs = new[] { 10, 20, 30, 40, 50, 60 };
        var slots = InterclubCalendarCalculator.Generate(clubs);

        foreach (var club in clubs)
        {
            int home = slots.Count(s => s.HomeClubId == club);
            int away = slots.Count(s => s.AwayClubId == club);
            Assert.Equal(home, away); // exact grâce au miroir aller-retour
        }
    }

    [Fact]
    public void NombreImpair_UnExemptParJournee()
    {
        var clubs = new[] { 1, 2, 3 };
        var slots = InterclubCalendarCalculator.Generate(clubs);

        // 3 clubs : 3 journées aller + 3 retour, 1 rencontre par journée (1 exempt)
        Assert.Equal(6, slots.Max(s => s.Round));
        for (int round = 1; round <= 6; round++)
        {
            var roundSlots = slots.Where(s => s.Round == round).ToList();
            Assert.Single(roundSlots);
        }
        // Chaque club joue 4 rencontres (2 contre chacun des 2 autres)
        foreach (var club in clubs)
            Assert.Equal(4, slots.Count(s => s.HomeClubId == club || s.AwayClubId == club));
    }

    [Fact]
    public void DeuxClubs_AllerRetour()
    {
        var slots = InterclubCalendarCalculator.Generate(new[] { 1, 2 });

        Assert.Equal(2, slots.Count);
        Assert.Equal(1, slots[0].Round);
        Assert.Equal(2, slots[1].Round);
        Assert.Equal(slots[0].HomeClubId, slots[1].AwayClubId);
        Assert.Equal(slots[0].AwayClubId, slots[1].HomeClubId);
    }

    [Fact]
    public void AllerSimple_MoitieDesJournees()
    {
        var slots = InterclubCalendarCalculator.Generate(new[] { 1, 2, 3, 4 }, doubleRoundRobin: false);

        Assert.Equal(3, slots.Max(s => s.Round));
        Assert.Equal(6, slots.Count);
    }

    [Fact]
    public void MoinsDeDeuxClubs_Exception()
    {
        Assert.Throws<InvalidOperationException>(() =>
            InterclubCalendarCalculator.Generate(new[] { 1 }));
    }
}
