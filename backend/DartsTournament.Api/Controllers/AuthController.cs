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

        var token = await _authService.LoginAsync(request.Username, request.Password);
        return Ok(new AuthResponse(token!, user.Username));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var token = await _authService.LoginAsync(request.Username, request.Password);

        if (token == null)
        {
            return Unauthorized("Invalid username or password");
        }

        return Ok(new AuthResponse(token, request.Username));
    }
}
