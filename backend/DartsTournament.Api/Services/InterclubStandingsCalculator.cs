namespace DartsTournament.Api.Services;

public record EncounterResult(int HomeClubId, int AwayClubId, int HomeScore, int AwayScore);

public record InterclubStandingRow(
    int ClubId,
    string ClubName,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int Points,
    int MatchesWon,
    int MatchesLost,
    int Rank
);

/// <summary>
/// Classement de saison d'un championnat interclubs à partir des rencontres
/// terminées. Barème configurable (défaut V2/N1/D0), départage au nombre de
/// matchs individuels gagnés, rangs "compétition" partagés (1, 2, 2, 4...).
/// Fonction pure, testable sans base de données.
/// </summary>
public static class InterclubStandingsCalculator
{
    public static List<InterclubStandingRow> Compute(
        IReadOnlyList<EncounterResult> completedEncounters,
        IReadOnlyList<(int ClubId, string Name)> clubs,
        int pointsForWin = 2,
        int pointsForDraw = 1,
        int pointsForLoss = 0)
    {
        var rows = new Dictionary<int, (int Played, int Wins, int Draws, int Losses, int MWon, int MLost)>();
        foreach (var (clubId, _) in clubs)
            rows[clubId] = (0, 0, 0, 0, 0, 0);

        foreach (var e in completedEncounters)
        {
            if (rows.TryGetValue(e.HomeClubId, out var home))
            {
                rows[e.HomeClubId] = (
                    home.Played + 1,
                    home.Wins + (e.HomeScore > e.AwayScore ? 1 : 0),
                    home.Draws + (e.HomeScore == e.AwayScore ? 1 : 0),
                    home.Losses + (e.HomeScore < e.AwayScore ? 1 : 0),
                    home.MWon + e.HomeScore,
                    home.MLost + e.AwayScore);
            }

            if (rows.TryGetValue(e.AwayClubId, out var away))
            {
                rows[e.AwayClubId] = (
                    away.Played + 1,
                    away.Wins + (e.AwayScore > e.HomeScore ? 1 : 0),
                    away.Draws + (e.AwayScore == e.HomeScore ? 1 : 0),
                    away.Losses + (e.AwayScore < e.HomeScore ? 1 : 0),
                    away.MWon + e.AwayScore,
                    away.MLost + e.HomeScore);
            }
        }

        var sorted = clubs
            .Select(c =>
            {
                var r = rows[c.ClubId];
                int points = r.Wins * pointsForWin + r.Draws * pointsForDraw + r.Losses * pointsForLoss;
                return (c.ClubId, c.Name, r.Played, r.Wins, r.Draws, r.Losses, Points: points, r.MWon, r.MLost);
            })
            .OrderByDescending(c => c.Points)
            .ThenByDescending(c => c.MWon)
            .ThenBy(c => c.Name)
            .ToList();

        // Rangs "compétition" : même (points, matchs gagnés) => rang partagé
        var standings = new List<InterclubStandingRow>();
        int position = 0;
        int currentRank = 0;
        (int, int)? previousKey = null;

        foreach (var c in sorted)
        {
            position++;
            var key = (c.Points, c.MWon);
            if (previousKey == null || key != previousKey)
                currentRank = position;
            previousKey = key;

            standings.Add(new InterclubStandingRow(
                c.ClubId, c.Name, c.Played, c.Wins, c.Draws, c.Losses,
                c.Points, c.MWon, c.MLost, currentRank));
        }

        return standings;
    }
}
