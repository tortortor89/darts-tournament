using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.DTOs;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Services;

public class InterclubService
{
    private readonly AppDbContext _context;

    public InterclubService(AppDbContext context)
    {
        _context = context;
    }

    // ----- Championnats -----

    public async Task<List<ChampionshipResponse>> GetChampionshipsAsync()
    {
        var championships = await _context.InterclubChampionships
            .Include(c => c.Clubs)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return championships.Select(ToResponse).ToList();
    }

    public async Task<ChampionshipDetailResponse?> GetChampionshipAsync(int id)
    {
        var championship = await _context.InterclubChampionships
            .Include(c => c.Clubs)
            .ThenInclude(cc => cc.Club)
            .Include(c => c.Roster)
            .ThenInclude(r => r.Player)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (championship == null)
            return null;

        return new ChampionshipDetailResponse(
            championship.Id,
            championship.Name,
            championship.Status,
            championship.SinglesPerEncounter,
            championship.DoublesPerEncounter,
            championship.LegsToWin,
            championship.GameMode,
            championship.DoubleOut,
            championship.PointsForWin,
            championship.PointsForDraw,
            championship.PointsForLoss,
            championship.CreatedAt,
            championship.Clubs
                .OrderBy(cc => cc.Club.Name)
                .Select(cc => new ChampionshipClubResponse(
                    cc.ClubId,
                    cc.Club.Name,
                    championship.Roster
                        .Where(r => r.ClubId == cc.ClubId)
                        .OrderBy(r => r.Player.LastName)
                        .Select(r => new ClubPlayerResponse(r.PlayerId, $"{r.Player.FirstName} {r.Player.LastName}", r.Player.Nickname))
                        .ToList()))
                .ToList());
    }

    public async Task<ChampionshipResponse> CreateChampionshipAsync(CreateChampionshipRequest request)
    {
        if (request.SinglesPerEncounter + request.DoublesPerEncounter < 1)
            throw new InvalidOperationException("Une rencontre doit comporter au moins un match");

        var championship = new InterclubChampionship
        {
            Name = request.Name,
            SinglesPerEncounter = request.SinglesPerEncounter,
            DoublesPerEncounter = request.DoublesPerEncounter,
            LegsToWin = request.LegsToWin,
            GameMode = request.GameMode,
            DoubleOut = request.DoubleOut,
            PointsForWin = request.PointsForWin,
            PointsForDraw = request.PointsForDraw,
            PointsForLoss = request.PointsForLoss
        };

        _context.InterclubChampionships.Add(championship);
        await _context.SaveChangesAsync();

        return ToResponse(championship);
    }

    public async Task<bool> DeleteChampionshipAsync(int id)
    {
        var championship = await _context.InterclubChampionships.FindAsync(id);
        if (championship == null)
            return false;

        _context.InterclubChampionships.Remove(championship);
        await _context.SaveChangesAsync();
        return true;
    }

    // ----- Clubs engagés & effectifs -----

    public async Task AttachClubAsync(int championshipId, int clubId)
    {
        var championship = await _context.InterclubChampionships.FindAsync(championshipId)
            ?? throw new InvalidOperationException("Championnat introuvable");

        if (championship.Status != ChampionshipStatus.Draft)
            throw new InvalidOperationException("Impossible de modifier les clubs d'un championnat démarré");

        var clubExists = await _context.Clubs.AnyAsync(c => c.Id == clubId);
        if (!clubExists)
            throw new InvalidOperationException("Club introuvable");

        var already = await _context.ChampionshipClubs
            .AnyAsync(cc => cc.ChampionshipId == championshipId && cc.ClubId == clubId);
        if (already)
            throw new InvalidOperationException("Ce club est déjà engagé dans le championnat");

        _context.ChampionshipClubs.Add(new ChampionshipClub
        {
            ChampionshipId = championshipId,
            ClubId = clubId
        });
        await _context.SaveChangesAsync();
    }

