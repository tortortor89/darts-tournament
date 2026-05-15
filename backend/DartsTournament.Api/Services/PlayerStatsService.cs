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

    public async Task<PlayerCareerStatsResponse> GetCareerStatsAsync(int playerId)
    {
        // 1. Récupérer Player
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new InvalidOperationException($"Player {playerId} not found");

        // 2. Tous les matchs terminés (wins/losses)
        var matches = await _context.Matches
            .Include(m => m.Tournament)
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId)
                        && m.Status == MatchStatus.Completed)
            .ToListAsync();

        int totalMatches = matches.Count;
        int matchesWon = matches.Count(m => m.WinnerId == playerId);
        int matchesLost = totalMatches - matchesWon;
        double winPct = totalMatches > 0 ? (double)matchesWon / totalMatches * 100 : 0;

        // 3. MatchSessions avec Throws (stats détaillées)
        var sessionsWithThrows = await _context.MatchSessions
            .Include(ms => ms.Throws)
            .Include(ms => ms.Match)
            .Where(ms => (ms.Match.Player1Id == playerId || ms.Match.Player2Id == playerId)
                         && ms.Status == MatchSessionStatus.Finished
                         && ms.Throws.Any())
            .ToListAsync();

        PlayerStatsAggregated? detailedStats = null;
        if (sessionsWithThrows.Any())
        {
            detailedStats = CalculateAggregatedStats(sessionsWithThrows, playerId);
        }

        // 4. Tournois
        var tournaments = await _context.TournamentPlayers
            .Where(tp => tp.PlayerId == playerId && tp.Status == RegistrationStatus.Approved)
            .Select(tp => tp.TournamentId)
            .Distinct()
            .CountAsync();

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
            .OrderByDescending(tp => tp.Tournament.StartDate ?? tp.Tournament.CreatedAt)
            .ToListAsync();

        var history = new List<PlayerTournamentHistoryItem>();

        foreach (var tp in tournamentPlayers)
        {
            var matches = await _context.Matches
                .Where(m => m.TournamentId == tp.TournamentId
                            && (m.Player1Id == playerId || m.Player2Id == playerId)
                            && m.Status == MatchStatus.Completed)
                .ToListAsync();

            int matchesPlayed = matches.Count;
            int matchesWon = matches.Count(m => m.WinnerId == playerId);
            int matchesLost = matchesPlayed - matchesWon;

            // Déterminer résultat
            string result = "Participant";
            if (tp.Tournament.Status == TournamentStatus.Completed)
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
            else if (tp.Tournament.Status == TournamentStatus.InProgress)
            {
                result = matchesPlayed > 0 ? $"En cours ({matchesWon}W-{matchesLost}L)" : "En cours";
            }
            else
            {
                result = "Inscrit";
            }

            history.Add(new PlayerTournamentHistoryItem(
                tp.TournamentId,
                tp.Tournament.Name,
                tp.Tournament.Format,
                tp.Tournament.Status,
                tp.Tournament.StartDate,
                matchesPlayed,
                matchesWon,
                matchesLost,
                result,
                tp.GroupId,
                tp.Group?.Name,
                null // TODO: Group rank calculation
            ));
        }

        return history;
    }

    public async Task<List<HeadToHeadRecord>> GetHeadToHeadStatsAsync(int playerId)
    {
        var matches = await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Tournament)
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId)
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
            s.Match.Player1Id == playerId ? s.Player1LegsWon : s.Player2LegsWon
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
