import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Player, Tournament, TournamentDetail, Match, TournamentFormat, GroupStanding } from '../models';
import { environment } from '../../../../environments/environment';

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
}
