import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Player, Tournament, TournamentDetail, TournamentTeam, Match, TournamentFormat, GroupStanding, MatchSession, MatchSessionSpectator, StartMatchSessionRequest, RecordThrowRequest, MatchStats, PlayerCareerStats, PlayerTournamentHistoryItem, HeadToHeadRecord, CricketTurnResponse, CricketHit, ActiveSessionSummary, Circuit, CircuitDetail, CircuitStanding, CircuitPointsRule, Club, ClubDetail, InterclubChampionship, InterclubChampionshipDetail, CalendarRound, EncounterDetail, BoardLineup, InterclubStanding, GameMode } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly API_URL = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Players
  getPlayers(): Observable<Player[]> {
    return this.http.get<Player[]>(`${this.API_URL}/players`);
  }

  getPlayer(id: number): Observable<Player> {
    return this.http.get<Player>(`${this.API_URL}/players/${id}`);
  }

  createPlayer(player: { firstName: string; lastName: string; nickname?: string }): Observable<Player> {
    return this.http.post<Player>(`${this.API_URL}/players`, player);
  }

  updatePlayer(id: number, player: { firstName: string; lastName: string; nickname?: string }): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/players/${id}`, player);
  }

  deletePlayer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/players/${id}`);
  }

  // Player-User Linking
  getAvailablePlayers(): Observable<Player[]> {
    return this.http.get<Player[]>(`${this.API_URL}/players/available`);
  }

  getPlayerDetail(id: number): Observable<Player> {
    return this.http.get<Player>(`${this.API_URL}/players/${id}/detail`);
  }

  createOwnPlayer(player: { firstName: string; lastName: string; nickname?: string }): Observable<Player> {
    return this.http.post<Player>(`${this.API_URL}/players/create-own`, player);
  }

  updateOwnPlayer(player: { firstName: string; lastName: string; nickname?: string }): Observable<Player> {
    return this.http.put<Player>(`${this.API_URL}/players/update-own`, player);
  }

  linkToPlayer(playerId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/players/link`, { playerId });
  }

  unlinkPlayer(playerId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/players/${playerId}/unlink`);
  }

  getPlayerCareerStats(id: number): Observable<PlayerCareerStats> {
    return this.http.get<PlayerCareerStats>(`${this.API_URL}/players/${id}/stats`);
  }

  getPlayerTournamentHistory(id: number): Observable<PlayerTournamentHistoryItem[]> {
    return this.http.get<PlayerTournamentHistoryItem[]>(`${this.API_URL}/players/${id}/tournament-history`);
  }

  getPlayerHeadToHead(id: number): Observable<HeadToHeadRecord[]> {
    return this.http.get<HeadToHeadRecord[]>(`${this.API_URL}/players/${id}/head-to-head`);
  }

  adminLinkPlayerToUser(playerId: number, userId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/players/${playerId}/link-user/${userId}`, {});
  }

  // Auth
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/auth/change-password`, { currentPassword, newPassword });
  }

  // Tournaments
  getTournaments(): Observable<Tournament[]> {
    return this.http.get<Tournament[]>(`${this.API_URL}/tournaments`);
  }

  getTournament(id: number): Observable<TournamentDetail> {
    return this.http.get<TournamentDetail>(`${this.API_URL}/tournaments/${id}`);
  }

  createTournament(tournament: {
    name: string;
    format: TournamentFormat;
    startDate?: Date;
    numberOfGroups?: number;
    playersPerGroup?: number;
    qualifiersPerGroup?: number;
    hasKnockoutPhase?: boolean;
    circuitId?: number;
    isDoubles?: boolean;
  }): Observable<Tournament> {
    return this.http.post<Tournament>(`${this.API_URL}/tournaments`, tournament);
  }

  updateTournament(id: number, tournament: { name: string; startDate?: Date }): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/tournaments/${id}`, tournament);
  }

  deleteTournament(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/tournaments/${id}`);
  }

  addPlayerToTournament(tournamentId: number, playerId: number, seed?: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/tournaments/${tournamentId}/players`, { playerId, seed });
  }

  removePlayerFromTournament(tournamentId: number, playerId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/tournaments/${tournamentId}/players/${playerId}`);
  }

  // Doubles (paires)
  addTeamToTournament(tournamentId: number, player1Id: number, player2Id: number, seed?: number): Observable<TournamentTeam> {
    return this.http.post<TournamentTeam>(`${this.API_URL}/tournaments/${tournamentId}/teams`, { player1Id, player2Id, seed });
  }

  removeTeamFromTournament(tournamentId: number, teamId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/tournaments/${tournamentId}/teams/${teamId}`);
  }

  // Self-registration
  registerToTournament(tournamentId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/tournaments/${tournamentId}/register`, {});
  }

  unregisterFromTournament(tournamentId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/tournaments/${tournamentId}/unregister`);
  }

  approveRegistration(tournamentId: number, playerId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/tournaments/${tournamentId}/registrations/${playerId}/approve`, {});
  }

  rejectRegistration(tournamentId: number, playerId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/tournaments/${tournamentId}/registrations/${playerId}/reject`, {});
  }

  generateBracket(tournamentId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/tournaments/${tournamentId}/generate`, {});
  }

  getStandings(tournamentId: number): Observable<GroupStanding[]> {
    return this.http.get<GroupStanding[]>(`${this.API_URL}/tournaments/${tournamentId}/standings`);
  }

  // Circuits
  getCircuits(): Observable<Circuit[]> {
    return this.http.get<Circuit[]>(`${this.API_URL}/circuits`);
  }

  getCircuit(id: number): Observable<CircuitDetail> {
    return this.http.get<CircuitDetail>(`${this.API_URL}/circuits/${id}`);
  }

  getCircuitRanking(id: number): Observable<CircuitStanding[]> {
    return this.http.get<CircuitStanding[]>(`${this.API_URL}/circuits/${id}/ranking`);
  }

  createCircuit(circuit: { name: string; description?: string; participationPoints?: number; pointsRules?: CircuitPointsRule[] }): Observable<Circuit> {
    return this.http.post<Circuit>(`${this.API_URL}/circuits`, circuit);
  }

  updateCircuit(id: number, circuit: { name: string; description?: string; participationPoints: number; pointsRules: CircuitPointsRule[] }): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/circuits/${id}`, circuit);
  }

  deleteCircuit(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/circuits/${id}`);
  }

  attachTournamentToCircuit(circuitId: number, tournamentId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/circuits/${circuitId}/tournaments`, { tournamentId });
  }

  detachTournamentFromCircuit(circuitId: number, tournamentId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/circuits/${circuitId}/tournaments/${tournamentId}`);
  }

  // Clubs
  getClubs(): Observable<Club[]> {
    return this.http.get<Club[]>(`${this.API_URL}/clubs`);
  }

  getClub(id: number): Observable<ClubDetail> {
    return this.http.get<ClubDetail>(`${this.API_URL}/clubs/${id}`);
  }

  createClub(name: string): Observable<Club> {
    return this.http.post<Club>(`${this.API_URL}/clubs`, { name });
  }

  deleteClub(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/clubs/${id}`);
  }

  assignPlayerToClub(clubId: number, playerId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/clubs/${clubId}/players`, { playerId });
  }

  removePlayerFromClub(clubId: number, playerId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/clubs/${clubId}/players/${playerId}`);
  }

  // Interclubs
  getChampionships(): Observable<InterclubChampionship[]> {
    return this.http.get<InterclubChampionship[]>(`${this.API_URL}/interclubs`);
  }

  getChampionship(id: number): Observable<InterclubChampionshipDetail> {
    return this.http.get<InterclubChampionshipDetail>(`${this.API_URL}/interclubs/${id}`);
  }

  createChampionship(championship: {
    name: string;
    singlesPerEncounter: number;
    doublesPerEncounter: number;
    legsToWin: number;
    gameMode: GameMode;
    doubleOut: boolean;
  }): Observable<InterclubChampionship> {
    return this.http.post<InterclubChampionship>(`${this.API_URL}/interclubs`, championship);
  }

  deleteChampionship(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/interclubs/${id}`);
  }

  attachClubToChampionship(championshipId: number, clubId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/interclubs/${championshipId}/clubs`, { clubId });
  }

  detachClubFromChampionship(championshipId: number, clubId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/interclubs/${championshipId}/clubs/${clubId}`);
  }

  setChampionshipRoster(championshipId: number, clubId: number, playerIds: number[]): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/interclubs/${championshipId}/clubs/${clubId}/roster`, { playerIds });
  }

  generateInterclubCalendar(championshipId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/interclubs/${championshipId}/generate-calendar`, {});
  }

  getInterclubCalendar(championshipId: number): Observable<CalendarRound[]> {
    return this.http.get<CalendarRound[]>(`${this.API_URL}/interclubs/${championshipId}/calendar`);
  }

  getInterclubStandings(championshipId: number): Observable<InterclubStanding[]> {
    return this.http.get<InterclubStanding[]>(`${this.API_URL}/interclubs/${championshipId}/standings`);
  }

  getEncounter(id: number): Observable<EncounterDetail> {
    return this.http.get<EncounterDetail>(`${this.API_URL}/interclubs/encounters/${id}`);
  }

  setEncounterLineup(encounterId: number, boards: BoardLineup[]): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/interclubs/encounters/${encounterId}/lineup`, { boards });
  }

  // Matches
  getMatch(id: number): Observable<Match> {
    return this.http.get<Match>(`${this.API_URL}/matches/${id}`);
  }

  updateMatchScore(id: number, player1Score: number, player2Score: number): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/matches/${id}/score`, { player1Score, player2Score });
  }

  // Match Sessions (Live Game)
  getMatchSession(matchId: number): Observable<MatchSession> {
    return this.http.get<MatchSession>(`${this.API_URL}/matches/${matchId}/session`);
  }

  startMatchSession(matchId: number, request: StartMatchSessionRequest): Observable<MatchSession> {
    return this.http.post<MatchSession>(`${this.API_URL}/matches/${matchId}/session/start`, request);
  }

  recordThrow(matchId: number, request: RecordThrowRequest): Observable<MatchSession> {
    return this.http.post<MatchSession>(`${this.API_URL}/matches/${matchId}/session/throw`, request);
  }

  undoLastThrow(matchId: number): Observable<MatchSession> {
    return this.http.delete<MatchSession>(`${this.API_URL}/matches/${matchId}/session/throws/last`);
  }

  recordCricketTurn(matchId: number, hits: CricketHit[]): Observable<CricketTurnResponse> {
    return this.http.post<CricketTurnResponse>(`${this.API_URL}/matches/${matchId}/session/cricket-turn`, { hits });
  }

  validateMatchSession(matchId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/matches/${matchId}/session/validate`, {});
  }

  cancelMatchSession(matchId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/matches/${matchId}/session`);
  }

  getMatchSpectator(matchId: number): Observable<MatchSessionSpectator> {
    return this.http.get<MatchSessionSpectator>(`${this.API_URL}/matches/${matchId}/spectate`);
  }

  getActiveSessions(): Observable<ActiveSessionSummary[]> {
    return this.http.get<ActiveSessionSummary[]>(`${this.API_URL}/matches/active-sessions`);
  }

  getMatchStats(matchId: number): Observable<MatchStats> {
    return this.http.get<MatchStats>(`${this.API_URL}/matches/${matchId}/session/stats`);
  }
}
