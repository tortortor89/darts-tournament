using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Gestion des circuits de tournois (regroupement de tournois avec classement à points)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CircuitsController : ControllerBase
{
    private readonly CircuitService _circuitService;

    public CircuitsController(CircuitService circuitService)
    {
        _circuitService = circuitService;
    }

    /// <summary>
    /// Récupérer la liste de tous les circuits
    /// </summary>
    /// <returns>Liste des circuits triés par date de création décroissante</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CircuitResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CircuitResponse>>> GetCircuits()
    {
        return Ok(await _circuitService.GetCircuitsAsync());
    }

    /// <summary>
    /// Récupérer les détails d'un circuit (barème et tournois rattachés)
    /// </summary>
    /// <param name="id">Identifiant du circuit</param>
    /// <response code="200">Circuit trouvé</response>
    /// <response code="404">Circuit non trouvé</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CircuitDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CircuitDetailResponse>> GetCircuit(int id)
    {
        var circuit = await _circuitService.GetCircuitAsync(id);
        if (circuit == null)
        {
            return NotFound();
        }

        return Ok(circuit);
    }

    /// <summary>
    /// Récupérer le classement général du circuit
    /// </summary>
    /// <remarks>Seuls les tournois terminés rapportent des points.</remarks>
    /// <param name="id">Identifiant du circuit</param>
    /// <response code="200">Classement calculé</response>
    /// <response code="404">Circuit non trouvé</response>
    [HttpGet("{id}/ranking")]
    [ProducesResponseType(typeof(IEnumerable<CircuitStandingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CircuitStandingResponse>>> GetRanking(int id)
    {
        var ranking = await _circuitService.GetRankingAsync(id);
        if (ranking == null)
        {
            return NotFound();
        }

        return Ok(ranking);
    }

    /// <summary>
    /// Créer un nouveau circuit
    /// </summary>
    /// <remarks>Sans barème fourni, le barème par défaut est appliqué (1er=100, 2e=60, 3e-4e=40, 5e-8e=20, participation=10).</remarks>
    /// <param name="request">Paramètres du circuit</param>
    /// <response code="201">Circuit créé avec succès</response>
    /// <response code="400">Données invalides (barème incohérent)</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Accès refusé (rôle Admin requis)</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CircuitResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CircuitResponse>> CreateCircuit(CreateCircuitRequest request)
    {
        try
        {
            var circuit = await _circuitService.CreateCircuitAsync(request);
            return CreatedAtAction(nameof(GetCircuit), new { id = circuit.Id }, circuit);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Modifier un circuit (nom, description, barème)
    /// </summary>
    /// <remarks>Le barème fourni remplace intégralement l'ancien.</remarks>
    /// <param name="id">Identifiant du circuit</param>
    /// <param name="request">Nouvelles valeurs</param>
    /// <response code="204">Circuit modifié</response>
    /// <response code="400">Données invalides (barème incohérent)</response>
    /// <response code="404">Circuit non trouvé</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCircuit(int id, UpdateCircuitRequest request)
    {
        try
        {
            var updated = await _circuitService.UpdateCircuitAsync(id, request);
            if (!updated)
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
    /// Supprimer un circuit
    /// </summary>
    /// <remarks>Les tournois rattachés sont détachés du circuit mais ne sont pas supprimés.</remarks>
    /// <param name="id">Identifiant du circuit</param>
    /// <response code="204">Circuit supprimé</response>
    /// <response code="404">Circuit non trouvé</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCircuit(int id)
    {
        var deleted = await _circuitService.DeleteCircuitAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Rattacher un tournoi existant au circuit
    /// </summary>
    /// <param name="id">Identifiant du circuit</param>
    /// <param name="request">Identifiant du tournoi à rattacher</param>
    /// <response code="204">Tournoi rattaché</response>
    /// <response code="400">Tournoi introuvable ou déjà rattaché à un autre circuit</response>
    [HttpPost("{id}/tournaments")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AttachTournament(int id, AttachTournamentRequest request)
    {
        try
        {
            await _circuitService.AttachTournamentAsync(id, request.TournamentId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Détacher un tournoi du circuit
    /// </summary>
    /// <param name="id">Identifiant du circuit</param>
    /// <param name="tournamentId">Identifiant du tournoi à détacher</param>
    /// <response code="204">Tournoi détaché</response>
    /// <response code="400">Le tournoi n'est pas rattaché à ce circuit</response>
    [HttpDelete("{id}/tournaments/{tournamentId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DetachTournament(int id, int tournamentId)
    {
        try
        {
            await _circuitService.DetachTournamentAsync(id, tournamentId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