    public async Task DetachClubAsync(int championshipId, int clubId)
    {
        var championship = await _context.InterclubChampionships.FindAsync(championshipId)
            ?? throw new InvalidOperationException("Championnat introuvable");

        if (championship.Status != ChampionshipStatus.Draft)
            throw new InvalidOperationException("Impossible de modifier les clubs d'un championnat démarré");

        var link = await _context.ChampionshipClubs
            .FirstOrDefaultAsync(cc => cc.ChampionshipId == championshipId && cc.ClubId == clubId)
            ?? throw new InvalidOperationException("Ce club n'est pas engagé dans le championnat");

        // Retirer aussi l'effectif déclaré du club
        var roster = await _context.ChampionshipRosterEntries
            .Where(r => r.ChampionshipId == championshipId && r.ClubId == clubId)
            .ToListAsync();
        _context.ChampionshipRosterEntries.RemoveRange(roster);

        _context.ChampionshipClubs.Remove(link);
        await _context.SaveChangesAsync();
    }

    public async Task SetRosterAsync(int championshipId, int clubId, List<int> playerIds)
    {
        var engaged = await _context.ChampionshipClubs
            .AnyAsync(cc => cc.ChampionshipId == championshipId && cc.ClubId == clubId);
        if (!engaged)
            throw new InvalidOperationException("Ce club n'est pas engagé dans le championnat");

        if (playerIds.Distinct().Count() != playerIds.Count)
            throw new InvalidOperationException("L'effectif contient des doublons");

        var validPlayerCount = await _context.Players.CountAsync(p => playerIds.Contains(p.Id));
        if (validPlayerCount != playerIds.Count)
            throw new InvalidOperationException("Un des joueurs est introuvable");

        // Un joueur ne peut jouer que pour un seul club par championnat
        var conflicts = await _context.ChampionshipRosterEntries
            .Include(r => r.Club)
            .Where(r => r.ChampionshipId == championshipId
                && r.ClubId != clubId
                && playerIds.Contains(r.PlayerId))
            .ToListAsync();
        if (conflicts.Count > 0)
            throw new InvalidOperationException(
                $"Joueur déjà déclaré dans l'effectif du club {conflicts[0].Club.Name} pour ce championnat");

        // Remplacement complet de l'effectif du club
        var existing = await _context.ChampionshipRosterEntries
            .Where(r => r.ChampionshipId == championshipId && r.ClubId == clubId)
            .ToListAsync();
        _context.ChampionshipRosterEntries.RemoveRange(existing);

        foreach (var playerId in playerIds)
        {
            _context.ChampionshipRosterEntries.Add(new ChampionshipRosterEntry
            {
                ChampionshipId = championshipId,
                ClubId = clubId,
                PlayerId = playerId
            });
        }

        await _context.SaveChangesAsync();
    }

    // ----- Calendrier -----

    public async Task GenerateCalendarAsync(int championshipId)
    {
        var championship = await _context.InterclubChampionships
            .Include(c => c.Clubs)
            .FirstOrDefaultAsync(c => c.Id == championshipId)
            ?? throw new InvalidOperationException("Championnat introuvable");

        if (championship.Status != ChampionshipStatus.Draft)
            throw new InvalidOperationException("Le calendrier a déjà été généré");

        var clubIds = championship.Clubs.Select(cc => cc.ClubId).ToList();
        var slots = InterclubCalendarCalculator.Generate(clubIds);

        foreach (var slot in slots)
        {
            _context.InterclubEncounters.Add(new InterclubEncounter
            {
                ChampionshipId = championshipId,
                Round = slot.Round,
                HomeClubId = slot.HomeClubId,
                AwayClubId = slot.AwayClubId
            });
        }

        championship.Status = ChampionshipStatus.InProgress;
        await _context.SaveChangesAsync();
    }

