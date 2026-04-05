using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
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

    public MatchesController(AppDbContext context, TournamentService tournamentService)
    {
        _context = context;
        _tournamentService = tournamentService;
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
}
