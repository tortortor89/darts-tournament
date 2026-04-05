import { Component, OnInit, OnDestroy, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { ApiService } from '../../core/services/api.service';
import { MatchSessionSpectator, MatchSessionStatus } from '../../core/models';

@Component({
  selector: 'app-match-spectate',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="spectate-container">
      @if (loading) {
        <div class="loading">
          <div class="spinner"></div>
          <p>En attente du match...</p>
        </div>
      }

      @if (error) {
        <div class="error">
          <p>{{ error }}</p>
          <button (click)="loadSession()">Réessayer</button>
        </div>
      }

      @if (session) {
        <div class="spectate-screen" [class.finished]="session.status === MatchSessionStatus.Finished">
          <!-- Tournament Name -->
          <div class="tournament-name">{{ session.tournamentName }}</div>

          <!-- Main Score Display -->
          <div class="main-score">
            <div class="player-side left" [class.active]="session.currentPlayerId === session.player1.playerId" [class.winner]="isWinner(session.player1.playerId)">
              <div class="player-name">{{ session.player1.name }}</div>
              <div class="score-display">
                <div class="remaining">{{ session.player1.currentScore }}</div>
                <div class="legs-won">{{ session.player1.legsWon }}</div>
              </div>
            </div>

            <div class="center-info">
              <div class="legs-target">Premier à {{ session.legsToWin }}</div>
              <div class="vs">VS</div>
              <div class="current-leg">Leg {{ session.currentLeg }}</div>
              @if (session.status === MatchSessionStatus.Finished) {
                <div class="match-finished">TERMINE</div>
              }
            </div>

            <div class="player-side right" [class.active]="session.currentPlayerId === session.player2.playerId" [class.winner]="isWinner(session.player2.playerId)">
              <div class="player-name">{{ session.player2.name }}</div>
              <div class="score-display">
                <div class="remaining">{{ session.player2.currentScore }}</div>
                <div class="legs-won">{{ session.player2.legsWon }}</div>
              </div>
            </div>
          </div>

          <!-- Legs History -->
          @if (session.legsHistory.length > 0) {
            <div class="legs-history">
              <h3>Historique des legs</h3>
              <div class="legs-list">
                @for (leg of session.legsHistory; track leg.legNumber) {
                  <div class="leg-item">
                    <span class="leg-number">Leg {{ leg.legNumber }}</span>
                    <span class="leg-winner">{{ leg.winnerName }}</span>
                    @if (leg.winnerAverage) {
                      <span class="leg-avg">Moy: {{ leg.winnerAverage }}</span>
                    }
                  </div>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .spectate-container {
      min-height: 100vh;
      background: linear-gradient(135deg, #0f0f23 0%, #1a1a3e 100%);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 20px;
    }

    .loading {
      text-align: center;
    }

    .spinner {
      width: 60px;
      height: 60px;
      border: 4px solid rgba(255,255,255,0.2);
      border-top-color: #007bff;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 20px;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .error {
      text-align: center;
    }

    .error button {
      padding: 10px 20px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      margin-top: 15px;
    }

    .spectate-screen {
      width: 100%;
      max-width: 1200px;
    }

    .tournament-name {
      text-align: center;
      font-size: 1.5em;
      color: rgba(255,255,255,0.6);
      margin-bottom: 30px;
    }

    .main-score {
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      gap: 30px;
      align-items: center;
    }

    .player-side {
      background: rgba(255,255,255,0.05);
      border-radius: 20px;
      padding: 40px;
      text-align: center;
      transition: all 0.3s;
    }

    .player-side.left {
      border-left: 5px solid transparent;
    }

    .player-side.right {
      border-right: 5px solid transparent;
    }

    .player-side.active {
      background: rgba(40, 167, 69, 0.2);
    }

    .player-side.active.left {
      border-left-color: #28a745;
    }

    .player-side.active.right {
      border-right-color: #28a745;
    }

    .player-side.winner {
      background: rgba(255, 193, 7, 0.2);
    }

    .player-side.winner.left {
      border-left-color: #ffc107;
    }

    .player-side.winner.right {
      border-right-color: #ffc107;
    }

    .player-name {
      font-size: 2em;
      font-weight: bold;
      margin-bottom: 20px;
    }

    .score-display {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 10px;
    }

    .remaining {
      font-size: 6em;
      font-weight: bold;
      line-height: 1;
    }

    .legs-won {
      font-size: 3em;
      color: #ffc107;
      background: rgba(255, 193, 7, 0.2);
      padding: 10px 30px;
      border-radius: 10px;
    }

    .center-info {
      text-align: center;
    }

    .legs-target {
      font-size: 1em;
      color: rgba(255,255,255,0.5);
      margin-bottom: 10px;
    }

    .vs {
      font-size: 2em;
      color: rgba(255,255,255,0.3);
      margin: 20px 0;
    }

    .current-leg {
      font-size: 1.2em;
      color: rgba(255,255,255,0.7);
    }

    .match-finished {
      margin-top: 20px;
      padding: 15px 30px;
      background: #ffc107;
      color: #000;
      font-weight: bold;
      font-size: 1.5em;
      border-radius: 10px;
    }

    .legs-history {
      margin-top: 40px;
      background: rgba(255,255,255,0.05);
      border-radius: 12px;
      padding: 20px;
    }

    .legs-history h3 {
      margin: 0 0 15px 0;
      font-size: 1em;
      color: rgba(255,255,255,0.5);
      text-transform: uppercase;
    }

    .legs-list {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
    }

    .leg-item {
      background: rgba(255,255,255,0.1);
      padding: 10px 15px;
      border-radius: 8px;
      display: flex;
      gap: 15px;
      align-items: center;
    }

    .leg-number {
      color: rgba(255,255,255,0.5);
      font-size: 0.9em;
    }

    .leg-winner {
      font-weight: bold;
    }

    .leg-avg {
      color: #28a745;
      font-size: 0.9em;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .main-score {
        grid-template-columns: 1fr;
        gap: 20px;
      }

      .player-side {
        padding: 20px;
      }

      .player-side.left, .player-side.right {
        border-left: none;
        border-right: none;
        border-top: 5px solid transparent;
      }

      .player-side.active.left, .player-side.active.right {
        border-top-color: #28a745;
      }

      .player-side.winner.left, .player-side.winner.right {
        border-top-color: #ffc107;
      }

      .remaining {
        font-size: 4em;
      }

      .legs-won {
        font-size: 2em;
      }

      .center-info {
        order: -1;
      }
    }
  `]
})
export class MatchSpectateComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private route = inject(ActivatedRoute);
  private apiService = inject(ApiService);

  matchId: number = 0;
  session: MatchSessionSpectator | null = null;
  loading = true;
  error: string | null = null;

  MatchSessionStatus = MatchSessionStatus;

  private refreshInterval = 3000; // Refresh every 3 seconds

  ngOnInit() {
    this.matchId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadSession();
    this.startAutoRefresh();
  }

  loadSession() {
    this.loading = true;
    this.error = null;

    this.apiService.getMatchSpectator(this.matchId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.session = session;
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.error = 'Aucune session en cours pour ce match';
        }
      });
  }

  startAutoRefresh() {
    interval(this.refreshInterval)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.apiService.getMatchSpectator(this.matchId))
      )
      .subscribe({
        next: (session) => {
          this.session = session;
          this.error = null;
        },
        error: () => {
          // Silently ignore refresh errors
        }
      });
  }

  isWinner(playerId: number): boolean {
    if (!this.session || this.session.status !== MatchSessionStatus.Finished) {
      return false;
    }
    return this.session.player1.playerId === playerId
      ? this.session.player1.legsWon > this.session.player2.legsWon
      : this.session.player2.legsWon > this.session.player1.legsWon;
  }
}
