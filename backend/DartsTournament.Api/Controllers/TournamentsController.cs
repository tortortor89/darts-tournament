using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TournamentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TournamentService _tournamentService;

    public TournamentsController(AppDbContext context, TournamentService tournamentService)
    {
        _context = context;
        _tournamentService = tournamentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TournamentResponse>>> GetTournaments()
    {
        var tournaments = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TournamentResponse(
                t.Id,
                t.Name,
                t.Format,
                t.Status,
                t.StartDate,
                t.CreatedAt,
                t.TournamentPlayers.Count,
                t.NumberOfGroups,
                t.PlayersPerGroup,
                t.QualifiersPerGroup,
                t.HasKnockoutPhase,
                t.AllowBracketReset
            ))
            .ToListAsync();

        return Ok(tournaments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TournamentDetailResponse>> GetTournament(int id)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
            .ThenInclude(tp => tp.Player)
            .Include(t => t.Groups)
            .ThenInclude(g => g.Players)
            .ThenInclude(tp => tp.Player)
            .Include(t => t.Matches)
            .ThenInclude(m => m.Player1)
            .Include(t => t.Matches)
            .ThenInclude(m => m.Player2)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tournament == null)
        {
            return NotFound();
        }

        var response = new TournamentDetailResponse(
            tournament.Id,
            tournament.Name,
            tournament.Format,
            tournament.Status,
            tournament.StartDate,
            tournament.CreatedAt,
            tournament.NumberOfGroups,
            tournament.PlayersPerGroup,
            tournament.QualifiersPerGroup,
            tournament.HasKnockoutPhase,
            tournament.AllowBracketReset,
            tournament.TournamentPlayers.Select(tp => new TournamentPlayerResponse(
                tp.PlayerId,
                tp.Player.FirstName,
                tp.Player.LastName,
                tp.Player.Nickname,
                tp.Seed,
                tp.GroupId
            )).ToList(),
            tournament.Groups.Select(g => new GroupResponse(
                g.Id,
                g.Name,
                g.Players.Select(tp => new TournamentPlayerResponse(
                    tp.PlayerId,
                    tp.Player.FirstName,
                    tp.Player.LastName,
                    tp.Player.Nickname,
                    tp.Seed,
                    tp.GroupId
                )).ToList()
            )).ToList(),
            tournament.Matches.OrderBy(m => m.Round).ThenBy(m => m.Position).Select(m => new MatchResponse(
                m.Id,
                m.TournamentId,
                m.GroupId,
                m.Round,
                m.Position,
                m.Player1Id,
                m.Player1 != null ? $"{m.Player1.FirstName} {m.Player1.LastName}" : null,
                m.Player2Id,
                m.Player2 != null ? $"{m.Player2.FirstName} {m.Player2.LastName}" : null,
                m.Player1Score,
                m.Player2Score,
                m.WinnerId,
                m.Status,
                m.ScheduledAt,
                m.IsKnockoutMatch,
                m.BracketType,
                m.IsBracketReset
            )).ToList()
        );

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TournamentResponse>> CreateTournament(CreateTournamentRequest request)
    {
        var tournament = new Tournament
        {
            Name = request.Name,
            Format = request.Format,
            StartDate = request.StartDate,
            NumberOfGroups = request.NumberOfGroups,
            PlayersPerGroup = request.PlayersPerGroup,
            QualifiersPerGroup = request.QualifiersPerGroup,
            HasKnockoutPhase = request.HasKnockoutPhase,
            AllowBracketReset = request.AllowBracketReset
        };

        _context.Tournaments.Add(tournament);
        await _context.SaveChangesAsync();

        var response = new TournamentResponse(
            tournament.Id,
            tournament.Name,
            tournament.Format,
            tournament.Status,
            tournament.StartDate,
            tournament.CreatedAt,
            0,
            tournament.NumberOfGroups,
            tournament.PlayersPerGroup,
            tournament.QualifiersPerGroup,
            tournament.HasKnockoutPhase,
            tournament.AllowBracketReset
        );

        return CreatedAtAction(nameof(GetTournament), new { id = tournament.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTournament(int id, UpdateTournamentRequest request)
    {
        var tournament = await _context.Tournaments.FindAsync(id);

        if (tournament == null)
        {
            return NotFound();
        }

        if (tournament.Status != TournamentStatus.Draft)
        {
            return BadRequest("Cannot update a tournament that has already started");
        }

        tournament.Name = request.Name;
        tournament.StartDate = request.StartDate;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTournament(int id)
    {
        var tournament = await _context.Tournaments.FindAsync(id);

        if (tournament == null)
        {
            return NotFound();
        }

        _context.Tournaments.Remove(tournament);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/players")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddPlayer(int id, AddPlayerToTournamentRequest request)
    {
        var tournament = await _context.Tournaments.FindAsync(id);

        if (tournament == null)
        {
            return NotFound("Tournament not found");
        }

        if (tournament.Status != TournamentStatus.Draft)
        {
            return BadRequest("Cannot add players to a tournament that has already started");
        }

        var player = await _context.Players.FindAsync(request.PlayerId);

        if (player == null)
        {
            return NotFound("Player not found");
        }

        var exists = await _context.TournamentPlayers
            .AnyAsync(tp => tp.TournamentId == id && tp.PlayerId == request.PlayerId);

        if (exists)
        {
            return BadRequest("Player is already in this tournament");
        }

        var tournamentPlayer = new TournamentPlayer
        {
            TournamentId = id,
            PlayerId = request.PlayerId,
            Seed = request.Seed
        };

        _context.TournamentPlayers.Add(tournamentPlayer);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}/players/{playerId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemovePlayer(int id, int playerId)
    {
        var tournament = await _context.Tournaments.FindAsync(id);

        if (tournament == null)
        {
            return NotFound("Tournament not found");
        }

        if (tournament.Status != TournamentStatus.Draft)
        {
            return BadRequest("Cannot remove players from a tournament that has already started");
        }

        var tournamentPlayer = await _context.TournamentPlayers
            .FirstOrDefaultAsync(tp => tp.TournamentId == id && tp.PlayerId == playerId);

        if (tournamentPlayer == null)
        {
            return NotFound("Player is not in this tournament");
        }

        _context.TournamentPlayers.Remove(tournamentPlayer);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/generate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateBracket(int id)
    {
        try
        {
            await _tournamentService.GenerateBracketAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/standings")]
    public async Task<ActionResult<List<GroupStandingResponse>>> GetStandings(int id)
    {
        try
        {
            var standings = await _tournamentService.GetStandingsAsync(id);
            return Ok(standings);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