    public async Task<List<CalendarRoundResponse>?> GetCalendarAsync(int championshipId)
    {
        var exists = await _context.InterclubChampionships.AnyAsync(c => c.Id == championshipId);
        if (!exists)
            return null;

        var encounters = await _context.InterclubEncounters
            .Include(e => e.HomeClub)
            .Include(e => e.AwayClub)
            .Where(e => e.ChampionshipId == championshipId)
            .OrderBy(e => e.Round)
            .ThenBy(e => e.Id)
            .ToListAsync();

        return encounters
            .GroupBy(e => e.Round)
            .Select(g => new CalendarRoundResponse(
                g.Key,
                g.Select(ToSummary).ToList()))
            .ToList();
    }

    // ----- Classement -----

    public async Task<List<InterclubStandingResponse>?> GetStandingsAsync(int championshipId)
    {
        var championship = await _context.InterclubChampionships
            .Include(c => c.Clubs)
            .ThenInclude(cc => cc.Club)
            .FirstOrDefaultAsync(c => c.Id == championshipId);

        if (championship == null)
            return null;

        var completed = await _context.InterclubEncounters
            .Where(e => e.ChampionshipId == championshipId && e.Status == EncounterStatus.Completed)
            .Select(e => new EncounterResult(e.HomeClubId, e.AwayClubId, e.HomeScore, e.AwayScore))
            .ToListAsync();

        var clubs = championship.Clubs
            .Select(cc => (cc.ClubId, cc.Club.Name))
            .ToList();

        return InterclubStandingsCalculator
            .Compute(completed, clubs, championship.PointsForWin, championship.PointsForDraw, championship.PointsForLoss)
            .Select(r => new InterclubStandingResponse(
                r.ClubId, r.ClubName, r.Played, r.Wins, r.Draws, r.Losses,
                r.Points, r.MatchesWon, r.MatchesLost, r.Rank))
            .ToList();
    }

    // ----- Feuille de rencontre -----

    public async Task<EncounterDetailResponse?> GetEncounterAsync(int encounterId)
    {
        var encounter = await LoadEncounterAsync(encounterId);
        if (encounter == null)
            return null;

        var championship = encounter.Championship;
        int totalBoards = championship.SinglesPerEncounter + championship.DoublesPerEncounter;

        var roster = await _context.ChampionshipRosterEntries
            .Include(r => r.Player)
            .Where(r => r.ChampionshipId == encounter.ChampionshipId
                && (r.ClubId == encounter.HomeClubId || r.ClubId == encounter.AwayClubId))
            .ToListAsync();

        var boards = new List<EncounterBoardResponse>();
        for (int position = 1; position <= totalBoards; position++)
        {
            var match = encounter.Matches.FirstOrDefault(m => m.Position == position);
            boards.Add(new EncounterBoardResponse(
                position,
                position > championship.SinglesPerEncounter,
                match != null ? ToMatchResponse(match, encounter, championship) : null));
        }

        return new EncounterDetailResponse(
            encounter.Id,
            encounter.ChampionshipId,
            championship.Name,
            encounter.Round,
            encounter.HomeClubId,
            encounter.HomeClub.Name,
            encounter.AwayClubId,
            encounter.AwayClub.Name,
            encounter.ScheduledAt,
            encounter.Status,
            encounter.HomeScore,
            encounter.AwayScore,
            championship.SinglesPerEncounter,
            championship.DoublesPerEncounter,
            RosterOf(roster, encounter.HomeClubId),
            RosterOf(roster, encounter.AwayClubId),
            boards);
    }

    // ----- Composition -----

