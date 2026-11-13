using System.Text.RegularExpressions;

namespace DartsTournament.Api.Services;

/// <summary>
/// Calculs purs du format FSGT : répartition des côtés dans des poules de tailles imposées
/// et résolution des affiches du tableau final (« 1p1 » = 1er de la poule 1).
/// </summary>
public static partial class FsgtDrawCalculator
{
    [GeneratedRegex(@"^\s*(\d+)\s*p\s*(\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex SlotRegex();

    /// <summary>
    /// Index de poule (base 0) de chaque côté, dans l'ordre des seeds. Serpentin
    /// (A, B, C, C, B, A...) en sautant les poules déjà pleines.
    /// </summary>
    public static int[] AssignGroups(int sideCount, IReadOnlyList<int> groupSizes)
    {
        if (groupSizes.Sum() != sideCount)
            throw new InvalidOperationException(
                $"Les poules totalisent {groupSizes.Sum()} places pour {sideCount} inscrits");

        var result = new int[sideCount];
        var filled = new int[groupSizes.Count];
        int assigned = 0;
        bool forward = true;

        while (assigned < sideCount)
        {
            for (int k = 0; k < groupSizes.Count && assigned < sideCount; k++)
            {
                int g = forward ? k : groupSizes.Count - 1 - k;
                if (filled[g] >= groupSizes[g])
                    continue;

                result[assigned++] = g;
                filled[g]++;
            }
            forward = !forward;
        }

        return result;
    }

    /// <summary>« 2p3 » → (rang 2, poule 3), poule numérotée à partir de 1.</summary>
    public static (int Rank, int Group) ParseSlot(string token)
    {
        var match = SlotRegex().Match(token);
        if (!match.Success)
            throw new InvalidOperationException($"Affiche FSGT invalide : « {token} »");

        return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
    }

    /// <summary>Affiches figées sur le tournoi à la génération : « 1p1-2p2,1p2-2p1 ».</summary>
    public static string SerializePairings(IEnumerable<IReadOnlyList<string>> bracket) =>
        string.Join(",", bracket.Select(p => $"{p[0].Replace(" ", "")}-{p[1].Replace(" ", "")}"));

    public static List<((int Rank, int Group) Side1, (int Rank, int Group) Side2)> ParsePairings(string pairings) =>
        pairings
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p =>
            {
                var parts = p.Split('-');
                if (parts.Length != 2)
                    throw new InvalidOperationException($"Affiche FSGT invalide : « {p} »");
                return (ParseSlot(parts[0]), ParseSlot(parts[1]));
            })
            .ToList();

    /// <summary>
    /// Côtés du premier tour du tableau final, dans l'ordre des positions (2 par affiche).
    /// <paramref name="qualifiedByGroup"/> : pour chaque poule (base 0), les côtés qualifiés
    /// dans l'ordre du classement.
    /// </summary>
    public static List<int> ResolveFirstRound(string pairings, IReadOnlyList<IReadOnlyList<int>> qualifiedByGroup)
    {
        int Resolve((int Rank, int Group) slot)
        {
            if (slot.Group < 1 || slot.Group > qualifiedByGroup.Count
                || slot.Rank < 1 || slot.Rank > qualifiedByGroup[slot.Group - 1].Count)
                throw new InvalidOperationException(
                    $"Affiche FSGT {slot.Rank}p{slot.Group} sans qualifié correspondant");
            return qualifiedByGroup[slot.Group - 1][slot.Rank - 1];
        }

        var positions = new List<int>();
        foreach (var (side1, side2) in ParsePairings(pairings))
        {
            positions.Add(Resolve(side1));
            positions.Add(Resolve(side2));
        }
        return positions;
    }
}
