using System.ComponentModel.DataAnnotations;

namespace DartsTournament.Api.DTOs;

public record RegisterRequest(
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 50 caractères")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Le nom d'utilisateur ne peut contenir que des lettres, chiffres, tirets et underscores")]
    string Username,

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    string Password
);

public record LoginRequest(
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    string Username,

    [Required(ErrorMessage = "Le mot de passe est requis")]
    string Password
);

public record AuthResponse(string Token, string Username, string Role);
