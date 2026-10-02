using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public record FinalPlacement(int PlayerId, string PlayerName, int Rank);
public record SidePlacement(int SideId, string SideName, int Rank);

/// <summary>
/// Calcule la place finale de chaque côté d'un tournoi (joueur en simple, paire en
/// double), selon le format. Classement "compétition" : les ex æquo partagent le
/// même rang (1, 2, 3, 3, 5...).
/// Fonction pure : le tournoi doit être chargé avec Matches et TournamentPlayers.Player
/// (simple) ou Teams.Player1/Player2 (double).
/// </summary>
public static class FinalPlacementCalculator
{
    private sealed record SideEntry(int Id, string Name, int? GroupId, List<(int PlayerId, string PlayerName)> Members);

    /// <summary>
    /// Place finale par côté (utilisé par les classements de tournoi).
    /// </summary>
    public static List<SidePlacement> ComputeSides(Tournament tournament)
    {
        var sides = BuildSides(tournament);
        if (sides.Count == 0)
            return new List<SidePlacement>();

        bool isDoubles = MatchSideAccessor.IsDoubles(tournament);
        var matches = tournament.Matches.ToList();

        return tournament.Format switch
        {
            TournamentFormat.SingleElimination => ComputeEliminationBracket(matches, sides, isDoubles),
            TournamentFormat.RoundRobin => ComputeRoundRobin(matches, sides, isDoubles),
            TournamentFormat.GroupStage => ComputeGroupStage(tournament, sides, isDoubles),
            TournamentFormat.DoubleElimination => ComputeDoubleElimination(matches, sides, isDoubles),
            _ => new List<SidePlacement>()
        };
    }

    /// <summary>
    /// Place finale par joueur (utilisé par les points de circuit).
    /// En double, chaque membre hérite du rang de sa paire.
    /// </summary>
    public static List<FinalPlacement> Compute(Tournament tournament)
    {
        var sides = BuildSides(tournament);
        var membersBySide = sides.ToDictionary(s => s.Id, s => s.Members);

        return ComputeSides(tournament)
            .SelectMany(sp => membersBySide[sp.SideId]
                .Select(m => new FinalPlacement(m.PlayerId, m.PlayerName, sp.Rank)))
            .OrderBy(p => p.Rank)
            .ThenBy(p => p.PlayerName)
            .ToList();
    }

    // ----- Construction des côtés -----

    private static List<SideEntry> BuildSides(Tournament tournament)
    {
        if (MatchSideAccessor.IsDoubles(tournament))
        {
            return tournament.Teams
                .Select(tt => new SideEntry(tt.Id, tt.Name, tt.GroupId,
                    new List<(int, string)>
                    {
                        (tt.Player1Id, $"{tt.Player1.FirstName} {tt.Player1.LastName}"),
                        (tt.Player2Id, $"{tt.Player2.FirstName} {tt.Player2.LastName}")
                    }))
                .ToList();
        }

        // Un joueur ayant réellement disputé des matchs compte, même si son statut
        // d'inscription est resté Pending (données antérieures à la migration
        // AddRegistrationStatus, remplies avec Pending par défaut)
        var playerIdsInMatches = tournament.Matches
            .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
            .Where(id => id != null)
            .Select(id => id!.Value)
            .ToHashSet();

        return tournament.TournamentPlayers
            .Where(tp => tp.Status == RegistrationStatus.Approved
                || playerIdsInMatches.Contains(tp.PlayerId))
            .Select(tp =>
            {
                var name = $"{tp.Player.FirstName} {tp.Player.LastName}";
                return new SideEntry(tp.PlayerId, name, tp.GroupId,
                    new List<(int, string)> { (tp.PlayerId, name) });
            })
            .ToList();
    }

    // ----- Élimination directe (utilisé aussi pour le knockout du GroupStage) -----

