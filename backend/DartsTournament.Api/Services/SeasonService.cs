using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DartsTournament.Api.Configuration;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

/// <summary>
/// Saisons (mode Ligue) : rattachement des tournois, circuits et championnats
/// et règlement commun (501 double out).
/// </summary>
public class SeasonService
{
    private readonly AppDbContext _context;
    private readonly IOptionsMonitor<FeatureOptions> _features;

    public SeasonService(AppDbContext context, IOptionsMonitor<FeatureOptions> features)
    {
        _context = context;
        _features = features;
    }

    public async Task<List<SeasonResponse>> GetSeasonsAsync()
    {
        var seasons = await _context.Seasons
            .Include(s => s.Tournaments)
            .Include(s => s.Circuits)
            .Include(s => s.Championships)
            .OrderByDescending(s => s.StartDate ?? s.CreatedAt)
            .ToListAsync();

        return seasons.Select(ToResponse).ToList();
    }

    public async Task<SeasonDetailResponse?> GetSeasonAsync(int id)
    {
        var season = await _context.Seasons
            .Include(s => s.Tournaments)
            .ThenInclude(t => t.TournamentPlayers)
            .Include(s => s.Circuits)
            .ThenInclude(c => c.Tournaments)
            .Include(s => s.Circuits)
            .ThenInclude(c => c.PointsRules)
            .Include(s => s.Championships)
            .ThenInclude(c => c.Clubs)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (season == null)
            return null;

        return new SeasonDetailResponse(
            season.Id,
            season.Name,
            season.StartDate,
            season.EndDate,
            season.Status,
            season.LegsToWin,
            season.CreatedAt,
            season.Tournaments
                .OrderByDescending(t => t.StartDate ?? t.CreatedAt)
                .Select(t => new TournamentResponse(
                    t.Id, t.Name, t.Format, t.Status, t.StartDate, t.CreatedAt,
                    t.TournamentPlayers.Count,
                    t.NumberOfGroups, t.PlayersPerGroup, t.QualifiersPerGroup,
                    t.HasKnockoutPhase, t.AllowBracketReset,
                    null, null,
                    t.TeamSize == 2,
                    season.Id, season.Name,
                    t.UseFsgtFormat))
                .ToList(),
            season.Circuits
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CircuitResponse(
                    c.Id, c.Name, c.Description, c.ParticipationPoints, c.CreatedAt,
                    c.Tournaments.Count,
                    c.Tournaments.Count(t => t.Status == TournamentStatus.Completed),
                    c.PointsRules
                        .OrderBy(r => r.MinRank)
                        .Select(r => new PointsRuleDto(r.MinRank, r.MaxRank, r.Points))
                        .ToList(),
                    season.Id, season.Name,
                    c.UseFsgtPoints))
                .ToList(),
            season.Championships
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ChampionshipResponse(
                    c.Id, c.Name, c.Status,
                    c.SinglesPerEncounter, c.DoublesPerEncounter, c.LegsToWin, c.GameMode, c.DoubleOut,
                    c.PointsForWin, c.PointsForDraw, c.PointsForLoss,
                    c.CreatedAt, c.Clubs.Count,
                    season.Id, season.Name))
                .ToList());
    }

    public async Task<SeasonResponse> CreateSeasonAsync(CreateSeasonRequest request)
    {
        ValidateDates(request.StartDate, request.EndDate);

        var season = new Season
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            LegsToWin = request.LegsToWin
        };

        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        return ToResponse(season);
    }

    public async Task<bool> UpdateSeasonAsync(int id, UpdateSeasonRequest request)
    {
        var season = await _context.Seasons.FindAsync(id);
        if (season == null)
            return false;

        ValidateDates(request.StartDate, request.EndDate);

        season.Name = request.Name;
        season.StartDate = request.StartDate;
        season.EndDate = request.EndDate;
        season.LegsToWin = request.LegsToWin;
        season.Status = request.Status;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSeasonAsync(int id)
    {
        var season = await _context.Seasons.FindAsync(id);
        if (season == null)
            return false;

        // Tournois, circuits et championnats sont détachés (SeasonId -> null), jamais supprimés
        _context.Seasons.Remove(season);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Vérifie qu'une saison existe et accepte encore des rattachements.
    /// </summary>
    public async Task<Season> GetOpenSeasonAsync(int seasonId)
    {
        if (!_features.CurrentValue.Seasons)
            throw new InvalidOperationException("Les saisons ne sont pas activées");

        var season = await _context.Seasons.FindAsync(seasonId)
            ?? throw new InvalidOperationException("Saison introuvable");

        if (season.Status == SeasonStatus.Closed)
            throw new InvalidOperationException("Cette saison est clôturée");

        return season;
    }

    // ----- Rattachements -----

    public async Task AttachTournamentAsync(int seasonId, int tournamentId)
    {
        await GetOpenSeasonAsync(seasonId);

        var tournament = await _context.Tournaments.FindAsync(tournamentId)
            ?? throw new InvalidOperationException("Tournoi introuvable");

        if (tournament.CircuitId != null)
            throw new InvalidOperationException("Ce tournoi fait partie d'un circuit : rattachez le circuit à la saison");
        if (tournament.SeasonId != null && tournament.SeasonId != seasonId)
            throw new InvalidOperationException("Ce tournoi est déjà rattaché à une autre saison");

        tournament.SeasonId = seasonId;
        await _context.SaveChangesAsync();
    }

    public async Task DetachTournamentAsync(int seasonId, int tournamentId)
    {
        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId && t.SeasonId == seasonId)
            ?? throw new InvalidOperationException("Ce tournoi n'est pas rattaché à cette saison");

        tournament.SeasonId = null;
        await _context.SaveChangesAsync();
    }

    public async Task AttachCircuitAsync(int seasonId, int circuitId)
    {
        await GetOpenSeasonAsync(seasonId);

        var circuit = await _context.Circuits.FindAsync(circuitId)
            ?? throw new InvalidOperationException("Circuit introuvable");

        if (circuit.SeasonId != null && circuit.SeasonId != seasonId)
            throw new InvalidOperationException("Ce circuit est déjà rattaché à une autre saison");

        circuit.SeasonId = seasonId;
        await _context.SaveChangesAsync();
    }

    public async Task DetachCircuitAsync(int seasonId, int circuitId)
    {
        var circuit = await _context.Circuits
            .FirstOrDefaultAsync(c => c.Id == circuitId && c.SeasonId == seasonId)
            ?? throw new InvalidOperationException("Ce circuit n'est pas rattaché à cette saison");

        circuit.SeasonId = null;
        await _context.SaveChangesAsync();
    }

    public async Task AttachChampionshipAsync(int seasonId, int championshipId)
    {
        await GetOpenSeasonAsync(seasonId);

        var championship = await _context.InterclubChampionships.FindAsync(championshipId)
            ?? throw new InvalidOperationException("Championnat introuvable");

        if (championship.SeasonId != null && championship.SeasonId != seasonId)
            throw new InvalidOperationException("Ce championnat est déjà rattaché à une autre saison");

        // Le règlement de la saison s'impose au championnat, sans changer de règles en cours de route
        bool rulesDiffer = championship.GameMode != GameMode.FiveOhOne || !championship.DoubleOut;
        if (rulesDiffer && championship.Status != ChampionshipStatus.Draft)
            throw new InvalidOperationException("Ce championnat a commencé avec un autre mode de jeu que le 501 double out");

        championship.SeasonId = seasonId;
        championship.GameMode = GameMode.FiveOhOne;
        championship.DoubleOut = true;
        await _context.SaveChangesAsync();
    }

    public async Task DetachChampionshipAsync(int seasonId, int championshipId)
    {
        var championship = await _context.InterclubChampionships
            .FirstOrDefaultAsync(c => c.Id == championshipId && c.SeasonId == seasonId)
            ?? throw new InvalidOperationException("Ce championnat n'est pas rattaché à cette saison");

        championship.SeasonId = null;
        await _context.SaveChangesAsync();
    }

    // ----- Règlement -----

    /// <summary>
    /// Saison applicable à un match : celle du tournoi (propre ou héritée du circuit)
    /// ou celle du championnat de la rencontre. Null hors saison.
    /// </summary>
    public async Task<Season?> GetSeasonForMatchAsync(int matchId)
    {
        var seasonId = await _context.Matches
            .Where(m => m.Id == matchId)
            .Select(m => m.TournamentId != null
                ? m.Tournament!.SeasonId ?? (m.Tournament.Circuit != null ? m.Tournament.Circuit.SeasonId : null)
                : m.Encounter!.Championship.SeasonId)
            .FirstOrDefaultAsync();

        return seasonId == null ? null : await _context.Seasons.FindAsync(seasonId.Value);
    }

    // ----- Helpers -----

    private static void ValidateDates(DateTime? startDate, DateTime? endDate)
    {
        if (startDate != null && endDate != null && endDate < startDate)
            throw new InvalidOperationException("La date de fin doit être postérieure à la date de début");
    }

    private static SeasonResponse ToResponse(Season season) => new(
        season.Id,
        season.Name,
        season.StartDate,
        season.EndDate,
        season.Status,
        season.LegsToWin,
        season.CreatedAt,
        season.Tournaments.Count,
        season.Circuits.Count,
        season.Championships.Count);
}
