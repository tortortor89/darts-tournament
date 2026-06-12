using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Hubs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public class MatchSessionService
{
    private readonly AppDbContext _context;
    private readonly TournamentService _tournamentService;
    private readonly IHubContext<MatchHub, IMatchHubClient> _matchHub;
    private readonly MatchStatsService _statsService;
    private readonly CricketService _cricketService;

    public MatchSessionService(
        AppDbContext context,
        TournamentService tournamentService,
        IHubContext<MatchHub, IMatchHubClient> matchHub,
        MatchStatsService statsService,
        CricketService cricketService)
    {
        _context = context;
        _tournamentService = tournamentService;
        _matchHub = matchHub;
        _statsService = statsService;
        _cricketService = cricketService;
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
            GameMode = request.GameMode,
            StartingPlayerId = request.StartingPlayerId,
            CurrentPlayerId = request.StartingPlayerId,
            CurrentLegStartingPlayerId = request.StartingPlayerId,
            Status = MatchSessionStatus.InProgress,
            TrackDoubles = request.TrackDoubles,
            StartedAt = DateTime.UtcNow
        };

        // Initialisation selon le mode
        if (request.GameMode == GameMode.FiveOhOne)
        {
            session.Player1CurrentScore = 501;
            session.Player2CurrentScore = 501;
        }
        else if (request.GameMode == GameMode.Cricket)
        {
            var cricketState = _cricketService.InitializeState(match.Player1Id!.Value, match.Player2Id!.Value);
            session.CricketState = cricketState;
            session.Player1CurrentScore = 0;  // Réutilisé pour le score Cricket
            session.Player2CurrentScore = 0;
        }

        _context.MatchSessions.Add(session);

        // Mettre à jour le statut du match
        match.Status = MatchStatus.InProgress;

        await _context.SaveChangesAsync();

        // Recharger avec les relations
        var reloadedSession = (await GetSessionByIdAsync(session.Id))!;

        // Broadcaster l'événement de démarrage
        var sessionResponse = BuildSessionResponse(reloadedSession);
        await _matchHub.Clients.Group($"match-{matchId}")
            .SessionStarted(new SessionStartedEvent(matchId, sessionResponse));

        return reloadedSession;
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

        if (session.GameMode != GameMode.FiveOhOne)
            throw new InvalidOperationException("Cette session n'est pas en mode 501");

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
            IsBust = isBust,
            DartsUsed = request.DartsUsed,
            DoublesAttempted = request.DoublesAttempted
        };

        _context.Throws.Add(throwEntity);

        // Mettre à jour le score du joueur
        if (session.CurrentPlayerId == session.Match.Player1Id)
            session.Player1CurrentScore = newScore;
        else
            session.Player2CurrentScore = newScore;

        // Capturer les infos avant de potentiellement modifier le leg
        var legNumberBeforeCheckout = session.CurrentLeg;
        var winnerId = session.CurrentPlayerId;
        var winnerPlayer = session.CurrentPlayerId == session.Match.Player1Id
            ? session.Match.Player1!
            : session.Match.Player2!;

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

        var reloadedSession = (await GetSessionByIdAsync(sessionId))!;

        // Construire la réponse de la volée
        var throwResponse = new ThrowResponse(
            throwEntity.Id,
            throwEntity.PlayerId,
            $"{winnerPlayer.FirstName} {winnerPlayer.LastName}",
            throwEntity.LegNumber,
            throwEntity.ThrowNumber,
            throwEntity.Score,
            throwEntity.Dart1,
            throwEntity.Dart2,
            throwEntity.Dart3,
            throwEntity.RemainingScore,
            throwEntity.IsCheckout,
            throwEntity.IsBust,
            throwEntity.DartsUsed,
            throwEntity.DoublesAttempted,
            throwEntity.CreatedAt
        );

        // Calculer les stats
        var stats = _statsService.CalculateStats(reloadedSession);

        // Broadcaster l'événement de volée
        await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
            .ThrowRecorded(new ThrowRecordedEvent(
                reloadedSession.MatchId,
                throwResponse,
                reloadedSession.Player1CurrentScore,
                reloadedSession.Player2CurrentScore,
                reloadedSession.CurrentPlayerId,
                stats
            ));

        // Si leg gagné, broadcaster l'événement
        if (isCheckout)
        {
            var legThrows = reloadedSession.Throws
                .Where(t => t.LegNumber == legNumberBeforeCheckout && t.PlayerId == winnerId)
                .ToList();
            var dartsThrown = legThrows.Count * 3;
            var totalScored = legThrows.Sum(t => t.Score);
            var average = dartsThrown > 0 ? (double)totalScored / dartsThrown * 3 : 0;

            var legSummary = new LegSummary(
                legNumberBeforeCheckout,
                winnerId,
                $"{winnerPlayer.FirstName} {winnerPlayer.LastName}",
                dartsThrown,
                Math.Round(average, 2)
            );

            await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
                .LegWon(new LegWonEvent(
                    reloadedSession.MatchId,
                    legNumberBeforeCheckout,
                    winnerId,
                    $"{winnerPlayer.FirstName} {winnerPlayer.LastName}",
                    reloadedSession.Player1LegsWon,
                    reloadedSession.Player2LegsWon,
                    reloadedSession.CurrentLeg,
                    legSummary
                ));

            // Si match terminé, broadcaster l'événement
            if (reloadedSession.Status == MatchSessionStatus.Finished)
            {
                await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
                    .MatchFinished(new MatchFinishedEvent(
                        reloadedSession.MatchId,
                        winnerId,
                        $"{winnerPlayer.FirstName} {winnerPlayer.LastName}",
                        reloadedSession.Player1LegsWon,
                        reloadedSession.Player2LegsWon,
                        stats
                    ));
            }
        }

        return reloadedSession;
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

        var matchId = session.MatchId;
        session.Status = MatchSessionStatus.Cancelled;
        session.Match.Status = MatchStatus.Pending;

        await _context.SaveChangesAsync();

        // Broadcaster l'annulation
        await _matchHub.Clients.Group($"match-{matchId}")
            .SessionCancelled(matchId);
    }

    /// <summary>
    /// Annule la dernière volée enregistrée et restaure l'état de la partie
    /// </summary>
    public async Task<MatchSession> UndoLastThrowAsync(int sessionId)
    {
        var session = await GetSessionByIdAsync(sessionId);

        if (session == null)
            throw new InvalidOperationException("Session non trouvée");

        if (session.Status != MatchSessionStatus.InProgress && session.Status != MatchSessionStatus.Finished)
            throw new InvalidOperationException("Cette session n'est pas en cours");

        if (session.Match.Status == MatchStatus.Completed)
            throw new InvalidOperationException("Impossible d'annuler une volée : le match a déjà été validé");

        var lastThrow = session.Throws.OrderByDescending(t => t.Id).FirstOrDefault();

        if (lastThrow == null)
            throw new InvalidOperationException("Aucune volée à annuler");

        // Si la volée annulée avait gagné un leg, le rendre
        if (lastThrow.IsCheckout)
        {
            if (lastThrow.PlayerId == session.Match.Player1Id)
                session.Player1LegsWon--;
            else
                session.Player2LegsWon--;

            if (session.Status == MatchSessionStatus.Finished)
            {
                session.Status = MatchSessionStatus.InProgress;
                session.FinishedAt = null;
            }
        }

        // Revenir au leg de la volée annulée, c'est à ce joueur de rejouer
        session.CurrentLeg = lastThrow.LegNumber;
        session.CurrentLegStartingPlayerId = GetLegStartingPlayerId(session, lastThrow.LegNumber);
        session.CurrentPlayerId = lastThrow.PlayerId;

        _context.Throws.Remove(lastThrow);
        session.Throws.Remove(lastThrow);

        // Recalculer l'état du leg à partir des volées restantes
        var legThrows = session.Throws
            .Where(t => t.LegNumber == lastThrow.LegNumber)
            .OrderBy(t => t.Id)
            .ToList();

        if (session.GameMode == GameMode.FiveOhOne)
        {
            // Les volées bust sont stockées avec Score = 0, la somme reste donc correcte
            session.Player1CurrentScore = 501 - legThrows
                .Where(t => t.PlayerId == session.Match.Player1Id)
                .Sum(t => t.Score);
            session.Player2CurrentScore = 501 - legThrows
                .Where(t => t.PlayerId == session.Match.Player2Id)
                .Sum(t => t.Score);
        }
        else if (session.GameMode == GameMode.Cricket)
        {
            var state = ReplayCricketLeg(session, legThrows);
            session.CricketState = state;
            session.Player1CurrentScore = state.PlayerStates[session.Match.Player1Id!.Value].Score;
            session.Player2CurrentScore = state.PlayerStates[session.Match.Player2Id!.Value].Score;
        }

        await _context.SaveChangesAsync();

        var reloadedSession = (await GetSessionByIdAsync(sessionId))!;

        await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
            .ThrowUndone(new ThrowUndoneEvent(reloadedSession.MatchId));

        return reloadedSession;
    }

    /// <summary>
    /// Détermine quel joueur commence un leg donné (alternance à partir de StartingPlayerId au leg 1)
    /// </summary>
    private int GetLegStartingPlayerId(MatchSession session, int legNumber)
    {
        if (legNumber % 2 == 1)
            return session.StartingPlayerId;

        return session.StartingPlayerId == session.Match.Player1Id
            ? session.Match.Player2Id!.Value
            : session.Match.Player1Id!.Value;
    }

    /// <summary>
    /// Reconstruit l'état Cricket d'un leg en rejouant les visites depuis CricketDataJson
    /// </summary>
    private CricketGameState ReplayCricketLeg(MatchSession session, List<Throw> legThrows)
    {
        var player1Id = session.Match.Player1Id!.Value;
        var player2Id = session.Match.Player2Id!.Value;
        var state = _cricketService.InitializeState(player1Id, player2Id);

        foreach (var legThrow in legThrows)
        {
            if (string.IsNullOrEmpty(legThrow.CricketDataJson))
                continue;

            var data = System.Text.Json.JsonSerializer.Deserialize<CricketThrowData>(legThrow.CricketDataJson);
            if (data?.Hits == null)
                continue;

            var opponentId = legThrow.PlayerId == player1Id ? player2Id : player1Id;
            _cricketService.ProcessTurn(state, legThrow.PlayerId, opponentId, data.Hits);
        }

        return state;
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
                t.DartsUsed,
                t.DoublesAttempted,
                t.CreatedAt
            ))
            .ToList();

        // Construire l'état Cricket si applicable
        CricketDisplayState? cricketDisplayState = null;
        if (session.GameMode == GameMode.Cricket && session.CricketState != null)
        {
            cricketDisplayState = _cricketService.BuildDisplayState(
                session.CricketState,
                match.Player1Id!.Value,
                match.Player2Id!.Value
            );
        }

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
            session.FinishedAt,
            session.TrackDoubles,
            cricketDisplayState
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

        // Construire l'état Cricket si applicable
        CricketDisplayState? cricketDisplayState = null;
        if (session.GameMode == GameMode.Cricket && session.CricketState != null)
        {
            cricketDisplayState = _cricketService.BuildDisplayState(
                session.CricketState,
                match.Player1Id!.Value,
                match.Player2Id!.Value
            );
        }

        return new MatchSessionSpectatorResponse(
            session.MatchId,
            match.Tournament?.Name ?? "Unknown",
            session.LegsToWin,
            session.GameMode,
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
            legsHistory,
            cricketDisplayState
        );
    }

    /// <summary>
    /// Enregistre une visite complète Cricket (turn)
    /// </summary>
    public async Task<CricketTurnResponse> RecordCricketTurnAsync(int sessionId, RecordCricketTurnRequest request)
    {
        var session = await GetSessionByIdAsync(sessionId);

        if (session == null)
            throw new InvalidOperationException("Session non trouvée");

        if (session.Status != MatchSessionStatus.InProgress)
            throw new InvalidOperationException("Cette session n'est pas en cours");

        if (session.GameMode != GameMode.Cricket)
            throw new InvalidOperationException("Cette session n'est pas en mode Cricket");

        // Validation de la visite
        _cricketService.ValidateTurn(request.Hits);

        var cricketState = session.CricketState!;
        var currentPlayerId = session.CurrentPlayerId;
        var opponentId = currentPlayerId == session.Match.Player1Id
            ? session.Match.Player2Id!.Value
            : session.Match.Player1Id!.Value;

        // Traiter toute la visite
        var hitResults = _cricketService.ProcessTurn(
            cricketState,
            currentPlayerId,
            opponentId,
            request.Hits
        );

        // Sauvegarder l'état mis à jour
        session.CricketState = cricketState;

        // Mettre à jour les scores dans les champs CurrentScore
        session.Player1CurrentScore = cricketState.PlayerStates[session.Match.Player1Id!.Value].Score;
        session.Player2CurrentScore = cricketState.PlayerStates[session.Match.Player2Id!.Value].Score;

        // Calculer le numéro du throw
        var throwNumber = session.Throws
            .Count(t => t.LegNumber == session.CurrentLeg && t.PlayerId == currentPlayerId) + 1;

        // Calculer total de points marqués
        var totalPoints = hitResults.Sum(r => r.PointsScored);

        // Formater les darts pour l'entité Throw
        var dartStrings = FormatCricketTurn(request.Hits);

        // Créer le Throw entity
        var throwEntity = new Throw
        {
            MatchSessionId = sessionId,
            PlayerId = currentPlayerId,
            LegNumber = session.CurrentLeg,
            ThrowNumber = throwNumber,
            Score = totalPoints,  // Total points marqués dans la visite
            Dart1 = dartStrings.Length > 0 ? dartStrings[0] : "MISS",
            Dart2 = dartStrings.Length > 1 ? dartStrings[1] : null,
            Dart3 = dartStrings.Length > 2 ? dartStrings[2] : null,
            RemainingScore = 0,  // Non utilisé en Cricket
            IsCheckout = false,  // Sera mis à true si leg gagné
            IsBust = false,
            CricketDataJson = System.Text.Json.JsonSerializer.Serialize(new { hits = request.Hits, results = hitResults })
        };

        _context.Throws.Add(throwEntity);

        // Vérifier si le joueur a gagné le leg
        bool legWon = _cricketService.HasPlayerWonLeg(cricketState, currentPlayerId, opponentId);

        // Capturer le numéro du leg avant que HandleCricketLegWon ne l'incrémente
        var legNumberBeforeWin = session.CurrentLeg;

        if (legWon)
        {
            throwEntity.IsCheckout = true;
            HandleCricketLegWon(session);
        }
        else
        {
            // Passer au joueur suivant
            session.CurrentPlayerId = opponentId;
        }

        await _context.SaveChangesAsync();

        // Recharger la session avec toutes les relations
        var reloadedSession = (await GetSessionByIdAsync(sessionId))!;

        var displayState = _cricketService.BuildDisplayState(
            reloadedSession.CricketState!,
            reloadedSession.Match.Player1Id!.Value,
            reloadedSession.Match.Player2Id!.Value
        );

        var currentPlayer = currentPlayerId == reloadedSession.Match.Player1Id
            ? reloadedSession.Match.Player1!
            : reloadedSession.Match.Player2!;

        var response = new CricketTurnResponse(
            currentPlayerId,
            $"{currentPlayer.FirstName} {currentPlayer.LastName}",
            hitResults,
            totalPoints,
            displayState
        );

        // Broadcaster l'événement de visite Cricket
        await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
            .CricketTurnRecorded(new CricketTurnRecordedEvent(
                reloadedSession.MatchId,
                response,
                reloadedSession.Player1CurrentScore,
                reloadedSession.Player2CurrentScore,
                reloadedSession.CurrentPlayerId
            ));

        // Si leg gagné, broadcaster l'événement
        if (legWon)
        {
            var legThrows = reloadedSession.Throws
                .Where(t => t.LegNumber == legNumberBeforeWin && t.PlayerId == currentPlayerId)
                .ToList();
            var dartsThrown = legThrows.Count * 3; // Approximation pour Cricket
            var totalScored = legThrows.Sum(t => t.Score);

            var legSummary = new LegSummary(
                legNumberBeforeWin,
                currentPlayerId,
                $"{currentPlayer.FirstName} {currentPlayer.LastName}",
                dartsThrown,
                0 // Moyenne non calculée pour Cricket pour le moment
            );

            await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
                .LegWon(new LegWonEvent(
                    reloadedSession.MatchId,
                    legNumberBeforeWin,
                    currentPlayerId,
                    $"{currentPlayer.FirstName} {currentPlayer.LastName}",
                    reloadedSession.Player1LegsWon,
                    reloadedSession.Player2LegsWon,
                    reloadedSession.CurrentLeg,
                    legSummary
                ));

            // Si match terminé, broadcaster l'événement
            if (reloadedSession.Status == MatchSessionStatus.Finished)
            {
                var finalStats = _statsService.CalculateStats(reloadedSession);

                await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
                    .MatchFinished(new MatchFinishedEvent(
                        reloadedSession.MatchId,
                        currentPlayerId,
                        $"{currentPlayer.FirstName} {currentPlayer.LastName}",
                        reloadedSession.Player1LegsWon,
                        reloadedSession.Player2LegsWon,
                        finalStats
                    ));
            }
        }

        return response;
    }

    /// <summary>
    /// Formate les hits d'une visite Cricket en tableau de strings pour Dart1/2/3
    /// </summary>
    private string[] FormatCricketTurn(List<CricketHit> hits)
    {
        if (hits.Count == 0)
            return new[] { "MISS", "MISS", "MISS" };

        var darts = new List<string>();

        foreach (var hit in hits)
        {
            // Chaque "mark" représente une touche (simple/double/triple)
            for (int i = 0; i < hit.Marks; i++)
            {
                var targetStr = hit.Target == 25 ? "BULL" : hit.Target.ToString();
                darts.Add(targetStr);
            }
        }

        // Compléter avec MISS si moins de 3 fléchettes
        while (darts.Count < 3)
        {
            darts.Add("MISS");
        }

        return darts.Take(3).ToArray();
    }

    /// <summary>
    /// Gère la fin d'un leg Cricket
    /// </summary>
    private void HandleCricketLegWon(MatchSession session)
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
            // Nouveau leg - réinitialiser l'état Cricket
            session.CurrentLeg++;
            var cricketState = _cricketService.InitializeState(
                session.Match.Player1Id!.Value,
                session.Match.Player2Id!.Value
            );
            session.CricketState = cricketState;
            session.Player1CurrentScore = 0;
            session.Player2CurrentScore = 0;

            // Alterner qui commence
            session.CurrentLegStartingPlayerId = session.CurrentLegStartingPlayerId == session.Match.Player1Id
                ? session.Match.Player2Id!.Value
                : session.Match.Player1Id!.Value;
            session.CurrentPlayerId = session.CurrentLegStartingPlayerId;
        }
    }

}
