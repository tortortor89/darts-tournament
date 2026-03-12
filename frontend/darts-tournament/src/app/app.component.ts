import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <nav>
      <div class="nav-brand">Darts Tournament</div>
      <div class="nav-links">
        <a routerLink="/tournaments" routerLinkActive="active">Tournois</a>
        <a routerLink="/players" routerLinkActive="active">Joueurs</a>
      </div>
      <div class="nav-auth">
        @if (authService.isAuthenticated()) {
          <span>{{ authService.currentUser() }}</span>
          <button (click)="logout()">Déconnexion</button>
        } @else {
          <a routerLink="/login" routerLinkActive="active">Connexion</a>
          <a routerLink="/register" routerLinkActive="active">Inscription</a>
        }
      </div>
    </nav>
    <main>
      <router-outlet></router-outlet>
    </main>
  `,
  styles: [`
    nav {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 15px 30px;
      background: #343a40;
      color: white;
    }
    .nav-brand {
      font-size: 1.5em;
      font-weight: bold;
    }
    .nav-links {
      display: flex;
      gap: 20px;
    }
    .nav-links a, .nav-auth a {
      color: #adb5bd;
      text-decoration: none;
      padding: 5px 10px;
    }
    .nav-links a:hover, .nav-auth a:hover {
      color: white;
    }
    .nav-links a.active, .nav-auth a.active {
      color: white;
      background: #495057;
      border-radius: 4px;
    }
    .nav-auth {
      display: flex;
      align-items: center;
      gap: 15px;
    }
    .nav-auth span {
      color: #adb5bd;
    }
    .nav-auth button {
      padding: 5px 15px;
      background: #dc3545;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    main {
      min-height: calc(100vh - 60px);
      background: #f8f9fa;
    }
  `]
})
export class AppComponent {
  constructor(public authService: AuthService) {}

  logout() {
    this.authService.logout();
  }
}
