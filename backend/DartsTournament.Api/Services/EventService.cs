using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

/// <summary>
/// Événements multi-tableaux et inscription en ligne groupée (simple + double avec
/// partenaire). Les inscriptions en ligne partent en attente de validation par l'admin ;
/// l'ajout direct par l'admin (joueurs, paires) reste possible sur chaque tableau.
/// </summary>
public class EventService
{
    private readonly AppDbContext _context;
    private readonly SeasonService _seasonService;

    public EventService(AppDbContext context, SeasonService seasonService)
    {
        _context = context;
        _seasonService = seasonService;
    }

    public async Task<List<EventResponse>> GetEventsAsync()
    {
        var events = await _context.Events
            .Include(e => e.Season)
            .Include(e => e.Tournaments)
            .OrderByDescending(e => e.StartDate ?? e.CreatedAt)
            .ToListAsync();

        var now = DateTime.UtcNow;
        return events.Select(e => ToResponse(e, now)).ToList();
    }

    public async Task<EventDetailResponse?> GetEventAsync(int id, int? playerId, bool isAdmin)
    {
        var evt = await _context.Events
            .Include(e => e.Season)
            .Include(e => e.Tournaments)
            .ThenInclude(t => t.TournamentPlayers)
            .ThenInclude(tp => tp.Player)
            .Include(e => e.Tournaments)
            .ThenInclude(t => t.Teams)
            .ThenInclude(tt => tt.Player1)
            .Include(e => e.Tournaments)
            .ThenInclude(t => t.Teams)
            .ThenInclude(tt => tt.Player2)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null)
            return null;

        var tableaux = evt.Tournaments
            .OrderBy(t => t.TeamSize)
            .ThenBy(t => t.Name)
            .ToList();

        var tableauResponses = tableaux.Select(t => t.TeamSize == 2
            ? new EventTableauResponse(t.Id, t.Name, t.Format, t.Status, true,
                t.Teams.Count(tt => tt.Status == RegistrationStatus.Approved),
                t.Teams.Count(tt => tt.Status == RegistrationStatus.Pending))
            : new EventTableauResponse(t.Id, t.Name, t.Format, t.Status, false,
                t.TournamentPlayers.Count(tp => tp.Status == RegistrationStatus.Approved),
                t.TournamentPlayers.Count(tp => tp.Status == RegistrationStatus.Pending)))
            .ToList();

        var myRegistrations = playerId != null
            ? tableaux.SelectMany(t => RegistrationsOf(t, playerId.Value)).ToList()
            : null;

        List<PendingRegistrationResponse>? pending = null;
        if (isAdmin)
        {
            pending = tableaux
                .Where(t => t.Status == TournamentStatus.Draft)
                .SelectMany(t => t.TeamSize == 2
                    ? t.Teams.Where(tt => tt.Status == RegistrationStatus.Pending)
                        .Select(tt => new PendingRegistrationResponse(t.Id, t.Name, true, tt.Id, tt.Name))
                    : t.TournamentPlayers.Where(tp => tp.Status == RegistrationStatus.Pending)
                        .Select(tp => new PendingRegistrationResponse(t.Id, t.Name, false, tp.PlayerId,
                            $"{tp.Player.FirstName} {tp.Player.LastName}")))
                .ToList();
        }

