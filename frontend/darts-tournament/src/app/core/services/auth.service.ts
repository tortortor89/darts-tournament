import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = `${environment.apiUrl}/auth`;
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USERNAME_KEY = 'username';

  isAuthenticated = signal(this.hasToken());
  currentUser = signal<string | null>(this.getStoredUsername());

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
    this.isAuthenticated.set(false);
    this.currentUser.set(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.USERNAME_KEY, response.username);
    this.isAuthenticated.set(true);
    this.currentUser.set(response.username);
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }

  private getStoredUsername(): string | null {
    return localStorage.getItem(this.USERNAME_KEY);
  }
}
