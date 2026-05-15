import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Player, Tournament, TournamentDetail, Match, TournamentFormat, GroupStanding, MatchSession, MatchSessionSpectator, StartMatchSessionRequest, RecordThrowRequest, MatchStats } from '../models';
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

  validateMatchSession(matchId: number): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/matches/${matchId}/session/validate`, {});
  }

  cancelMatchSession(matchId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/matches/${matchId}/session`);
  }

  getMatchSpectator(matchId: number): Observable<MatchSessionSpectator> {
    return this.http.get<MatchSessionSpectator>(`${this.API_URL}/matches/${matchId}/spectate`);
  }

  getMatchStats(matchId: number): Observable<MatchStats> {
    return this.http.get<MatchStats>(`${this.API_URL}/matches/${matchId}/session/stats`);
  }
}
