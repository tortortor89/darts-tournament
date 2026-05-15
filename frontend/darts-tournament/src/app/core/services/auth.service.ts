import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, UserRole } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = `${environment.apiUrl}/auth`;
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USERNAME_KEY = 'username';
  private readonly ROLE_KEY = 'user_role';
  private readonly PLAYER_ID_KEY = 'linked_player_id';
  private readonly PLAYER_NAME_KEY = 'linked_player_name';

  isAuthenticated = signal(this.hasToken());
  currentUser = signal<string | null>(this.getStoredUsername());
  currentRole = signal<string | null>(this.getStoredRole());
  linkedPlayerId = signal<number | null>(this.getStoredPlayerId());
  linkedPlayerName = signal<string | null>(this.getStoredPlayerName());

  isAdmin = computed(() => this.currentRole() === UserRole.Admin);
  hasLinkedPlayer = computed(() => this.linkedPlayerId() !== null);

  constructor(private http: HttpClient) {}

  register(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/register`, { username, password })
      .pipe(tap(response => this.setSession(response)));
  }

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, { username, password })
      .pipe(tap(response => this.setSession(response)));
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USERNAME_KEY);
    localStorage.removeItem(this.ROLE_KEY);
    localStorage.removeItem(this.PLAYER_ID_KEY);
    localStorage.removeItem(this.PLAYER_NAME_KEY);
    this.isAuthenticated.set(false);
    this.currentUser.set(null);
    this.currentRole.set(null);
    this.linkedPlayerId.set(null);
    this.linkedPlayerName.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.USERNAME_KEY, response.username);
    localStorage.setItem(this.ROLE_KEY, response.role);
    this.isAuthenticated.set(true);
    this.currentUser.set(response.username);
    this.currentRole.set(response.role);

    // Store linked player info if present
    if (response.linkedPlayerId) {
      localStorage.setItem(this.PLAYER_ID_KEY, response.linkedPlayerId.toString());
      localStorage.setItem(this.PLAYER_NAME_KEY, response.linkedPlayerName!);
      this.linkedPlayerId.set(response.linkedPlayerId);
      this.linkedPlayerName.set(response.linkedPlayerName!);
    } else {
      localStorage.removeItem(this.PLAYER_ID_KEY);
      localStorage.removeItem(this.PLAYER_NAME_KEY);
      this.linkedPlayerId.set(null);
      this.linkedPlayerName.set(null);
    }
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  private getStoredUsername(): string | null {
    return localStorage.getItem(this.USERNAME_KEY);
  }

  private getStoredRole(): string | null {
    return localStorage.getItem(this.ROLE_KEY);
  }

  private getStoredPlayerId(): number | null {
    const id = localStorage.getItem(this.PLAYER_ID_KEY);
    return id ? parseInt(id, 10) : null;
  }

  private getStoredPlayerName(): string | null {
    return localStorage.getItem(this.PLAYER_NAME_KEY);
  }

  // Public methods to update linked player after linking/unlinking
  updateLinkedPlayer(playerId: number, playerName: string): void {
    localStorage.setItem(this.PLAYER_ID_KEY, playerId.toString());
    localStorage.setItem(this.PLAYER_NAME_KEY, playerName);
    this.linkedPlayerId.set(playerId);
    this.linkedPlayerName.set(playerName);
  }

  clearLinkedPlayer(): void {
    localStorage.removeItem(this.PLAYER_ID_KEY);
    localStorage.removeItem(this.PLAYER_NAME_KEY);
    this.linkedPlayerId.set(null);
    this.linkedPlayerName.set(null);
  }
}
