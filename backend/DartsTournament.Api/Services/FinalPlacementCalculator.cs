using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public record FinalPlacement(int PlayerId, string PlayerName, int Rank);

/// <summary>
/// Calcule la place finale de chaque joueur d'un tournoi, selon le format.
/// Classement "compétition" : les ex æquo partagent le même rang (1, 2, 3, 3, 5...).
/// Fonction pure : le tournoi doit être chargé avec Matches et TournamentPlayers.Player.
/// </summary>
public static class FinalPlacementCalculator
{
    public static List<FinalPlacement> Compute(Tournament tournament)
    {
        // Un joueur ayant réellement disputé des matchs compte, même si son statut
        // d'inscription est resté Pending (données antérieures à la migration
        // AddRegistrationStatus, remplies avec Pending par défaut)
        var playerIdsInMatches = tournament.Matches
            .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
            .Where(id => id != null)
            .Select(id => id!.Value)
            .ToHashSet();

        var players = tournament.TournamentPlayers
            .Where(tp => tp.Status == RegistrationStatus.Approved
                || playerIdsInMatches.Contains(tp.PlayerId))
            .ToList();

        if (players.Count == 0)
            return new List<FinalPlacement>();

        return tournament.Format switch
        {
            TournamentFormat.SingleElimination => ComputeEliminationBracket(tournament.Matches.ToList(), players),
            TournamentFormat.RoundRobin => ComputeRoundRobin(tournament.Matches.ToList(), players),
            TournamentFormat.GroupStage => ComputeGroupStage(tournament, players),
            TournamentFormat.DoubleElimination => ComputeDoubleElimination(tournament.Matches.ToList(), players),
            _ => new List<FinalPlacement>()
        };
    }

    // ----- Élimination directe (utilisé aussi pour le knockout du GroupStage) -----

    private static List<FinalPlacement> ComputeEliminationBracket(List<Match> matches, List<TournamentPlayer> players)
    {
        if (matches.Count == 0)
            return new List<FinalPlacement>();

        int maxRound = matches.Max(m => m.Round);

        var finalMatch = matches.FirstOrDefault(m =>
            m.Round == maxRound && m.Status == MatchStatus.Completed && m.WinnerId != null);
        int? championId = finalMatch?.WinnerId;

        var placements = new List<FinalPlacement>();

        foreach (var tp in players)
        {
            var playerMatches = matches
                .Where(m => m.Player1Id == tp.PlayerId || m.Player2Id == tp.PlayerId)
                .ToList();

            if (playerMatches.Count == 0)
                continue; // joueur absent de ce bracket (ex: non qualifié pour le knockout)

            int rank;
            if (tp.PlayerId == championId)
            {
                rank = 1;
            }
            else
            {
                // Match d'élimination : défaite dans un match complété avec deux vrais joueurs
                // (les byes ont un joueur null et ne comptent pas comme élimination)
                var eliminationMatch = playerMatches.FirstOrDefault(m =>
                    m.Status == MatchStatus.Completed
                    && m.WinnerId != null && m.WinnerId != tp.PlayerId
                    && m.Player1Id != null && m.Player2Id != null);

                // Non éliminé (tournoi incomplet) : place selon le dernier round atteint
                int round = eliminationMatch?.Round ?? playerMatches.Max(m => m.Round);

                // Perdant de la finale (round max) -> 2, demi -> 3, quart -> 5...
                rank = (int)Math.Pow(2, maxRound - round) + 1;
            }

            placements.Add(new FinalPlacement(tp.PlayerId, PlayerName(tp), rank));
        }

        return placements.OrderBy(p => p.Rank).ThenBy(p => p.PlayerName).ToList();
    }

    // ----- Round Robin -----

    private static List<FinalPlacement> ComputeRoundRobin(List<Match> matches, List<TournamentPlayer> players)
    {
        var stats = players
            .Select(tp => (Player: tp, Stats: ComputePoints(matches, tp.PlayerId)))
            .ToList();

        return AssignCompetitionRanks(
            stats.OrderByDescending(s => s.Stats.Points)
                .ThenByDescending(s => s.Stats.Diff)
                .ThenByDescending(s => s.Stats.PointsFor),
            s => s.Stats, // même triplet (points, diff, pour) => rang partagé
            s => s.Player);
    }

    // ----- Group Stage -----

