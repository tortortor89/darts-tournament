using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public class CircuitService
{
    // Barème par défaut appliqué à la création si aucun n'est fourni
    private static readonly (int MinRank, int MaxRank, int Points)[] DefaultPointsRules =
    {
        (1, 1, 100),
        (2, 2, 60),
        (3, 4, 40),
        (5, 8, 20)
    };

    private readonly AppDbContext _context;

    public CircuitService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CircuitResponse>> GetCircuitsAsync()
    {
        var circuits = await _context.Circuits
            .Include(c => c.PointsRules)
            .Include(c => c.Tournaments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return circuits.Select(ToResponse).ToList();
    }

    public async Task<CircuitDetailResponse?> GetCircuitAsync(int id)
    {
        var circuit = await _context.Circuits
            .Include(c => c.PointsRules)
            .Include(c => c.Tournaments)
            .ThenInclude(t => t.TournamentPlayers)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (circuit == null)
            return null;

        return new CircuitDetailResponse(
            circuit.Id,
            circuit.Name,
            circuit.Description,
            circuit.ParticipationPoints,
            circuit.CreatedAt,
            SortedRules(circuit),
            circuit.Tournaments
                .OrderByDescending(t => t.StartDate ?? t.CreatedAt)
                .Select(t => new TournamentResponse(
                    t.Id, t.Name, t.Format, t.Status, t.StartDate, t.CreatedAt,
                    t.TournamentPlayers.Count,
                    t.NumberOfGroups, t.PlayersPerGroup, t.QualifiersPerGroup,
                    t.HasKnockoutPhase, t.AllowBracketReset,
                    circuit.Id, circuit.Name,
                    t.TeamSize == 2))
                .ToList());
    }

    public async Task<CircuitResponse> CreateCircuitAsync(CreateCircuitRequest request)
    {
        var rules = request.PointsRules != null
            ? ValidateRules(request.PointsRules)
            : DefaultPointsRules.Select(r => new PointsRuleDto(r.MinRank, r.MaxRank, r.Points)).ToList();

        var circuit = new Circuit
        {
            Name = request.Name,
            Description = request.Description,
            ParticipationPoints = request.ParticipationPoints
        };

        foreach (var rule in rules)
        {
            circuit.PointsRules.Add(new CircuitPointsRule
            {
                MinRank = rule.MinRank,
                MaxRank = rule.MaxRank,
                Points = rule.Points
            });
        }

        _context.Circuits.Add(circuit);
        await _context.SaveChangesAsync();

        return ToResponse(circuit);
    }

    public async Task<bool> UpdateCircuitAsync(int id, UpdateCircuitRequest request)
    {
        var circuit = await _context.Circuits
            .Include(c => c.PointsRules)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (circuit == null)
            return false;

        var rules = ValidateRules(request.PointsRules);

        circuit.Name = request.Name;
        circuit.Description = request.Description;
        circuit.ParticipationPoints = request.ParticipationPoints;

        // Remplacement complet du barème
        _context.CircuitPointsRules.RemoveRange(circuit.PointsRules);
        circuit.PointsRules.Clear();
        foreach (var rule in rules)
        {
            circuit.PointsRules.Add(new CircuitPointsRule
            {
                CircuitId = circuit.Id,
                MinRank = rule.MinRank,
                MaxRank = rule.MaxRank,
                Points = rule.Points
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCircuitAsync(int id)
    {
        var circuit = await _context.Circuits.FindAsync(id);
        if (circuit == null)
            return false;

        // Les tournois sont détachés (CircuitId -> null), jamais supprimés
        _context.Circuits.Remove(circuit);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AttachTournamentAsync(int circuitId, int tournamentId)
    {
        var circuitExists = await _context.Circuits.AnyAsync(c => c.Id == circuitId);
        if (!circuitExists)
            throw new InvalidOperationException("Circuit introuvable");

        var tournament = await _context.Tournaments.FindAsync(tournamentId);
        if (tournament == null)
            throw new InvalidOperationException("Tournoi introuvable");

        if (tournament.CircuitId != null && tournament.CircuitId != circuitId)
            throw new InvalidOperationException("Ce tournoi est déjà rattaché à un autre circuit");

        tournament.CircuitId = circuitId;
        await _context.SaveChangesAsync();
    }

    public async Task DetachTournamentAsync(int circuitId, int tournamentId)
    {
        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId && t.CircuitId == circuitId);

        if (tournament == null)
            throw new InvalidOperationException("Ce tournoi n'est pas rattaché à ce circuit");

        tournament.CircuitId = null;
        await _context.SaveChangesAsync();
    }

    public async Task<List<CircuitStandingResponse>?> GetRankingAsync(int circuitId)
    {
        var circuit = await _context.Circuits
            .Include(c => c.PointsRules)
            .FirstOrDefaultAsync(c => c.Id == circuitId);

        if (circuit == null)
            return null;

        // Seuls les tournois terminés rapportent des points
        var completedTournaments = await _context.Tournaments
            .Where(t => t.CircuitId == circuitId && t.Status == TournamentStatus.Completed)
            .Include(t => t.Groups)
            .Include(t => t.Matches)
            .Include(t => t.TournamentPlayers)
            .ThenInclude(tp => tp.Player)
            .Include(t => t.Teams)
            .ThenInclude(tt => tt.Player1)
            .Include(t => t.Teams)
            .ThenInclude(tt => tt.Player2)
            .ToListAsync();

        var placements = completedTournaments
            .Select(t => (Tournament: t, Placements: FinalPlacementCalculator.Compute(t)));

        return CircuitRankingCalculator.Aggregate(
            placements, circuit.PointsRules.ToList(), circuit.ParticipationPoints);
    }

    // ----- Helpers -----

    private static CircuitResponse ToResponse(Circuit circuit) => new(
        circuit.Id,
        circuit.Name,
        circuit.Description,
        circuit.ParticipationPoints,
        circuit.CreatedAt,
        circuit.Tournaments.Count,
        circuit.Tournaments.Count(t => t.Status == TournamentStatus.Completed),
        SortedRules(circuit));

    private static List<PointsRuleDto> SortedRules(Circuit circuit) =>
        circuit.PointsRules
            .OrderBy(r => r.MinRank)
            .Select(r => new PointsRuleDto(r.MinRank, r.MaxRank, r.Points))
            .ToList();

    private static List<PointsRuleDto> ValidateRules(List<PointsRuleDto> rules)
    {
        var sorted = rules.OrderBy(r => r.MinRank).ToList();

        foreach (var rule in sorted)
        {
            if (rule.MaxRank < rule.MinRank)
                throw new InvalidOperationException(
                    $"Barème invalide : le rang maximum ({rule.MaxRank}) doit être supérieur ou égal au rang minimum ({rule.MinRank})");
        }

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].MinRank <= sorted[i - 1].MaxRank)
                throw new InvalidOperationException(
                    $"Barème invalide : les plages de rangs {sorted[i - 1].MinRank}-{sorted[i - 1].MaxRank} et {sorted[i].MinRank}-{sorted[i].MaxRank} se chevauchent");
        }

        return sorted;
    }
}
