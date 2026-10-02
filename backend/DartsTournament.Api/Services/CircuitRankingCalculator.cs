using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

/// <summary>
/// Attribue les points de circuit selon la place finale et agrège le classement général.
/// Fonction pure, testable sans base de données.
/// </summary>
public static class CircuitRankingCalculator
{
    public static int PointsForRank(int rank, IReadOnlyCollection<CircuitPointsRule> rules, int participationPoints)
    {
        var rule = rules.FirstOrDefault(r => rank >= r.MinRank && rank <= r.MaxRank);
        return rule?.Points ?? participationPoints;
    }

    public static List<CircuitStandingResponse> Aggregate(
        IEnumerable<(Tournament Tournament, List<FinalPlacement> Placements)> completedTournaments,
        IReadOnlyCollection<CircuitPointsRule> rules,
        int participationPoints)
    {
        var byPlayer = new Dictionary<int, (string Name, List<CircuitTournamentPointsResponse> Details)>();

        foreach (var (tournament, placements) in completedTournaments)
        {
            foreach (var placement in placements)
            {
                int points = PointsForRank(placement.Rank, rules, participationPoints);

                if (!byPlayer.TryGetValue(placement.PlayerId, out var entry))
                {
                    entry = (placement.PlayerName, new List<CircuitTournamentPointsResponse>());
                    byPlayer[placement.PlayerId] = entry;
                }

                entry.Details.Add(new CircuitTournamentPointsResponse(
                    tournament.Id, tournament.Name, placement.Rank, points));
            }
        }

        var sorted = byPlayer
            .Select(kvp => new
            {
                PlayerId = kvp.Key,
                kvp.Value.Name,
                kvp.Value.Details,
                TotalPoints = kvp.Value.Details.Sum(d => d.Points)
            })
            .OrderByDescending(p => p.TotalPoints)
            .ThenByDescending(p => p.Details.Count)
            .ThenBy(p => p.Name)
            .ToList();

        // Rangs "compétition" : total identique => rang partagé
        var standings = new List<CircuitStandingResponse>();
        int currentRank = 0;
        int? previousTotal = null;

        for (int i = 0; i < sorted.Count; i++)
        {
            var p = sorted[i];
            if (previousTotal == null || p.TotalPoints != previousTotal)
                currentRank = i + 1;
            previousTotal = p.TotalPoints;

            standings.Add(new CircuitStandingResponse(
                p.PlayerId, p.Name, p.Details.Count, p.TotalPoints, currentRank, p.Details));
        }

        return standings;
    }
}
