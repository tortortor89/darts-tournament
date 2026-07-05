using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;
using DartsTournament.Api.Services;

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
    private readonly PlayerService _playerService;
    private readonly PlayerStatsService _playerStatsService;

    public PlayersController(AppDbContext context, PlayerService playerService, PlayerStatsService playerStatsService)
    {
        _context = context;
        _playerService = playerService;
        _playerStatsService = playerStatsService;
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
            .Select(p => new PlayerResponse(p.Id, p.FirstName, p.LastName, p.Nickname, p.CreatedAt, p.ClubId))
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

    // ===== User-Player Linking Endpoints =====

    /// <summary>
    /// Récupérer la liste des joueurs disponibles (non liés) pour liaison
    /// </summary>
    /// <returns>Liste des joueurs non liés</returns>
    [HttpGet("available")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PlayerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlayerResponse>>> GetAvailablePlayers()
    {
        var players = await _playerService.GetAvailablePlayersAsync();
        return Ok(players);
    }

    /// <summary>
    /// Récupérer les détails d'un joueur avec information de l'utilisateur lié
    /// </summary>
    /// <param name="id">Identifiant du joueur</param>
    /// <returns>Détails du joueur avec utilisateur lié</returns>
    [HttpGet("{id}/detail")]
    [ProducesResponseType(typeof(PlayerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDetailResponse>> GetPlayerDetail(int id)
    {
        var player = await _playerService.GetPlayerDetailAsync(id);

        if (player == null)
        {
            return NotFound();
        }

        return Ok(player);
    }

    /// <summary>
    /// Créer son propre profil joueur (lié au compte utilisateur)
    /// </summary>
    /// <param name="request">Informations du joueur</param>
    /// <returns>Le profil joueur créé</returns>
    [HttpPost("create-own")]
    [Authorize]
    [ProducesResponseType(typeof(PlayerDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlayerDetailResponse>> CreateOwnPlayer(CreateOwnPlayerRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        try
        {
            var player = await _playerService.CreatePlayerForUserAsync(
                userId,
                request.FirstName,
                request.LastName,
                request.Nickname
            );

            var response = await _playerService.GetPlayerDetailAsync(player.Id);
            return CreatedAtAction(nameof(GetPlayerDetail), new { id = player.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Modifier son propre profil joueur
    /// </summary>
    /// <param name="request">Nouvelles informations du profil</param>
    /// <returns>Le profil joueur mis à jour</returns>
    [HttpPut("update-own")]
    [Authorize]
    [ProducesResponseType(typeof(PlayerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDetailResponse>> UpdateOwnPlayer(UpdatePlayerRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        try
        {
            var player = await _playerService.UpdateOwnPlayerAsync(
                userId,
                request.FirstName,
                request.LastName,
                request.Nickname
            );

            var response = await _playerService.GetPlayerDetailAsync(player.Id);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lier son compte à un profil joueur existant
    /// </summary>
    /// <param name="request">ID du joueur à lier</param>
    /// <returns>Confirmation de la liaison</returns>
    [HttpPost("link")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkToPlayer(LinkPlayerRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");

        try
        {
            await _playerService.LinkPlayerToUserAsync(userId, request.PlayerId, isAdmin);
            return Ok(new { message = "Profil joueur lié avec succès" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Délier un profil joueur de son utilisateur
    /// </summary>
    /// <param name="id">ID du joueur à délier</param>
    /// <returns>Confirmation du déliage</returns>
    [HttpDelete("{id}/unlink")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlinkPlayer(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");

        try
        {
            await _playerService.UnlinkPlayerAsync(id, userId, isAdmin);
            return Ok(new { message = "Profil joueur délié avec succès" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Admin] Lier un joueur à un utilisateur spécifique
    /// </summary>
    /// <param name="playerId">ID du joueur</param>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Confirmation de la liaison</returns>
    [HttpPost("{playerId}/link-user/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminLinkPlayerToUser(int playerId, int userId)
    {
        try
        {
            await _playerService.LinkPlayerToUserAsync(userId, playerId, isAdmin: true);
            return Ok(new { message = "Liaison effectuée avec succès" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer les statistiques complètes d'un joueur
    /// </summary>
    /// <param name="id">Identifiant du joueur</param>
    /// <returns>Statistiques de carrière complètes</returns>
    /// <response code="200">Statistiques récupérées</response>
    /// <response code="404">Joueur non trouvé</response>
    [HttpGet("{id}/stats")]
    [ProducesResponseType(typeof(PlayerCareerStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerCareerStatsResponse>> GetPlayerStats(int id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player == null)
        {
            return NotFound(new { message = "Joueur non trouvé" });
        }

        try
        {
            var stats = await _playerStatsService.GetCareerStatsAsync(id);
            return Ok(stats);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer l'historique des tournois d'un joueur
    /// </summary>
    /// <param name="id">Identifiant du joueur</param>
    /// <returns>Liste des tournois avec résultats</returns>
    /// <response code="200">Historique récupéré</response>
    /// <response code="404">Joueur non trouvé</response>
    [HttpGet("{id}/tournament-history")]
    [ProducesResponseType(typeof(List<PlayerTournamentHistoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PlayerTournamentHistoryItem>>> GetPlayerTournamentHistory(int id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player == null)
        {
            return NotFound(new { message = "Joueur non trouvé" });
        }

        var history = await _playerStatsService.GetTournamentHistoryAsync(id);
        return Ok(history);
    }

    /// <summary>
    /// Récupérer les confrontations directes d'un joueur contre ses adversaires
    /// </summary>
    /// <param name="id">Identifiant du joueur</param>
    /// <returns>Liste des confrontations directes</returns>
    /// <response code="200">Confrontations récupérées</response>
    /// <response code="404">Joueur non trouvé</response>
    [HttpGet("{id}/head-to-head")]
    [ProducesResponseType(typeof(List<HeadToHeadRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<HeadToHeadRecord>>> GetPlayerHeadToHead(int id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player == null)
        {
            return NotFound(new { message = "Joueur non trouvé" });
        }

        var h2h = await _playerStatsService.GetHeadToHeadStatsAsync(id);
        return Ok(h2h);
    }
}