        return new EventDetailResponse(
            evt.Id, evt.Name, evt.Description, evt.Location, evt.StartDate, evt.RegistrationDeadline,
            evt.IsRegistrationOpen(DateTime.UtcNow),
            evt.SeasonId, evt.Season?.Name, evt.CreatedAt,
            tableauResponses, myRegistrations, pending);
    }

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request)
    {
        var season = request.SeasonId != null
            ? await _seasonService.GetOpenSeasonAsync(request.SeasonId.Value)
            : null;

        var evt = new Event
        {
            Name = request.Name,
            Description = request.Description,
            Location = request.Location,
            StartDate = ToUtc(request.StartDate),
            RegistrationDeadline = ToUtc(request.RegistrationDeadline),
            Season = season
        };

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        return ToResponse(evt, DateTime.UtcNow);
    }

    public async Task<bool> UpdateEventAsync(int id, UpdateEventRequest request)
    {
        var evt = await _context.Events
            .Include(e => e.Tournaments)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null)
            return false;

        if (request.SeasonId != evt.SeasonId)
        {
            if (request.SeasonId != null)
                await _seasonService.GetOpenSeasonAsync(request.SeasonId.Value);

            // La saison de l'événement s'applique à ses tableaux hors circuit
            // (un tableau de circuit suit la saison du circuit)
            foreach (var tournament in evt.Tournaments.Where(t => t.CircuitId == null))
                tournament.SeasonId = request.SeasonId;
        }

        evt.Name = request.Name;
        evt.Description = request.Description;
        evt.Location = request.Location;
        evt.StartDate = ToUtc(request.StartDate);
        evt.RegistrationDeadline = ToUtc(request.RegistrationDeadline);
        evt.SeasonId = request.SeasonId;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>Supprime l'événement ; ses tableaux sont détachés, pas supprimés.</summary>
    public async Task<bool> DeleteEventAsync(int id)
    {
        var evt = await _context.Events.FindAsync(id);
        if (evt == null)
            return false;

        _context.Events.Remove(evt);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Inscription groupée d'un joueur à plusieurs tableaux de l'événement, tout ou rien :
    /// simple = joueur en attente, double = paire (joueur + partenaire choisi) en attente.
    /// </summary>
    public async Task RegisterAsync(int eventId, int playerId, EventRegistrationRequest request)
    {
        var evt = await _context.Events
            .Include(e => e.Tournaments)
            .ThenInclude(t => t.TournamentPlayers)
            .Include(e => e.Tournaments)
            .ThenInclude(t => t.Teams)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new KeyNotFoundException("Événement non trouvé");

        if (!evt.IsRegistrationOpen(DateTime.UtcNow))
            throw new InvalidOperationException("Les inscriptions de cet événement sont closes");

        if (request.Entries.Select(e => e.TournamentId).Distinct().Count() != request.Entries.Count)
            throw new InvalidOperationException("Un même tableau apparaît plusieurs fois");

        foreach (var entry in request.Entries)
        {
            var tournament = evt.Tournaments.FirstOrDefault(t => t.Id == entry.TournamentId)
                ?? throw new InvalidOperationException("Ce tableau ne fait pas partie de l'événement");

            if (tournament.Status != TournamentStatus.Draft)
                throw new InvalidOperationException($"Le tableau « {tournament.Name} » a déjà commencé");

            if (tournament.TeamSize == 2)
                await AddPendingTeamAsync(tournament, playerId, entry.PartnerId);
            else
                AddPendingPlayer(tournament, playerId, entry.PartnerId);
        }

        // Une seule sauvegarde : rien n'est enregistré si une ligne est refusée
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Inscriptions du joueur aux tournois pas encore terminés (avec ou sans événement),
    /// pour la section « Mes inscriptions » du profil.
    /// </summary>
    public async Task<List<MyRegistrationResponse>> GetPlayerRegistrationsAsync(int playerId)
    {
        var tournaments = await _context.Tournaments
            .Where(t => t.Status != TournamentStatus.Completed
                && (t.TournamentPlayers.Any(tp => tp.PlayerId == playerId)
                    || t.Teams.Any(tt => tt.Player1Id == playerId || tt.Player2Id == playerId)))
            .Include(t => t.Event)
            .Include(t => t.TournamentPlayers)
            .Include(t => t.Teams)
            .ThenInclude(tt => tt.Player1)
            .Include(t => t.Teams)
            .ThenInclude(tt => tt.Player2)
            .ToListAsync();

        return tournaments
            .SelectMany(t => RegistrationsOf(t, playerId))
            .OrderBy(r => r.StartDate ?? DateTime.MaxValue)
            .ThenBy(r => r.TournamentName)
            .ToList();
    }

    // ----- Helpers -----

    private void AddPendingPlayer(Tournament tournament, int playerId, int? partnerId)
    {
        if (partnerId != null)
            throw new InvalidOperationException($"Le tableau « {tournament.Name} » se joue en simple : pas de partenaire");

        if (tournament.TournamentPlayers.Any(tp => tp.PlayerId == playerId))
            throw new InvalidOperationException($"Vous êtes déjà inscrit au tableau « {tournament.Name} »");

        _context.TournamentPlayers.Add(new TournamentPlayer
        {
            TournamentId = tournament.Id,
            PlayerId = playerId,
            Status = RegistrationStatus.Pending
        });
    }

    private async Task AddPendingTeamAsync(Tournament tournament, int playerId, int? partnerId)
    {
        if (partnerId == null)
            throw new InvalidOperationException($"Choisissez un partenaire pour le tableau « {tournament.Name} »");

        if (partnerId == playerId)
            throw new InvalidOperationException("Vous ne pouvez pas être votre propre partenaire");

        var partner = await _context.Players.FindAsync(partnerId.Value)
            ?? throw new InvalidOperationException("Partenaire introuvable");

        bool InTeam(int id) => tournament.Teams.Any(tt => tt.Player1Id == id || tt.Player2Id == id);

        if (InTeam(playerId))
            throw new InvalidOperationException($"Vous êtes déjà inscrit en paire au tableau « {tournament.Name} »");

        if (InTeam(partner.Id))
            throw new InvalidOperationException(
                $"{partner.FirstName} {partner.LastName} est déjà inscrit en paire au tableau « {tournament.Name} »");

        _context.TournamentTeams.Add(new TournamentTeam
        {
            TournamentId = tournament.Id,
            Player1Id = playerId,
            Player2Id = partner.Id,
            Status = RegistrationStatus.Pending
        });
    }

    // Inscription(s) du joueur à un tournoi (Teams.Player1/Player2 chargés)
    private static IEnumerable<MyRegistrationResponse> RegistrationsOf(Tournament t, int playerId)
    {
        bool canUnregister = t.Status == TournamentStatus.Draft;

        if (t.TeamSize == 2)
        {
            foreach (var team in t.Teams.Where(tt => tt.Player1Id == playerId || tt.Player2Id == playerId))
            {
                var partner = team.Player1Id == playerId ? team.Player2 : team.Player1;
                yield return new MyRegistrationResponse(
                    t.Id, t.Name, t.Status, t.StartDate, t.EventId, t.Event?.Name, true,
                    partner.Id, $"{partner.FirstName} {partner.LastName}", team.Status, canUnregister);
            }
            yield break;
        }

        var registration = t.TournamentPlayers.FirstOrDefault(tp => tp.PlayerId == playerId);
        if (registration != null)
        {
            yield return new MyRegistrationResponse(
                t.Id, t.Name, t.Status, t.StartDate, t.EventId, t.Event?.Name, false,
                null, null, registration.Status, canUnregister);
        }
    }

    // Npgsql refuse les DateTime « Unspecified » en timestamptz (date seule « 2026-12-01 »)
    private static DateTime? ToUtc(DateTime? date) => date switch
    {
        null => null,
        { Kind: DateTimeKind.Unspecified } d => DateTime.SpecifyKind(d, DateTimeKind.Utc),
        var d => d.Value.ToUniversalTime()
    };

    private static EventResponse ToResponse(Event e, DateTime now) => new(
        e.Id, e.Name, e.Description, e.Location, e.StartDate, e.RegistrationDeadline,
        e.IsRegistrationOpen(now), e.SeasonId, e.Season?.Name, e.Tournaments.Count, e.CreatedAt);
}
