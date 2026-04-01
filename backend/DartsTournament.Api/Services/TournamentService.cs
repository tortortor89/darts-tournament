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
            case TournamentFormat.DoubleElimination:
                GenerateDoubleEliminationBracket(tournament, players);
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

        var matches = new List<Match>();

        // Calculate position offset for knockout matches
        int positionOffset = isKnockout ? _context.Matches.Local.Count(m => m.TournamentId == tournament.Id) : 0;
        int matchPosition = positionOffset;

        // Get bracket positions for proper seeding
        var bracketPositions = GenerateBracketPositions(bracketSize);

        // Create position-to-player mapping
        // Seeds 1 to playerCount have players, rest are byes
        var positionToPlayer = new int?[bracketSize];
        for (int seed = 1; seed <= playerCount; seed++)
        {
            int pos = bracketPositions[seed - 1];
            positionToPlayer[pos] = players[seed - 1].Id;
        }

        // First round - create matches based on bracket positions
        int matchesInFirstRound = bracketSize / 2;

        for (int i = 0; i < matchesInFirstRound; i++)
        {
            int pos1 = i * 2;      // Even positions: 0, 2, 4, 6...
            int pos2 = i * 2 + 1;  // Odd positions: 1, 3, 5, 7...

            var match = new Match
            {
                TournamentId = tournament.Id,
                Round = 1 + roundOffset,
                Position = matchPosition++,
                IsKnockoutMatch = isKnockout,
                Player1Id = positionToPlayer[pos1],
                Player2Id = positionToPlayer[pos2]
            };

            // Handle byes - if one player is null, the other wins automatically
            if (match.Player1Id != null && match.Player2Id == null)
            {
                match.WinnerId = match.Player1Id;
                match.Status = MatchStatus.Completed;
            }
            else if (match.Player2Id != null && match.Player1Id == null)
            {
                match.WinnerId = match.Player2Id;
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
                    Position = matchPosition++,
                    IsKnockoutMatch = isKnockout
                });
            }
        }

        // Advance bye winners to next round
        var firstRoundMatches = matches.Where(m => m.Round == 1 + roundOffset).OrderBy(m => m.Position).ToList();
        var secondRoundMatches = matches.Where(m => m.Round == 2 + roundOffset).OrderBy(m => m.Position).ToList();

        for (int i = 0; i < firstRoundMatches.Count; i++)
        {
            var match = firstRoundMatches[i];
            if (match.WinnerId != null && match.Status == MatchStatus.Completed)
            {
                // This is a bye match, advance winner to next round
                int nextMatchIndex = i / 2;
                if (nextMatchIndex < secondRoundMatches.Count)
                {
                    var nextMatch = secondRoundMatches[nextMatchIndex];
                    if (i % 2 == 0)
                        nextMatch.Player1Id = match.WinnerId;
                    else
                        nextMatch.Player2Id = match.WinnerId;
                }
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

    private void GenerateDoubleEliminationBracket(Tournament tournament, List<Player> players)
    {
        int playerCount = players.Count;
        int winnersRounds = (int)Math.Ceiling(Math.Log2(playerCount));
        int bracketSize = (int)Math.Pow(2, winnersRounds);

        var allMatches = new List<Match>();
        int matchPosition = 0;

        // Get bracket positions for proper seeding
        var bracketPositions = GenerateBracketPositions(bracketSize);

        // Create position-to-player mapping
        var positionToPlayer = new int?[bracketSize];
        for (int seed = 1; seed <= playerCount; seed++)
        {
            int pos = bracketPositions[seed - 1];
            positionToPlayer[pos] = players[seed - 1].Id;
        }

        // ========== WINNER'S BRACKET ==========
        var winnersMatches = new List<Match>();

        // Winner's bracket first round
        int matchesInFirstRound = bracketSize / 2;
        for (int i = 0; i < matchesInFirstRound; i++)
        {
            int pos1 = i * 2;
            int pos2 = i * 2 + 1;

            var match = new Match
            {
                TournamentId = tournament.Id,
                Round = 1,
                Position = matchPosition++,
                BracketType = BracketType.Winners,
                Player1Id = positionToPlayer[pos1],
                Player2Id = positionToPlayer[pos2]
            };

            // Handle byes
            if (match.Player1Id != null && match.Player2Id == null)
            {
                match.WinnerId = match.Player1Id;
                match.Status = MatchStatus.Completed;
            }
            else if (match.Player2Id != null && match.Player1Id == null)
            {
                match.WinnerId = match.Player2Id;
                match.Status = MatchStatus.Completed;
            }

            winnersMatches.Add(match);
        }

        // Winner's bracket subsequent rounds
        for (int round = 2; round <= winnersRounds; round++)
        {
            int matchesInRound = bracketSize / (int)Math.Pow(2, round);
            for (int i = 0; i < matchesInRound; i++)
            {
                winnersMatches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Round = round,
                    Position = matchPosition++,
                    BracketType = BracketType.Winners
                });
            }
        }

        // Advance bye winners in winner's bracket
        var winnersFirstRound = winnersMatches.Where(m => m.Round == 1).OrderBy(m => m.Position).ToList();
        var winnersSecondRound = winnersMatches.Where(m => m.Round == 2).OrderBy(m => m.Position).ToList();

        for (int i = 0; i < winnersFirstRound.Count; i++)
        {
            var match = winnersFirstRound[i];
            if (match.WinnerId != null && match.Status == MatchStatus.Completed)
            {
                int nextMatchIndex = i / 2;
                if (nextMatchIndex < winnersSecondRound.Count)
                {
                    var nextMatch = winnersSecondRound[nextMatchIndex];
                    if (i % 2 == 0)
                        nextMatch.Player1Id = match.WinnerId;
                    else
                        nextMatch.Player2Id = match.WinnerId;
                }
            }
        }

        allMatches.AddRange(winnersMatches);

        // ========== LOSER'S BRACKET ==========
        // The loser's bracket structure:
        // - For each winner's round R (except final), losers drop to loser's bracket
        // - Loser's rounds alternate: drop-down round, then consolidation round
        // Total loser's rounds = 2 * (winnersRounds - 1)

        var losersMatches = new List<Match>();
        int losersRoundCount = 2 * (winnersRounds - 1);

        // Calculate matches per loser's round
        // LR1: bracketSize/4 matches (losers from WR1 pair up)
        // LR2: bracketSize/4 matches (LR1 winners vs WR2 losers)
        // LR3: bracketSize/8 matches (LR2 winners pair up)
        // LR4: bracketSize/8 matches (LR3 winners vs WR3 losers)
        // etc.

        int currentLosersCount = bracketSize / 2; // Losers from first winner's round

        for (int losersRound = 1; losersRound <= losersRoundCount; losersRound++)
        {
            int matchesInRound;

            if (losersRound % 2 == 1)
            {
                // Odd rounds: consolidation (internal matches)
                matchesInRound = currentLosersCount / 2;
            }
            else
            {
                // Even rounds: drop-down + consolidation
                // Winners from previous loser's round face new losers from winner's bracket
                matchesInRound = currentLosersCount / 2;
                currentLosersCount = matchesInRound; // Update for next pair of rounds
            }

            for (int i = 0; i < matchesInRound; i++)
            {
                losersMatches.Add(new Match
                {
                    TournamentId = tournament.Id,
                    Round = losersRound,
                    Position = matchPosition++,
                    BracketType = BracketType.Losers
                });
            }
        }

        allMatches.AddRange(losersMatches);

        // ========== HANDLE DOUBLE BYES IN LOSER'S BRACKET ==========
        // When both paired Winners R1 matches are byes, the corresponding LR1 match has no players
        var losersRound1 = losersMatches.Where(m => m.Round == 1).OrderBy(m => m.Position).ToList();

        for (int i = 0; i < losersRound1.Count && i * 2 + 1 < winnersFirstRound.Count; i++)
        {
            var wr1Match1 = winnersFirstRound[i * 2];
            var wr1Match2 = winnersFirstRound[i * 2 + 1];

            bool match1IsBye = (wr1Match1.Player1Id == null || wr1Match1.Player2Id == null);
            bool match2IsBye = (wr1Match2.Player1Id == null || wr1Match2.Player2Id == null);

            if (match1IsBye && match2IsBye)
            {
                // Both were byes - no losers will come, mark LR1 match as completed (double bye)
                var lr1Match = losersRound1[i];
                lr1Match.Status = MatchStatus.Completed;
            }
        }

        // ========== GRAND FINAL ==========
        // Grand Final 1: Winner's bracket champion vs Loser's bracket champion
        allMatches.Add(new Match
        {
            TournamentId = tournament.Id,
            Round = 100,
            Position = matchPosition++,
            BracketType = BracketType.GrandFinal,
            IsBracketReset = false
        });

        // Grand Final 2 (bracket reset): Only played if loser's bracket champion wins GF1
        if (tournament.AllowBracketReset)
        {
            allMatches.Add(new Match
            {
                TournamentId = tournament.Id,
                Round = 101,
                Position = matchPosition++,
                BracketType = BracketType.GrandFinal,
                IsBracketReset = true
            });
        }

        _context.Matches.AddRange(allMatches);
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

    public async Task<List<GroupStandingResponse>> GetStandingsAsync(int tournamentId)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.Groups)
            .Include(t => t.Matches)
            .Include(t => t.TournamentPlayers)
            .ThenInclude(tp => tp.Player)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null)
            throw new InvalidOperationException("Tournament not found");

        // Route vers la bonne méthode selon le format
        if (tournament.Format == TournamentFormat.RoundRobin)
        {
            return GetRoundRobinStandings(tournament);
        }
        else if (tournament.Format == TournamentFormat.GroupStage)
        {
            return GetGroupStageStandings(tournament);
        }
        else if (tournament.Format == TournamentFormat.DoubleElimination)
        {
            return GetDoubleEliminationStandings(tournament);
        }

        return new List<GroupStandingResponse>();
    }

    private List<GroupStandingResponse> GetRoundRobinStandings(Tournament tournament)
    {
        var completedMatches = tournament.Matches
            .Where(m => m.Status == MatchStatus.Completed)
            .ToList();

        var standings = new List<PlayerStandingResponse>();

        foreach (var tp in tournament.TournamentPlayers)
        {
            var playerMatches = completedMatches
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

        // Return as a single "group" for consistency with frontend
        return new List<GroupStandingResponse>
        {
            new GroupStandingResponse(0, "Classement", sortedStandings)
        };
    }

    private List<GroupStandingResponse> GetGroupStageStandings(Tournament tournament)
    {
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

    private List<GroupStandingResponse> GetDoubleEliminationStandings(Tournament tournament)
    {
        var completedMatches = tournament.Matches
            .Where(m => m.Status == MatchStatus.Completed)
            .ToList();

        if (!completedMatches.Any())
        {
            return new List<GroupStandingResponse>
            {
                new GroupStandingResponse(0, "Classement", new List<PlayerStandingResponse>())
            };
        }

        var standings = new List<(int PlayerId, string PlayerName, int Won, int Lost, int EliminationRound, BracketType EliminationBracket, bool IsChampion)>();

        // Find Grand Final matches
        var grandFinalMatches = completedMatches
            .Where(m => m.BracketType == BracketType.GrandFinal)
            .OrderByDescending(m => m.Round)
            .ToList();

        int? championId = null;
        int? runnerUpId = null;

        // Determine champion and runner-up from Grand Final
        if (grandFinalMatches.Any())
        {
            var finalMatch = grandFinalMatches.First(); // Last GF match (could be bracket reset)
            championId = finalMatch.WinnerId;

            // Runner-up is the loser of the final match
            runnerUpId = finalMatch.Player1Id == championId ? finalMatch.Player2Id : finalMatch.Player1Id;
        }

        // For each player, find their elimination point
        foreach (var tp in tournament.TournamentPlayers)
        {
            var playerId = tp.PlayerId;
            var playerName = $"{tp.Player.FirstName} {tp.Player.LastName}";

            // Count wins and losses
            var playerMatches = completedMatches
                .Where(m => m.Player1Id == playerId || m.Player2Id == playerId)
                .ToList();

            int won = playerMatches.Count(m => m.WinnerId == playerId);
            int lost = playerMatches.Count(m => m.WinnerId != playerId && m.WinnerId != null);

            // Find elimination match (where they lost and were eliminated)
            // In Double Elim: eliminated when losing in Losers bracket or Grand Final
            var eliminationMatch = playerMatches
                .Where(m => m.WinnerId != playerId && m.WinnerId != null)
                .Where(m => m.BracketType == BracketType.Losers || m.BracketType == BracketType.GrandFinal)
                .OrderByDescending(m => m.BracketType == BracketType.GrandFinal ? 1000 : 0)
                .ThenByDescending(m => m.Round)
                .FirstOrDefault();

            bool isChampion = playerId == championId;
            int eliminationRound = 0;
            BracketType eliminationBracket = BracketType.None;

            if (eliminationMatch != null)
            {
                eliminationRound = eliminationMatch.Round;
                eliminationBracket = eliminationMatch.BracketType;
            }
            else if (isChampion)
            {
                // Champion wasn't eliminated
                eliminationRound = int.MaxValue;
                eliminationBracket = BracketType.GrandFinal;
            }
            else if (playerMatches.Count == 0)
            {
                // Player hasn't played yet
                eliminationRound = -1;
            }

            standings.Add((playerId, playerName, won, lost, eliminationRound, eliminationBracket, isChampion));
        }

        // Sort: Champion first, then by elimination point (later = better)
        // GrandFinal > Losers (higher round better)
        var sortedStandings = standings
            .OrderByDescending(s => s.IsChampion)
            .ThenByDescending(s => s.EliminationBracket == BracketType.GrandFinal ? 1 : 0)
            .ThenByDescending(s => s.EliminationRound)
            .ThenByDescending(s => s.Won)
            .Select((s, index) => new PlayerStandingResponse(
                s.PlayerId,
                s.PlayerName,
                s.Won + s.Lost, // Played
                s.Won,
                s.Lost,
                0, // PointsFor not applicable
                0, // PointsAgainst not applicable
                0, // PointsDiff not applicable
                0, // Points not applicable for elimination format
                index + 1 // Rank
            ))
            .ToList();

        return new List<GroupStandingResponse>
        {
            new GroupStandingResponse(0, "Classement", sortedStandings)
        };
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
        var standings = await GetStandingsAsync(tournamentId);

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

        // Arrange players for knockout bracket with proper seeding
        // - Top seeds get byes (if any)
        // - Top seeds meet as late as possible (protection)
        // - Cross-group matchups: 1A vs last qualifier from other group
        var arrangedPlayers = ArrangePlayersForKnockout(qualifiedPlayers, standings.Count, standings);

        // Calculate round offset (group stage uses round 1)
        int roundOffset = 1;

        // Generate knockout bracket
        GenerateSingleEliminationBracket(tournament, arrangedPlayers, isKnockout: true, roundOffset: roundOffset);

        await _context.SaveChangesAsync();
    }

    private List<Player> ArrangePlayersForKnockout(
        List<(Player Player, int GroupIndex, int Rank)> qualifiedPlayers,
        int groupCount,
        List<GroupStandingResponse> standings)
    {
        // Create global seeding based on rank in group, then stats (points, diff, for)
        // This ensures:
        // - All 1st place finishers are seeded before 2nd place, etc.
        // - Within each rank, players are sorted by performance
        // - Top seeds get byes and are protected (meet late)
        // - Cross-group matchups happen naturally (1A vs 2B, 2A vs 3B, etc.)
        var seededPlayers = qualifiedPlayers
            .Select(p => {
                var groupStanding = standings.FirstOrDefault(g =>
                    g.Standings.Any(s => s.PlayerId == p.Player.Id));
                var playerStats = groupStanding?.Standings
                    .FirstOrDefault(s => s.PlayerId == p.Player.Id);
                return new {
                    p.Player,
                    p.GroupIndex,
                    p.Rank,
                    Points = playerStats?.Points ?? 0,
                    PointsDiff = playerStats?.PointsDiff ?? 0,
                    PointsFor = playerStats?.PointsFor ?? 0
                };
            })
            .OrderBy(p => p.Rank)            // Primary: rank in group (1st > 2nd > 3rd)
            .ThenByDescending(p => p.Points) // Then by points
            .ThenByDescending(p => p.PointsDiff) // Then by point difference
            .ThenByDescending(p => p.PointsFor)  // Then by points scored
            .Select(p => p.Player)
            .ToList();

        // Return players in seed order (seed 1 first, seed 2 second, etc.)
        // GenerateSingleEliminationBracket will place them at correct bracket positions
        return seededPlayers;
    }

    /// <summary>
    /// Generates standard bracket positions for seeding.
    /// For a bracket of size 8: positions ensure 1v8, 4v5, 3v6, 2v7 matchups
    /// with 1 and 2 on opposite sides of the bracket (meeting only in final).
    /// Byes go to the highest seeds (they face missing opponents).
    /// </summary>
    private int[] GenerateBracketPositions(int bracketSize)
    {
        // positions[seed-1] = bracket position for that seed
        var positions = new int[bracketSize];

        if (bracketSize == 1)
        {
            positions[0] = 0;
            return positions;
        }

        // Build bracket recursively
        // Start with seeds 1 and 2 on opposite ends
        var seeds = new List<int> { 1, 2 };

        while (seeds.Count < bracketSize)
        {
            // For each round, add complementary seeds
            // Sum of paired seeds should equal currentSize + 1
            var newSeeds = new List<int>();
            int targetSum = seeds.Count * 2 + 1;

            foreach (int seed in seeds)
            {
                newSeeds.Add(seed);
                newSeeds.Add(targetSum - seed);
            }
            seeds = newSeeds;
        }

        // Now seeds list contains the order: e.g., for 8: [1, 8, 4, 5, 3, 6, 2, 7]
        // This means position 0 has seed 1, position 1 has seed 8, etc.
        // We need the inverse: positions[seed-1] = position
        for (int pos = 0; pos < seeds.Count; pos++)
        {
            positions[seeds[pos] - 1] = pos;
        }

        return positions;
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

        // For double elimination, handle both winner and loser advancement
        if (tournament.Format == TournamentFormat.DoubleElimination && match.WinnerId != null)
        {
            await AdvanceDoubleEliminationAsync(match);
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

    private async Task AdvanceDoubleEliminationAsync(Match completedMatch)
    {
        var loserId = completedMatch.Player1Id == completedMatch.WinnerId
            ? completedMatch.Player2Id
            : completedMatch.Player1Id;

        switch (completedMatch.BracketType)
        {
            case BracketType.Winners:
                await AdvanceWinnerInWinnersBracketAsync(completedMatch);
                if (loserId != null)
                {
                    await DropLoserToLosersBracketAsync(completedMatch, loserId.Value);
                }
                break;

            case BracketType.Losers:
                await AdvanceWinnerInLosersBracketAsync(completedMatch);
                // Loser is eliminated (second loss)
                break;

            case BracketType.GrandFinal:
                await HandleGrandFinalCompletionAsync(completedMatch);
                break;
        }
    }

    private async Task AdvanceWinnerInWinnersBracketAsync(Match completedMatch)
    {
        // Get next round in winner's bracket
        var nextRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == completedMatch.TournamentId
                && m.BracketType == BracketType.Winners
                && m.Round == completedMatch.Round + 1)
            .OrderBy(m => m.Position)
            .ToListAsync();

        if (!nextRoundMatches.Any())
        {
            // Winner's bracket final completed, advance to Grand Final
            var grandFinal = await _context.Matches
                .FirstOrDefaultAsync(m => m.TournamentId == completedMatch.TournamentId
                    && m.BracketType == BracketType.GrandFinal
                    && !m.IsBracketReset);

            if (grandFinal != null)
            {
                grandFinal.Player1Id = completedMatch.WinnerId;
            }
            return;
        }

        // Find current match index and advance winner
        var currentRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == completedMatch.TournamentId
                && m.BracketType == BracketType.Winners
                && m.Round == completedMatch.Round)
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

    private async Task DropLoserToLosersBracketAsync(Match winnersMatch, int loserId)
    {
        // Determine which loser's bracket round receives this loser
        // Winner's round R losers go to Loser's round (R * 2 - 1) for R >= 2
        // Winner's round 1 losers go to Loser's round 1

        int losersRound;
        if (winnersMatch.Round == 1)
        {
            losersRound = 1;
        }
        else
        {
            // For winner's round R (R >= 2), losers drop to loser's round 2*(R-1)
            losersRound = 2 * (winnersMatch.Round - 1);
        }

        var losersRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == winnersMatch.TournamentId
                && m.BracketType == BracketType.Losers
                && m.Round == losersRound)
            .OrderBy(m => m.Position)
            .ToListAsync();

        if (!losersRoundMatches.Any())
            return;

        // Get current round matches to find index
        var winnersRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == winnersMatch.TournamentId
                && m.BracketType == BracketType.Winners
                && m.Round == winnersMatch.Round)
            .OrderBy(m => m.Position)
            .ToListAsync();

        int matchIndexInRound = winnersRoundMatches.FindIndex(m => m.Id == winnersMatch.Id);
        Match? targetMatch = null;

        if (winnersMatch.Round == 1)
        {
            // For round 1 losers: pair them up (0,1 -> match 0), (2,3 -> match 1), etc.
            int targetMatchIndex = matchIndexInRound / 2;
            if (targetMatchIndex < losersRoundMatches.Count)
            {
                targetMatch = losersRoundMatches[targetMatchIndex];
                if (matchIndexInRound % 2 == 0)
                    targetMatch.Player1Id = loserId;
                else
                    targetMatch.Player2Id = loserId;

                // Check if the other slot will remain empty (bye case)
                // The paired Winners R1 match
                int pairedMatchIndex = (matchIndexInRound % 2 == 0) ? matchIndexInRound + 1 : matchIndexInRound - 1;
                if (pairedMatchIndex < winnersRoundMatches.Count)
                {
                    var pairedMatch = winnersRoundMatches[pairedMatchIndex];
                    // If paired match was a bye (one player null), no loser will come from it
                    bool pairedWasBye = (pairedMatch.Player1Id == null || pairedMatch.Player2Id == null);
                    if (pairedWasBye)
                    {
                        // This loser gets a bye in Losers R1 - auto advance
                        targetMatch.WinnerId = loserId;
                        targetMatch.Status = MatchStatus.Completed;
                        targetMatch.Player1Score = 0;
                        targetMatch.Player2Score = 0;

                        // Advance to next losers round
                        await AdvanceWinnerInLosersBracketAsync(targetMatch);
                    }
                }
            }
        }
        else
        {
            // For round 2+ losers: they join as Player2 to face winners from previous loser's round
            if (matchIndexInRound < losersRoundMatches.Count)
            {
                targetMatch = losersRoundMatches[matchIndexInRound];
                targetMatch.Player2Id = loserId;

                // Check if Player1 slot (from previous losers round) is already filled
                // If Player1 is set and match is not completed, both players are ready
                // If Player1 is null, the previous losers match might have been a bye cascade
                // We need to check if the previous losers round match was auto-completed as bye
            }
        }
    }

    private async Task AdvanceWinnerInLosersBracketAsync(Match completedMatch)
    {
        // Check if this is the final loser's bracket match
        var maxLosersRound = await _context.Matches
            .Where(m => m.TournamentId == completedMatch.TournamentId
                && m.BracketType == BracketType.Losers)
            .MaxAsync(m => m.Round);

        if (completedMatch.Round == maxLosersRound)
        {
            // Loser's bracket champion advances to Grand Final
            var grandFinal = await _context.Matches
                .FirstOrDefaultAsync(m => m.TournamentId == completedMatch.TournamentId
                    && m.BracketType == BracketType.GrandFinal
                    && !m.IsBracketReset);

            if (grandFinal != null)
            {
                grandFinal.Player2Id = completedMatch.WinnerId;
            }
            return;
        }

        // Advance to next loser's round
        var nextLosersRound = completedMatch.Round + 1;
        var nextRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == completedMatch.TournamentId
                && m.BracketType == BracketType.Losers
                && m.Round == nextLosersRound)
            .OrderBy(m => m.Position)
            .ToListAsync();

        if (!nextRoundMatches.Any())
            return;

        var currentRoundMatches = await _context.Matches
            .Where(m => m.TournamentId == completedMatch.TournamentId
                && m.BracketType == BracketType.Losers
                && m.Round == completedMatch.Round)
            .OrderBy(m => m.Position)
            .ToListAsync();

        int matchIndexInRound = currentRoundMatches.FindIndex(m => m.Id == completedMatch.Id);

        // Odd loser's rounds lead to Player1 slots, even rounds are drop-down rounds
        if (completedMatch.Round % 2 == 1)
        {
            // Consolidation round winners go to Player1 slots
            if (matchIndexInRound < nextRoundMatches.Count)
            {
                nextRoundMatches[matchIndexInRound].Player1Id = completedMatch.WinnerId;
            }
        }
        else
        {
            // Drop-down round winners: pair up for next consolidation round
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

    private async Task HandleGrandFinalCompletionAsync(Match grandFinalMatch)
    {
        var tournament = await _context.Tournaments.FindAsync(grandFinalMatch.TournamentId);
        if (tournament == null) return;

        if (!grandFinalMatch.IsBracketReset)
        {
            // Grand Final 1
            // Player1 is winner's bracket champion, Player2 is loser's bracket champion
            if (grandFinalMatch.WinnerId == grandFinalMatch.Player1Id)
            {
                // Winner's bracket champion wins - tournament complete
                tournament.Status = TournamentStatus.Completed;

                // Mark bracket reset match as not needed (skip it)
                var resetMatch = await _context.Matches
                    .FirstOrDefaultAsync(m => m.TournamentId == grandFinalMatch.TournamentId
                        && m.IsBracketReset);
                if (resetMatch != null)
                {
                    resetMatch.Status = MatchStatus.Completed;
                    resetMatch.WinnerId = grandFinalMatch.WinnerId;
                    resetMatch.Player1Score = 0;
                    resetMatch.Player2Score = 0;
                }
            }
            else
            {
                // Loser's bracket champion wins GF1 - need bracket reset
                var resetMatch = await _context.Matches
                    .FirstOrDefaultAsync(m => m.TournamentId == grandFinalMatch.TournamentId
                        && m.IsBracketReset);

                if (resetMatch != null && tournament.AllowBracketReset)
                {
                    resetMatch.Player1Id = grandFinalMatch.Player1Id;
                    resetMatch.Player2Id = grandFinalMatch.Player2Id;
                }
                else
                {
                    // No bracket reset - loser's bracket champion wins
                    tournament.Status = TournamentStatus.Completed;
                }
            }
        }
        else
        {
            // Grand Final 2 (bracket reset) - winner takes tournament
            tournament.Status = TournamentStatus.Completed;
        }

        await _context.SaveChangesAsync();
    }
}
