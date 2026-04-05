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

    public MatchesController(AppDbContext context, TournamentService tournamentService, MatchSessionService matchSessionService)
    {
        _context = context;
        _tournamentService = tournamentService;
        _matchSessionService = matchSessionService;
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
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null)
        {
            return NotFound();
        }

        var response = new MatchResponse(
            match.Id,
            match.TournamentId,
            match.GroupId,
            match.Round,
            match.Position,
            match.Player1Id,
            match.Player1 != null ? $"{match.Player1.FirstName} {match.Player1.LastName}" : null,
            match.Player2Id,
            match.Player2 != null ? $"{match.Player2.FirstName} {match.Player2.LastName}" : null,
            match.Player1Score,
            match.Player2Score,
            match.WinnerId,
            match.Status,
            match.ScheduledAt,
            match.IsKnockoutMatch,
            match.BracketType,
            match.IsBracketReset
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

    #endregion
}
