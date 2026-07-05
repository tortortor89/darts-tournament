using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.Models;
using DartsTournament.Api.DTOs;

namespace DartsTournament.Api.Services;

public class PlayerService
{
    private readonly AppDbContext _context;

    public PlayerService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a new player linked to a user
    /// </summary>
    public async Task<Player> CreatePlayerForUserAsync(int userId, string firstName, string lastName, string? nickname)
    {
        // Business rule: User cannot have multiple players
        var existingPlayer = await _context.Players
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (existingPlayer != null)
        {
            throw new InvalidOperationException("L'utilisateur a déjà un profil joueur lié");
        }

        var player = new Player
        {
            FirstName = firstName,
            LastName = lastName,
            Nickname = nickname,
            UserId = userId
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        return player;
    }

    /// <summary>
    /// Link an existing player to a user
    /// </summary>
    public async Task<Player> LinkPlayerToUserAsync(int userId, int playerId, bool isAdmin)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
        {
            throw new InvalidOperationException("Joueur non trouvé");
        }

        // Business rule: Player already linked to another user
        if (player.UserId != null)
        {
            if (!isAdmin)
            {
                throw new InvalidOperationException("Ce joueur est déjà lié à un autre utilisateur");
            }
            // Admin can override existing link
        }

        // Business rule: User already has a player
        var userHasPlayer = await _context.Players
            .AnyAsync(p => p.UserId == userId && p.Id != playerId);

        if (userHasPlayer)
        {
            throw new InvalidOperationException("L'utilisateur a déjà un profil joueur lié");
        }

        player.UserId = userId;
        await _context.SaveChangesAsync();

        return player;
    }

    /// <summary>
    /// Unlink a player from its user
    /// </summary>
    public async Task UnlinkPlayerAsync(int playerId, int requestingUserId, bool isAdmin)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
        {
            throw new InvalidOperationException("Joueur non trouvé");
        }

        // Business rule: Only admin or the linked user can unlink
        if (!isAdmin && player.UserId != requestingUserId)
        {
            throw new InvalidOperationException("Non autorisé à délier ce joueur");
        }

        player.UserId = null;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Get player detail with linked user info
    /// </summary>
    public async Task<PlayerDetailResponse?> GetPlayerDetailAsync(int playerId)
    {
        var player = await _context.Players
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == playerId);

        if (player == null)
        {
            return null;
        }

        return new PlayerDetailResponse(
            player.Id,
            player.FirstName,
            player.LastName,
            player.Nickname,
            player.CreatedAt,
            player.UserId,
            player.User?.Username
        );
    }

    /// <summary>
    /// Get all unlinked players (available for linking)
    /// </summary>
    public async Task<List<PlayerResponse>> GetAvailablePlayersAsync()
    {
        var players = await _context.Players
            .Where(p => p.UserId == null)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PlayerResponse(
                p.Id,
                p.FirstName,
                p.LastName,
                p.Nickname,
                p.CreatedAt,
                p.ClubId
            ))
            .ToListAsync();

        return players;
    }

    /// <summary>
    /// Get the player linked to a specific user
    /// </summary>
    public async Task<Player?> GetLinkedPlayerForUserAsync(int userId)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    /// <summary>
    /// Update own player profile (only for the linked user)
    /// </summary>
    public async Task<Player> UpdateOwnPlayerAsync(int userId, string firstName, string lastName, string? nickname)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (player == null)
        {
            throw new InvalidOperationException("Aucun profil joueur lié à cet utilisateur");
        }

        player.FirstName = firstName;
        player.LastName = lastName;
        player.Nickname = nickname;

        await _context.SaveChangesAsync();

        return player;
    }
}