    public async Task SetLineupAsync(int encounterId, SetEncounterLineupRequest request)
    {
        var encounter = await LoadEncounterAsync(encounterId)
            ?? throw new InvalidOperationException("Rencontre introuvable");

        var championship = encounter.Championship;

        var rosterEntries = await _context.ChampionshipRosterEntries
            .Where(r => r.ChampionshipId == encounter.ChampionshipId)
            .ToListAsync();
        var homeRoster = rosterEntries.Where(r => r.ClubId == encounter.HomeClubId).Select(r => r.PlayerId).ToHashSet();
        var awayRoster = rosterEntries.Where(r => r.ClubId == encounter.AwayClubId).Select(r => r.PlayerId).ToHashSet();

        var boards = request.Boards
            .Select(b => new BoardLineupInput(b.Position, b.HomePlayerIds, b.AwayPlayerIds))
            .ToList();

        var errors = EncounterLineupValidator.Validate(
            boards, championship.SinglesPerEncounter, championship.DoublesPerEncounter, homeRoster, awayRoster);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ; ", errors));

        foreach (var board in request.Boards)
        {
            var existing = encounter.Matches.FirstOrDefault(m => m.Position == board.Position);

            if (existing != null && existing.Status != MatchStatus.Pending)
                throw new InvalidOperationException(
                    $"Le board {board.Position} a déjà été joué : composition non modifiable");

            bool isDoubleBoard = board.Position > championship.SinglesPerEncounter;

            if (existing == null)
            {
                existing = new Match
                {
                    EncounterId = encounter.Id,
                    Round = encounter.Round,
                    Position = board.Position
                };
                _context.Matches.Add(existing);
                encounter.Matches.Add(existing);
            }

            if (isDoubleBoard)
            {
                // Côté 1 = domicile, côté 2 = extérieur ; recomposer = réécrire les paires
                existing.Team1 = await UpsertEncounterPairAsync(existing.Team1Id, encounter.Id, board.HomePlayerIds);
                existing.Team1Id = existing.Team1.Id;
                existing.Team2 = await UpsertEncounterPairAsync(existing.Team2Id, encounter.Id, board.AwayPlayerIds);
                existing.Team2Id = existing.Team2.Id;
                existing.Player1Id = null;
                existing.Player2Id = null;
            }
            else
            {
                existing.Player1Id = board.HomePlayerIds[0];
                existing.Player2Id = board.AwayPlayerIds[0];
                // Nettoyer d'éventuelles paires si le board était mal configuré
                existing.Team1Id = null;
                existing.Team2Id = null;
            }
        }

        if (encounter.Status == EncounterStatus.Pending && encounter.Matches.Count > 0)
            encounter.Status = EncounterStatus.InProgress;

        await _context.SaveChangesAsync();
        await CleanOrphanPairsAsync(encounter.Id);
    }

    private async Task<TournamentTeam> UpsertEncounterPairAsync(int? existingTeamId, int encounterId, List<int> playerIds)
    {
        if (existingTeamId != null)
        {
            var team = await _context.TournamentTeams.FindAsync(existingTeamId.Value);
            if (team != null)
            {
                team.Player1Id = playerIds[0];
                team.Player2Id = playerIds[1];
                return team;
            }
        }

        var newTeam = new TournamentTeam
        {
            EncounterId = encounterId,
            Player1Id = playerIds[0],
            Player2Id = playerIds[1]
        };
        _context.TournamentTeams.Add(newTeam);
        await _context.SaveChangesAsync(); // besoin de l'Id pour la FK du match
        return newTeam;
    }

    private async Task CleanOrphanPairsAsync(int encounterId)
    {
        // Matérialiser avant le SelectMany : les tableaux inline ne sont pas
        // traduisibles en SQL par EF Core
        var matchTeams = await _context.Matches
            .Where(m => m.EncounterId == encounterId)
            .Select(m => new { m.Team1Id, m.Team2Id })
            .ToListAsync();

        var usedTeamIds = matchTeams
            .SelectMany(m => new[] { m.Team1Id, m.Team2Id })
            .Where(id => id != null)
            .Select(id => id!.Value)
            .ToHashSet();

        var orphans = await _context.TournamentTeams
            .Where(tt => tt.EncounterId == encounterId)
            .ToListAsync();
        orphans = orphans.Where(tt => !usedTeamIds.Contains(tt.Id)).ToList();

        if (orphans.Count > 0)
        {
            _context.TournamentTeams.RemoveRange(orphans);
            await _context.SaveChangesAsync();
        }
    }

