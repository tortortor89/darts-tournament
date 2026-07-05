namespace DartsTournament.Api.Services;

public record CalendarSlot(int Round, int HomeClubId, int AwayClubId);

/// <summary>
/// Génération du calendrier d'un championnat interclubs par la méthode du cercle
/// (tables de Berger) : chaque club joue une fois par journée (exempt si nombre
/// impair). En aller-retour, la phase retour est le miroir domicile/extérieur de
/// la phase aller — l'équilibre domicile/extérieur est alors exact sur la saison.
/// Fonction pure, testable sans base de données.
/// </summary>
public static class InterclubCalendarCalculator
{
    private const int Phantom = -1; // club fictif pour les nombres impairs (exempt)

    public static List<CalendarSlot> Generate(IReadOnlyList<int> clubIds, bool doubleRoundRobin = true)
    {
        if (clubIds.Count < 2)
            throw new InvalidOperationException("Au moins 2 clubs sont nécessaires pour générer un calendrier");
        if (clubIds.Distinct().Count() != clubIds.Count)
            throw new InvalidOperationException("La liste des clubs contient des doublons");

        var teams = new List<int>(clubIds);
        if (teams.Count % 2 == 1)
            teams.Add(Phantom);

        int n = teams.Count;
        int roundsPerLeg = n - 1;
        var firstLeg = new List<CalendarSlot>();

        for (int round = 0; round < roundsPerLeg; round++)
        {
            for (int i = 0; i < n / 2; i++)
            {
                int t1 = teams[i];
                int t2 = teams[n - 1 - i];

                if (t1 == Phantom || t2 == Phantom)
                    continue; // exempt cette journée

                // Alternance domicile/extérieur d'une journée sur l'autre
                var (home, away) = round % 2 == 0 ? (t1, t2) : (t2, t1);
                firstLeg.Add(new CalendarSlot(round + 1, home, away));
            }

            // Rotation du cercle : le premier reste fixe, les autres tournent
            var last = teams[n - 1];
            teams.RemoveAt(n - 1);
            teams.Insert(1, last);
        }

        if (!doubleRoundRobin)
            return firstLeg;

        // Phase retour : miroir domicile/extérieur, journées décalées
        var secondLeg = firstLeg
            .Select(s => new CalendarSlot(s.Round + roundsPerLeg, s.AwayClubId, s.HomeClubId))
            .ToList();

        return firstLeg.Concat(secondLeg).ToList();
    }
}
