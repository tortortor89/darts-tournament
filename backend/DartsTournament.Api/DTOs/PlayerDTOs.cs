using System.ComponentModel.DataAnnotations;

namespace DartsTournament.Api.DTOs;

public record CreatePlayerRequest(
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Le prénom doit contenir entre 1 et 50 caractères")]
    string FirstName,

    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Le nom doit contenir entre 1 et 50 caractères")]
    string LastName,

    [StringLength(50, ErrorMessage = "Le surnom ne peut pas dépasser 50 caractères")]
    string? Nickname
);

public record UpdatePlayerRequest(
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Le prénom doit contenir entre 1 et 50 caractères")]
    string FirstName,

    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Le nom doit contenir entre 1 et 50 caractères")]
    string LastName,

    [StringLength(50, ErrorMessage = "Le surnom ne peut pas dépasser 50 caractères")]
    string? Nickname
);

public record PlayerResponse(int Id, string FirstName, string LastName, string? Nickname, DateTime CreatedAt);