    // ----- Scores -----

    /// <summary>
    /// Enregistre (ou corrige) le score d'un match de rencontre, puis recalcule
    /// le score de la rencontre et les statuts. Pas d'avancement de bracket :
    /// la correction est toujours permise.
    /// </summary>
    public async Task UpdateEncounterMatchScoreAsync(int matchId, int player1Score, int player2Score)
    {
        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException("Match not found");

        if (match.EncounterId == null)
            throw new InvalidOperationException("Ce match n'appartient pas à une rencontre interclubs");

        bool isDoubles = MatchSideAccessor.IsDoublesMatch(match);

        match.Player1Score = player1Score;
        match.Player2Score = player2Score;
        match.Status = MatchStatus.Completed;

        MatchSideAccessor.SetWinnerSideId(match, null, isDoubles);
        if (player1Score > player2Score)
            MatchSideAccessor.SetWinnerSideId(match, MatchSideAccessor.GetSide1Id(match, isDoubles), isDoubles);
        else if (player2Score > player1Score)
            MatchSideAccessor.SetWinnerSideId(match, MatchSideAccessor.GetSide2Id(match, isDoubles), isDoubles);

        await _context.SaveChangesAsync();
        await RecalculateEncounterAsync(match.EncounterId.Value);
    }

    /// <summary>
    /// Recalcule intégralement le score et le statut d'une rencontre à partir de
    /// ses matchs (idempotent), puis le statut du championnat.
    /// </summary>
    public async Task RecalculateEncounterAsync(int encounterId)
    {
        var encounter = await _context.InterclubEncounters
            .Include(e => e.Championship)
            .Include(e => e.Matches)
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException("Rencontre introuvable");

        int totalBoards = encounter.Championship.SinglesPerEncounter + encounter.Championship.DoublesPerEncounter;

        int homeWins = 0;
        int awayWins = 0;
        int completed = 0;

        foreach (var match in encounter.Matches.Where(m => m.Status == MatchStatus.Completed))
        {
            completed++;
            bool isDoubles = MatchSideAccessor.IsDoublesMatch(match);
            var winner = MatchSideAccessor.GetWinnerSideId(match, isDoubles);
            if (winner == null)
                continue;
            if (winner == MatchSideAccessor.GetSide1Id(match, isDoubles))
                homeWins++;
            else
                awayWins++;
        }

        encounter.HomeScore = homeWins;
        encounter.AwayScore = awayWins;
        encounter.Status = completed >= totalBoards
            ? EncounterStatus.Completed
            : (encounter.Matches.Count > 0 ? EncounterStatus.InProgress : EncounterStatus.Pending);

        // Statut du championnat (réversible : une correction peut rouvrir)
        var championship = encounter.Championship;
        if (championship.Status != ChampionshipStatus.Draft)
        {
            bool allCompleted = !await _context.InterclubEncounters
                .Where(e => e.ChampionshipId == championship.Id && e.Id != encounter.Id)
                .AnyAsync(e => e.Status != EncounterStatus.Completed)
                && encounter.Status == EncounterStatus.Completed;

            championship.Status = allCompleted ? ChampionshipStatus.Completed : ChampionshipStatus.InProgress;
        }

        await _context.SaveChangesAsync();
    }

    // ----- Helpers -----

