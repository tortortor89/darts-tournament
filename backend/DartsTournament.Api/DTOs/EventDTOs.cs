using System.ComponentModel.DataAnnotations;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.DTOs;

public record CreateEventRequest(
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    string Name,

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    string? Description = null,

    [StringLength(200, ErrorMessage = "Le lieu ne peut pas dépasser 200 caractères")]
    string? Location = null,

    DateTime? StartDate = null,

    // Clôture des inscriptions en ligne (null = ouvertes tant que les tableaux ne sont pas lancés)
    DateTime? RegistrationDeadline = null,

    int? SeasonId = null
);

public record UpdateEventRequest(
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
    string Name,

    [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
    string? Description,

    [StringLength(200, ErrorMessage = "Le lieu ne peut pas dépasser 200 caractères")]
    string? Location,

    DateTime? StartDate,
    DateTime? RegistrationDeadline,
    int? SeasonId
);

public record EventResponse(
    int Id,
    string Name,
    string? Description,
    string? Location,
    DateTime? StartDate,
    DateTime? RegistrationDeadline,
    bool RegistrationOpen,
    int? SeasonId,
    string? SeasonName,
    int TableauCount,
    DateTime CreatedAt
);

/// <summary>Tableau d'un événement (un tournoi simple ou double).</summary>
public record EventTableauResponse(
    int TournamentId,
    string Name,
    TournamentFormat Format,
    TournamentStatus Status,
    bool IsDoubles,
    int ApprovedCount,
    int PendingCount
);

/// <summary>Inscription en attente de validation (joueur en simple, paire en double).</summary>
public record PendingRegistrationResponse(
    int TournamentId,
    string TournamentName,
    bool IsDoubles,
    // Simple : id du joueur ; double : id de la paire
    int SideId,
    string Label
);

public record EventDetailResponse(
    int Id,
    string Name,
    string? Description,
    string? Location,
    DateTime? StartDate,
    DateTime? RegistrationDeadline,
    bool RegistrationOpen,
    int? SeasonId,
    string? SeasonName,
    DateTime CreatedAt,
    List<EventTableauResponse> Tableaux,
    // Inscriptions du joueur connecté (null si anonyme ou sans profil joueur)
    List<MyRegistrationResponse>? MyRegistrations,
    // Inscriptions à valider (admin uniquement)
    List<PendingRegistrationResponse>? PendingRegistrations
);

/// <summary>Une ligne de l'inscription groupée : simple (sans partenaire) ou double (avec).</summary>
public record EventRegistrationEntry(
    [Range(1, int.MaxValue, ErrorMessage = "Identifiant de tableau invalide")]
    int TournamentId,

    int? PartnerId = null
);

public record EventRegistrationRequest(
    [Required(ErrorMessage = "Choisissez au moins un tableau")]
    [MinLength(1, ErrorMessage = "Choisissez au moins un tableau")]
    List<EventRegistrationEntry> Entries
);

/// <summary>Inscription d'un joueur à un tournoi (profil « Mes inscriptions »).</summary>
public record MyRegistrationResponse(
    int TournamentId,
    string TournamentName,
    TournamentStatus TournamentStatus,
    DateTime? StartDate,
    int? EventId,
    string? EventName,
    bool IsDoubles,
    int? PartnerId,
    string? PartnerName,
    RegistrationStatus Status,
    // Désinscription possible (tableau pas encore lancé)
    bool CanUnregister
);
