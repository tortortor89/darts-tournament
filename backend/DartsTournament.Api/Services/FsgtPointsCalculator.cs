using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public record PlayerTournamentPoints(int PlayerId, string PlayerName, int Rank, int Points);

/// <summary>
/// Barème FSGT « Point Tournois » d'un tournoi terminé (option des circuits).
/// Qualifiés : points du tour atteint, ligne choisie selon le nombre d'inscrits (côtés) ;
/// ils incluent la participation. Éliminés en poules (et poule unique) : participation
/// + points par victoire en poule. En double, chaque membre reçoit les points de sa paire.
/// Fonction pure : même chargement que <see cref="FinalPlacementCalculator"/>.
/// </summary>
public static class FsgtPointsCalculator
{
    public static List<PlayerTournamentPoints> Compute(Tournament tournament, FsgtPoints points)
    {
        bool isDoubles = MatchSideAccessor.IsDoubles(tournament);
        var placements = FinalPlacementCalculator.ComputeSides(tournament);
        var members = FinalPlacementCalculator.GetSideMembers(tournament);
        int sideCount = members.Count;

        // Matchs de tableau : phase finale d'un tournoi en poules, ou tout le tableau
        // d'un tournoi à élimination
        bool allBracket = tournament.Format is TournamentFormat.SingleElimination or TournamentFormat.DoubleElimination;
        var bracketMatches = tournament.Matches.Where(m => allBracket || m.IsKnockoutMatch).ToList();
        var poolMatches = tournament.Matches.Where(m => !allBracket && !m.IsKnockoutMatch).ToList();

        var result = new List<PlayerTournamentPoints>();
        foreach (var placement in placements)
        {
            bool inBracket = bracketMatches.Any(m =>
                MatchSideAccessor.GetSide1Id(m, isDoubles) == placement.SideId
                || MatchSideAccessor.GetSide2Id(m, isDoubles) == placement.SideId);
            int poolWins = poolMatches.Count(m =>
                m.Status == MatchStatus.Completed
                && MatchSideAccessor.GetWinnerSideId(m, isDoubles) == placement.SideId);

            int sidePoints = PointsFor(sideCount, inBracket, placement.Rank, poolWins, points);

            foreach (var (playerId, playerName) in members.GetValueOrDefault(placement.SideId) ?? new())
                result.Add(new PlayerTournamentPoints(playerId, playerName, placement.Rank, sidePoints));
        }

        return result.OrderBy(p => p.Rank).ThenBy(p => p.PlayerName).ToList();
    }

    /// <summary>
    /// Points d'un côté. <paramref name="rank"/> = place finale (1 vainqueur, 2 finaliste,
    /// 3 demi, 5 quart, 9 8e, 17 16e). Un tour absent de la ligne (tableau plus profond
    /// que prévu pour ce nombre d'inscrits, format FSGT contourné) compte comme une
    /// élimination en poules.
    /// </summary>
    public static int PointsFor(int sideCount, bool inBracket, int rank, int poolWins, FsgtPoints points)
    {
        int poolPoints = points.PoolElimination.Participation + points.PoolElimination.PerWin * poolWins;
        if (!inBracket)
            return poolPoints;

        var row = RowFor(sideCount, points);
        if (row == null)
            return poolPoints;

        int? stagePoints = rank switch
        {
            1 => row.Winner,
            2 => row.Final,
            <= 4 => row.SemiFinal,
            <= 8 => row.QuarterFinal,
            <= 16 => row.RoundOf16,
            <= 32 => row.RoundOf32,
            _ => null
        };

        return stagePoints ?? poolPoints;
    }

    // Ligne du barème pour ce nombre d'inscrits ; hors grille (tableau sans poules de
    // moins de 6, ou plus de 80), la ligne la plus proche
    private static FsgtKnockoutPoints? RowFor(int sideCount, FsgtPoints points)
    {
        var row = points.Knockout.FirstOrDefault(r => sideCount >= r.MinPlayers && sideCount <= r.MaxPlayers);
        if (row != null || points.Knockout.Count == 0)
            return row;

        return sideCount < points.Knockout.Min(r => r.MinPlayers)
            ? points.Knockout.MinBy(r => r.MinPlayers)
            : points.Knockout.MaxBy(r => r.MaxPlayers);
    }
}
