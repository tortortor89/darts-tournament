using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TournamentService _tournamentService;

    public MatchesController(AppDbContext context, TournamentService tournamentService)
    {
        _context = context;
        _tournamentService = tournamentService;
    }

    [HttpGet("{id}")]
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
            match.IsKnockoutMatch
        );

        return Ok(response);
    }

    [HttpPut("{id}/score")]
    [Authorize]
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
