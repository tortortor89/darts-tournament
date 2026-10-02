using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Gestion des clubs (interclubs)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClubsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClubsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Récupérer la liste des clubs
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClubResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClubResponse>>> GetClubs()
    {
        var clubs = await _context.Clubs
            .Include(c => c.Players)
            .OrderBy(c => c.Name)
            .Select(c => new ClubResponse(c.Id, c.Name, c.CreatedAt, c.Players.Count))
            .ToListAsync();

        return Ok(clubs);
    }

    /// <summary>
    /// Récupérer un club avec ses joueurs
    /// </summary>
    /// <response code="404">Club non trouvé</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClubDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClubDetailResponse>> GetClub(int id)
    {
        var club = await _context.Clubs
            .Include(c => c.Players)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (club == null)
        {
            return NotFound();
        }

        return Ok(new ClubDetailResponse(
            club.Id,
            club.Name,
            club.CreatedAt,
            club.Players
                .OrderBy(p => p.LastName)
                .Select(p => new ClubPlayerResponse(p.Id, $"{p.FirstName} {p.LastName}", p.Nickname))
                .ToList()
        ));
    }

    /// <summary>
    /// Créer un club
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ClubResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClubResponse>> CreateClub(CreateClubRequest request)
    {
        var club = new Club { Name = request.Name };
        _context.Clubs.Add(club);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClub), new { id = club.Id },
            new ClubResponse(club.Id, club.Name, club.CreatedAt, 0));
    }

    /// <summary>
    /// Renommer un club
    /// </summary>
    /// <response code="404">Club non trouvé</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClub(int id, CreateClubRequest request)
    {
        var club = await _context.Clubs.FindAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        club.Name = request.Name;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Supprimer un club
    /// </summary>
    /// <response code="400">Club engagé dans un championnat</response>
    /// <response code="404">Club non trouvé</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClub(int id)
    {
        var club = await _context.Clubs.FindAsync(id);
        if (club == null)
        {
            return NotFound();
        }

        var engaged = await _context.ChampionshipClubs.AnyAsync(cc => cc.ClubId == id);
        if (engaged)
        {
            return BadRequest(new { message = "Impossible de supprimer un club engagé dans un championnat" });
        }

        _context.Clubs.Remove(club);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Rattacher un joueur au club
    /// </summary>
    /// <response code="400">Joueur déjà membre d'un autre club</response>
    /// <response code="404">Club ou joueur non trouvé</response>
    [HttpPost("{id}/players")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPlayer(int id, AssignPlayerToClubRequest request)
    {
        var club = await _context.Clubs.FindAsync(id);
        if (club == null)
        {
            return NotFound("Club not found");
        }

        var player = await _context.Players.FindAsync(request.PlayerId);
        if (player == null)
        {
            return NotFound("Player not found");
        }

        if (player.ClubId != null && player.ClubId != id)
        {
            return BadRequest(new { message = "Ce joueur est déjà membre d'un autre club" });
        }

        player.ClubId = id;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Détacher un joueur du club
    /// </summary>
    /// <response code="404">Le joueur n'est pas membre de ce club</response>
    [HttpDelete("{id}/players/{playerId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePlayer(int id, int playerId)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId && p.ClubId == id);

        if (player == null)
        {
            return NotFound("Le joueur n'est pas membre de ce club");
        }

        player.ClubId = null;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
