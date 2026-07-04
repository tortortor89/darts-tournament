using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Gestion des matchs et des scores
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TournamentService _tournamentService;
    private readonly MatchSessionService _matchSessionService;
    private readonly MatchStatsService _statsService;

    public MatchesController(
        AppDbContext context,
        TournamentService tournamentService,
        MatchSessionService matchSessionService,
        MatchStatsService statsService)
    {
        _context = context;
        _tournamentService = tournamentService;
        _matchSessionService = matchSessionService;
        _statsService = statsService;
    }

    /// <summary>
    /// Récupérer les détails d'un match
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <returns>Informations complètes du match</returns>
    /// <response code="200">Match trouvé</response>
    /// <response code="404">Match non trouvé</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchResponse>> GetMatch(int id)
    {
        var match = await _context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Team1)
            .ThenInclude(tt => tt!.Player1)
            .Include(m => m.Team1)
            .ThenInclude(tt => tt!.Player2)
            .Include(m => m.Team2)
            .ThenInclude(tt => tt!.Player1)
            .Include(m => m.Team2)
            .ThenInclude(tt => tt!.Player2)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
        {
            return NotFound();
        }

        bool isDoubles = match.Tournament.TeamSize == 2;

        // En double : ids/noms de côté aliasés sur les champs joueur (cf. MatchResponse)
        var response = new MatchResponse(
            match.Id,
            match.TournamentId,
            match.GroupId,
            match.Round,
            match.Position,
            isDoubles ? match.Team1Id : match.Player1Id,
            isDoubles
                ? match.Team1?.Name
                : (match.Player1 != null ? $"{match.Player1.FirstName} {match.Player1.LastName}" : null),
            isDoubles ? match.Team2Id : match.Player2Id,
            isDoubles
                ? match.Team2?.Name
                : (match.Player2 != null ? $"{match.Player2.FirstName} {match.Player2.LastName}" : null),
            match.Player1Score,
            match.Player2Score,
            isDoubles ? match.WinnerTeamId : match.WinnerId,
            match.Status,
            match.ScheduledAt,
            match.IsKnockoutMatch,
            match.BracketType,
            match.IsBracketReset,
            isDoubles,
            isDoubles && match.Team1 != null
                ? new List<TeamMemberInfo>
                {
                    new(match.Team1.Player1Id, $"{match.Team1.Player1.FirstName} {match.Team1.Player1.LastName}"),
                    new(match.Team1.Player2Id, $"{match.Team1.Player2.FirstName} {match.Team1.Player2.LastName}")
                }
                : null,
            isDoubles && match.Team2 != null
                ? new List<TeamMemberInfo>
                {
                    new(match.Team2.Player1Id, $"{match.Team2.Player1.FirstName} {match.Team2.Player1.LastName}"),
                    new(match.Team2.Player2Id, $"{match.Team2.Player2.FirstName} {match.Team2.Player2.LastName}")
                }
                : null
        );

        return Ok(response);
    }

    /// <summary>
    /// Enregistrer le score d'un match
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <param name="request">Scores des deux joueurs</param>
    /// <remarks>
    /// Le vainqueur est déterminé automatiquement en fonction des scores.
    /// Pour les formats à élimination, le vainqueur est automatiquement avancé au tour suivant.
    /// </remarks>
    /// <response code="200">Score enregistré avec succès</response>
    /// <response code="400">Match non trouvé ou déjà terminé</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Accès refusé (rôle Admin requis)</response>
    [HttpPut("{id}/score")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateScore(int id, UpdateMatchScoreRequest request)
    {
        try
        {
            await _tournamentService.UpdateMatchScoreAsync(id, request.Player1Score, request.Player2Score);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #region Match Session (Live Game)

    /// <summary>
    /// Récupère toutes les sessions en cours (usage : écran TV de bar)
    /// </summary>
    /// <returns>Liste des matchs actifs avec scores et noms</returns>
    [HttpGet("active-sessions")]
    [ProducesResponseType(typeof(List<ActiveSessionSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ActiveSessionSummaryResponse>>> GetActiveSessions()
    {
        var sessions = await _matchSessionService.GetActiveSessionsAsync();
        return Ok(sessions);
    }

    /// <summary>
    /// Récupérer la session en cours d'un match
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <returns>État de la session ou null si aucune session active</returns>
    /// <response code="200">Session trouvée</response>
    /// <response code="404">Match non trouvé ou pas de session active</response>
    [HttpGet("{id}/session")]
    [ProducesResponseType(typeof(MatchSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchSessionResponse>> GetSession(int id)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        return Ok(_matchSessionService.BuildSessionResponse(session));
    }

    /// <summary>
    /// Démarrer une nouvelle session de match
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <param name="request">Configuration de la partie (legs à gagner, joueur qui commence)</param>
    /// <returns>Session créée</returns>
    /// <response code="201">Session créée</response>
    /// <response code="400">Configuration invalide ou match non disponible</response>
    [HttpPost("{id}/session/start")]
    [ProducesResponseType(typeof(MatchSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MatchSessionResponse>> StartSession(int id, StartMatchSessionRequest request)
    {
        try
        {
            var session = await _matchSessionService.StartSessionAsync(id, request);
            return CreatedAtAction(
                nameof(GetSession),
                new { id = session.MatchId },
                _matchSessionService.BuildSessionResponse(session)
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Enregistrer une volée (3 fléchettes)
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <param name="request">Score de la volée et optionnellement le détail des fléchettes</param>
    /// <returns>État mis à jour de la session</returns>
    /// <response code="200">Volée enregistrée</response>
    /// <response code="400">Score invalide ou session non en cours</response>
    /// <response code="404">Session non trouvée</response>
    [HttpPost("{id}/session/throw")]
    [ProducesResponseType(typeof(MatchSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchSessionResponse>> RecordThrow(int id, RecordThrowRequest request)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        try
        {
            var updatedSession = await _matchSessionService.RecordThrowAsync(session.Id, request);
            return Ok(_matchSessionService.BuildSessionResponse(updatedSession));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Enregistrer une visite complète Cricket (turn)
    /// </summary>
    [HttpPost("{id}/session/cricket-turn")]
    [ProducesResponseType(typeof(CricketTurnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CricketTurnResponse>> RecordCricketTurn(int id, [FromBody] RecordCricketTurnRequest request)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        try
        {
            var response = await _matchSessionService.RecordCricketTurnAsync(session.Id, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Annuler la dernière volée enregistrée (correction d'une erreur de saisie)
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <returns>État mis à jour de la session</returns>
    /// <response code="200">Volée annulée</response>
    /// <response code="400">Aucune volée à annuler ou match déjà validé</response>
    /// <response code="404">Session non trouvée</response>
    [HttpDelete("{id}/session/throws/last")]
    [ProducesResponseType(typeof(MatchSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchSessionResponse>> UndoLastThrow(int id)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        try
        {
            var updatedSession = await _matchSessionService.UndoLastThrowAsync(session.Id);
            return Ok(_matchSessionService.BuildSessionResponse(updatedSession));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Valider et clôturer le match (met à jour le score du tournoi)
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <response code="200">Match validé et score du tournoi mis à jour</response>
    /// <response code="400">Session non terminée</response>
    /// <response code="404">Session non trouvée</response>
    [HttpPost("{id}/session/validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidateSession(int id)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        try
        {
            await _matchSessionService.ValidateMatchAsync(session.Id);
            return Ok(new { message = "Match validé et score enregistré" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Annuler la session en cours
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <response code="200">Session annulée</response>
    /// <response code="400">Session déjà terminée</response>
    /// <response code="404">Session non trouvée</response>
    [HttpDelete("{id}/session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSession(int id)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        try
        {
            await _matchSessionService.CancelSessionAsync(session.Id);
            return Ok(new { message = "Session annulée" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Vue spectateur - État du match en lecture seule
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <returns>État simplifié pour affichage public</returns>
    /// <response code="200">Données spectateur</response>
    /// <response code="404">Session non trouvée</response>
    [HttpGet("{id}/spectate")]
    [ProducesResponseType(typeof(MatchSessionSpectatorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchSessionSpectatorResponse>> GetSpectatorView(int id)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        return Ok(_matchSessionService.BuildSpectatorResponse(session));
    }

    /// <summary>
    /// Récupérer les statistiques en temps réel du match
    /// </summary>
    /// <param name="id">Identifiant du match</param>
    /// <returns>Statistiques des deux joueurs</returns>
    /// <response code="200">Statistiques calculées</response>
    /// <response code="404">Session non trouvée</response>
    [HttpGet("{id}/session/stats")]
    [ProducesResponseType(typeof(MatchStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchStatsResponse>> GetMatchStats(int id)
    {
        var session = await _matchSessionService.GetOrCreateSessionAsync(id);

        if (session == null)
        {
            return NotFound("Aucune session active pour ce match");
        }

        var stats = _statsService.CalculateStats(session);
        return Ok(stats);
    }

    #endregion
}
