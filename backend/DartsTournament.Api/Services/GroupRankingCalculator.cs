using System.Text.Json.Serialization;

namespace DartsTournament.Api.Services;

/// <summary>Critère de départage en poule, appliqués dans l'ordre configuré.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TieBreaker>))]
public enum TieBreaker
{
    // Matchs gagnés
    Wins,
    // Manches gagnées - manches perdues
    LegDifference,
    // Mini-championnat entre les seuls ex-aequo (victoires entre eux)
    HeadToHead,
    // Manches gagnées
    LegsWon
}

public record GroupMatchResult(int Side1Id, int Side2Id, int Side1Legs, int Side2Legs, int? WinnerId);

public record GroupRankEntry(
    int SideId,
    int Played,
    int Won,
    int LegsFor,
    int LegsAgainst,
    int Rank,
    // Égalité parfaite non tranchée (pas d'ordre manuel) : rang partagé
    bool TieUnresolved);

/// <summary>
/// Classement d'une poule : critères ordonnés, puis ordre manuel de l'admin
/// (ex. 701 à plusieurs joué hors appli) pour les égalités parfaites restantes.
/// </summary>
public static class GroupRankingCalculator
{
    /// <param name="sideIds">Côtés de la poule (ordre de repli si les égalités ne sont pas partagées).</param>
    /// <param name="matches">Matchs terminés de la poule.</param>
    /// <param name="criteria">Critères de départage, dans l'ordre.</param>
    /// <param name="manualOrder">Ordre saisi par l'admin (ids de côtés), appliqué aux égalités parfaites.</param>
    /// <param name="shareUnresolvedRanks">
    /// true : une égalité non tranchée partage le rang et est signalée ; false : rangs
    /// distincts dans l'ordre de <paramref name="sideIds"/> (comportement historique).
    /// </param>
    public static List<GroupRankEntry> Rank(
        IReadOnlyList<int> sideIds,
        IReadOnlyList<GroupMatchResult> matches,
        IReadOnlyList<TieBreaker> criteria,
        IReadOnlyList<int>? manualOrder,
        bool shareUnresolvedRanks)
    {
        var stats = sideIds.ToDictionary(id => id, id => ComputeStats(id, matches));

        // Blocs d'ex-aequo successifs, affinés critère par critère
        var blocks = new List<List<int>> { sideIds.ToList() };
        foreach (var criterion in criteria)
        {
            blocks = blocks
                .SelectMany(block => block.Count > 1 ? Split(block, criterion, stats, matches) : new() { block })
                .ToList();
        }

        var result = new List<GroupRankEntry>();
        int position = 1;
        foreach (var block in blocks)
        {
            var ordered = block;
            bool resolved = block.Count == 1;

            if (!resolved && manualOrder != null && block.All(manualOrder.Contains))
            {
                ordered = block.OrderBy(id => IndexOf(manualOrder, id)).ToList();
                resolved = true;
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                var s = stats[ordered[i]];
                bool shared = !resolved && shareUnresolvedRanks;
                result.Add(new GroupRankEntry(
                    ordered[i], s.Played, s.Won, s.LegsFor, s.LegsAgainst,
                    shared ? position : position + i,
                    shared));
            }
            position += block.Count;
        }

        return result;
    }

    private static List<List<int>> Split(
        List<int> block, TieBreaker criterion,
        Dictionary<int, Stats> stats, IReadOnlyList<GroupMatchResult> matches)
    {
        if (criterion == TieBreaker.HeadToHead)
            return SplitHeadToHead(block, matches);

        return block
            .GroupBy(id => criterion switch
            {
                TieBreaker.Wins => stats[id].Won,
                TieBreaker.LegDifference => stats[id].LegsFor - stats[id].LegsAgainst,
                TieBreaker.LegsWon => stats[id].LegsFor,
                _ => 0
            })
            .OrderByDescending(g => g.Key)
            .Select(g => g.ToList())
            .ToList();
    }

    // Victoires entre les seuls ex-aequo ; si le bloc se scinde, on recommence sur
    // chaque sous-bloc encore à égalité (ex. 3 à égalité, un se détache, les 2 autres
    // sont départagés par leur match)
    private static List<List<int>> SplitHeadToHead(List<int> block, IReadOnlyList<GroupMatchResult> matches)
    {
        var members = block.ToHashSet();
        var subBlocks = block
            .GroupBy(id => matches.Count(m => m.WinnerId == id
                && members.Contains(m.Side1Id) && members.Contains(m.Side2Id)))
            .OrderByDescending(g => g.Key)
            .Select(g => g.ToList())
            .ToList();

        if (subBlocks.Count == 1)
            return subBlocks;

        return subBlocks
            .SelectMany(sub => sub.Count > 1 ? SplitHeadToHead(sub, matches) : new() { sub })
            .ToList();
    }

    private static int IndexOf(IReadOnlyList<int> list, int value)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == value)
                return i;
        return int.MaxValue;
    }

    private record Stats(int Played, int Won, int LegsFor, int LegsAgainst);

    private static Stats ComputeStats(int sideId, IReadOnlyList<GroupMatchResult> matches)
    {
        int played = 0, won = 0, legsFor = 0, legsAgainst = 0;
        foreach (var m in matches)
        {
            if (m.Side1Id == sideId)
            {
                played++;
                legsFor += m.Side1Legs;
                legsAgainst += m.Side2Legs;
            }
            else if (m.Side2Id == sideId)
            {
                played++;
                legsFor += m.Side2Legs;
                legsAgainst += m.Side1Legs;
            }
            else continue;

            if (m.WinnerId == sideId)
                won++;
        }
        return new Stats(played, won, legsFor, legsAgainst);
    }
}
