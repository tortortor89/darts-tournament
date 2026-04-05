using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public class MatchSessionService
{
    private readonly AppDbContext _context;
    private readonly TournamentService _tournamentService;

    public MatchSessionService(AppDbContext context, TournamentService tournamentService)
    {
        _context = context;
        _tournamentService = tournamentService;
    }

    /// <summary>
    /// Récupère ou crée une session pour un match
    /// </summary>
    public async Task<MatchSession?> GetOrCreateSessionAsync(int matchId)
    {
        var session = await _context.MatchSessions
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Player2)
            .Include(ms => ms.Throws)
            .FirstOrDefaultAsync(ms => ms.MatchId == matchId && ms.Status != MatchSessionStatus.Cancelled);

        return session;
    }

    /// <summary>
    /// Démarre une nouvelle session de match
    /// </summary>
    public async Task<MatchSession> StartSessionAsync(int matchId, StartMatchSessionRequest request)
    {
        var match = await _context.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
            throw new InvalidOperationException("Match non trouvé");

        if (match.Player1Id == null || match.Player2Id == null)
            throw new InvalidOperationException("Les deux joueurs doivent être définis");

        if (match.Status == MatchStatus.Completed)
            throw new InvalidOperationException("Ce match est déjà terminé");

        // Vérifier que le joueur qui commence est bien un des deux joueurs
        if (request.StartingPlayerId != match.Player1Id && request.StartingPlayerId != match.Player2Id)
            throw new InvalidOperationException("Le joueur qui commence doit être un des deux joueurs du match");

        // Annuler toute session existante
        var existingSession = await _context.MatchSessions
            .FirstOrDefaultAsync(ms => ms.MatchId == matchId && ms.Status != MatchSessionStatus.Cancelled);

        if (existingSession != null)
        {
            existingSession.Status = MatchSessionStatus.Cancelled;
        }

        var session = new MatchSession
        {
            MatchId = matchId,
            LegsToWin = request.LegsToWin,
            GameMode = GameMode.FiveOhOne,
            StartingPlayerId = request.StartingPlayerId,
            CurrentPlayerId = request.StartingPlayerId,
            CurrentLegStartingPlayerId = request.StartingPlayerId,
            Status = MatchSessionStatus.InProgress,
            Player1CurrentScore = 501,
            Player2CurrentScore = 501,
            StartedAt = DateTime.UtcNow
        };

        _context.MatchSessions.Add(session);

        // Mettre à jour le statut du match
        match.Status = MatchStatus.InProgress;

        await _context.SaveChangesAsync();

        // Recharger avec les relations
        return (await GetSessionByIdAsync(session.Id))!;
    }

    /// <summary>
    /// Récupère une session par son ID
    /// </summary>
    public async Task<MatchSession?> GetSessionByIdAsync(int sessionId)
    {
        return await _context.MatchSessions
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Player2)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Tournament)
            .Include(ms => ms.Throws)
                .ThenInclude(t => t.Player)
            .FirstOrDefaultAsync(ms => ms.Id == sessionId);
    }

    /// <summary>
    /// Enregistre une volée et met à jour l'état de la partie
    /// </summary>
    public async Task<MatchSession> RecordThrowAsync(int sessionId, RecordThrowRequest request)
    {
        var session = await GetSessionByIdAsync(sessionId);

        if (session == null)
            throw new InvalidOperationException("Session non trouvée");

        if (session.Status != MatchSessionStatus.InProgress)
            throw new InvalidOperationException("Cette session n'est pas en cours");

        var currentScore = session.CurrentPlayerId == session.Match.Player1Id
            ? session.Player1CurrentScore
            : session.Player2CurrentScore;

        // Calculer le nombre de volées pour ce leg et ce joueur
        var throwNumber = session.Throws
            .Count(t => t.LegNumber == session.CurrentLeg && t.PlayerId == session.CurrentPlayerId) + 1;

        // Vérifier la validité du score
        var isBust = false;
        var isCheckout = false;
        var newScore = currentScore - request.Score;

        if (newScore < 0 || newScore == 1)
        {
            // Bust : score reste inchangé
            isBust = true;
            newScore = currentScore;
        }
        else if (newScore == 0)
        {
            // Vérifier si c'est un double (simplifié pour l'instant)
            // Dans une vraie implémentation, on vérifierait Dart3
            isCheckout = IsValidCheckout(request);
            if (!isCheckout)
            {
                isBust = true;
                newScore = currentScore;
            }
        }

        // Créer le throw
        var throwEntity = new Throw
        {
            MatchSessionId = sessionId,
            PlayerId = session.CurrentPlayerId,
            LegNumber = session.CurrentLeg,
            ThrowNumber = throwNumber,
            Score = isBust ? 0 : request.Score,
            Dart1 = request.Dart1,
            Dart2 = request.Dart2,
            Dart3 = request.Dart3,
            RemainingScore = newScore,
            IsCheckout = isCheckout,
            IsBust = isBust
        };

        _context.Throws.Add(throwEntity);

        // Mettre à jour le score du joueur
        if (session.CurrentPlayerId == session.Match.Player1Id)
            session.Player1CurrentScore = newScore;
        else
            session.Player2CurrentScore = newScore;

        // Si checkout, le joueur gagne le leg
        if (isCheckout)
        {
            HandleLegWon(session);
        }
        else
        {
            // Passer au joueur suivant
            session.CurrentPlayerId = session.CurrentPlayerId == session.Match.Player1Id
                ? session.Match.Player2Id!.Value
                : session.Match.Player1Id!.Value;
        }

        await _context.SaveChangesAsync();

        return (await GetSessionByIdAsync(sessionId))!;
    }

    /// <summary>
    /// Gère la fin d'un leg
    /// </summary>
    private void HandleLegWon(MatchSession session)
    {
        // Incrémenter les legs gagnés
        if (session.CurrentPlayerId == session.Match.Player1Id)
            session.Player1LegsWon++;
        else
            session.Player2LegsWon++;

        // Vérifier si le match est gagné
        if (session.Player1LegsWon >= session.LegsToWin || session.Player2LegsWon >= session.LegsToWin)
        {
            session.Status = MatchSessionStatus.Finished;
            session.FinishedAt = DateTime.UtcNow;
        }
        else
        {
            // Nouveau leg
            session.CurrentLeg++;
            session.Player1CurrentScore = 501;
            session.Player2CurrentScore = 501;

            // Alterner qui commence
            session.CurrentLegStartingPlayerId = session.CurrentLegStartingPlayerId == session.Match.Player1Id
                ? session.Match.Player2Id!.Value
                : session.Match.Player1Id!.Value;
            session.CurrentPlayerId = session.CurrentLegStartingPlayerId;
        }
    }

    /// <summary>
    /// Vérifie si le checkout est valide (doit finir sur un double)
    /// </summary>
    private bool IsValidCheckout(RecordThrowRequest request)
    {
        // Si on a le détail des fléchettes, vérifier que la dernière est un double
        if (!string.IsNullOrEmpty(request.Dart3))
            return request.Dart3.StartsWith("D") || request.Dart3 == "DB";
        if (!string.IsNullOrEmpty(request.Dart2) && string.IsNullOrEmpty(request.Dart3))
            return request.Dart2.StartsWith("D") || request.Dart2 == "DB";
        if (!string.IsNullOrEmpty(request.Dart1) && string.IsNullOrEmpty(request.Dart2))
            return request.Dart1.StartsWith("D") || request.Dart1 == "DB";

        // Si on n'a pas le détail, on fait confiance au score
        // (l'utilisateur est responsable de la validation côté client)
        return true;
    }

    /// <summary>
    /// Valide et clôture le match, met à jour le score du tournoi
    /// </summary>
    public async Task ValidateMatchAsync(int sessionId)
    {
        var session = await GetSessionByIdAsync(sessionId);

        if (session == null)
            throw new InvalidOperationException("Session non trouvée");

        if (session.Status != MatchSessionStatus.Finished)
            throw new InvalidOperationException("Cette session n'est pas terminée");

        // Mettre à jour le match avec les scores finaux (legs gagnés)
        await _tournamentService.UpdateMatchScoreAsync(
            session.MatchId,
            session.Player1LegsWon,
            session.Player2LegsWon
        );
    }

    /// <summary>
    /// Annule une session en cours
    /// </summary>
    public async Task CancelSessionAsync(int sessionId)
    {
        var session = await GetSessionByIdAsync(sessionId);

        if (session == null)
            throw new InvalidOperationException("Session non trouvée");

        if (session.Status == MatchSessionStatus.Finished)
            throw new InvalidOperationException("Impossible d'annuler une session terminée");

        session.Status = MatchSessionStatus.Cancelled;
        session.Match.Status = MatchStatus.Pending;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Construit la réponse DTO pour une session
    /// </summary>
    public MatchSessionResponse BuildSessionResponse(MatchSession session)
    {
        var match = session.Match;
        var currentLegThrows = session.Throws
            .Where(t => t.LegNumber == session.CurrentLeg)
            .OrderBy(t => t.ThrowNumber)
            .Select(t => new ThrowResponse(
                t.Id,
                t.PlayerId,
                t.Player?.FirstName + " " + t.Player?.LastName ?? "Unknown",
                t.LegNumber,
                t.ThrowNumber,
                t.Score,
                t.Dart1,
                t.Dart2,
                t.Dart3,
                t.RemainingScore,
                t.IsCheckout,
                t.IsBust,
                t.CreatedAt
            ))
            .ToList();

        return new MatchSessionResponse(
            session.Id,
            session.MatchId,
            session.LegsToWin,
            session.GameMode,
            session.Status,
            new PlayerSessionInfo(
                match.Player1Id!.Value,
                $"{match.Player1!.FirstName} {match.Player1.LastName}",
                session.Player1LegsWon,
                session.Player1CurrentScore,
                session.StartingPlayerId == match.Player1Id
            ),
            new PlayerSessionInfo(
                match.Player2Id!.Value,
                $"{match.Player2!.FirstName} {match.Player2.LastName}",
                session.Player2LegsWon,
                session.Player2CurrentScore,
                session.StartingPlayerId == match.Player2Id
            ),
            session.CurrentPlayerId,
            session.CurrentLeg,
            currentLegThrows,
            session.CreatedAt,
            session.StartedAt,
            session.FinishedAt
        );
    }

    /// <summary>
    /// Construit la réponse spectateur pour une session
    /// </summary>
    public MatchSessionSpectatorResponse BuildSpectatorResponse(MatchSession session)
    {
        var match = session.Match;

        // Calculer l'historique des legs
        var legsHistory = new List<LegSummary>();
        var completedLegs = session.Throws
            .Where(t => t.IsCheckout)
            .OrderBy(t => t.LegNumber)
            .ToList();

        foreach (var checkoutThrow in completedLegs)
        {
            var legThrows = session.Throws.Where(t => t.LegNumber == checkoutThrow.LegNumber).ToList();
            var winnerThrows = legThrows.Where(t => t.PlayerId == checkoutThrow.PlayerId).ToList();
            var dartsThrown = winnerThrows.Count * 3; // Approximation
            var totalScored = winnerThrows.Sum(t => t.Score);
            var average = dartsThrown > 0 ? (double)totalScored / dartsThrown * 3 : 0;

            var winner = checkoutThrow.PlayerId == match.Player1Id ? match.Player1 : match.Player2;

            legsHistory.Add(new LegSummary(
                checkoutThrow.LegNumber,
                checkoutThrow.PlayerId,
                $"{winner!.FirstName} {winner.LastName}",
                dartsThrown,
                Math.Round(average, 2)
            ));
        }

        return new MatchSessionSpectatorResponse(
            session.MatchId,
            match.Tournament?.Name ?? "Unknown",
            session.LegsToWin,
            session.Status,
            new PlayerSpectatorInfo(
                match.Player1Id!.Value,
                $"{match.Player1!.FirstName} {match.Player1.LastName}",
                session.Player1LegsWon,
                session.Player1CurrentScore
            ),
            new PlayerSpectatorInfo(
                match.Player2Id!.Value,
                $"{match.Player2!.FirstName} {match.Player2.LastName}",
                session.Player2LegsWon,
                session.Player2CurrentScore
            ),
            session.CurrentPlayerId,
            session.CurrentLeg,
            legsHistory
        );
    }
}
