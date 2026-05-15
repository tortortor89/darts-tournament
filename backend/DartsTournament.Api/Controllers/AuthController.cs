using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

/// <summary>
/// Gestion de l'authentification des utilisateurs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Créer un nouveau compte utilisateur
    /// </summary>
    /// <param name="request">Informations d'inscription</param>
    /// <returns>Token JWT et informations utilisateur</returns>
    /// <response code="200">Inscription réussie</response>
    /// <response code="400">Nom d'utilisateur déjà existant ou données invalides</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request.Username, request.Password);

        if (user == null)
        {
            return BadRequest("Username already exists");
        }

        var loginResult = await _authService.LoginAsync(request.Username, request.Password);
        var (token, _, linkedPlayer) = loginResult!.Value;

        return Ok(new AuthResponse(
            token,
            user.Username,
            user.Role.ToString(),
            linkedPlayer?.Id,
            linkedPlayer != null ? $"{linkedPlayer.FirstName} {linkedPlayer.LastName}" : null
        ));
    }

    /// <summary>
    /// Connexion d'un utilisateur existant
    /// </summary>
    /// <param name="request">Identifiants de connexion</param>
    /// <returns>Token JWT et informations utilisateur</returns>
    /// <response code="200">Connexion réussie</response>
    /// <response code="401">Identifiants invalides</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var loginResult = await _authService.LoginAsync(request.Username, request.Password);

        if (loginResult == null)
        {
            return Unauthorized("Invalid username or password");
        }

        var (token, user, linkedPlayer) = loginResult.Value;
        return Ok(new AuthResponse(
            token,
            user.Username,
            user.Role.ToString(),
            linkedPlayer?.Id,
            linkedPlayer != null ? $"{linkedPlayer.FirstName} {linkedPlayer.LastName}" : null
        ));
    }

    /// <summary>
    /// Changer le mot de passe de l'utilisateur connecté
    /// </summary>
    /// <param name="request">Mot de passe actuel et nouveau mot de passe</param>
    /// <returns>Confirmation du changement</returns>
    /// <response code="200">Mot de passe changé avec succès</response>
    /// <response code="400">Mot de passe actuel incorrect ou données invalides</response>
    /// <response code="401">Non authentifié</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

        if (!success)
        {
            return BadRequest(new { message = "Le mot de passe actuel est incorrect" });
        }

        return Ok(new { message = "Mot de passe changé avec succès" });
    }
}
