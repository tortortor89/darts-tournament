using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

/// <summary>
/// Un « côté » de match : un joueur en simple, une paire en double.
/// L'Id référence Player.Id (simple) ou TournamentTeam.Id (double).
/// </summary>
public record Side(int Id, string Name, int? GroupId = null);

/// <summary>
/// Couche d'accès unique aux FK de côté d'un Match : colonnes Player en simple,
/// colonnes Team en double. Toute la logique bracket/avancement/classement passe
/// par ici — ne jamais lire/écrire Player1Id/Player2Id/WinnerId directement
/// dans TournamentService.
/// </summary>
public static class MatchSideAccessor
{
    public static bool IsDoubles(Tournament tournament) => tournament.TeamSize == 2;

    /// <summary>
    /// « Double » au niveau du MATCH : déduit des FK d'équipe. Nécessaire pour les
    /// rencontres interclubs qui mélangent simples et doubles. Dans un tournoi en
    /// double, la génération écrit toujours les FK Team ; un slot de bracket vide
    /// (deux côtés null) est lu « simple », inoffensif car une session exige les
    /// deux côtés.
    /// </summary>
    public static bool IsDoublesMatch(Match match) => match.Team1Id != null || match.Team2Id != null;

    public static int? GetSide1Id(Match m, bool isDoubles) => isDoubles ? m.Team1Id : m.Player1Id;
    public static int? GetSide2Id(Match m, bool isDoubles) => isDoubles ? m.Team2Id : m.Player2Id;
    public static int? GetWinnerSideId(Match m, bool isDoubles) => isDoubles ? m.WinnerTeamId : m.WinnerId;

    public static void SetSide1Id(Match m, int? sideId, bool isDoubles)
    {
        if (isDoubles) m.Team1Id = sideId;
        else m.Player1Id = sideId;
    }

    public static void SetSide2Id(Match m, int? sideId, bool isDoubles)
    {
        if (isDoubles) m.Team2Id = sideId;
        else m.Player2Id = sideId;
    }

    public static void SetWinnerSideId(Match m, int? sideId, bool isDoubles)
    {
        if (isDoubles) m.WinnerTeamId = sideId;
        else m.WinnerId = sideId;
    }

    /// <summary>
    /// Les côtés inscrits au tournoi, triés par seed.
    /// Simple : joueurs approuvés. Double : paires (créées par l'admin, pas de statut).
    /// Nécessite Teams.Player1/Player2 ou TournamentPlayers.Player chargés.
    /// </summary>
    public static List<Side> GetSides(Tournament tournament)
    {
        if (IsDoubles(tournament))
        {
            return tournament.Teams
                .OrderBy(tt => tt.Seed ?? int.MaxValue)
                .Select(tt => new Side(tt.Id, tt.Name, tt.GroupId))
                .ToList();
        }

        return tournament.TournamentPlayers
            .Where(tp => tp.Status == RegistrationStatus.Approved)
            .OrderBy(tp => tp.Seed ?? int.MaxValue)
            .Select(tp => new Side(tp.PlayerId, $"{tp.Player.FirstName} {tp.Player.LastName}", tp.GroupId))
            .ToList();
    }
}
