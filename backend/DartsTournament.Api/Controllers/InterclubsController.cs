using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Championnats interclubs : clubs, effectifs, calendrier, rencontres, classement
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InterclubsController : ControllerBase
{
    private readonly InterclubService _interclubService;

    public InterclubsController(InterclubService interclubService)
    {
        _interclubService = interclubService;
    }

    /// <summary>Liste des championnats</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChampionshipResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChampionshipResponse>>> GetChampionships()
    {
        return Ok(await _interclubService.GetChampionshipsAsync());
    }

    /// <summary>Détail d'un championnat (config, clubs, effectifs)</summary>
    /// <response code="404">Championnat non trouvé</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ChampionshipDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChampionshipDetailResponse>> GetChampionship(int id)
    {
        var championship = await _interclubService.GetChampionshipAsync(id);
        if (championship == null)
        {
            return NotFound();
        }

        return Ok(championship);
    }

    /// <summary>Créer un championnat</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ChampionshipResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChampionshipResponse>> CreateChampionship(CreateChampionshipRequest request)
    {
        try
        {
            var championship = await _interclubService.CreateChampionshipAsync(request);
            return CreatedAtAction(nameof(GetChampionship), new { id = championship.Id }, championship);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Supprimer un championnat (rencontres et matchs inclus)</summary>
    /// <response code="404">Championnat non trouvé</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChampionship(int id)
    {
        var deleted = await _interclubService.DeleteChampionshipAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>Engager un club dans le championnat (avant génération du calendrier)</summary>
    [HttpPost("{id}/clubs")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AttachClub(int id, AttachClubRequest request)
    {
        try
        {
            await _interclubService.AttachClubAsync(id, request.ClubId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Retirer un club du championnat (avant génération du calendrier)</summary>
    [HttpDelete("{id}/clubs/{clubId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DetachClub(int id, int clubId)
    {
        try
        {
            await _interclubService.DetachClubAsync(id, clubId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Déclarer l'effectif d'un club pour le championnat (remplacement complet)</summary>
    [HttpPut("{id}/clubs/{clubId}/roster")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetRoster(int id, int clubId, SetRosterRequest request)
    {
        try
        {
            await _interclubService.SetRosterAsync(id, clubId, request.PlayerIds);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Générer le calendrier aller-retour (démarre le championnat)</summary>
    [HttpPost("{id}/generate-calendar")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateCalendar(int id)
    {
        try
        {
            await _interclubService.GenerateCalendarAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Calendrier du championnat groupé par journée</summary>
    /// <response code="404">Championnat non trouvé</response>
    [HttpGet("{id}/calendar")]
    [ProducesResponseType(typeof(IEnumerable<CalendarRoundResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CalendarRoundResponse>>> GetCalendar(int id)
    {
        var calendar = await _interclubService.GetCalendarAsync(id);
        if (calendar == null)
        {
            return NotFound();
        }

        return Ok(calendar);
    }

    /// <summary>Classement de la saison</summary>
    /// <response code="404">Championnat non trouvé</response>
    [HttpGet("{id}/standings")]
    [ProducesResponseType(typeof(IEnumerable<InterclubStandingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<InterclubStandingResponse>>> GetStandings(int id)
    {
        var standings = await _interclubService.GetStandingsAsync(id);
        if (standings == null)
        {
            return NotFound();
        }

        return Ok(standings);
    }

    /// <summary>Feuille de rencontre (boards, effectifs, score)</summary>
    /// <response code="404">Rencontre non trouvée</response>
    [HttpGet("encounters/{id}")]
    [ProducesResponseType(typeof(EncounterDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EncounterDetailResponse>> GetEncounter(int id)
    {
        var encounter = await _interclubService.GetEncounterAsync(id);
        if (encounter == null)
        {
            return NotFound();
        }

        return Ok(encounter);
    }

    /// <summary>Composer les matchs d'une rencontre (boards non joués uniquement)</summary>
    [HttpPut("encounters/{id}/lineup")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetLineup(int id, SetEncounterLineupRequest request)
    {
        try
        {
            await _interclubService.SetLineupAsync(id, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
