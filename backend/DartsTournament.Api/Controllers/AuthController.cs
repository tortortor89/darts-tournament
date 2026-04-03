using Microsoft.AspNetCore.Mvc;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Services;

namespace DartsTournament.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(request.Username, request.Password);

        if (user == null)
        {
            return BadRequest("Username already exists");
        }

        var loginResult = await _authService.LoginAsync(request.Username, request.Password);
        return Ok(new AuthResponse(loginResult!.Value.Token, user.Username, user.Role.ToString()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var loginResult = await _authService.LoginAsync(request.Username, request.Password);

        if (loginResult == null)
        {
            return Unauthorized("Invalid username or password");
        }

        var (token, user) = loginResult.Value;
        return Ok(new AuthResponse(token, user.Username, user.Role.ToString()));
    }
}
