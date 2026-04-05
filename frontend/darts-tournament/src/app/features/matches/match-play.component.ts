import { Component, OnInit, OnDestroy, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { NotificationService } from '../../core/services/notification.service';
import { Match, MatchSession, MatchSessionStatus, PlayerSessionInfo } from '../../core/models';

type GamePhase = 'loading' | 'config' | 'playing' | 'finished';
type InputMode = 'total' | 'darts';

@Component({
  selector: 'app-match-play',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="match-container">
      <!-- Loading -->
      @if (phase === 'loading') {
        <div class="loading">Chargement...</div>
      }

      <!-- Configuration -->
      @if (phase === 'config' && match) {
        <div class="config-screen">
          <h2>Configuration du match</h2>
          <div class="match-info">
            <span class="player">{{ match.player1Name }}</span>
            <span class="vs">VS</span>
            <span class="player">{{ match.player2Name }}</span>
          </div>

          <div class="config-form">
            <div class="form-group">
              <label>Nombre de legs gagnants</label>
              <div class="legs-selector">
                @for (n of [1, 2, 3, 4, 5]; track n) {
                  <button
                    type="button"
                    [class.selected]="config.legsToWin === n"
                    (click)="config.legsToWin = n">
                    {{ n }}
                  </button>
                }
              </div>
              <span class="hint">Premier à {{ config.legsToWin }} leg(s)</span>
            </div>

            <div class="form-group">
              <label>Qui commence ?</label>
              <div class="starter-selector">
                <button
                  type="button"
                  [class.selected]="config.startingPlayerId === match.player1Id"
                  (click)="config.startingPlayerId = match.player1Id!">
                  {{ match.player1Name }}
                </button>
                <button
                  type="button"
                  [class.selected]="config.startingPlayerId === match.player2Id"
                  (click)="config.startingPlayerId = match.player2Id!">
                  {{ match.player2Name }}
                </button>
              </div>
            </div>

            <div class="form-group">
              <label>Mode de jeu</label>
              <div class="game-mode">501 - Straight In, Double Out</div>
            </div>

            <button class="start-btn" (click)="startMatch()" [disabled]="!config.startingPlayerId">
              Commencer le match
            </button>
          </div>
        </div>
      }

      <!-- Playing -->
      @if (phase === 'playing' && session) {
        <div class="play-screen">
          <!-- Score Header -->
          <div class="score-header">
            <div class="player-score" [class.active]="session.currentPlayerId === session.player1.playerId">
              <div class="player-name">{{ session.player1.name }}</div>
              <div class="legs">Legs: {{ session.player1.legsWon }}</div>
              <div class="remaining">{{ session.player1.currentScore }}</div>
              @if (session.currentPlayerId === session.player1.playerId) {
                <div class="turn-indicator">A toi !</div>
              }
            </div>

            <div class="match-status">
              <div class="legs-to-win">Premier à {{ session.legsToWin }}</div>
              <div class="current-leg">Leg {{ session.currentLeg }}</div>
            </div>

            <div class="player-score" [class.active]="session.currentPlayerId === session.player2.playerId">
              <div class="player-name">{{ session.player2.name }}</div>
              <div class="legs">Legs: {{ session.player2.legsWon }}</div>
              <div class="remaining">{{ session.player2.currentScore }}</div>
              @if (session.currentPlayerId === session.player2.playerId) {
                <div class="turn-indicator">A toi !</div>
              }
            </div>
          </div>

          <!-- Throw History for current leg -->
          <div class="throw-history">
            <h4>Historique du leg</h4>
            <div class="throws-list">
              @for (t of session.currentLegThrows; track t.id) {
                <div class="throw-item" [class.bust]="t.isBust" [class.checkout]="t.isCheckout">
                  <span class="throw-player">{{ getPlayerShortName(t.playerId) }}</span>
                  <span class="throw-score">{{ t.isBust ? 'BUST' : t.score }}</span>
                  <span class="throw-remaining">→ {{ t.remainingScore }}</span>
                </div>
              }
            </div>
          </div>

          <!-- Score Input -->
          <div class="score-input-section">
            <div class="input-mode-toggle">
              <button [class.active]="inputMode === 'total'" (click)="inputMode = 'total'">
                Total
              </button>
              <button [class.active]="inputMode === 'darts'" (click)="inputMode = 'darts'">
                Fléchettes
              </button>
            </div>

            @if (inputMode === 'total') {
              <div class="total-input">
                <input
                  type="number"
                  [(ngModel)]="throwInput.score"
                  min="0"
                  max="180"
                  placeholder="Score"
                  (keyup.enter)="submitThrow()">
                <div class="quick-scores">
                  @for (s of quickScores; track s) {
                    <button (click)="throwInput.score = s; submitThrow()">{{ s }}</button>
                  }
                </div>
              </div>
            } @else {
              <div class="darts-input">
                <div class="dart-inputs">
                  <input [(ngModel)]="throwInput.dart1" placeholder="D1" maxlength="4">
                  <input [(ngModel)]="throwInput.dart2" placeholder="D2" maxlength="4">
                  <input [(ngModel)]="throwInput.dart3" placeholder="D3" maxlength="4">
                </div>
                <div class="dart-help">Format: S1-S20, D1-D20, T1-T20, BULL, DB</div>
                <div class="calculated-score">Score: {{ calculateDartsScore() }}</div>
              </div>
            }

            <button class="submit-throw" (click)="submitThrow()" [disabled]="!canSubmitThrow()">
              Valider
            </button>
          </div>

          <!-- Actions -->
          <div class="match-actions">
            <button class="cancel-btn" (click)="cancelMatch()">Annuler le match</button>
          </div>
        </div>
      }

      <!-- Finished -->
      @if (phase === 'finished' && session) {
        <div class="finished-screen">
          <h2>Match terminé !</h2>

          <div class="final-score">
            <div class="player-final" [class.winner]="session.player1.legsWon > session.player2.legsWon">
              <div class="name">{{ session.player1.name }}</div>
              <div class="legs-final">{{ session.player1.legsWon }}</div>
            </div>
            <div class="separator">-</div>
            <div class="player-final" [class.winner]="session.player2.legsWon > session.player1.legsWon">
              <div class="legs-final">{{ session.player2.legsWon }}</div>
              <div class="name">{{ session.player2.name }}</div>
            </div>
          </div>

          <div class="winner-announcement">
            Vainqueur : {{ getWinner()?.name }}
          </div>

          <div class="validation-actions">
            <button class="validate-btn" (click)="validateMatch()">
              Valider et enregistrer le résultat
            </button>
            <button class="cancel-btn" (click)="cancelMatch()">
              Annuler (ne pas enregistrer)
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .match-container {
      max-width: 800px;
      margin: 20px auto;
      padding: 20px;
    }

    .loading {
      text-align: center;
      padding: 40px;
      color: #666;
    }

    /* Config Screen */
    .config-screen {
      background: #f8f9fa;
      border-radius: 12px;
      padding: 30px;
    }

    .config-screen h2 {
      text-align: center;
      margin-bottom: 20px;
    }

    .match-info {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 20px;
      margin-bottom: 30px;
      font-size: 1.5em;
    }

    .match-info .player {
      font-weight: bold;
    }

    .match-info .vs {
      color: #666;
    }

    .config-form {
      max-width: 400px;
      margin: 0 auto;
    }

    .form-group {
      margin-bottom: 25px;
    }

    .form-group label {
      display: block;
      margin-bottom: 10px;
      font-weight: 600;
    }

    .legs-selector, .starter-selector {
      display: flex;
      gap: 10px;
    }

    .legs-selector button, .starter-selector button {
      flex: 1;
      padding: 12px;
      border: 2px solid #ddd;
      border-radius: 8px;
      background: white;
      cursor: pointer;
      font-size: 1.1em;
      transition: all 0.2s;
    }

    .legs-selector button.selected, .starter-selector button.selected {
      border-color: #007bff;
      background: #007bff;
      color: white;
    }

    .hint {
      display: block;
      margin-top: 8px;
      color: #666;
      font-size: 0.9em;
    }

    .game-mode {
      padding: 12px;
      background: #e9ecef;
      border-radius: 8px;
      text-align: center;
      font-weight: 500;
    }

    .start-btn {
      width: 100%;
      padding: 15px;
      background: #28a745;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 1.2em;
      cursor: pointer;
      margin-top: 20px;
    }

    .start-btn:disabled {
      background: #ccc;
      cursor: not-allowed;
    }

    /* Play Screen */
    .play-screen {
      background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
      border-radius: 12px;
      padding: 20px;
      color: white;
    }

    .score-header {
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      gap: 20px;
      margin-bottom: 20px;
    }

    .player-score {
      background: rgba(255,255,255,0.1);
      border-radius: 12px;
      padding: 20px;
      text-align: center;
      transition: all 0.3s;
    }

    .player-score.active {
      background: rgba(40, 167, 69, 0.3);
      box-shadow: 0 0 20px rgba(40, 167, 69, 0.5);
    }

    .player-name {
      font-size: 1.2em;
      margin-bottom: 5px;
    }

    .legs {
      font-size: 0.9em;
      color: #aaa;
    }

    .remaining {
      font-size: 3em;
      font-weight: bold;
      margin: 10px 0;
    }

    .turn-indicator {
      background: #28a745;
      padding: 5px 15px;
      border-radius: 20px;
      font-size: 0.9em;
    }

    .match-status {
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      gap: 10px;
    }

    .legs-to-win {
      font-size: 0.9em;
      color: #aaa;
    }

    .current-leg {
      font-size: 1.2em;
      font-weight: bold;
    }

    /* Throw History */
    .throw-history {
      background: rgba(255,255,255,0.05);
      border-radius: 8px;
      padding: 15px;
      margin-bottom: 20px;
      max-height: 150px;
      overflow-y: auto;
    }

    .throw-history h4 {
      margin: 0 0 10px 0;
      font-size: 0.9em;
      color: #aaa;
    }

    .throws-list {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }

    .throw-item {
      background: rgba(255,255,255,0.1);
      padding: 5px 10px;
      border-radius: 4px;
      font-size: 0.85em;
    }

    .throw-item.bust {
      background: rgba(220, 53, 69, 0.3);
    }

    .throw-item.checkout {
      background: rgba(40, 167, 69, 0.3);
    }

    .throw-player {
      color: #aaa;
      margin-right: 5px;
    }

    .throw-score {
      font-weight: bold;
      margin-right: 5px;
    }

    .throw-remaining {
      color: #888;
    }

    /* Score Input */
    .score-input-section {
      background: rgba(255,255,255,0.1);
      border-radius: 12px;
      padding: 20px;
      margin-bottom: 20px;
    }

    .input-mode-toggle {
      display: flex;
      gap: 10px;
      margin-bottom: 15px;
    }

    .input-mode-toggle button {
      flex: 1;
      padding: 10px;
      border: none;
      border-radius: 8px;
      background: rgba(255,255,255,0.1);
      color: white;
      cursor: pointer;
    }

    .input-mode-toggle button.active {
      background: #007bff;
    }

    .total-input input {
      width: 100%;
      padding: 15px;
      font-size: 2em;
      text-align: center;
      border: none;
      border-radius: 8px;
      margin-bottom: 15px;
    }

    .quick-scores {
      display: grid;
      grid-template-columns: repeat(6, 1fr);
      gap: 8px;
    }

    .quick-scores button {
      padding: 10px;
      border: none;
      border-radius: 8px;
      background: rgba(255,255,255,0.2);
      color: white;
      cursor: pointer;
      font-size: 1em;
    }

    .quick-scores button:hover {
      background: rgba(255,255,255,0.3);
    }

    .darts-input .dart-inputs {
      display: flex;
      gap: 10px;
      margin-bottom: 10px;
    }

    .darts-input input {
      flex: 1;
      padding: 15px;
      font-size: 1.2em;
      text-align: center;
      border: none;
      border-radius: 8px;
      text-transform: uppercase;
    }

    .dart-help {
      font-size: 0.8em;
      color: #aaa;
      text-align: center;
      margin-bottom: 10px;
    }

    .calculated-score {
      text-align: center;
      font-size: 1.5em;
      font-weight: bold;
    }

    .submit-throw {
      width: 100%;
      padding: 15px;
      background: #28a745;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 1.2em;
      cursor: pointer;
      margin-top: 15px;
    }

    .submit-throw:disabled {
      background: #666;
      cursor: not-allowed;
    }

    .match-actions {
      text-align: center;
    }

    .cancel-btn {
      padding: 10px 20px;
      background: transparent;
      color: #dc3545;
      border: 1px solid #dc3545;
      border-radius: 8px;
      cursor: pointer;
    }

    /* Finished Screen */
    .finished-screen {
      background: linear-gradient(135deg, #155724 0%, #1e7e34 100%);
      border-radius: 12px;
      padding: 40px;
      color: white;
      text-align: center;
    }

    .finished-screen h2 {
      margin-bottom: 30px;
    }

    .final-score {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 30px;
      margin-bottom: 20px;
    }

    .player-final {
      text-align: center;
    }

    .player-final .name {
      font-size: 1.2em;
      margin-bottom: 10px;
    }

    .player-final .legs-final {
      font-size: 4em;
      font-weight: bold;
    }

    .player-final.winner {
      color: #ffc107;
    }

    .separator {
      font-size: 3em;
      color: rgba(255,255,255,0.5);
    }

    .winner-announcement {
      font-size: 1.5em;
      margin-bottom: 30px;
    }

    .validation-actions {
      display: flex;
      flex-direction: column;
      gap: 15px;
      max-width: 300px;
      margin: 0 auto;
    }

    .validate-btn {
      padding: 15px;
      background: #ffc107;
      color: #000;
      border: none;
      border-radius: 8px;
      font-size: 1.1em;
      cursor: pointer;
      font-weight: bold;
    }

    .finished-screen .cancel-btn {
      color: rgba(255,255,255,0.7);
      border-color: rgba(255,255,255,0.3);
    }
  `]
})
export class MatchPlayComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);

  matchId: number = 0;
  match: Match | null = null;
  session: MatchSession | null = null;
  phase: GamePhase = 'loading';
  inputMode: InputMode = 'total';

  config = {
    legsToWin: 3,
    startingPlayerId: 0
  };

  throwInput = {
    score: 0,
    dart1: '',
    dart2: '',
    dart3: ''
  };

  quickScores = [0, 26, 41, 45, 60, 85, 100, 120, 140, 180];

  ngOnInit() {
    this.matchId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadMatch();
  }

  loadMatch() {
    this.phase = 'loading';

    // Load match info
    this.apiService.getMatch(this.matchId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (match) => {
          this.match = match;
          this.config.startingPlayerId = match.player1Id || 0;
          this.checkForExistingSession();
        },
        error: () => {
          this.notificationService.showError('Match non trouvé');
          this.router.navigate(['/tournaments']);
        }
      });
  }

  checkForExistingSession() {
    this.apiService.getMatchSession(this.matchId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.session = session;
          this.updatePhase();
        },
        error: () => {
          // No session exists, go to config
          this.phase = 'config';
        }
      });
  }

  updatePhase() {
    if (!this.session) {
      this.phase = 'config';
      return;
    }

    switch (this.session.status) {
      case MatchSessionStatus.Configuration:
        this.phase = 'config';
        break;
      case MatchSessionStatus.InProgress:
        this.phase = 'playing';
        break;
      case MatchSessionStatus.Finished:
        this.phase = 'finished';
        break;
      default:
        this.phase = 'config';
    }
  }

  startMatch() {
    if (!this.config.startingPlayerId) return;

    this.apiService.startMatchSession(this.matchId, {
      legsToWin: this.config.legsToWin,
      startingPlayerId: this.config.startingPlayerId
    }).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.session = session;
          this.phase = 'playing';
          this.notificationService.showSuccess('Match démarré !');
        },
        error: (err) => {
          this.notificationService.showError(err.error || 'Erreur lors du démarrage');
        }
      });
  }

  submitThrow() {
    if (!this.canSubmitThrow()) return;

    const score = this.inputMode === 'total'
      ? this.throwInput.score
      : this.calculateDartsScore();

    const request = {
      score,
      dart1: this.throwInput.dart1 || undefined,
      dart2: this.throwInput.dart2 || undefined,
      dart3: this.throwInput.dart3 || undefined
    };

    this.apiService.recordThrow(this.matchId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          this.session = session;
          this.resetThrowInput();
          this.updatePhase();
        },
        error: (err) => {
          this.notificationService.showError(err.error || 'Erreur lors de l\'enregistrement');
        }
      });
  }

  canSubmitThrow(): boolean {
    if (this.inputMode === 'total') {
      return this.throwInput.score >= 0 && this.throwInput.score <= 180;
    } else {
      return this.calculateDartsScore() >= 0;
    }
  }

  calculateDartsScore(): number {
    let total = 0;
    total += this.parseDartScore(this.throwInput.dart1);
    total += this.parseDartScore(this.throwInput.dart2);
    total += this.parseDartScore(this.throwInput.dart3);
    return total;
  }

  parseDartScore(dart: string): number {
    if (!dart) return 0;
    dart = dart.toUpperCase().trim();

    if (dart === 'BULL') return 25;
    if (dart === 'DB') return 50;

    const match = dart.match(/^([SDT])(\d+)$/);
    if (!match) return 0;

    const [, type, numStr] = match;
    const num = parseInt(numStr, 10);

    if (num < 1 || num > 20) return 0;

    switch (type) {
      case 'S': return num;
      case 'D': return num * 2;
      case 'T': return num * 3;
      default: return 0;
    }
  }

  resetThrowInput() {
    this.throwInput = {
      score: 0,
      dart1: '',
      dart2: '',
      dart3: ''
    };
  }

  validateMatch() {
    this.apiService.validateMatchSession(this.matchId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Match validé et enregistré !');
          this.router.navigate(['/tournaments', this.match?.tournamentId]);
        },
        error: (err) => {
          this.notificationService.showError(err.error || 'Erreur lors de la validation');
        }
      });
  }

  cancelMatch() {
    if (!confirm('Voulez-vous vraiment annuler ce match ? Les données seront perdues.')) {
      return;
    }

    this.apiService.cancelMatchSession(this.matchId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Match annulé');
          this.router.navigate(['/tournaments', this.match?.tournamentId]);
        },
        error: (err) => {
          this.notificationService.showError(err.error || 'Erreur lors de l\'annulation');
        }
      });
  }

  getPlayerShortName(playerId: number): string {
    if (!this.session) return '';
    if (playerId === this.session.player1.playerId) {
      return this.session.player1.name.split(' ')[0];
    }
    return this.session.player2.name.split(' ')[0];
  }

  getWinner(): PlayerSessionInfo | null {
    if (!this.session) return null;
    if (this.session.player1.legsWon > this.session.player2.legsWon) {
      return this.session.player1;
    }
    return this.session.player2;
  }
}
