using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DartsTournament.Api.Configuration;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Événements multi-tableaux (simple / double) et inscription en ligne groupée
/// </summary>
[ApiController]
[RequiresFeature(Feature.Events)]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly EventService _eventService;
    private readonly PlayerService _playerService;

    public EventsController(EventService eventService, PlayerService playerService)
    {
        _eventService = eventService;
        _playerService = playerService;
    }

    /// <summary>
    /// Récupérer la liste des événements
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetEvents()
    {
        return Ok(await _eventService.GetEventsAsync());
    }

    /// <summary>
    /// Récupérer un événement avec ses tableaux
    /// </summary>
    /// <remarks>
    /// Connecté avec un profil joueur : ses inscriptions. Admin : les inscriptions à valider.
    /// </remarks>
    /// <param name="id">Identifiant de l'événement</param>
    /// <response code="200">Événement trouvé</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> GetEvent(int id)
    {
        var player = await GetCurrentPlayerAsync();
        var evt = await _eventService.GetEventAsync(id, player?.Id, User.IsInRole("Admin"));
        if (evt == null)
        {
            return NotFound();
        }

        return Ok(evt);
    }

    /// <summary>
    /// Créer un événement
    /// </summary>
    /// <response code="201">Événement créé</response>
    /// <response code="400">Données invalides (saison clôturée...)</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest request)
    {
        try
        {
            var evt = await _eventService.CreateEventAsync(request);
            return CreatedAtAction(nameof(GetEvent), new { id = evt.Id }, evt);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Modifier un événement
    /// </summary>
    /// <remarks>Un changement de saison s'applique aux tableaux hors circuit.</remarks>
    /// <response code="204">Événement modifié</response>
    /// <response code="400">Données invalides</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, UpdateEventRequest request)
    {
        try
        {
            if (!await _eventService.UpdateEventAsync(id, request))
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
    /// Supprimer un événement
    /// </summary>
    /// <remarks>Les tableaux sont détachés, pas supprimés.</remarks>
    /// <response code="204">Événement supprimé</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (!await _eventService.DeleteEventAsync(id))
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// S'inscrire à un ou plusieurs tableaux de l'événement (tout ou rien)
    /// </summary>
    /// <remarks>
    /// Simple : sans partenaire. Double : avec le partenaire choisi (pas besoin de compte).
    /// Les inscriptions partent en attente de validation par l'admin.
    /// </remarks>
    /// <response code="200">Inscriptions envoyées</response>
    /// <response code="400">Inscriptions closes, tableau lancé, déjà inscrit, partenaire invalide</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpPost("{id}/register")]
    [RequiresFeature(Feature.OnlineRegistration)]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Register(int id, EventRegistrationRequest request)
    {
        var player = await GetCurrentPlayerAsync();
        if (player == null)
        {
            return BadRequest(new { message = "Vous devez avoir un profil joueur pour vous inscrire" });
        }

        try
        {
            await _eventService.RegisterAsync(id, player.Id, request);
            return Ok(new { message = "Inscription envoyée. En attente de validation par l'administrateur." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<Models.Player?> GetCurrentPlayerAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null && int.TryParse(userIdClaim, out int userId)
            ? await _playerService.GetLinkedPlayerForUserAsync(userId)
            : null;
    }
}