    private static List<SidePlacement> ComputeEliminationBracket(List<Match> matches, List<SideEntry> sides, bool isDoubles)
    {
        if (matches.Count == 0)
            return new List<SidePlacement>();

        int maxRound = matches.Max(m => m.Round);

        var finalMatch = matches.FirstOrDefault(m =>
            m.Round == maxRound && m.Status == MatchStatus.Completed
            && MatchSideAccessor.GetWinnerSideId(m, isDoubles) != null);
        int? championId = finalMatch != null ? MatchSideAccessor.GetWinnerSideId(finalMatch, isDoubles) : null;

        var placements = new List<SidePlacement>();

        foreach (var side in sides)
        {
            var sideMatches = matches
                .Where(m => MatchSideAccessor.GetSide1Id(m, isDoubles) == side.Id
                    || MatchSideAccessor.GetSide2Id(m, isDoubles) == side.Id)
                .ToList();

            if (sideMatches.Count == 0)
                continue; // côté absent de ce bracket (ex: non qualifié pour le knockout)

            int rank;
            if (side.Id == championId)
            {
                rank = 1;
            }
            else
            {
                // Match d'élimination : défaite dans un match complété avec deux vrais côtés
                // (les byes ont un côté null et ne comptent pas comme élimination)
                var eliminationMatch = sideMatches.FirstOrDefault(m =>
                    m.Status == MatchStatus.Completed
                    && MatchSideAccessor.GetWinnerSideId(m, isDoubles) != null
                    && MatchSideAccessor.GetWinnerSideId(m, isDoubles) != side.Id
                    && MatchSideAccessor.GetSide1Id(m, isDoubles) != null
                    && MatchSideAccessor.GetSide2Id(m, isDoubles) != null);

                // Non éliminé (tournoi incomplet) : place selon le dernier round atteint
                int round = eliminationMatch?.Round ?? sideMatches.Max(m => m.Round);

                // Perdant de la finale (round max) -> 2, demi -> 3, quart -> 5...
                rank = (int)Math.Pow(2, maxRound - round) + 1;
            }

            placements.Add(new SidePlacement(side.Id, side.Name, rank));
        }

        return placements.OrderBy(p => p.Rank).ThenBy(p => p.SideName).ToList();
    }

    // ----- Round Robin -----

    private static List<SidePlacement> ComputeRoundRobin(List<Match> matches, List<SideEntry> sides, bool isDoubles)
    {
        var stats = sides
            .Select(s => (Side: s, Stats: ComputePoints(matches, s.Id, isDoubles)))
            .ToList();

        return AssignCompetitionRanks(
            stats.OrderByDescending(s => s.Stats.Points)
                .ThenByDescending(s => s.Stats.Diff)
                .ThenByDescending(s => s.Stats.PointsFor),
            s => s.Stats, // même triplet (points, diff, pour) => rang partagé
            s => s.Side);
    }

    // ----- Group Stage -----

    private static List<SidePlacement> ComputeGroupStage(Tournament tournament, List<SideEntry> sides, bool isDoubles)
    {
        var matches = tournament.Matches.ToList();
        var knockoutMatches = matches.Where(m => m.IsKnockoutMatch).ToList();

        // Classement en groupe (rang de chaque côté dans son groupe)
        var groupRanks = new Dictionary<int, int>(); // sideId -> rang dans le groupe
        foreach (var group in tournament.Groups)
        {
            var groupSides = sides.Where(s => s.GroupId == group.Id).ToList();
            var groupMatches = matches.Where(m => m.GroupId == group.Id).ToList();

            var ranked = AssignCompetitionRanks(
                groupSides
                    .Select(s => (Side: s, Stats: ComputePoints(groupMatches, s.Id, isDoubles)))
                    .OrderByDescending(s => s.Stats.Points)
                    .ThenByDescending(s => s.Stats.Diff)
                    .ThenByDescending(s => s.Stats.PointsFor),
                s => s.Stats,
                s => s.Side);

            foreach (var p in ranked)
                groupRanks[p.SideId] = p.Rank;
        }

        var placements = new List<SidePlacement>();

        // Qualifiés : classés par le bracket knockout
        var knockoutPlacements = ComputeEliminationBracket(knockoutMatches, sides, isDoubles);
        placements.AddRange(knockoutPlacements);

        // Non qualifiés : derrière les qualifiés, par palier de rang de groupe
        // (tous les 3es de groupe à égalité, puis tous les 4es, etc.)
        var knockoutSideIds = knockoutPlacements.Select(p => p.SideId).ToHashSet();
        var nonQualified = sides.Where(s => !knockoutSideIds.Contains(s.Id)).ToList();

        int currentRank = knockoutSideIds.Count + 1;
        foreach (var tier in nonQualified
            .GroupBy(s => groupRanks.GetValueOrDefault(s.Id, int.MaxValue))
            .OrderBy(g => g.Key))
        {
            foreach (var side in tier)
                placements.Add(new SidePlacement(side.Id, side.Name, currentRank));
            currentRank += tier.Count();
        }

        return placements.OrderBy(p => p.Rank).ThenBy(p => p.SideName).ToList();
    }

