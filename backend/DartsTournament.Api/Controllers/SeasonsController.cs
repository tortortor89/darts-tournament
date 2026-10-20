using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DartsTournament.Api.Configuration;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Gestion des saisons (mode Ligue : tournois, circuits et championnats en 501 double out)
/// </summary>
[ApiController]
[RequiresFeature(Feature.Seasons)]
[Route("api/[controller]")]
[Produces("application/json")]
public class SeasonsController : ControllerBase
{
    private readonly SeasonService _seasonService;

    public SeasonsController(SeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    /// <summary>
    /// Récupérer la liste des saisons
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SeasonResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SeasonResponse>>> GetSeasons()
    {
        return Ok(await _seasonService.GetSeasonsAsync());
    }

    /// <summary>
    /// Récupérer une saison avec ses tournois, circuits et championnats
    /// </summary>
    /// <param name="id">Identifiant de la saison</param>
    /// <response code="200">Saison trouvée</response>
    /// <response code="404">Saison non trouvée</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SeasonDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeasonDetailResponse>> GetSeason(int id)
    {
        var season = await _seasonService.GetSeasonAsync(id);
        if (season == null)
        {
            return NotFound();
        }

        return Ok(season);
    }

    /// <summary>
    /// Créer une saison
    /// </summary>
    /// <response code="201">Saison créée</response>
    /// <response code="400">Données invalides</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SeasonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SeasonResponse>> CreateSeason(CreateSeasonRequest request)
    {
        try
        {
            var season = await _seasonService.CreateSeasonAsync(request);
            return CreatedAtAction(nameof(GetSeason), new { id = season.Id }, season);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Modifier une saison (nom, dates, legs, statut en cours / clôturée)
    /// </summary>
    /// <response code="204">Saison modifiée</response>
    /// <response code="400">Données invalides</response>
    /// <response code="404">Saison non trouvée</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeason(int id, UpdateSeasonRequest request)
    {
        try
        {
            if (!await _seasonService.UpdateSeasonAsync(id, request))
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Supprimer une saison
    /// </summary>
    /// <remarks>Les tournois, circuits et championnats rattachés sont détachés, pas supprimés.</remarks>
    /// <response code="204">Saison supprimée</response>
    /// <response code="404">Saison non trouvée</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSeason(int id)
    {
        if (!await _seasonService.DeleteSeasonAsync(id))
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Rattacher un tournoi (hors circuit) à la saison
    /// </summary>
    [HttpPost("{id}/tournaments")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AttachTournament(int id, AttachToSeasonRequest request) =>
        Run(() => _seasonService.AttachTournamentAsync(id, request.Id));

    /// <summary>
    /// Détacher un tournoi de la saison
    /// </summary>
    [HttpDelete("{id}/tournaments/{tournamentId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> DetachTournament(int id, int tournamentId) =>
        Run(() => _seasonService.DetachTournamentAsync(id, tournamentId));

    /// <summary>
    /// Rattacher un circuit (et donc ses tournois) à la saison
    /// </summary>
    [HttpPost("{id}/circuits")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AttachCircuit(int id, AttachToSeasonRequest request) =>
        Run(() => _seasonService.AttachCircuitAsync(id, request.Id));

    /// <summary>
    /// Détacher un circuit de la saison
    /// </summary>
    [HttpDelete("{id}/circuits/{circuitId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> DetachCircuit(int id, int circuitId) =>
        Run(() => _seasonService.DetachCircuitAsync(id, circuitId));

    /// <summary>
    /// Rattacher un championnat interclubs à la saison (passe le championnat en 501 double out)
    /// </summary>
    [HttpPost("{id}/championships")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AttachChampionship(int id, AttachToSeasonRequest request) =>
        Run(() => _seasonService.AttachChampionshipAsync(id, request.Id));

    /// <summary>
    /// Détacher un championnat de la saison
    /// </summary>
    [HttpDelete("{id}/championships/{championshipId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> DetachChampionship(int id, int championshipId) =>
        Run(() => _seasonService.DetachChampionshipAsync(id, championshipId));

    private async Task<IActionResult> Run(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
