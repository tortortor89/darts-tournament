using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public class PlayerStatsService
{
    private readonly AppDbContext _context;

    public PlayerStatsService(AppDbContext context)
    {
        _context = context;
    }

    // Le joueur participe au match : directement (simple) ou via sa paire (double)
    private static bool PlaysInMatch(Match m, int playerId) =>
        m.Player1Id == playerId || m.Player2Id == playerId
        || (m.Team1 != null && (m.Team1.Player1Id == playerId || m.Team1.Player2Id == playerId))
        || (m.Team2 != null && (m.Team2.Player1Id == playerId || m.Team2.Player2Id == playerId));

    private static bool WonMatch(Match m, int playerId) =>
        m.WinnerId == playerId
        || (m.WinnerTeam != null && (m.WinnerTeam.Player1Id == playerId || m.WinnerTeam.Player2Id == playerId));

    private static bool OnSide1(Match m, int playerId) =>
        m.Player1Id == playerId
        || (m.Team1 != null && (m.Team1.Player1Id == playerId || m.Team1.Player2Id == playerId));

    public async Task<PlayerCareerStatsResponse> GetCareerStatsAsync(int playerId)
    {
        // 1. Récupérer Player
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new InvalidOperationException($"Player {playerId} not found");

        // 2. Tous les matchs terminés (wins/losses) — les matchs de double comptent
        // via l'appartenance à une paire
        var matches = await _context.Matches
            .Include(m => m.Tournament)
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Include(m => m.WinnerTeam)
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId
                        || (m.Team1 != null && (m.Team1.Player1Id == playerId || m.Team1.Player2Id == playerId))
                        || (m.Team2 != null && (m.Team2.Player1Id == playerId || m.Team2.Player2Id == playerId)))
                        && m.Status == MatchStatus.Completed)
            .ToListAsync();

        int totalMatches = matches.Count;
        int matchesWon = matches.Count(m => WonMatch(m, playerId));
        int matchesLost = totalMatches - matchesWon;
        double winPct = totalMatches > 0 ? (double)matchesWon / totalMatches * 100 : 0;

        // 3. MatchSessions avec Throws (stats détaillées, x01 uniquement :
        // les visites Cricket fausseraient moyennes et checkouts)
        // Les volées étant attribuées au vrai lanceur, les stats de double sont exactes
        var sessionsWithThrows = await _context.MatchSessions
            .Include(ms => ms.Throws)
            .Include(ms => ms.Match)
            .ThenInclude(m => m.Team1)
            .Include(ms => ms.Match)
            .ThenInclude(m => m.Team2)
            .Where(ms => (ms.Match.Player1Id == playerId || ms.Match.Player2Id == playerId
                         || (ms.Match.Team1 != null && (ms.Match.Team1.Player1Id == playerId || ms.Match.Team1.Player2Id == playerId))
                         || (ms.Match.Team2 != null && (ms.Match.Team2.Player1Id == playerId || ms.Match.Team2.Player2Id == playerId)))
                         && ms.Status == MatchSessionStatus.Finished
                         && ms.GameMode != GameMode.Cricket
                         && ms.Throws.Any())
            .ToListAsync();

        PlayerStatsAggregated? detailedStats = null;
        if (sessionsWithThrows.Any())
        {
            detailedStats = CalculateAggregatedStats(sessionsWithThrows, playerId);
        }

        // 4. Tournois (inscriptions individuelles + paires)
        var soloTournaments = await _context.TournamentPlayers
            .Where(tp => tp.PlayerId == playerId && tp.Status == RegistrationStatus.Approved)
            .Select(tp => tp.TournamentId)
            .ToListAsync();
        var teamTournaments = await _context.TournamentTeams
            .Where(tt => tt.Player1Id == playerId || tt.Player2Id == playerId)
            .Select(tt => tt.TournamentId)
            .ToListAsync();
        var tournaments = soloTournaments.Concat(teamTournaments).Distinct().Count();

        // 5. Tournois gagnés (TODO: affiner pour vérifier que c'est bien la finale)
        var tournamentsWon = 0; // Placeholder pour l'instant

        return new PlayerCareerStatsResponse(
            playerId,
            $"{player.FirstName} {player.LastName}",
            totalMatches,
            matchesWon,
            matchesLost,
            winPct,
            detailedStats,
            tournaments,
            tournamentsWon,
            matches.FirstOrDefault()?.ScheduledAt,
            matches.LastOrDefault()?.ScheduledAt
        );
    }

    public async Task<List<PlayerTournamentHistoryItem>> GetTournamentHistoryAsync(int playerId)
    {
        var tournamentPlayers = await _context.TournamentPlayers
            .Include(tp => tp.Tournament)
            .Include(tp => tp.Group)
            .Where(tp => tp.PlayerId == playerId && tp.Status == RegistrationStatus.Approved)
            .ToListAsync();

        // Participations en double (via une paire)
        var tournamentTeams = await _context.TournamentTeams
            .Include(tt => tt.Tournament)
            .Include(tt => tt.Group)
            .Where(tt => tt.Player1Id == playerId || tt.Player2Id == playerId)
            .ToListAsync();

        // (Tournoi, Groupe) de chaque participation, tous types confondus
        var participations = tournamentPlayers
            .Select(tp => (tp.Tournament, tp.GroupId, GroupName: tp.Group?.Name))
            .Concat(tournamentTeams.Select(tt => (tt.Tournament, tt.GroupId, GroupName: tt.Group?.Name)))
            .OrderByDescending(p => p.Tournament.StartDate ?? p.Tournament.CreatedAt)
            .ToList();

        var history = new List<PlayerTournamentHistoryItem>();

        foreach (var (tournament, groupId, groupName) in participations)
        {
            var matches = await _context.Matches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .Include(m => m.WinnerTeam)
                .Where(m => m.TournamentId == tournament.Id
                            && (m.Player1Id == playerId || m.Player2Id == playerId
                                || (m.Team1 != null && (m.Team1.Player1Id == playerId || m.Team1.Player2Id == playerId))
                                || (m.Team2 != null && (m.Team2.Player1Id == playerId || m.Team2.Player2Id == playerId)))
                            && m.Status == MatchStatus.Completed)
                .ToListAsync();

            int matchesPlayed = matches.Count;
            int matchesWon = matches.Count(m => WonMatch(m, playerId));
            int matchesLost = matchesPlayed - matchesWon;

            // Déterminer résultat
            string result = "Participant";
            if (tournament.Status == TournamentStatus.Completed)
            {
                // Logique simple: si a gagné tous les matchs, probablement winner
                if (matchesLost == 0 && matchesPlayed > 0)
                {
                    result = "Winner";
                }
                else if (matchesPlayed > 0)
                {
                    result = $"{matchesWon}W-{matchesLost}L";
                }
                else
                {
                    result = "Did not play";
                }
            }
            else if (tournament.Status == TournamentStatus.InProgress)
            {
                result = matchesPlayed > 0 ? $"En cours ({matchesWon}W-{matchesLost}L)" : "En cours";
            }
            else
            {
                result = "Inscrit";
            }

            history.Add(new PlayerTournamentHistoryItem(
                tournament.Id,
                tournament.Name,
                tournament.Format,
                tournament.Status,
                tournament.StartDate,
                matchesPlayed,
                matchesWon,
                matchesLost,
                result,
                groupId,
                groupName,
                null // TODO: Group rank calculation
            ));
        }

        return history;
    }

    public async Task<List<HeadToHeadRecord>> GetHeadToHeadStatsAsync(int playerId)
    {
        // Head-to-head limité aux tournois en simple : en double, le face-à-face
        // est une affaire de paires, pas de joueurs (à revoir plus tard)
        var matches = await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Tournament)
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId)
                        && m.Tournament.TeamSize != 2
                        && m.Status == MatchStatus.Completed)
            .ToListAsync();

        var h2hRecords = matches
            .Select(m => new
            {
                OpponentId = m.Player1Id == playerId ? m.Player2Id : m.Player1Id,
                Opponent = m.Player1Id == playerId ? m.Player2 : m.Player1,
                Match = m,
                Won = m.WinnerId == playerId,
                LegsWon = m.Player1Id == playerId ? m.Player1Score ?? 0 : m.Player2Score ?? 0,
                LegsLost = m.Player1Id == playerId ? m.Player2Score ?? 0 : m.Player1Score ?? 0
            })
            .Where(x => x.OpponentId.HasValue && x.Opponent != null)
            .GroupBy(x => x.OpponentId)
            .Select(g => new HeadToHeadRecord(
                g.Key!.Value,
                $"{g.First().Opponent!.FirstName} {g.First().Opponent.LastName}",
                g.Count(),
                g.Count(x => x.Won),
                g.Count(x => !x.Won),
                g.Count() > 0 ? (double)g.Count(x => x.Won) / g.Count() * 100 : 0,
                g.Sum(x => x.LegsWon),
                g.Sum(x => x.LegsLost),
                g.Max(x => x.Match.ScheduledAt),
                g.OrderByDescending(x => x.Match.ScheduledAt).First().Match.Tournament?.Name
            ))
            .OrderByDescending(h => h.MatchesPlayed)
            .ToList();

        return h2hRecords;
    }

    private PlayerStatsAggregated CalculateAggregatedStats(List<MatchSession> sessions, int playerId)
    {
        // Agréger tous les Throws du joueur
        var allThrows = sessions
            .SelectMany(s => s.Throws)
            .Where(t => t.PlayerId == playerId)
            .ToList();

        if (!allThrows.Any())
        {
            return new PlayerStatsAggregated(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        int totalScore = allThrows.Sum(t => t.Score);
        int totalDarts = allThrows.Count * 3; // Approximation (3 darts par volée)
        double avg3Darts = totalDarts > 0 ? (double)totalScore / totalDarts * 3 : 0;

        var checkouts = allThrows.Where(t => t.IsCheckout).ToList();
        int checkoutSuccesses = checkouts.Count;
        // Checkout attempts = tous les Throws où RemainingScore <= 170 (possible de finir)
        int checkoutAttempts = allThrows.Count(t => t.RemainingScore <= 170 && !t.IsBust);
        double? checkoutPct = checkoutAttempts > 0 ? (double)checkoutSuccesses / checkoutAttempts * 100 : null;

        // Calcul highestCheckout: score fait sur le checkout = 501 - remaining avant le throw
        int highestCheckout = 0;
        foreach (var checkout in checkouts)
        {
            int checkoutScore = checkout.Score; // Le score fait pour finir
            if (checkoutScore > highestCheckout)
                highestCheckout = checkoutScore;
        }

        int highestScore = allThrows.Any() ? allThrows.Max(t => t.Score) : 0;
        int oneEighties = allThrows.Count(t => t.Score == 180);

        int totalLegsWon = sessions.Sum(s =>
            OnSide1(s.Match, playerId) ? s.Player1LegsWon : s.Player2LegsWon
        );

        // First 9 average: moyenne des 3 premiers throws de chaque leg
        double? first9Avg = null;
        int legCount = 0;
        double first9Total = 0;

        foreach (var session in sessions)
        {
            // Compter le nombre de legs joués
            int legsCount = session.Throws
                .Where(t => t.PlayerId == playerId)
                .Select(t => t.LegNumber)
                .Distinct()
                .Count();

            for (int leg = 1; leg <= legsCount; leg++)
            {
                var first3Throws = session.Throws
                    .Where(t => t.PlayerId == playerId && t.LegNumber == leg)
                    .OrderBy(t => t.ThrowNumber)
                    .Take(3)
                    .ToList();

                if (first3Throws.Count == 3)
                {
                    int first9Score = first3Throws.Sum(t => t.Score);
                    first9Total += first9Score / 9.0;
                    legCount++;
                }
            }
        }

        first9Avg = legCount > 0 ? first9Total / legCount : null;

        return new PlayerStatsAggregated(
            avg3Darts,
            checkoutPct,
            first9Avg,
            highestCheckout > 0 ? highestCheckout : null,
            totalDarts,
            totalScore,
            totalLegsWon,
            checkoutAttempts,
            checkoutSuccesses,
            highestScore > 0 ? highestScore : null,
            oneEighties,
            sessions.Count
        );
    }
}