    private async Task<InterclubEncounter?> LoadEncounterAsync(int encounterId)
    {
        return await _context.InterclubEncounters
            .Include(e => e.Championship)
            .Include(e => e.HomeClub)
            .Include(e => e.AwayClub)
            .Include(e => e.Matches)
            .ThenInclude(m => m.Player1)
            .Include(e => e.Matches)
            .ThenInclude(m => m.Player2)
            .Include(e => e.Matches)
            .ThenInclude(m => m.Team1)
            .ThenInclude(tt => tt!.Player1)
            .Include(e => e.Matches)
            .ThenInclude(m => m.Team1)
            .ThenInclude(tt => tt!.Player2)
            .Include(e => e.Matches)
            .ThenInclude(m => m.Team2)
            .ThenInclude(tt => tt!.Player1)
            .Include(e => e.Matches)
            .ThenInclude(m => m.Team2)
            .ThenInclude(tt => tt!.Player2)
            .FirstOrDefaultAsync(e => e.Id == encounterId);
    }

    private static ChampionshipResponse ToResponse(InterclubChampionship c) => new(
        c.Id, c.Name, c.Status,
        c.SinglesPerEncounter, c.DoublesPerEncounter, c.LegsToWin, c.GameMode, c.DoubleOut,
        c.PointsForWin, c.PointsForDraw, c.PointsForLoss,
        c.CreatedAt, c.Clubs.Count);

    private static EncounterSummaryResponse ToSummary(InterclubEncounter e) => new(
        e.Id, e.Round,
        e.HomeClubId, e.HomeClub.Name,
        e.AwayClubId, e.AwayClub.Name,
        e.ScheduledAt, e.Status, e.HomeScore, e.AwayScore);

    private static List<ClubPlayerResponse> RosterOf(List<ChampionshipRosterEntry> roster, int clubId) =>
        roster.Where(r => r.ClubId == clubId)
            .OrderBy(r => r.Player.LastName)
            .Select(r => new ClubPlayerResponse(r.PlayerId, $"{r.Player.FirstName} {r.Player.LastName}", r.Player.Nickname))
            .ToList();

    // MatchResponse d'un match de rencontre : aliasing par match (cf. MatchDTOs)
    private static MatchResponse ToMatchResponse(Match m, InterclubEncounter encounter, InterclubChampionship championship)
    {
        bool isDoubles = MatchSideAccessor.IsDoublesMatch(m);

        return new MatchResponse(
            m.Id,
            null,
            null,
            m.Round,
            m.Position,
            isDoubles ? m.Team1Id : m.Player1Id,
            isDoubles
                ? m.Team1?.Name
                : (m.Player1 != null ? $"{m.Player1.FirstName} {m.Player1.LastName}" : null),
            isDoubles ? m.Team2Id : m.Player2Id,
            isDoubles
                ? m.Team2?.Name
                : (m.Player2 != null ? $"{m.Player2.FirstName} {m.Player2.LastName}" : null),
            m.Player1Score,
            m.Player2Score,
            isDoubles ? m.WinnerTeamId : m.WinnerId,
            m.Status,
            m.ScheduledAt,
            false,
            m.BracketType,
            m.IsBracketReset,
            isDoubles,
            isDoubles && m.Team1 != null
                ? new List<TeamMemberInfo>
                {
                    new(m.Team1.Player1Id, $"{m.Team1.Player1.FirstName} {m.Team1.Player1.LastName}"),
                    new(m.Team1.Player2Id, $"{m.Team1.Player2.FirstName} {m.Team1.Player2.LastName}")
                }
                : null,
            isDoubles && m.Team2 != null
                ? new List<TeamMemberInfo>
                {
                    new(m.Team2.Player1Id, $"{m.Team2.Player1.FirstName} {m.Team2.Player1.LastName}"),
                    new(m.Team2.Player2Id, $"{m.Team2.Player2.FirstName} {m.Team2.Player2.LastName}")
                }
                : null,
            encounter.Id,
            $"{encounter.HomeClub.Name} vs {encounter.AwayClub.Name}",
            championship.LegsToWin,
            championship.GameMode,
            championship.DoubleOut
        );
    }
}
