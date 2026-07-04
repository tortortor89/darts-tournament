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

    // ----- Helpers de côté (simple : 1 joueur, double : paire avec ordre de passage) -----

    private static bool IsDoubles(MatchSession session) => session.Match.Tournament.TeamSize == 2;

    // Ordre de passage des lanceurs du côté (fixé à la config en double)
    private static List<int> Side1Order(MatchSession s) => IsDoubles(s)
        ? new List<int> { s.Side1Player1Id!.Value, s.Side1Player2Id!.Value }
        : new List<int> { s.Match.Player1Id!.Value };

    private static List<int> Side2Order(MatchSession s) => IsDoubles(s)
        ? new List<int> { s.Side2Player1Id!.Value, s.Side2Player2Id!.Value }
        : new List<int> { s.Match.Player2Id!.Value };

    // Id de côté (clé cricket, ids des DTOs) : joueur en simple, équipe en double
    private static int Side1Key(MatchSession s) => IsDoubles(s) ? s.Match.Team1Id!.Value : s.Match.Player1Id!.Value;
    private static int Side2Key(MatchSession s) => IsDoubles(s) ? s.Match.Team2Id!.Value : s.Match.Player2Id!.Value;

    private static int SideOf(MatchSession s, int throwerId) =>
        TurnRotationCalculator.SideOfThrower(throwerId, Side1Order(s), Side2Order(s));

    private static int SideKeyOf(MatchSession s, int throwerId) =>
        SideOf(s, throwerId) == 1 ? Side1Key(s) : Side2Key(s);

    private static string SideName(MatchSession s, int sideNumber)
    {
        var m = s.Match;
        if (IsDoubles(s))
            return sideNumber == 1 ? m.Team1!.Name : m.Team2!.Name;
        var p = sideNumber == 1 ? m.Player1! : m.Player2!;
        return $"{p.FirstName} {p.LastName}";
    }

    private static List<TeamMemberInfo>? SideMembers(MatchSession s, int sideNumber)
    {
        if (!IsDoubles(s))
            return null;
        var team = sideNumber == 1 ? s.Match.Team1! : s.Match.Team2!;
        return new List<TeamMemberInfo>
        {
            new(team.Player1Id, $"{team.Player1.FirstName} {team.Player1.LastName}"),
            new(team.Player2Id, $"{team.Player2.FirstName} {team.Player2.LastName}")
        };
    }

    private static string ThrowerName(MatchSession s, int playerId)
    {
        var m = s.Match;
        if (m.Player1Id == playerId && m.Player1 != null) return $"{m.Player1.FirstName} {m.Player1.LastName}";
        if (m.Player2Id == playerId && m.Player2 != null) return $"{m.Player2.FirstName} {m.Player2.LastName}";
        foreach (var team in new[] { m.Team1, m.Team2 })
        {
            if (team == null) continue;
            if (team.Player1Id == playerId) return $"{team.Player1.FirstName} {team.Player1.LastName}";
            if (team.Player2Id == playerId) return $"{team.Player2.FirstName} {team.Player2.LastName}";
        }
        return "Inconnu";
    }

    // Rotation du leg courant, et lanceur de la prochaine volée
    private static int NextThrower(MatchSession s, int throwsAlreadyInLeg)
    {
        bool side1StartsLeg = SideOf(s, s.CurrentLegStartingPlayerId) == 1;
        var rotation = TurnRotationCalculator.BuildLegRotation(Side1Order(s), Side2Order(s), side1StartsLeg);
        return TurnRotationCalculator.NextThrower(rotation, throwsAlreadyInLeg);
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
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Tournament)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team1)
                .ThenInclude(tt => tt!.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team1)
                .ThenInclude(tt => tt!.Player2)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team2)
                .ThenInclude(tt => tt!.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team2)
                .ThenInclude(tt => tt!.Player2)
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
            .Include(m => m.Tournament)
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Team1)
            .ThenInclude(tt => tt!.Player1)
            .Include(m => m.Team1)
            .ThenInclude(tt => tt!.Player2)
            .Include(m => m.Team2)
            .ThenInclude(tt => tt!.Player1)
            .Include(m => m.Team2)
            .ThenInclude(tt => tt!.Player2)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
            throw new InvalidOperationException("Match non trouvé");

        bool isDoubles = match.Tournament.TeamSize == 2;

        if (isDoubles)
        {
            if (match.Team1Id == null || match.Team2Id == null)
                throw new InvalidOperationException("Les deux équipes doivent être définies");
        }
        else if (match.Player1Id == null || match.Player2Id == null)
        {
            throw new InvalidOperationException("Les deux joueurs doivent être définis");
        }

        if (match.Status == MatchStatus.Completed)
            throw new InvalidOperationException("Ce match est déjà terminé");

        int startingThrowerId;
        List<int>? side1Order = null;
        List<int>? side2Order = null;

        if (isDoubles)
        {
            // Valider l'équipe qui commence et les ordres de passage
            if (request.StartingTeamId != match.Team1Id && request.StartingTeamId != match.Team2Id)
                throw new InvalidOperationException("L'équipe qui commence doit être une des deux équipes du match");

            side1Order = ValidateTeamOrder(request.Side1PlayerOrder, match.Team1!);
            side2Order = ValidateTeamOrder(request.Side2PlayerOrder, match.Team2!);

            startingThrowerId = request.StartingTeamId == match.Team1Id ? side1Order[0] : side2Order[0];
        }
        else
        {
            // Vérifier que le joueur qui commence est bien un des deux joueurs
            if (request.StartingPlayerId != match.Player1Id && request.StartingPlayerId != match.Player2Id)
                throw new InvalidOperationException("Le joueur qui commence doit être un des deux joueurs du match");

            startingThrowerId = request.StartingPlayerId;
        }

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
            StartingPlayerId = startingThrowerId,
            CurrentPlayerId = startingThrowerId,
            CurrentLegStartingPlayerId = startingThrowerId,
            Status = MatchSessionStatus.InProgress,
            // Le tracking des doubles n'a de sens qu'en double out
            TrackDoubles = request.TrackDoubles && request.DoubleOut,
            DoubleOut = request.DoubleOut,
            StartedAt = DateTime.UtcNow,
            Side1Player1Id = side1Order?[0],
            Side1Player2Id = side1Order?[1],
            Side2Player1Id = side2Order?[0],
            Side2Player2Id = side2Order?[1]
        };

        // Initialisation selon le mode
        if (request.GameMode.IsX01())
        {
            session.Player1CurrentScore = request.GameMode.StartingScore();
            session.Player2CurrentScore = request.GameMode.StartingScore();
        }
        else if (request.GameMode == GameMode.Cricket)
        {
            // État Cricket indexé par côté : joueur en simple, équipe en double
            // (tableau de cibles partagé par la paire)
            var cricketState = _cricketService.InitializeState(
                isDoubles ? match.Team1Id!.Value : match.Player1Id!.Value,
                isDoubles ? match.Team2Id!.Value : match.Player2Id!.Value);
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
    /// Valide que l'ordre fourni est une permutation exacte des deux membres de l'équipe
    /// </summary>
    private static List<int> ValidateTeamOrder(List<int>? order, TournamentTeam team)
    {
        if (order == null || order.Count != 2
            || !order.Contains(team.Player1Id) || !order.Contains(team.Player2Id)
            || order[0] == order[1])
        {
            throw new InvalidOperationException(
                $"L'ordre de passage de l'équipe {team.Name} doit contenir ses deux joueurs");
        }
        return order;
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
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team1)
                .ThenInclude(tt => tt!.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team1)
                .ThenInclude(tt => tt!.Player2)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team2)
                .ThenInclude(tt => tt!.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team2)
                .ThenInclude(tt => tt!.Player2)
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

        if (!session.GameMode.IsX01())
            throw new InvalidOperationException("Cette session n'est pas en mode x01 (501/301)");

        int currentSide = SideOf(session, session.CurrentPlayerId);
        var currentScore = currentSide == 1
            ? session.Player1CurrentScore
            : session.Player2CurrentScore;

        // Calculer le nombre de volées pour ce leg et ce joueur
        var throwNumber = session.Throws
            .Count(t => t.LegNumber == session.CurrentLeg && t.PlayerId == session.CurrentPlayerId) + 1;

        // Volées déjà jouées dans ce leg (tous lanceurs), avant celle-ci
        var throwsInLegBefore = session.Throws.Count(t => t.LegNumber == session.CurrentLeg);

        // Vérifier la validité du score
        var isBust = false;
        var isCheckout = false;
        var newScore = currentScore - request.Score;

        // En double out, rester à 1 est un bust (impossible de finir sur un double)
        if (newScore < 0 || (session.DoubleOut && newScore == 1))
        {
            // Bust : score reste inchangé
            isBust = true;
            newScore = currentScore;
        }
        else if (newScore == 0)
        {
            // En straight out, atteindre 0 suffit ; en double out, la dernière
            // fléchette doit être un double (vérifié si le détail est fourni)
            isCheckout = !session.DoubleOut || IsValidCheckout(request);
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

        // Mettre à jour le score du côté
        if (currentSide == 1)
            session.Player1CurrentScore = newScore;
        else
            session.Player2CurrentScore = newScore;

        // Capturer les infos avant de potentiellement modifier le leg
        var legNumberBeforeCheckout = session.CurrentLeg;
        var throwerId = session.CurrentPlayerId;
        var throwerName = ThrowerName(session, throwerId);
        var winnerSideKey = SideKeyOf(session, throwerId);
        var winnerSideName = SideName(session, currentSide);

        // Si checkout, le côté gagne le leg
        if (isCheckout)
        {
            HandleLegWon(session);
        }
        else
        {
            // Passer au lanceur suivant dans la rotation (A1→B1 en simple, A1→B1→A2→B2 en double)
            session.CurrentPlayerId = NextThrower(session, throwsInLegBefore + 1);
        }

        await _context.SaveChangesAsync();

        var reloadedSession = (await GetSessionByIdAsync(sessionId))!;

        // Construire la réponse de la volée
        var throwResponse = new ThrowResponse(
            throwEntity.Id,
            throwEntity.PlayerId,
            throwerName,
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
                stats,
                SideKeyOf(reloadedSession, reloadedSession.CurrentPlayerId)
            ));

        // Si leg gagné, broadcaster l'événement (WinnerId = id du côté gagnant)
        if (isCheckout)
        {
            var winnerSideMembers = currentSide == 1 ? Side1Order(reloadedSession) : Side2Order(reloadedSession);
            var legThrows = reloadedSession.Throws
                .Where(t => t.LegNumber == legNumberBeforeCheckout && winnerSideMembers.Contains(t.PlayerId))
                .ToList();
            var dartsThrown = legThrows.Count * 3;
            var totalScored = legThrows.Sum(t => t.Score);
            var average = dartsThrown > 0 ? (double)totalScored / dartsThrown * 3 : 0;

            var legSummary = new LegSummary(
                legNumberBeforeCheckout,
                winnerSideKey,
                winnerSideName,
                dartsThrown,
                Math.Round(average, 2)
            );

            await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
                .LegWon(new LegWonEvent(
                    reloadedSession.MatchId,
                    legNumberBeforeCheckout,
                    winnerSideKey,
                    winnerSideName,
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
                        winnerSideKey,
                        winnerSideName,
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
        // Incrémenter les legs gagnés du côté du lanceur
        if (SideOf(session, session.CurrentPlayerId) == 1)
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
            session.Player1CurrentScore = session.GameMode.StartingScore();
            session.Player2CurrentScore = session.GameMode.StartingScore();

            // Le côté qui commence alterne ; le leg repart sur son premier lanceur
            session.CurrentLegStartingPlayerId = GetLegStartingPlayerId(session, session.CurrentLeg);
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

        // Si la volée annulée avait gagné un leg, le rendre (au côté du lanceur)
        if (lastThrow.IsCheckout)
        {
            if (SideOf(session, lastThrow.PlayerId) == 1)
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

        if (session.GameMode.IsX01())
        {
            // Les volées bust sont stockées avec Score = 0, la somme reste donc correcte
            // (score du côté = somme des volées de ses lanceurs)
            var side1Members = Side1Order(session);
            var side2Members = Side2Order(session);
            session.Player1CurrentScore = session.GameMode.StartingScore() - legThrows
                .Where(t => side1Members.Contains(t.PlayerId))
                .Sum(t => t.Score);
            session.Player2CurrentScore = session.GameMode.StartingScore() - legThrows
                .Where(t => side2Members.Contains(t.PlayerId))
                .Sum(t => t.Score);
        }
        else if (session.GameMode == GameMode.Cricket)
        {
            var state = ReplayCricketLeg(session, legThrows);
            session.CricketState = state;
            session.Player1CurrentScore = state.PlayerStates[Side1Key(session)].Score;
            session.Player2CurrentScore = state.PlayerStates[Side2Key(session)].Score;
        }

        await _context.SaveChangesAsync();

        var reloadedSession = (await GetSessionByIdAsync(sessionId))!;

        await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
            .ThrowUndone(new ThrowUndoneEvent(reloadedSession.MatchId));

        return reloadedSession;
    }

    /// <summary>
    /// Détermine quel lanceur commence un leg donné : le côté qui démarre alterne
    /// à chaque leg, et le leg repart sur le premier lanceur de ce côté.
    /// </summary>
    private static int GetLegStartingPlayerId(MatchSession session, int legNumber)
    {
        bool side1StartedLeg1 = TurnRotationCalculator.SideOfThrower(
            session.StartingPlayerId, Side1Order(session), Side2Order(session)) == 1;

        return TurnRotationCalculator.Side1StartsLeg(legNumber, side1StartedLeg1)
            ? Side1Order(session)[0]
            : Side2Order(session)[0];
    }

    /// <summary>
    /// Reconstruit l'état Cricket d'un leg en rejouant les visites depuis CricketDataJson
    /// </summary>
    private CricketGameState ReplayCricketLeg(MatchSession session, List<Throw> legThrows)
    {
        // État indexé par côté (tableau partagé par la paire en double)
        var side1Key = Side1Key(session);
        var side2Key = Side2Key(session);
        var state = _cricketService.InitializeState(side1Key, side2Key);

        foreach (var legThrow in legThrows)
        {
            if (string.IsNullOrEmpty(legThrow.CricketDataJson))
                continue;

            var data = System.Text.Json.JsonSerializer.Deserialize<CricketThrowData>(legThrow.CricketDataJson);
            if (data?.Hits == null)
                continue;

            var throwerSideKey = SideKeyOf(session, legThrow.PlayerId);
            var opponentKey = throwerSideKey == side1Key ? side2Key : side1Key;
            _cricketService.ProcessTurn(state, throwerSideKey, opponentKey, data.Hits);
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

        // Construire l'état Cricket si applicable (indexé par côté)
        CricketDisplayState? cricketDisplayState = null;
        if (session.GameMode == GameMode.Cricket && session.CricketState != null)
        {
            cricketDisplayState = _cricketService.BuildDisplayState(
                session.CricketState,
                Side1Key(session),
                Side2Key(session)
            );
        }

        int startingSide = SideOf(session, session.StartingPlayerId);

        return new MatchSessionResponse(
            session.Id,
            session.MatchId,
            session.LegsToWin,
            session.GameMode,
            session.Status,
            new PlayerSessionInfo(
                Side1Key(session),
                SideName(session, 1),
                session.Player1LegsWon,
                session.Player1CurrentScore,
                startingSide == 1,
                SideMembers(session, 1)
            ),
            new PlayerSessionInfo(
                Side2Key(session),
                SideName(session, 2),
                session.Player2LegsWon,
                session.Player2CurrentScore,
                startingSide == 2,
                SideMembers(session, 2)
            ),
            session.CurrentPlayerId,
            session.CurrentLeg,
            currentLegThrows,
            session.CreatedAt,
            session.StartedAt,
            session.FinishedAt,
            session.TrackDoubles,
            cricketDisplayState,
            session.DoubleOut,
            IsDoubles(session),
            SideKeyOf(session, session.CurrentPlayerId),
            ThrowerName(session, session.CurrentPlayerId)
        );
    }

    /// <summary>
    /// Récupère toutes les sessions de match en cours pour l'écran TV
    /// </summary>
    public async Task<List<ActiveSessionSummaryResponse>> GetActiveSessionsAsync()
    {
        var sessions = await _context.MatchSessions
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Player2)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Tournament)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team1)
                .ThenInclude(tt => tt!.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team1)
                .ThenInclude(tt => tt!.Player2)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team2)
                .ThenInclude(tt => tt!.Player1)
            .Include(ms => ms.Match)
                .ThenInclude(m => m.Team2)
                .ThenInclude(tt => tt!.Player2)
            .Where(ms => ms.Status == MatchSessionStatus.InProgress)
            .OrderBy(ms => ms.StartedAt)
            .ToListAsync();

        return sessions.Select(s => new ActiveSessionSummaryResponse(
            s.MatchId,
            s.Match.Tournament?.Name ?? "Tournoi",
            SideName(s, 1),
            SideName(s, 2),
            s.Player1LegsWon,
            s.Player2LegsWon,
            s.LegsToWin,
            s.GameMode,
            s.CurrentLeg,
            s.StartedAt
        )).ToList();
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
            int winnerSide = SideOf(session, checkoutThrow.PlayerId);
            var winnerSideMembers = winnerSide == 1 ? Side1Order(session) : Side2Order(session);
            var legThrows = session.Throws.Where(t => t.LegNumber == checkoutThrow.LegNumber).ToList();
            var winnerThrows = legThrows.Where(t => winnerSideMembers.Contains(t.PlayerId)).ToList();
            var dartsThrown = winnerThrows.Count * 3; // Approximation
            var totalScored = winnerThrows.Sum(t => t.Score);
            var average = dartsThrown > 0 ? (double)totalScored / dartsThrown * 3 : 0;

            legsHistory.Add(new LegSummary(
                checkoutThrow.LegNumber,
                winnerSide == 1 ? Side1Key(session) : Side2Key(session),
                SideName(session, winnerSide),
                dartsThrown,
                Math.Round(average, 2)
            ));
        }

        // Construire l'état Cricket si applicable (indexé par côté)
        CricketDisplayState? cricketDisplayState = null;
        if (session.GameMode == GameMode.Cricket && session.CricketState != null)
        {
            cricketDisplayState = _cricketService.BuildDisplayState(
                session.CricketState,
                Side1Key(session),
                Side2Key(session)
            );
        }

        return new MatchSessionSpectatorResponse(
            session.MatchId,
            match.Tournament?.Name ?? "Unknown",
            session.LegsToWin,
            session.GameMode,
            session.Status,
            new PlayerSpectatorInfo(
                Side1Key(session),
                SideName(session, 1),
                session.Player1LegsWon,
                session.Player1CurrentScore,
                SideMembers(session, 1)
            ),
            new PlayerSpectatorInfo(
                Side2Key(session),
                SideName(session, 2),
                session.Player2LegsWon,
                session.Player2CurrentScore,
                SideMembers(session, 2)
            ),
            session.CurrentPlayerId,
            session.CurrentLeg,
            legsHistory,
            cricketDisplayState,
            IsDoubles(session),
            SideKeyOf(session, session.CurrentPlayerId),
            ThrowerName(session, session.CurrentPlayerId)
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

        // L'état Cricket est indexé par côté : la paire partage le tableau en double
        var currentSideKey = SideKeyOf(session, currentPlayerId);
        var opponentSideKey = currentSideKey == Side1Key(session) ? Side2Key(session) : Side1Key(session);

        // Traiter toute la visite
        var hitResults = _cricketService.ProcessTurn(
            cricketState,
            currentSideKey,
            opponentSideKey,
            request.Hits
        );

        // Sauvegarder l'état mis à jour
        session.CricketState = cricketState;

        // Mettre à jour les scores dans les champs CurrentScore
        session.Player1CurrentScore = cricketState.PlayerStates[Side1Key(session)].Score;
        session.Player2CurrentScore = cricketState.PlayerStates[Side2Key(session)].Score;

        // Calculer le numéro du throw
        var throwNumber = session.Throws
            .Count(t => t.LegNumber == session.CurrentLeg && t.PlayerId == currentPlayerId) + 1;

        // Volées déjà jouées dans ce leg (tous lanceurs), avant celle-ci
        var throwsInLegBefore = session.Throws.Count(t => t.LegNumber == session.CurrentLeg);

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

        // Vérifier si le côté a gagné le leg
        bool legWon = _cricketService.HasPlayerWonLeg(cricketState, currentSideKey, opponentSideKey);

        // Capturer le numéro du leg avant que HandleCricketLegWon ne l'incrémente
        var legNumberBeforeWin = session.CurrentLeg;
        var winnerSideName = SideName(session, SideOf(session, currentPlayerId));

        if (legWon)
        {
            throwEntity.IsCheckout = true;
            HandleCricketLegWon(session);
        }
        else
        {
            // Passer au lanceur suivant dans la rotation
            session.CurrentPlayerId = NextThrower(session, throwsInLegBefore + 1);
        }

        await _context.SaveChangesAsync();

        // Recharger la session avec toutes les relations
        var reloadedSession = (await GetSessionByIdAsync(sessionId))!;

        var displayState = _cricketService.BuildDisplayState(
            reloadedSession.CricketState!,
            Side1Key(reloadedSession),
            Side2Key(reloadedSession)
        );

        var response = new CricketTurnResponse(
            currentPlayerId,
            ThrowerName(reloadedSession, currentPlayerId),
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
                reloadedSession.CurrentPlayerId,
                SideKeyOf(reloadedSession, reloadedSession.CurrentPlayerId)
            ));

        // Si leg gagné, broadcaster l'événement (WinnerId = id du côté gagnant)
        if (legWon)
        {
            var winnerSideMembers = SideOf(reloadedSession, currentPlayerId) == 1
                ? Side1Order(reloadedSession)
                : Side2Order(reloadedSession);
            var legThrows = reloadedSession.Throws
                .Where(t => t.LegNumber == legNumberBeforeWin && winnerSideMembers.Contains(t.PlayerId))
                .ToList();
            var dartsThrown = legThrows.Count * 3; // Approximation pour Cricket

            var legSummary = new LegSummary(
                legNumberBeforeWin,
                currentSideKey,
                winnerSideName,
                dartsThrown,
                0 // Moyenne non calculée pour Cricket pour le moment
            );

            await _matchHub.Clients.Group($"match-{reloadedSession.MatchId}")
                .LegWon(new LegWonEvent(
                    reloadedSession.MatchId,
                    legNumberBeforeWin,
                    currentSideKey,
                    winnerSideName,
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
                        currentSideKey,
                        winnerSideName,
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
        // Incrémenter les legs gagnés du côté du lanceur
        if (SideOf(session, session.CurrentPlayerId) == 1)
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
            // Nouveau leg - réinitialiser l'état Cricket (indexé par côté)
            session.CurrentLeg++;
            var cricketState = _cricketService.InitializeState(
                Side1Key(session),
                Side2Key(session)
            );
            session.CricketState = cricketState;
            session.Player1CurrentScore = 0;
            session.Player2CurrentScore = 0;

            // Le côté qui commence alterne ; le leg repart sur son premier lanceur
            session.CurrentLegStartingPlayerId = GetLegStartingPlayerId(session, session.CurrentLeg);
            session.CurrentPlayerId = session.CurrentLegStartingPlayerId;
        }
    }

}
