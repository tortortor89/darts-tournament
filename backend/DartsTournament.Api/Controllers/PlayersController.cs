using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Gestion des joueurs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlayersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Récupérer la liste de tous les joueurs
    /// </summary>
    /// <returns>Liste des joueurs triés par nom</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlayerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlayerResponse>>> GetPlayers()
    {
        var players = await _context.Players
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PlayerResponse(p.Id, p.FirstName, p.LastName, p.Nickname, p.CreatedAt))
            .ToListAsync();

        return Ok(players);
    }

    /// <summary>
    /// Récupérer un joueur par son identifiant
    /// </summary>
    /// <param name="id">Identifiant du joueur</param>
    /// <returns>Informations du joueur</returns>
    /// <response code="200">Joueur trouvé</response>
    /// <response code="404">Joueur non trouvé</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerResponse>> GetPlayer(int id)
    {
        var player = await _context.Players.FindAsync(id);

        if (player == null)
        {
            return NotFound();
        }

        return Ok(new PlayerResponse(player.Id, player.FirstName, player.LastName, player.Nickname, player.CreatedAt));
    }

    /// <summary>
    /// Créer un nouveau joueur
    /// </summary>
    /// <param name="request">Informations du joueur</param>
    /// <returns>Le joueur créé</returns>
    /// <response code="201">Joueur créé avec succès</response>
    /// <response code="400">Données invalides</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Accès refusé (rôle Admin requis)</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PlayerResponse>> CreatePlayer(CreatePlayerRequest request)
    {
        var player = new Player
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Nickname = request.Nickname
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        var response = new PlayerResponse(player.Id, player.FirstName, player.LastName, player.Nickname, player.CreatedAt);
        return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, response);
    }

    /// <summary>
    /// Modifier un joueur existant
    /// </summary>
    /// <param name="id">Identifiant du joueur</param>
    /// <param name="request">Nouvelles informations</param>
    /// <response code="204">Joueur modifié avec succès</response>
    /// <response code="404">Joueur non trouvé</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlayer(int id, UpdatePlayerRequest request)
    {
        var player = await _context.Players.FindAsync(id);

        if (player == null)
        {
            return NotFound();
        }

        player.FirstName = request.FirstName;
        player.LastName = request.LastName;
        player.Nickname = request.Nickname;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Supprimer un joueur
    /// </summary>
    /// <param name="id">Identifiant du joueur</param>
    /// <response code="204">Joueur supprimé avec succès</response>
    /// <response code="404">Joueur non trouvé</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        var player = await _context.Players.FindAsync(id);

        if (player == null)
        {
            return NotFound();
        }

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
