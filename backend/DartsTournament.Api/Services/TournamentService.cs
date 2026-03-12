using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Data;
using DartsTournament.Api.Models;
using DartsTournament.Api.DTOs;

namespace DartsTournament.Api.Services;

public class TournamentService
{
    private readonly AppDbContext _context;

    public TournamentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task GenerateBracketAsync(int tournamentId)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.TournamentPlayers)
            .ThenInclude(tp => tp.Player)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null)
            throw new InvalidOperationException("Tournament not found");

        if (tournament.Status != TournamentStatus.Draft)
            throw new InvalidOperationException("Tournament has already started");

        var players = tournament.TournamentPlayers
            .OrderBy(tp => tp.Seed ?? int.MaxValue)
            .Select(tp => tp.Player)
            .ToList();

        if (players.Count < 2)
            throw new InvalidOperationException("At least 2 players required");

        // Clear existing matches
        var existingMatches = await _context.Matches
            .Where(m => m.TournamentId == tournamentId)
            .ToListAsync();
        _context.Matches.RemoveRange(existingMatches);

        switch (tournament.Format)
        {
            case TournamentFormat.SingleElimination:
                GenerateSingleEliminationBracket(tournament, players);
                break;
            case TournamentFormat.RoundRobin:
                GenerateRoundRobinBracket(tournament, players);
                break;
            case TournamentFormat.GroupStage:
                await GenerateGroupStageBracketAsync(tournament, players);
                break;
        }

        tournament.Status = TournamentStatus.InProgress;
        await _context.SaveChangesAsync();
    }

    private void GenerateSingleEliminationBracket(Tournament tournament, List<Player> players, bool isKnockout = false, int roundOffset = 0)
    {
        int playerCount = players.Count;
        int rounds = (int)Math.Ceiling(Math.Log2(playerCount));
        int bracketSize = (int)Math.Pow(2, rounds);
        int byes = bracketSize - playerCount;

        var matches = new List<Match>();

        // Calculate position offset for knockout matches
        int positionOffset = isKnockout ? _context.Matches.Local.Count(m => m.TournamentId == tournament.Id) : 0;
        int position = positionOffset;

        // First round
        int matchesInFirstRound = bracketSize / 2;
        int playerIndex = 0;

        for (int i = 0; i < matchesInFirstRound; i++)
        {
            var match = new Match
            {
                TournamentId = tournament.Id,
                Round = 1 + roundOffset,
                Position = position++,
                IsKnockoutMatch = isKnockout
            };

            if (playerIndex < players.Count)
            {
                match.Player1Id = players[playerIndex++].Id;
            }

            if (i >= byes && playerIndex < players.Count)
            {
                match.Player2Id = players[playerIndex++].Id;
            }
            else if (match.Player1Id != null)
            {
                // Bye - player advances automatically
                match.WinnerId = match.Player1Id;
                match.Status = MatchStatus.Completed;
            }

            matches.Add(match);
        }

        // Subsequent rounds
        for (int round = 2; round <= rounds; round++)
        {
            int matchesInRound = bracketSize / (int)Math.Pow(2, round);
            for (int i = 0; i < matchesInRound; i++)
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Round = round + roundOffset,
                    Position = position++,
                    IsKnockoutMatch = isKnockout
                });
            }
        }

        _context.Matches.AddRange(matches);
    }

    private void GenerateRoundRobinBracket(Tournament tournament, List<Player> players)
    {
        var matches = new List<Match>();
        int round = 1;
        int position = 0;

        for (int i = 0; i < players.Count; i++)
        {
            for (int j = i + 1; j < players.Count; j++)
            {
                matches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Round = round,
                    Position = position++,
                    Player1Id = players[i].Id,
                    Player2Id = players[j].Id
                });
            }
        }

        _context.Matches.AddRange(matches);
    }

    private async Task GenerateGroupStageBracketAsync(Tournament tournament, List<Player> players)
    {
        // Clear existing groups
        var existingGroups = await _context.Groups
            .Where(g => g.TournamentId == tournament.Id)
            .ToListAsync();
        _context.Groups.RemoveRange(existingGroups);

        // Calculate number of groups
        int groupCount;
        if (tournament.NumberOfGroups.HasValue)
        {
            groupCount = tournament.NumberOfGroups.Value;
        }
        else if (tournament.PlayersPerGroup.HasValue)
        {
            groupCount = (int)Math.Ceiling((double)players.Count / tournament.PlayersPerGroup.Value);
        }
        else
        {
            // Default: aim for groups of 4
            groupCount = Math.Max(2, players.Count / 4);
        }

        // Don't create more groups than players
        groupCount = Math.Min(groupCount, players.Count);

        var groups = new List<Group>();
        for (int i = 0; i < groupCount; i++)
        {
            groups.Add(new Group
            {
                TournamentId = tournament.Id,
                Name = $"Groupe {(char)('A' + i)}"
            });
        }

        _context.Groups.AddRange(groups);
        await _context.SaveChangesAsync();

        // Assign players to groups using snake draft (respects seeding)
        // Snake: Group A, B, C, D, D, C, B, A, A, B, C, D...
        for (int i = 0; i < players.Count; i++)
        {
            int row = i / groupCount;
            int col = i % groupCount;
            int groupIndex = row % 2 == 0 ? col : groupCount - 1 - col;

            var tournamentPlayer = tournament.TournamentPlayers
                .First(tp => tp.PlayerId == players[i].Id);
            tournamentPlayer.GroupId = groups[groupIndex].Id;
        }

        // Generate matches for each group (round robin within groups)
        var matches = new List<Match>();
        int position = 0;

        foreach (var group in groups)
        {
            var groupPlayers = tournament.TournamentPlayers
                .Where(tp => tp.GroupId == group.Id)
                .Select(tp => tp.Player)
                .ToList();

            for (int i = 0; i < groupPlayers.Count; i++)
            {
                for (int j = i + 1; j < groupPlayers.Count; j++)
                {
                    matches.Add(new Match
                    {
                        TournamentId = tournament.Id,
                        GroupId = group.Id,
                        Round = 1,
                        Position = position++,
                        Player1Id = groupPlayers[i].Id,
                        Player2Id = groupPlayers[j].Id,
                        IsKnockoutMatch = false
                    });
                }
            }
        }

        _context.Matches.AddRange(matches);
    }

    public async Task<List<GroupStandingResponse>> GetGroupStandingsAsync(int tournamentId)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Groups)
            .Include(t => t.Matches)
            .Include(t => t.TournamentPlayers)
            .ThenInclude(tp => tp.Player)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null)
            throw new InvalidOperationException("Tournament not found");

        var result = new List<GroupStandingResponse>();

        foreach (var group in tournament.Groups.OrderBy(g => g.Name))
        {
            var groupPlayers = tournament.TournamentPlayers
                .Where(tp => tp.GroupId == group.Id)
                .ToList();

            var groupMatches = tournament.Matches
                .Where(m => m.GroupId == group.Id && m.Status == MatchStatus.Completed)
                .ToList();

            var standings = new List<PlayerStandingResponse>();

            foreach (var tp in groupPlayers)
            {
                var playerMatches = groupMatches
                    .Where(m => m.Player1Id == tp.PlayerId || m.Player2Id == tp.PlayerId)
                    .ToList();

                int played = playerMatches.Count;
                int won = playerMatches.Count(m => m.WinnerId == tp.PlayerId);
                int lost = played - won;

                int pointsFor = playerMatches.Sum(m =>
                    m.Player1Id == tp.PlayerId ? (m.Player1Score ?? 0) : (m.Player2Score ?? 0));
                int pointsAgainst = playerMatches.Sum(m =>
                    m.Player1Id == tp.PlayerId ? (m.Player2Score ?? 0) : (m.Player1Score ?? 0));

                int points = won * 3; // 3 points per win

                standings.Add(new PlayerStandingResponse(
                    tp.PlayerId,
                    $"{tp.Player.FirstName} {tp.Player.LastName}",
                    played,
                    won,
                    lost,
                    pointsFor,
                    pointsAgainst,
                    pointsFor - pointsAgainst,
                    points,
                    0 // Rank will be set after sorting
                ));
            }

            // Sort by points, then point difference, then points for
            var sortedStandings = standings
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.PointsDiff)
                .ThenByDescending(s => s.PointsFor)
                .Select((s, index) => new PlayerStandingResponse(
                    s.PlayerId,
                    s.PlayerName,
                    s.Played,
                    s.Won,
                    s.Lost,
                    s.PointsFor,
                    s.PointsAgainst,
                    s.PointsDiff,
                    s.Points,
                    index + 1
                ))
                .ToList();

            result.Add(new GroupStandingResponse(group.Id, group.Name, sortedStandings));
        }

        return result;
    }

    public async Task GenerateKnockoutPhaseAsync(int tournamentId)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Groups)
            .Include(t => t.TournamentPlayers)
            .ThenInclude(tp => tp.Player)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null)
            throw new InvalidOperationException("Tournament not found");

        if (!tournament.HasKnockoutPhase)
            return;

        int qualifiersPerGroup = tournament.QualifiersPerGroup ?? 2;

        // Get standings to determine qualifiers
        var standings = await GetGroupStandingsAsync(tournamentId);

        // Collect qualified players from each group
        var qualifiedPlayers = new List<(Player Player, int GroupIndex, int Rank)>();
        int groupIndex = 0;

        foreach (var groupStanding in standings.OrderBy(g => g.GroupName))
        {
            var qualifiers = groupStanding.Standings
                .Where(s => s.Rank <= qualifiersPerGroup)
                .ToList();

            foreach (var qualifier in qualifiers)
            {
                var player = tournament.TournamentPlayers
                    .First(tp => tp.PlayerId == qualifier.PlayerId)
                    .Player;
                qualifiedPlayers.Add((player, groupIndex, qualifier.Rank));
            }
            groupIndex++;
        }

        if (qualifiedPlayers.Count < 2)
            throw new InvalidOperationException("Not enough qualified players for knockout phase");

        // Arrange players for knockout bracket
        // First place teams should not meet each other until later rounds
        // Standard arrangement: 1A vs 2B, 1B vs 2A, etc.
        var arrangedPlayers = ArrangePlayersForKnockout(qualifiedPlayers, standings.Count);

        // Calculate round offset (group stage uses round 1)
        int roundOffset = 1;

        // Generate knockout bracket
        GenerateSingleEliminationBracket(tournament, arrangedPlayers, isKnockout: true, roundOffset: roundOffset);

        await _context.SaveChangesAsync();
    }

    private List<Player> ArrangePlayersForKnockout(List<(Player Player, int GroupIndex, int Rank)> qualifiedPlayers, int groupCount)
    {
        // Group players by rank
        var byRank = qualifiedPlayers
            .GroupBy(p => p.Rank)
            .OrderBy(g => g.Key)
            .ToList();

        var arranged = new List<Player>();
        int bracketSize = (int)Math.Pow(2, Math.Ceiling(Math.Log2(qualifiedPlayers.Count)));

        // Simple arrangement: alternate between groups for same-ranked players
        // This ensures players from the same group don't meet in early rounds
        if (byRank.Count >= 2 && groupCount >= 2)
        {
            var firstPlace = byRank[0].OrderBy(p => p.GroupIndex).ToList();
            var secondPlace = byRank.Count > 1 ? byRank[1].OrderBy(p => p.GroupIndex).ToList() : new List<(Player Player, int GroupIndex, int Rank)>();

            // Pair 1st from group A with 2nd from group B, etc.
            for (int i = 0; i < firstPlace.Count; i++)
            {
                arranged.Add(firstPlace[i].Player);
                int oppositeIndex = (firstPlace.Count - 1 - i) % secondPlace.Count;
                if (oppositeIndex < secondPlace.Count)
                {
                    arranged.Add(secondPlace[oppositeIndex].Player);
                }
            }

            // Add remaining players
            var addedIds = arranged.Select(p => p.Id).ToHashSet();
            foreach (var group in byRank.Skip(2))
            {
                foreach (var p in group)
                {
                    if (!addedIds.Contains(p.Player.Id))
                    {
                        arranged.Add(p.Player);
                        addedIds.Add(p.Player.Id);
                    }
                }
            }
        }
        else
        {
            // Fallback: just use all players in rank order
            arranged = qualifiedPlayers
                .OrderBy(p => p.Rank)
                .ThenBy(p => p.GroupIndex)
                .Select(p => p.Player)
                .ToList();
        }

        return arranged;
    }

    public async Task UpdateMatchScoreAsync(int matchId, int player1Score, int player2Score)
    {
        var match = await _context.Matches
            .Include(m => m.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
            throw new InvalidOperationException("Match not found");

        match.Player1Score = player1Score;
        match.Player2Score = player2Score;
        match.Status = MatchStatus.Completed;

        if (player1Score > player2Score)
            match.WinnerId = match.Player1Id;
        else if (player2Score > player1Score)
            match.WinnerId = match.Player2Id;

        var tournament = match.Tournament;

        // For single elimination or knockout matches, advance winner to next match
        if ((tournament.Format == TournamentFormat.SingleElimination || match.IsKnockoutMatch)
            && match.WinnerId != null)
        {
            await AdvanceWinnerAsync(match);
        }

        await _context.SaveChangesAsync();

        // For group stage, check if all group matches are done
        if (tournament.Format == TournamentFormat.GroupStage && !match.IsKnockoutMatch)
        {
            await CheckAndGenerateKnockoutAsync(match.TournamentId);
        }

        // Check if tournament is completed
        await CheckTournamentCompletionAsync(match.TournamentId);
    }

    private async Task CheckAndGenerateKnockoutAsync(int tournamentId)
    {
        var tournament = await _context.Tournaments.FindAsync(tournamentId);
        if (tournament == null || !tournament.HasKnockoutPhase)
            return;

        // Check if knockout matches already exist
        var knockoutMatchesExist = await _context.Matches
            .AnyAsync(m => m.TournamentId == tournamentId && m.IsKnockoutMatch);

        if (knockoutMatchesExist)
            return;

        // Check if all group matches are completed
        var allGroupMatchesCompleted = await _context.Matches
            .Where(m => m.TournamentId == tournamentId && !m.IsKnockoutMatch)
            .AllAsync(m => m.Status == MatchStatus.Completed);

        if (allGroupMatchesCompleted)
        {
            await GenerateKnockoutPhaseAsync(tournamentId);
        }
    }

    private async Task CheckTournamentCompletionAsync(int tournamentId)
    {
        var allMatchesCompleted = await _context.Matches
            .Where(m => m.TournamentId == tournamentId)
            .AllAsync(m => m.Status == MatchStatus.Completed);

        if (allMatchesCompleted)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament != null)
            {
                tournament.Status = TournamentStatus.Completed;
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task AdvanceWinnerAsync(Match completedMatch)
    {
        // Get all matches in the next round for the same phase (knockout or group)
        var nextRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == completedMatch.TournamentId
                && m.Round == completedMatch.Round + 1
                && m.IsKnockoutMatch == completedMatch.IsKnockoutMatch)
            .OrderBy(m => m.Position)
            .ToListAsync();

        if (!nextRoundMatches.Any())
            return;

        // Get all matches in current round to calculate proper index
        var currentRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == completedMatch.TournamentId
                && m.Round == completedMatch.Round
                && m.IsKnockoutMatch == completedMatch.IsKnockoutMatch)
            .OrderBy(m => m.Position)
            .ToListAsync();

        int matchIndexInRound = currentRoundMatches.FindIndex(m => m.Id == completedMatch.Id);
        int nextMatchIndex = matchIndexInRound / 2;

        if (nextMatchIndex < nextRoundMatches.Count)
        {
            var nextMatch = nextRoundMatches[nextMatchIndex];
            if (matchIndexInRound % 2 == 0)
                nextMatch.Player1Id = completedMatch.WinnerId;
            else
                nextMatch.Player2Id = completedMatch.WinnerId;
        }
    }
}
