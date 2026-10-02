using System.ComponentModel.DataAnnotations.Schema;

namespace DartsTournament.Api.Models;

/// <summary>
/// Paire de joueurs, rattachée soit à un tournoi en double (TeamSize = 2), soit à
/// une rencontre interclubs (matchs de double d'une soirée) — exactement un des
/// deux (check constraint en base). Pas d'équipe persistante entre compétitions.
/// </summary>
public class TournamentTeam
{
    public int Id { get; set; }

    public int? TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    public int? EncounterId { get; set; }
    public InterclubEncounter? Encounter { get; set; }

    public int Player1Id { get; set; }
    public Player Player1 { get; set; } = null!;

    public int Player2Id { get; set; }
    public Player Player2 { get; set; } = null!;

    public int? Seed { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    // Label d'affichage de la paire
    [NotMapped]
    public string Name =>
        $"{Player1.FirstName} {Player1.LastName} / {Player2.FirstName} {Player2.LastName}";
}
