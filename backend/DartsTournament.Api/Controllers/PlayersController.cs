using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlayersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerResponse>>> GetPlayers()
    {
        var players = await _context.Players
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PlayerResponse(p.Id, p.FirstName, p.LastName, p.Nickname, p.CreatedAt))
            .ToListAsync();

        return Ok(players);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PlayerResponse>> GetPlayer(int id)
    {
        var player = await _context.Players.FindAsync(id);

        if (player == null)
        {
            return NotFound();
        }

        return Ok(new PlayerResponse(player.Id, player.FirstName, player.LastName, player.Nickname, player.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
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

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
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
