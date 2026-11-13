namespace DartsTournament.Api.Models;

/// <summary>
/// Événement : regroupe plusieurs tableaux (tournois simple / double) joués le même
/// jour, avec une inscription en ligne commune. Chaque tableau reste un Tournament
/// autonome ; un tournoi sans événement garde le comportement historique.
/// </summary>
public class Event
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime? StartDate { get; set; }

    // Clôture des inscriptions en ligne (null = ouvertes tant que les tableaux ne sont pas lancés)
    public DateTime? RegistrationDeadline { get; set; }

    // Saison (optionnelle) appliquée aux tableaux hors circuit
    public int? SeasonId { get; set; }
    public Season? Season { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();

    public bool IsRegistrationOpen(DateTime utcNow) =>
        RegistrationDeadline == null || utcNow <= RegistrationDeadline.Value;
}
