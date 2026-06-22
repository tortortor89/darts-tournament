import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { ToastComponent } from './shared/components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ToastComponent],
  template: `
    <nav>
      <div class="nav-brand">Hacien'Darts Cup</div>
      <div class="nav-links">
        <a routerLink="/tournaments" routerLinkActive="active">Tournois</a>
        <a routerLink="/players" routerLinkActive="active">Joueurs</a>
      </div>
      <div class="nav-auth">
        @if (authService.isAuthenticated()) {
          <a routerLink="/profile" routerLinkActive="active">Profil</a>
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
    <app-toast></app-toast>
  `,
  styles: [`
    nav {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 15px 30px;
      background: var(--hd-green);
      color: white;
      border-bottom: 3px solid var(--hd-amber);
    }
    .nav-brand {
      font-family: 'Barlow Condensed', sans-serif;
      font-size: 1.6em;
      font-weight: 800;
      letter-spacing: 0.04em;
      color: var(--hd-amber);
      text-transform: uppercase;
    }
    .nav-links {
      display: flex;
      gap: 20px;
    }
    .nav-links a, .nav-auth a {
      color: rgba(255,255,255,0.8);
      text-decoration: none;
      padding: 5px 10px;
      font-weight: 500;
    }
    .nav-links a:hover, .nav-auth a:hover {
      color: white;
    }
    .nav-links a.active, .nav-auth a.active {
      color: var(--hd-green);
      background: var(--hd-amber);
      border-radius: 4px;
      font-weight: 600;
    }
    .nav-auth {
      display: flex;
      align-items: center;
      gap: 15px;
    }
    .nav-auth span {
      color: rgba(255,255,255,0.7);
      font-size: 0.9em;
    }
    .nav-auth button {
      padding: 5px 15px;
      background: var(--hd-danger);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
    }
    .nav-auth button:hover {
      background: var(--hd-danger-dark);
    }
    main {
      min-height: calc(100vh - 63px);
      background: var(--hd-cream);
    }
  `]
})
export class AppComponent {
  constructor(public authService: AuthService) {}

  logout() {
    this.authService.logout();
  }
}
