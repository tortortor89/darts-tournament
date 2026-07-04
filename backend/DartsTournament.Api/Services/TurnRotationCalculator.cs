namespace DartsTournament.Api.Services;

/// <summary>
/// Logique pure de rotation des lanceurs d'une session de match.
/// Simple : un lanceur par côté ([P1], [P2]) — comportement historique.
/// Double : deux lanceurs par côté, alternance stricte A1→B1→A2→B2 avec un ordre
/// interne fixé pour tout le match (simplification v1 par rapport aux règles
/// officielles qui autorisent à changer l'ordre entre les legs).
/// Le côté qui démarre alterne à chaque leg ; chaque leg repart sur le premier
/// lanceur de ce côté. Le lanceur courant est donc entièrement dérivable de
/// (rosters, parité du leg, nombre de volées du leg) — ce qui rend l'undo trivial.
/// </summary>
public static class TurnRotationCalculator
{
    /// <summary>
    /// Ordre de passage complet d'un leg : [A1, B1] en simple, [A1, B1, A2, B2] en double,
    /// où A est le côté qui démarre le leg.
    /// </summary>
    public static IReadOnlyList<int> BuildLegRotation(
        IReadOnlyList<int> side1Order,
        IReadOnlyList<int> side2Order,
        bool side1Starts)
    {
        var first = side1Starts ? side1Order : side2Order;
        var second = side1Starts ? side2Order : side1Order;

        var rotation = new List<int>();
        int max = Math.Max(first.Count, second.Count);
        for (int i = 0; i < max; i++)
        {
            if (i < first.Count) rotation.Add(first[i]);
            if (i < second.Count) rotation.Add(second[i]);
        }
        return rotation;
    }

    /// <summary>
    /// Le lanceur de la prochaine volée, sachant que throwsAlreadyInLeg volées
    /// ont déjà été jouées dans ce leg (tous lanceurs confondus).
    /// </summary>
    public static int NextThrower(IReadOnlyList<int> rotation, int throwsAlreadyInLeg)
    {
        return rotation[throwsAlreadyInLeg % rotation.Count];
    }

    /// <summary>
    /// Le côté 1 démarre-t-il ce leg ? (le côté qui démarre alterne à chaque leg)
    /// </summary>
    public static bool Side1StartsLeg(int legNumber, bool side1StartedLeg1)
    {
        return legNumber % 2 == 1 ? side1StartedLeg1 : !side1StartedLeg1;
    }

    /// <summary>
    /// Côté (1 ou 2) auquel appartient un lanceur.
    /// </summary>
    public static int SideOfThrower(int throwerId, IReadOnlyList<int> side1Order, IReadOnlyList<int> side2Order)
    {
        if (side1Order.Contains(throwerId)) return 1;
        if (side2Order.Contains(throwerId)) return 2;
        throw new InvalidOperationException($"Le lanceur {throwerId} n'appartient à aucun des deux côtés");
    }
}