    // ----- Double Elimination -----

    private static List<SidePlacement> ComputeDoubleElimination(List<Match> matches, List<SideEntry> sides, bool isDoubles)
    {
        var completedMatches = matches.Where(m => m.Status == MatchStatus.Completed).ToList();
        if (completedMatches.Count == 0)
            return new List<SidePlacement>();

        // Champion = vainqueur du dernier match de Grande Finale (bracket reset inclus)
        var lastGrandFinal = completedMatches
            .Where(m => m.BracketType == BracketType.GrandFinal)
            .OrderByDescending(m => m.Round)
            .FirstOrDefault();
        int? championId = lastGrandFinal != null
            ? MatchSideAccessor.GetWinnerSideId(lastGrandFinal, isDoubles)
            : null;

        // Point d'élimination de chaque côté (même logique que GetDoubleEliminationStandings)
        var entries = new List<(SideEntry Side, bool IsChampion, bool InGrandFinal, int Round)>();

        foreach (var side in sides)
        {
            var sideMatches = completedMatches
                .Where(m => MatchSideAccessor.GetSide1Id(m, isDoubles) == side.Id
                    || MatchSideAccessor.GetSide2Id(m, isDoubles) == side.Id)
                .ToList();

            var eliminationMatch = sideMatches
                .Where(m => MatchSideAccessor.GetWinnerSideId(m, isDoubles) != null
                    && MatchSideAccessor.GetWinnerSideId(m, isDoubles) != side.Id)
                .Where(m => m.BracketType == BracketType.Losers || m.BracketType == BracketType.GrandFinal)
                .OrderByDescending(m => m.BracketType == BracketType.GrandFinal ? 1000 : 0)
                .ThenByDescending(m => m.Round)
                .FirstOrDefault();

            bool isChampion = side.Id == championId;
            bool inGrandFinal = isChampion || eliminationMatch?.BracketType == BracketType.GrandFinal;
            int round = isChampion ? int.MaxValue
                : eliminationMatch?.Round ?? (sideMatches.Count == 0 ? -1 : 0);

            entries.Add((side, isChampion, inGrandFinal, round));
        }

        // Même palier (bracket, round d'élimination) => rang partagé
        return AssignCompetitionRanks(
            entries.OrderByDescending(e => e.IsChampion)
                .ThenByDescending(e => e.InGrandFinal)
                .ThenByDescending(e => e.Round),
            e => (e.IsChampion, e.InGrandFinal, e.Round),
            e => e.Side);
    }

    // ----- Helpers -----

    private static (int Points, int Diff, int PointsFor) ComputePoints(List<Match> matches, int sideId, bool isDoubles)
    {
        var sideMatches = matches
            .Where(m => m.Status == MatchStatus.Completed)
            .Where(m => MatchSideAccessor.GetSide1Id(m, isDoubles) == sideId
                || MatchSideAccessor.GetSide2Id(m, isDoubles) == sideId)
            .ToList();

        int won = sideMatches.Count(m => MatchSideAccessor.GetWinnerSideId(m, isDoubles) == sideId);
        int pointsFor = sideMatches.Sum(m =>
            MatchSideAccessor.GetSide1Id(m, isDoubles) == sideId ? (m.Player1Score ?? 0) : (m.Player2Score ?? 0));
        int pointsAgainst = sideMatches.Sum(m =>
            MatchSideAccessor.GetSide1Id(m, isDoubles) == sideId ? (m.Player2Score ?? 0) : (m.Player1Score ?? 0));

        return (won * 3, pointsFor - pointsAgainst, pointsFor);
    }

    // Attribue les rangs "compétition" sur une séquence déjà triée :
    // les éléments de même clé partagent le rang, le suivant saute d'autant
    private static List<SidePlacement> AssignCompetitionRanks<T, TKey>(
        IEnumerable<T> sorted,
        Func<T, TKey> tierKey,
        Func<T, SideEntry> side)
        where TKey : notnull
    {
        var placements = new List<SidePlacement>();
        int position = 0;
        int currentRank = 0;
        TKey? previousKey = default;

        foreach (var item in sorted)
        {
            position++;
            var key = tierKey(item);
            if (position == 1 || !key.Equals(previousKey))
                currentRank = position;
            previousKey = key;

            var s = side(item);
            placements.Add(new SidePlacement(s.Id, s.Name, currentRank));
        }

        return placements;
    }
}
