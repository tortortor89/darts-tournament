import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { interval, Subscription } from 'rxjs';
import { switchMap, startWith } from 'rxjs/operators';
import { ApiService } from '../../core/services/api.service';
import { ActiveSessionSummary, GameMode } from '../../core/models';

@Component({
  selector: 'app-tv-lobby',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="tv-lobby">
      <header class="tv-header">
        <h1 class="tv-title">Matchs en cours</h1>
        <div class="tv-clock">{{ currentTime }}</div>
      </header>

      @if (sessions.length === 0) {
        <div class="tv-empty">
          <p>Aucun match en cours</p>
          <p class="tv-empty-sub">En attente du prochain match…</p>
        </div>
      }

      @if (sessions.length > 0) {
        <div class="tv-match-list">
          @for (session of sessions; track session.matchId; let i = $index) {
            <div
              class="tv-match-card"
              [class.focused]="i === focusedIndex"
              [attr.data-index]="i">

              <div class="tv-match-tournament">{{ session.tournamentName }}</div>

              <div class="tv-match-score">
                <div class="tv-player" [class.leading]="session.player1LegsWon > session.player2LegsWon">
                  <span class="tv-player-name">{{ session.player1Name }}</span>
                  <span class="tv-legs">{{ session.player1LegsWon }}</span>
                </div>

                <div class="tv-match-center">
                  <span class="tv-mode">{{ gameModeLabel(session.gameMode) }}</span>
                  <span class="tv-vs">VS</span>
                  <span class="tv-leg-info">Leg {{ session.currentLeg }}/{{ session.legsToWin }}</span>
                </div>

                <div class="tv-player" [class.leading]="session.player2LegsWon > session.player1LegsWon">
                  <span class="tv-legs">{{ session.player2LegsWon }}</span>
                  <span class="tv-player-name">{{ session.player2Name }}</span>
                </div>
              </div>

              @if (i === focusedIndex) {
                <div class="tv-hint">Appuyez sur OK pour regarder</div>
              }
            </div>
          }
        </div>
      }

      <footer class="tv-footer">
        <span>↑ ↓ Naviguer</span>
        <span>OK Regarder</span>
        <span class="tv-refresh-indicator" [class.refreshing]="refreshing">●</span>
      </footer>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100vw;
      height: 100vh;
      background: #0a0a0a;
      color: #f0f0f0;
      font-family: 'Barlow Condensed', 'Arial Narrow', Arial, sans-serif;
      overflow: hidden;
    }

    .tv-lobby {
      display: flex;
      flex-direction: column;
      height: 100%;
      padding: 48px 80px;
      box-sizing: border-box;
    }

    .tv-header {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      margin-bottom: 48px;
    }

    .tv-title {
      font-size: 56px;
      font-weight: 700;
      letter-spacing: 2px;
      text-transform: uppercase;
      margin: 0;
      color: #fff;
    }

    .tv-clock {
      font-size: 40px;
      color: #888;
      font-variant-numeric: tabular-nums;
    }

    .tv-empty {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 16px;
    }

    .tv-empty p {
      font-size: 48px;
      color: #888;
      margin: 0;
    }

    .tv-empty-sub {
      font-size: 32px !important;
      color: #555 !important;
    }

    .tv-match-list {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 24px;
      overflow: hidden;
    }

    .tv-match-card {
      background: #1a1a1a;
      border: 3px solid #2a2a2a;
      border-radius: 12px;
      padding: 32px 48px;
      transition: border-color 0.1s, background 0.1s;
    }

    .tv-match-card.focused {
      border-color: #e8c547;
      background: #1e1c0e;
    }

    .tv-match-tournament {
      font-size: 24px;
      color: #888;
      text-transform: uppercase;
      letter-spacing: 2px;
      margin-bottom: 20px;
    }

    .tv-match-score {
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      align-items: center;
      gap: 32px;
    }

    .tv-player {
      display: flex;
      align-items: center;
      gap: 24px;
    }

    .tv-player:first-child {
      justify-content: flex-end;
    }

    .tv-player:last-child {
      justify-content: flex-start;
    }

    .tv-player-name {
      font-size: 44px;
      font-weight: 600;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 340px;
    }

    .tv-legs {
      font-size: 72px;
      font-weight: 800;
      line-height: 1;
      min-width: 72px;
      text-align: center;
      color: #ccc;
      font-variant-numeric: tabular-nums;
    }

    .tv-player.leading .tv-legs {
      color: #e8c547;
    }

    .tv-match-center {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
    }

    .tv-mode {
      font-size: 20px;
      color: #666;
      text-transform: uppercase;
      letter-spacing: 1px;
    }

    .tv-vs {
      font-size: 36px;
      font-weight: 700;
      color: #555;
    }

    .tv-leg-info {
      font-size: 22px;
      color: #666;
    }

    .tv-hint {
      text-align: center;
      margin-top: 20px;
      font-size: 22px;
      color: #e8c547;
      letter-spacing: 1px;
    }

    .tv-footer {
      margin-top: 32px;
      display: flex;
      justify-content: center;
      gap: 64px;
      font-size: 24px;
      color: #444;
    }

    .tv-refresh-indicator {
      color: #333;
      transition: color 0.3s;
    }

    .tv-refresh-indicator.refreshing {
      color: #4caf50;
    }
  `]
})
export class TvLobbyComponent implements OnInit, OnDestroy {
  sessions: ActiveSessionSummary[] = [];
  focusedIndex = 0;
  currentTime = '';
  refreshing = false;

  private pollSub?: Subscription;
  private clockSub?: Subscription;

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit(): void {
    this.clockSub = interval(1000).pipe(startWith(0)).subscribe(() => {
      this.currentTime = new Date().toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
    });

    this.pollSub = interval(10000).pipe(startWith(0), switchMap(() => {
      this.refreshing = true;
      return this.api.getActiveSessions();
    })).subscribe({
      next: (data) => {
        this.sessions = data;
        this.refreshing = false;
        if (this.focusedIndex >= this.sessions.length) {
          this.focusedIndex = Math.max(0, this.sessions.length - 1);
        }
      },
      error: () => { this.refreshing = false; }
    });
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
    this.clockSub?.unsubscribe();
  }

  @HostListener('window:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.focusedIndex = Math.min(this.focusedIndex + 1, this.sessions.length - 1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.focusedIndex = Math.max(this.focusedIndex - 1, 0);
        break;
      case 'Enter':
        if (this.sessions[this.focusedIndex]) {
          this.router.navigate(['/matches', this.sessions[this.focusedIndex].matchId, 'spectate']);
        }
        break;
    }
  }

  gameModeLabel(mode: GameMode): string {
    switch (mode) {
      case GameMode.FiveOhOne: return '501';
      case GameMode.ThreeOhOne: return '301';
      case GameMode.Cricket: return 'Cricket';
      default: return '';
    }
  }
}
