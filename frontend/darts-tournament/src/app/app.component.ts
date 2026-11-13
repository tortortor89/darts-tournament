import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { FeatureService } from './core/services/feature.service';
import { SkinService } from './core/services/skin.service';
import { ToastComponent } from './shared/components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ToastComponent],
  template: `
    <nav>
      <div class="nav-brand">{{ skinService.appName() }}</div>
      <div class="nav-links">
        @if (featureService.isEnabled('seasons')) {
          <a routerLink="/seasons" routerLinkActive="active">Saisons</a>
        }
        @if (featureService.isEnabled('events')) {
          <a routerLink="/events" routerLinkActive="active">Événements</a>
        }
        <a routerLink="/tournaments" routerLinkActive="active">Tournois</a>
        @if (featureService.isEnabled('circuits')) {
          <a routerLink="/circuits" routerLinkActive="active">Circuits</a>
        }
        @if (featureService.isEnabled('interclubs')) {
          <a routerLink="/interclubs" routerLinkActive="active">Interclubs</a>
        }
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
      background: var(--nav-bg);
      color: white;
      border-bottom: var(--nav-border);
    }
    .nav-brand {
      font-family: var(--font-heading);
      font-size: 1.5em;
      font-weight: bold;
      letter-spacing: var(--heading-spacing);
      color: var(--nav-brand);
      text-transform: var(--brand-transform);
    }
    .nav-links {
      display: flex;
      gap: 20px;
    }
    .nav-links a, .nav-auth a {
      color: var(--nav-text);
      text-decoration: none;
      padding: 5px 10px;
      font-weight: 500;
    }
    .nav-links a:hover, .nav-auth a:hover {
      color: white;
    }
    .nav-links a.active, .nav-auth a.active {
      color: var(--nav-active-text);
      background: var(--nav-active-bg);
      border-radius: 4px;
      font-weight: 600;
    }
    .nav-auth {
      display: flex;
      align-items: center;
      gap: 15px;
    }
    .nav-auth span {
      color: var(--nav-text);
      font-size: 0.9em;
    }
    .nav-auth button {
      padding: 5px 15px;
      background: var(--danger);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
    }
    .nav-auth button:hover {
      background: var(--danger-hover);
    }
    main {
      min-height: calc(100vh - 60px);
      background: var(--bg);
    }
  `]
})
export class AppComponent {
  constructor(
    public authService: AuthService,
    public featureService: FeatureService,
    public skinService: SkinService
  ) {}

  logout() {
    this.authService.logout();
  }
}