    private static List<FinalPlacement> ComputeGroupStage(Tournament tournament, List<TournamentPlayer> players)
    {
        var matches = tournament.Matches.ToList();
        var knockoutMatches = matches.Where(m => m.IsKnockoutMatch).ToList();

        // Classement en groupe (rang de chaque joueur dans son groupe)
        var groupRanks = new Dictionary<int, int>(); // playerId -> rang dans le groupe
        foreach (var group in tournament.Groups)
        {
            var groupPlayers = players.Where(tp => tp.GroupId == group.Id).ToList();
            var groupMatches = matches.Where(m => m.GroupId == group.Id).ToList();

            var ranked = AssignCompetitionRanks(
                groupPlayers
                    .Select(tp => (Player: tp, Stats: ComputePoints(groupMatches, tp.PlayerId)))
                    .OrderByDescending(s => s.Stats.Points)
                    .ThenByDescending(s => s.Stats.Diff)
                    .ThenByDescending(s => s.Stats.PointsFor),
                s => s.Stats,
                s => s.Player);

            foreach (var p in ranked)
                groupRanks[p.PlayerId] = p.Rank;
        }

        var placements = new List<FinalPlacement>();

        // Qualifiés : classés par le bracket knockout
        var knockoutPlacements = ComputeEliminationBracket(knockoutMatches, players);
        placements.AddRange(knockoutPlacements);

        // Non qualifiés : derrière les qualifiés, par palier de rang de groupe
        // (tous les 3es de groupe à égalité, puis tous les 4es, etc.)
        var knockoutPlayerIds = knockoutPlacements.Select(p => p.PlayerId).ToHashSet();
        var nonQualified = players.Where(tp => !knockoutPlayerIds.Contains(tp.PlayerId)).ToList();

        int currentRank = knockoutPlayerIds.Count + 1;
        foreach (var tier in nonQualified
            .GroupBy(tp => groupRanks.GetValueOrDefault(tp.PlayerId, int.MaxValue))
            .OrderBy(g => g.Key))
        {
            foreach (var tp in tier)
                placements.Add(new FinalPlacement(tp.PlayerId, PlayerName(tp), currentRank));
            currentRank += tier.Count();
        }

        return placements.OrderBy(p => p.Rank).ThenBy(p => p.PlayerName).ToList();
    }

    // ----- Double Elimination -----

    private static List<FinalPlacement> ComputeDoubleElimination(List<Match> matches, List<TournamentPlayer> players)
    {
        var completedMatches = matches.Where(m => m.Status == MatchStatus.Completed).ToList();
        if (completedMatches.Count == 0)
            return new List<FinalPlacement>();

        // Champion = vainqueur du dernier match de Grande Finale (bracket reset inclus)
        var lastGrandFinal = completedMatches
            .Where(m => m.BracketType == BracketType.GrandFinal)
            .OrderByDescending(m => m.Round)
            .FirstOrDefault();
        int? championId = lastGrandFinal?.WinnerId;

        // Point d'élimination de chaque joueur (même logique que GetDoubleEliminationStandings)
        var entries = new List<(TournamentPlayer Player, bool IsChampion, bool InGrandFinal, int Round)>();

        foreach (var tp in players)
        {
            var playerMatches = completedMatches
                .Where(m => m.Player1Id == tp.PlayerId || m.Player2Id == tp.PlayerId)
                .ToList();

            var eliminationMatch = playerMatches
                .Where(m => m.WinnerId != null && m.WinnerId != tp.PlayerId)
                .Where(m => m.BracketType == BracketType.Losers || m.BracketType == BracketType.GrandFinal)
                .OrderByDescending(m => m.BracketType == BracketType.GrandFinal ? 1000 : 0)
                .ThenByDescending(m => m.Round)
                .FirstOrDefault();

            bool isChampion = tp.PlayerId == championId;
            bool inGrandFinal = isChampion || eliminationMatch?.BracketType == BracketType.GrandFinal;
            int round = isChampion ? int.MaxValue
                : eliminationMatch?.Round ?? (playerMatches.Count == 0 ? -1 : 0);

            entries.Add((tp, isChampion, inGrandFinal, round));
        }

        // Même palier (bracket, round d'élimination) => rang partagé
        return AssignCompetitionRanks(
            entries.OrderByDescending(e => e.IsChampion)
                .ThenByDescending(e => e.InGrandFinal)
                .ThenByDescending(e => e.Round),
            e => (e.IsChampion, e.InGrandFinal, e.Round),
            e => e.Player);
    }

    // ----- Helpers -----

    private static string PlayerName(TournamentPlayer tp) =>
        $"{tp.Player.FirstName} {tp.Player.LastName}";

    private static (int Points, int Diff, int PointsFor) ComputePoints(List<Match> matches, int playerId)
    {
        var playerMatches = matches
            .Where(m => m.Status == MatchStatus.Completed)
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
            .ToList();

        int won = playerMatches.Count(m => m.WinnerId == playerId);
        int pointsFor = playerMatches.Sum(m =>
            m.Player1Id == playerId ? (m.Player1Score ?? 0) : (m.Player2Score ?? 0));
        int pointsAgainst = playerMatches.Sum(m =>
            m.Player1Id == playerId ? (m.Player2Score ?? 0) : (m.Player1Score ?? 0));

        return (won * 3, pointsFor - pointsAgainst, pointsFor);
    }

    // Attribue les rangs "compétition" sur une séquence déjà triée :
    // les éléments de même clé partagent le rang, le suivant saute d'autant
    private static List<FinalPlacement> AssignCompetitionRanks<T, TKey>(
        IEnumerable<T> sorted,
        Func<T, TKey> tierKey,
        Func<T, TournamentPlayer> player)
        where TKey : notnull
    {
        var placements = new List<FinalPlacement>();
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

            var tp = player(item);
            placements.Add(new FinalPlacement(tp.PlayerId, PlayerName(tp), currentRank));
        }

        return placements;
    }
}
