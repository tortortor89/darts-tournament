import { Component, OnInit, OnDestroy, DestroyRef, inject } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { ApiService } from '../../core/services/api.service';
import { SignalRService, ConnectionStatus } from '../../core/services/signalr.service';
import { MatchSessionSpectator, MatchSessionStatus, MatchStats, GameMode, isX01, startingScore } from '../../core/models';
import { CricketDisplayComponent } from './components/cricket-display.component';

@Component({
  selector: 'app-match-spectate',
  standalone: true,
  imports: [CommonModule, DecimalPipe, CricketDisplayComponent],
  template: `
    <div class="spectate-container">
      <!-- Connection Status -->
      <div class="connection-status" [class]="connectionStatus">
        <span class="status-dot"></span>
        {{ getConnectionStatusText() }}
      </div>

      @if (loading) {
        <div class="loading">
          <div class="spinner"></div>
          <p>En attente du match...</p>
        </div>
      }

      @if (error) {
        <div class="error">
          <p>{{ error }}</p>
          <button (click)="loadSession()">Reessayer</button>
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
              <div class="legs-target">Premier a {{ session.legsToWin }}</div>
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

          <!-- Cricket Display -->
          @if (session.gameMode === GameMode.Cricket && session.cricketState) {
            <div class="cricket-section">
              <app-cricket-display
                [cricketState]="session.cricketState"
                [player1Name]="session.player1.name"
                [player2Name]="session.player2.name">
              </app-cricket-display>
            </div>
          }

          <!-- Live Statistics (Cricket) -->
          @if (stats && session.gameMode === GameMode.Cricket) {
            <div class="stats-panel">
              <h3>Statistiques en direct</h3>
              <div class="stats-grid">
                <div class="stat-row">
                  <div class="stat-value left highlight">{{ stats.player1Stats.marksPerRound | number:'1.1-2' }}</div>
                  <div class="stat-label">MPR</div>
                  <div class="stat-value right highlight">{{ stats.player2Stats.marksPerRound | number:'1.1-2' }}</div>
                </div>
                <div class="stat-row">
                  <div class="stat-value left">{{ stats.player1Stats.totalScore }}</div>
                  <div class="stat-label">Points marques</div>
                  <div class="stat-value right">{{ stats.player2Stats.totalScore }}</div>
                </div>
                <div class="stat-row">
                  <div class="stat-value left">{{ stats.player1Stats.highestScore || '-' }}</div>
                  <div class="stat-label">Meilleure visite</div>
                  <div class="stat-value right">{{ stats.player2Stats.highestScore || '-' }}</div>
                </div>
              </div>
            </div>
          }

          <!-- Live Statistics (x01) -->
          @if (stats && isX01Session()) {
            <div class="stats-panel">
              <h3>Statistiques en direct</h3>
              <div class="stats-grid">
                <!-- Average -->
                <div class="stat-row">
                  <div class="stat-value left">{{ stats.player1Stats.threeDartAverage | number:'1.1-1' }}</div>
                  <div class="stat-label">Moyenne</div>
                  <div class="stat-value right">{{ stats.player2Stats.threeDartAverage | number:'1.1-1' }}</div>
                </div>

                <!-- First 9 Average -->
                @if (stats.player1Stats.first9Average || stats.player2Stats.first9Average) {
                  <div class="stat-row">
                    <div class="stat-value left">{{ stats.player1Stats.first9Average ? (stats.player1Stats.first9Average | number:'1.1-1') : '-' }}</div>
                    <div class="stat-label">Moy. 9 premieres</div>
                    <div class="stat-value right">{{ stats.player2Stats.first9Average ? (stats.player2Stats.first9Average | number:'1.1-1') : '-' }}</div>
                  </div>
                }

                <!-- Checkout % -->
                <div class="stat-row">
                  <div class="stat-value left">{{ stats.player1Stats.checkoutPercentage ? (stats.player1Stats.checkoutPercentage | number:'1.0-0') + '%' : '-' }}</div>
                  <div class="stat-label">Checkout</div>
                  <div class="stat-value right">{{ stats.player2Stats.checkoutPercentage ? (stats.player2Stats.checkoutPercentage | number:'1.0-0') + '%' : '-' }}</div>
                </div>

                <!-- Highest Checkout -->
                <div class="stat-row">
                  <div class="stat-value left">{{ stats.player1Stats.highestCheckout || '-' }}</div>
                  <div class="stat-label">+ Haut checkout</div>
                  <div class="stat-value right">{{ stats.player2Stats.highestCheckout || '-' }}</div>
                </div>

                <!-- 180s -->
                <div class="stat-row">
                  <div class="stat-value left highlight">{{ stats.player1Stats.oneEighties }}</div>
                  <div class="stat-label">180</div>
                  <div class="stat-value right highlight">{{ stats.player2Stats.oneEighties }}</div>
                </div>

                <!-- Highest Score -->
                <div class="stat-row">
                  <div class="stat-value left">{{ stats.player1Stats.highestScore || '-' }}</div>
                  <div class="stat-label">Meilleure volee</div>
                  <div class="stat-value right">{{ stats.player2Stats.highestScore || '-' }}</div>
                </div>
              </div>
            </div>
          }

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
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 20px;
      position: relative;
    }

    .connection-status {
      position: fixed;
      top: 15px;
      right: 15px;
      padding: 8px 15px;
      border-radius: 20px;
      font-size: 0.85em;
      display: flex;
      align-items: center;
      gap: 8px;
      background: rgba(0,0,0,0.5);
      z-index: 100;
    }

    .status-dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #6c757d;
    }

    .connection-status.connected .status-dot {
      background: #28a745;
      box-shadow: 0 0 8px #28a745;
    }

    .connection-status.connecting .status-dot,
    .connection-status.reconnecting .status-dot {
      background: #ffc107;
      animation: pulse 1s infinite;
    }

    .connection-status.disconnected .status-dot {
      background: #dc3545;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.5; }
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

    /* Statistics Panel */
    .stats-panel {
      margin-top: 30px;
      background: rgba(255,255,255,0.05);
      border-radius: 12px;
      padding: 20px;
    }

    .stats-panel h3 {
      margin: 0 0 15px 0;
      font-size: 1em;
      color: rgba(255,255,255,0.5);
      text-transform: uppercase;
      text-align: center;
    }

    .stats-grid {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .stat-row {
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      gap: 20px;
      align-items: center;
      padding: 8px 0;
      border-bottom: 1px solid rgba(255,255,255,0.1);
    }

    .stat-row:last-child {
      border-bottom: none;
    }

    .stat-value {
      font-size: 1.3em;
      font-weight: bold;
    }

    .stat-value.left {
      text-align: right;
    }

    .stat-value.right {
      text-align: left;
    }

    .stat-value.highlight {
      color: #ffc107;
    }

    .stat-label {
      color: rgba(255,255,255,0.5);
      font-size: 0.9em;
      text-align: center;
      min-width: 140px;
    }

    .legs-history {
      margin-top: 30px;
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

      .stat-row {
        gap: 10px;
      }

      .stat-label {
        min-width: 100px;
        font-size: 0.8em;
      }

      .stat-value {
        font-size: 1.1em;
      }
    }
  `]
})
export class MatchSpectateComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  private route = inject(ActivatedRoute);
  private apiService = inject(ApiService);
  private signalRService = inject(SignalRService);

  matchId: number = 0;
  session: MatchSessionSpectator | null = null;
  stats: MatchStats | null = null;
  loading = true;
  error: string | null = null;
  connectionStatus: ConnectionStatus = 'disconnected';

  MatchSessionStatus = MatchSessionStatus;
  GameMode = GameMode;

  private pollingSubscription: Subscription | null = null;
  private usePollingFallback = false;

  async ngOnInit() {
    this.matchId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadSession();
    await this.setupSignalR();
  }

  ngOnDestroy() {
    this.signalRService.leaveMatch(this.matchId);
    this.stopPolling();
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
          this.loadStats();
        },
        error: (err) => {
          this.loading = false;
          this.error = 'Aucune session en cours pour ce match';
        }
      });
  }

  loadStats() {
    this.apiService.getMatchStats(this.matchId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stats) => {
          this.stats = stats;
        },
        error: () => {
          // Stats not available, ignore
        }
      });
  }

  async setupSignalR() {
    try {
      await this.signalRService.startConnection();
      await this.signalRService.joinMatch(this.matchId);

      // Subscribe to connection status
      this.signalRService.connectionStatus;
      this.connectionStatus = this.signalRService.connectionStatus();

      // Watch for status changes
      const checkStatus = () => {
        this.connectionStatus = this.signalRService.connectionStatus();
        if (this.connectionStatus === 'disconnected' && !this.usePollingFallback) {
          this.startPollingFallback();
        }
      };

      // Use effect-like behavior with interval check
      setInterval(checkStatus, 1000);

      // Subscribe to SignalR events
      this.signalRService.onThrowRecorded
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(event => {
          if (event.matchId === this.matchId && this.session) {
            this.session.player1.currentScore = event.player1CurrentScore;
            this.session.player2.currentScore = event.player2CurrentScore;
            this.session.currentPlayerId = event.currentPlayerId;
            this.stats = event.stats;
          }
        });

      this.signalRService.onThrowUndone
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(event => {
          if (event.matchId === this.matchId) {
            // Une annulation peut toucher scores, legs et statut : on recharge tout
            this.loadSession();
          }
        });

      this.signalRService.onCricketTurnRecorded
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(event => {
          if (event.matchId === this.matchId && this.session) {
            this.session.player1.currentScore = event.player1CurrentScore;
            this.session.player2.currentScore = event.player2CurrentScore;
            this.session.currentPlayerId = event.currentPlayerId;
            this.session.cricketState = event.turn.currentState;
            this.loadStats();
          }
        });

      this.signalRService.onLegWon
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(event => {
          if (event.matchId === this.matchId && this.session) {
            this.session.player1.legsWon = event.player1LegsWon;
            this.session.player2.legsWon = event.player2LegsWon;
            this.session.currentLeg = event.newCurrentLeg;
            // Remise au score de départ selon le mode (501/301, Cricket = 0)
            const legStartScore = startingScore(this.session.gameMode);
            this.session.player1.currentScore = legStartScore;
            this.session.player2.currentScore = legStartScore;
            this.session.legsHistory.push(event.legSummary);
          }
        });

      this.signalRService.onMatchFinished
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(event => {
          if (event.matchId === this.matchId && this.session) {
            this.session.status = MatchSessionStatus.Finished;
            this.session.player1.legsWon = event.player1LegsWon;
            this.session.player2.legsWon = event.player2LegsWon;
            this.stats = event.finalStats;
          }
        });

      this.signalRService.onSessionStarted
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(event => {
          if (event.matchId === this.matchId) {
            this.loadSession();
          }
        });

      this.signalRService.onSessionCancelled
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(matchId => {
          if (matchId === this.matchId) {
            this.session = null;
            this.stats = null;
            this.error = 'La session a ete annulee';
          }
        });

    } catch (err) {
      console.error('SignalR setup failed, falling back to polling', err);
      this.startPollingFallback();
    }
  }

  startPollingFallback() {
    if (this.pollingSubscription) return;

    this.usePollingFallback = true;
    this.connectionStatus = 'disconnected';

    this.pollingSubscription = interval(3000)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.apiService.getMatchSpectator(this.matchId))
      )
      .subscribe({
        next: (session) => {
          this.session = session;
          this.error = null;
          this.loadStats();
        },
        error: () => {
          // Silently ignore refresh errors
        }
      });
  }

  stopPolling() {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }
  }

  getConnectionStatusText(): string {
    switch (this.connectionStatus) {
      case 'connected': return 'En direct';
      case 'connecting': return 'Connexion...';
      case 'reconnecting': return 'Reconnexion...';
      case 'disconnected': return this.usePollingFallback ? 'Actualisation auto' : 'Deconnecte';
    }
  }

  isX01Session(): boolean {
    return !!this.session && isX01(this.session.gameMode);
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
