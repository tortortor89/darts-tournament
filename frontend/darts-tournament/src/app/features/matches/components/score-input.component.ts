import { Component, EventEmitter, Input, Output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type InputMode = 'total' | 'darts';

export interface ThrowData {
  score: number;
  dart1?: string;
  dart2?: string;
  dart3?: string;
  dartsUsed?: number;          // Nombre de fléchettes utilisées (1, 2 ou 3)
  doublesAttempted?: number;   // Nombre de doubles tentés
}

@Component({
  selector: 'app-score-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="score-input-section">
      <div class="input-mode-toggle">
        <button [class.active]="inputMode === 'total'" (click)="setInputMode('total')">
          Total
        </button>
        <button [class.active]="inputMode === 'darts'" (click)="setInputMode('darts')">
          Fléchettes
        </button>
      </div>

      @if (inputMode === 'total') {
        <div class="total-input">
          <div class="score-mode-toggle">
            <button
              [class.active]="!isRemainingMode"
              (click)="setRemainingMode(false)">
              Score fait
            </button>
            <button
              [class.active]="isRemainingMode"
              (click)="setRemainingMode(true)">
              Il reste
            </button>
          </div>

          @if (isRemainingMode) {
            <div class="current-score-hint">Actuellement : {{ currentPlayerScore }}</div>
          }

          <div class="numpad-display" [class.invalid]="!isDisplayValid()">
            <span class="display-value">{{ numpadDisplay || '0' }}</span>
            @if (isRemainingMode && numpadDisplay) {
              <span class="calculated-inline">= {{ currentPlayerScore - getNumpadValue() }} pts</span>
            }
          </div>

          <div class="numpad">
            @for (digit of [1, 2, 3, 4, 5, 6, 7, 8, 9]; track digit) {
              <button class="numpad-btn" (click)="numpadPress(digit)">{{ digit }}</button>
            }
            <button class="numpad-btn numpad-clear" (click)="numpadClear()">C</button>
            <button class="numpad-btn" (click)="numpadPress(0)">0</button>
            <button class="numpad-btn numpad-back" (click)="numpadBack()">&#9003;</button>
          </div>

          @if (!isRemainingMode) {
            <div class="quick-scores">
              @for (s of quickScores; track s) {
                <button (click)="quickScore(s)">{{ s }}</button>
              }
            </div>
          }
        </div>
      } @else {
        <div class="darts-input">
          <!-- Darts display -->
          <div class="darts-display">
            <div class="dart-slot" [class.filled]="dart1" (click)="clearDart(1)">
              <span class="dart-label">D1</span>
              <span class="dart-value">{{ dart1 || '-' }}</span>
            </div>
            <div class="dart-slot" [class.filled]="dart2" (click)="clearDart(2)">
              <span class="dart-label">D2</span>
              <span class="dart-value">{{ dart2 || '-' }}</span>
            </div>
            <div class="dart-slot" [class.filled]="dart3" (click)="clearDart(3)">
              <span class="dart-label">D3</span>
              <span class="dart-value">{{ dart3 || '-' }}</span>
            </div>
            <div class="darts-total">
              <span class="total-label">Total</span>
              <span class="total-value">{{ calculateDartsScore() }}</span>
            </div>
          </div>

          <!-- Multiplier selector -->
          <div class="multiplier-selector">
            <button [class.active]="selectedMultiplier === 'S'" (click)="selectedMultiplier = 'S'">Simple</button>
            <button [class.active]="selectedMultiplier === 'D'" (click)="selectedMultiplier = 'D'">Double</button>
            <button [class.active]="selectedMultiplier === 'T'" (click)="selectedMultiplier = 'T'">Triple</button>
          </div>

          <!-- Number grid -->
          <div class="number-grid">
            @for (num of [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20]; track num) {
              <button class="num-btn" (click)="addDart(num)">{{ num }}</button>
            }
          </div>

          <!-- Bull and Miss -->
          <div class="bull-row">
            <button class="bull-btn" (click)="addBull()">BULL</button>
            <button class="bull-btn db" (click)="addDoubleBull()">DB</button>
            <button class="miss-btn" (click)="addMiss()">MISS</button>
          </div>
        </div>
      }

      @if (showCheckoutButton) {
        <button class="checkout-btn" (click)="initiateCheckout()" type="button">
          CHECKOUT ({{currentPlayerScore}})
        </button>
      }

      <button class="submit-throw" (click)="submitThrow()" [disabled]="!canSubmit()">
        Valider
      </button>

      <!-- Checkout Modal -->
      @if (checkoutModalVisible()) {
        <div class="modal-overlay" (click)="cancelCheckout()">
          <div class="modal-content" (click)="$event.stopPropagation()">
            <h3>Checkout en {{currentPlayerScore}} points</h3>

            @if (!selectedDartsForCheckout) {
              <!-- Étape 1 : Demander le nombre de fléchettes (seuls les cas possibles) -->
              <div class="modal-section">
                <label>Avec combien de fléchettes as-tu réussi ce checkout ?</label>
                <div class="dart-count-buttons">
                  @for (n of checkoutDartsOptions; track n) {
                    <button (click)="selectDartsForCheckout(n)">
                      {{ n }} {{ n === 1 ? 'fléchette' : 'fléchettes' }}
                    </button>
                  }
                </div>
              </div>

              <button class="cancel-btn" (click)="cancelCheckout()">
                Annuler
              </button>
            } @else {
              <!-- Étape 2 : Demander le nombre de doubles tentés (min = nb fléchettes, max = 3) -->
              <div class="modal-section">
                <label>Combien de fléchettes as-tu tentées sur un double durant cette volée ?</label>
                <div class="dart-count-buttons">
                  @for (count of getDoublesAttemptedOptions(selectedDartsForCheckout); track count) {
                    <button (click)="submitCheckout(selectedDartsForCheckout, count)">
                      {{ count }} {{ count === 1 ? 'fléchette' : 'fléchettes' }}
                    </button>
                  }
                </div>
              </div>

              <button class="back-btn" (click)="selectedDartsForCheckout = undefined">
                ← Retour
              </button>
            }
          </div>
        </div>
      }

      <!-- Doubles Attempted Modal (CAS 2 & 3: normal play) -->
      @if (doublesAttemptedModalVisible()) {
        <div class="modal-overlay" (click)="cancelDoublesAttemptedModal()">
          <div class="modal-content" (click)="$event.stopPropagation()">
            <h3>Tentative sur double</h3>
            <p class="modal-info">Score avant la volée : {{currentPlayerScore}} points</p>

            <div class="modal-section">
              <label>Combien de fléchettes as-tu tentées sur un double durant cette volée ?</label>
              <div class="dart-count-buttons doubles-grid">
                @for (count of normalPlayDoublesOptions; track count) {
                  <button (click)="submitThrowWithDoublesAttempted(count)">
                    {{ count === 0 ? 'Aucune' : count + (count === 1 ? ' fléchette' : ' fléchettes') }}
                  </button>
                }
              </div>
            </div>

            <button class="cancel-btn" (click)="cancelDoublesAttemptedModal()">
              Annuler
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .score-input-section {
      background: rgba(255,255,255,0.1);
      border-radius: 12px;
      padding: 20px;
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

    .score-mode-toggle {
      display: flex;
      gap: 5px;
      margin-bottom: 12px;
    }

    .score-mode-toggle button {
      flex: 1;
      padding: 8px;
      border: none;
      border-radius: 6px;
      background: rgba(255,255,255,0.05);
      color: #aaa;
      cursor: pointer;
      font-size: 0.9em;
      transition: all 0.2s;
    }

    .score-mode-toggle button.active {
      background: rgba(255,255,255,0.15);
      color: white;
    }

    .current-score-hint {
      font-size: 0.9em;
      color: #aaa;
      text-align: center;
      margin-bottom: 5px;
    }

    .numpad-display {
      background: rgba(0,0,0,0.3);
      border-radius: 8px;
      padding: 15px;
      margin-bottom: 12px;
      text-align: center;
      min-height: 50px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 10px;
    }

    .numpad-display.invalid {
      background: rgba(220, 53, 69, 0.2);
    }

    .display-value {
      font-size: 2.5em;
      font-weight: bold;
      font-family: monospace;
    }

    .calculated-inline {
      font-size: 1.2em;
      color: #4caf50;
    }

    .numpad {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 8px;
      margin-bottom: 12px;
    }

    .numpad-btn {
      padding: 18px;
      font-size: 1.5em;
      border: none;
      border-radius: 8px;
      background: rgba(255,255,255,0.15);
      color: white;
      cursor: pointer;
      transition: background 0.15s;
      font-weight: bold;
    }

    .numpad-btn:hover {
      background: rgba(255,255,255,0.25);
    }

    .numpad-btn:active {
      background: rgba(255,255,255,0.35);
    }

    .numpad-clear {
      background: rgba(220, 53, 69, 0.4);
    }

    .numpad-clear:hover {
      background: rgba(220, 53, 69, 0.6);
    }

    .numpad-back {
      background: rgba(255, 193, 7, 0.3);
    }

    .numpad-back:hover {
      background: rgba(255, 193, 7, 0.5);
    }

    .quick-scores {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
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

    /* Darts display */
    .darts-display {
      display: flex;
      gap: 8px;
      margin-bottom: 12px;
    }

    .dart-slot {
      flex: 1;
      background: rgba(0,0,0,0.3);
      border-radius: 8px;
      padding: 10px 5px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;
    }

    .dart-slot:hover {
      background: rgba(0,0,0,0.4);
    }

    .dart-slot.filled {
      background: rgba(0, 123, 255, 0.3);
    }

    .dart-slot.filled:hover {
      background: rgba(220, 53, 69, 0.4);
    }

    .dart-label {
      display: block;
      font-size: 0.7em;
      color: #888;
      margin-bottom: 2px;
    }

    .dart-value {
      font-size: 1.1em;
      font-weight: bold;
    }

    .darts-total {
      flex: 1;
      background: rgba(40, 167, 69, 0.2);
      border-radius: 8px;
      padding: 10px 5px;
      text-align: center;
    }

    .total-label {
      display: block;
      font-size: 0.7em;
      color: #888;
      margin-bottom: 2px;
    }

    .total-value {
      font-size: 1.3em;
      font-weight: bold;
      color: #4caf50;
    }

    /* Multiplier selector */
    .multiplier-selector {
      display: flex;
      gap: 6px;
      margin-bottom: 12px;
    }

    .multiplier-selector button {
      flex: 1;
      padding: 12px 8px;
      border: none;
      border-radius: 8px;
      background: rgba(255,255,255,0.1);
      color: #aaa;
      cursor: pointer;
      font-size: 0.95em;
      font-weight: 500;
      transition: all 0.2s;
    }

    .multiplier-selector button.active {
      color: white;
    }

    .multiplier-selector button:first-child.active {
      background: rgba(108, 117, 125, 0.6);
    }

    .multiplier-selector button:nth-child(2).active {
      background: rgba(0, 123, 255, 0.6);
    }

    .multiplier-selector button:last-child.active {
      background: rgba(220, 53, 69, 0.6);
    }

    /* Number grid */
    .number-grid {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 6px;
      margin-bottom: 10px;
    }

    .num-btn {
      padding: 14px 8px;
      font-size: 1.2em;
      border: none;
      border-radius: 8px;
      background: rgba(255,255,255,0.15);
      color: white;
      cursor: pointer;
      font-weight: bold;
      transition: background 0.15s;
    }

    .num-btn:hover {
      background: rgba(255,255,255,0.25);
    }

    .num-btn:active {
      background: rgba(255,255,255,0.35);
    }

    /* Bull row */
    .bull-row {
      display: flex;
      gap: 6px;
    }

    .bull-btn {
      flex: 1;
      padding: 14px;
      font-size: 1em;
      border: none;
      border-radius: 8px;
      background: rgba(40, 167, 69, 0.4);
      color: white;
      cursor: pointer;
      font-weight: bold;
      transition: background 0.15s;
    }

    .bull-btn:hover {
      background: rgba(40, 167, 69, 0.6);
    }

    .bull-btn.db {
      background: rgba(255, 193, 7, 0.4);
    }

    .bull-btn.db:hover {
      background: rgba(255, 193, 7, 0.6);
    }

    .miss-btn {
      flex: 1;
      padding: 14px;
      font-size: 1em;
      border: none;
      border-radius: 8px;
      background: rgba(108, 117, 125, 0.4);
      color: white;
      cursor: pointer;
      font-weight: bold;
      transition: background 0.15s;
    }

    .miss-btn:hover {
      background: rgba(108, 117, 125, 0.6);
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

    /* Checkout button */
    .checkout-btn {
      width: 100%;
      background: #28a745;
      color: white;
      font-weight: bold;
      padding: 15px;
      margin: 10px 0;
      border: none;
      border-radius: 8px;
      font-size: 1.2em;
      cursor: pointer;
      animation: pulse 1.5s infinite;
      transition: background 0.2s;
    }

    .checkout-btn:hover {
      background: #218838;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.7; }
    }

    /* Modal */
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.7);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }

    .modal-content {
      background: #1e1e2e;
      padding: 30px;
      border-radius: 12px;
      max-width: 400px;
      width: 90%;
      color: white;
    }

    .modal-content h3 {
      margin: 0 0 20px 0;
      text-align: center;
      color: #4caf50;
    }

    .modal-section {
      margin-bottom: 20px;
    }

    .modal-section label {
      display: block;
      margin-bottom: 10px;
      font-size: 0.95em;
      color: #aaa;
    }

    .dart-count-buttons {
      display: flex;
      gap: 10px;
      margin: 15px 0;
    }

    .dart-count-buttons button {
      flex: 1;
      padding: 20px;
      font-size: 1.1em;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      font-weight: bold;
      transition: background 0.2s;
    }

    .dart-count-buttons button:hover {
      background: #0056b3;
    }

    .dart-count-buttons button:active {
      background: #004085;
    }

    .cancel-btn {
      width: 100%;
      padding: 12px;
      background: rgba(220, 53, 69, 0.3);
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      font-size: 1em;
      transition: background 0.2s;
    }

    .cancel-btn:hover {
      background: rgba(220, 53, 69, 0.5);
    }

    .back-btn {
      width: 100%;
      padding: 12px;
      background: rgba(108, 117, 125, 0.3);
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      font-size: 1em;
      transition: background 0.2s;
    }

    .back-btn:hover {
      background: rgba(108, 117, 125, 0.5);
    }

    .modal-info {
      text-align: center;
      color: #4caf50;
      font-size: 1.1em;
      margin: 0 0 15px 0;
    }

    .doubles-grid {
      grid-template-columns: repeat(2, 1fr) !important;
    }
  `]
})
export class ScoreInputComponent {
  @Input() quickScores: number[] = [0, 26, 41, 45, 60, 85, 100, 120, 140, 180];
  @Input() currentPlayerScore: number = 501;
  @Input() trackDoubles: boolean = false;  // Active le tracking des doubles tentés

  @Output() throwSubmit = new EventEmitter<ThrowData>();

  inputMode: InputMode = 'total';
  isRemainingMode: boolean = false;
  numpadDisplay: string = '';
  dart1: string = '';
  dart2: string = '';
  dart3: string = '';
  selectedMultiplier: 'S' | 'D' | 'T' = 'S';

  // Checkout functionality
  checkoutModalVisible = signal(false);
  selectedDartsForCheckout?: number;

  // Doubles attempted modal (for normal play on scores <= 50)
  doublesAttemptedModalVisible = signal(false);
  pendingThrowData?: Omit<ThrowData, 'doublesAttempted'>;

  // Show checkout button when score is finishable in 3 darts
  // Finishable: 170, 167, 164, 161, 160, and all scores <= 158 (except 1)
  // Non-finishable in 3 darts: 169, 168, 166, 165, 163, 162, 159
  get showCheckoutButton(): boolean {
    const score = this.currentPlayerScore;
    if (score <= 1) return false;

    // Scores finishable in 3 darts
    if (score <= 158) return true;
    if (score === 160 || score === 161 || score === 164 || score === 167 || score === 170) return true;

    return false;
  }

  setInputMode(mode: InputMode) {
    this.inputMode = mode;
  }

  setRemainingMode(remaining: boolean) {
    this.isRemainingMode = remaining;
    this.numpadDisplay = '';
  }

  // Numpad methods
  numpadPress(digit: number) {
    if (this.numpadDisplay.length >= 3) return;
    this.numpadDisplay += digit.toString();
  }

  numpadBack() {
    this.numpadDisplay = this.numpadDisplay.slice(0, -1);
  }

  numpadClear() {
    this.numpadDisplay = '';
  }

  getNumpadValue(): number {
    return this.numpadDisplay ? parseInt(this.numpadDisplay, 10) : 0;
  }

  isDisplayValid(): boolean {
    const value = this.getNumpadValue();
    if (this.isRemainingMode) {
      const score = this.currentPlayerScore - value;
      return score >= 0 && score <= 180 && value <= this.currentPlayerScore;
    }
    return value >= 0 && value <= 180;
  }

  quickScore(score: number) {
    this.numpadDisplay = score.toString();
    this.submitThrow();
  }

  // Dart input methods
  addDart(num: number) {
    const dartValue = this.selectedMultiplier + num;
    this.setNextDart(dartValue);
  }

  addBull() {
    this.setNextDart('BULL');
  }

  addDoubleBull() {
    this.setNextDart('DB');
  }

  addMiss() {
    this.setNextDart('S0');
  }

  private setNextDart(value: string) {
    if (!this.dart1) {
      this.dart1 = value;
    } else if (!this.dart2) {
      this.dart2 = value;
    } else if (!this.dart3) {
      this.dart3 = value;
    }
  }

  clearDart(dartNum: number) {
    switch (dartNum) {
      case 1:
        this.dart1 = this.dart2;
        this.dart2 = this.dart3;
        this.dart3 = '';
        break;
      case 2:
        this.dart2 = this.dart3;
        this.dart3 = '';
        break;
      case 3:
        this.dart3 = '';
        break;
    }
  }

  submitThrow() {
    if (!this.canSubmit()) return;

    let score: number;
    if (this.inputMode === 'darts') {
      score = this.calculateDartsScore();
    } else if (this.isRemainingMode) {
      score = this.currentPlayerScore - this.getNumpadValue();
    } else {
      score = this.getNumpadValue();
    }

    const throwData: Omit<ThrowData, 'doublesAttempted'> = {
      score,
      dart1: this.dart1 || undefined,
      dart2: this.dart2 || undefined,
      dart3: this.dart3 || undefined
    };

    // MODE A : En mode fléchettes
    if (this.inputMode === 'darts') {
      // Si trackDoubles est activé, calculer automatiquement les doubles tentés
      if (this.trackDoubles) {
        const doublesAttempted = this.calculateDoublesAttempted();
        this.throwSubmit.emit({
          ...throwData,
          doublesAttempted: doublesAttempted > 0 ? doublesAttempted : undefined
        });
      } else {
        // Sans tracking, ne pas inclure doublesAttempted
        this.throwSubmit.emit(throwData);
      }
      this.resetInput();
    }
    // MODE B : En mode total
    else {
      // Si trackDoubles est activé, gérer les 4 cas selon les règles
      if (this.trackDoubles) {
        this.handleTotalModeSubmit(throwData, score);
      } else {
        // Sans tracking, soumettre directement sans popup
        this.throwSubmit.emit(throwData);
        this.resetInput();
      }
    }
  }

  /**
   * Gère la soumission en Mode B (score total) selon les 4 cas spécifiés
   */
  private handleTotalModeSubmit(throwData: Omit<ThrowData, 'doublesAttempted'>, score: number) {
    const scoreAvantVolee = this.currentPlayerScore;
    const scoreApresVolee = scoreAvantVolee - score;

    const estCheckout = scoreApresVolee === 0;
    const estBust = score > scoreAvantVolee || scoreApresVolee === 1;
    const etaitEnPositionDoubleAvant = this.isInDoublePosition(scoreAvantVolee);
    const estEnPositionDoubleApres = this.isInDoublePosition(estBust ? scoreAvantVolee : scoreApresVolee);

    // CAS 1 — Checkout réussi
    if (estCheckout) {
      this.pendingThrowData = throwData;
      this.openCheckoutFlow();
      return;
    }

    // CAS 2 — Pas de checkout, joueur était en position de double avant la volée
    if (etaitEnPositionDoubleAvant && !estCheckout) {
      this.pendingThrowData = throwData;
      this.doublesAttemptedModalVisible.set(true);
      return;
    }

    // CAS 3 — Pas de checkout, joueur n'était pas en position avant, mais l'est devenu
    // (Inclut le cas où après un bust, le score reste en position de double)
    if (!etaitEnPositionDoubleAvant && estEnPositionDoubleApres && !estCheckout) {
      this.pendingThrowData = throwData;
      this.doublesAttemptedModalVisible.set(true);
      return;
    }

    // CAS 4 — Aucune popup nécessaire
    // Pas en position avant, pas en position après, pas de checkout
    this.throwSubmit.emit({
      ...throwData,
      doublesAttempted: 0
    });
    this.resetInput();
  }

  submitThrowWithDoublesAttempted(doublesAttempted: number) {
    if (!this.pendingThrowData) return;

    this.throwSubmit.emit({
      ...this.pendingThrowData,
      doublesAttempted
    });

    this.doublesAttemptedModalVisible.set(false);
    this.pendingThrowData = undefined;
    this.resetInput();
  }

  cancelDoublesAttemptedModal() {
    this.doublesAttemptedModalVisible.set(false);
    this.pendingThrowData = undefined;
  }

  canSubmit(): boolean {
    if (this.inputMode === 'darts') {
      return this.calculateDartsScore() >= 0;
    }

    if (!this.numpadDisplay) return false;

    return this.isDisplayValid();
  }

  calculateDartsScore(): number {
    let total = 0;
    total += this.parseDartScore(this.dart1);
    total += this.parseDartScore(this.dart2);
    total += this.parseDartScore(this.dart3);
    return total;
  }

  parseDartScore(dart: string): number {
    if (!dart) return 0;
    dart = dart.toUpperCase().trim();

    if (dart === 'BULL') return 25;
    if (dart === 'DB') return 50;
    if (dart === 'S0') return 0; // Miss

    const match = dart.match(/^([SDT])(\d+)$/);
    if (!match) return 0;

    const [, type, numStr] = match;
    const num = parseInt(numStr, 10);

    if (num < 0 || num > 20) return 0;

    switch (type) {
      case 'S': return num;
      case 'D': return num * 2;
      case 'T': return num * 3;
      default: return 0;
    }
  }

  /**
   * Calcule automatiquement le nombre de doubles tentés en mode fléchettes
   * selon les règles exactes :
   * - Cas Bull (score = 50): miss (0), S25, ou DB (50)
   * - Cas doubles D1-D20 (score pair <= 40): miss (0), simple (moitié), ou double (score exact)
   */
  calculateDoublesAttempted(): number {
    let remainingScore = this.currentPlayerScore;
    let doublesAttempted = 0;

    const darts = [this.dart1, this.dart2, this.dart3].filter(d => d);

    for (const dart of darts) {
      const dartScore = this.parseDartScore(dart);

      // Vérifier si c'est une tentative sur double selon les règles exactes
      if (this.isDoubleAttempt(remainingScore, dartScore)) {
        doublesAttempted++;
      }

      // Soustraire le score de cette fléchette pour la prochaine itération
      remainingScore -= dartScore;

      // Si bust, le score reste inchangé
      if (remainingScore < 0 || remainingScore === 1) {
        remainingScore = this.currentPlayerScore;
      }
    }

    return doublesAttempted;
  }

  /**
   * Détermine si une fléchette est une tentative sur double selon les règles exactes
   * @param scoreAvant Score restant AVANT le lancer
   * @param valeurSaisie Points marqués par la fléchette
   */
  private isDoubleAttempt(scoreAvant: number, valeurSaisie: number): boolean {
    // Cas Bull (score = 50)
    if (scoreAvant === 50) {
      return valeurSaisie === 0     // Miss complet
          || valeurSaisie === 25    // S25 (Bull simple), tentative ratée
          || valeurSaisie === 50;   // DB réussi, checkout
    }

    // Cas doubles classiques D1..D20
    if (scoreAvant % 2 === 0 && scoreAvant <= 40) {
      return valeurSaisie === 0                    // Miss complet
          || valeurSaisie === scoreAvant / 2       // Simple = moitié (ex: S16 pour 32)
          || valeurSaisie === scoreAvant;          // Double réussi, checkout
    }

    return false;
  }

  /**
   * Vérifie si un score est en position de double
   * Position de double = 50 (Bull) OU (pair ET <= 40)
   */
  isInDoublePosition(score: number): boolean {
    return score === 50 || (score % 2 === 0 && score >= 2 && score <= 40);
  }

  /**
   * Vérifie si un score est finissable (alias pour isInDoublePosition)
   */
  isScoreFinishable(score: number): boolean {
    return this.isInDoublePosition(score);
  }

  /**
   * Retourne le double cible et le single correspondant pour un score donné
   */
  getTargetDouble(score: number): { double: string, single: string } | null {
    if (score === 50) return { double: 'DB', single: 'BULL' };
    if (score >= 2 && score <= 40 && score % 2 === 0) {
      const num = score / 2;
      return { double: `D${num}`, single: `S${num}` };
    }
    return null;
  }

  // Toutes les valeurs atteignables avec une seule fléchette (simples, doubles, triples, 25, 50)
  private static readonly SINGLE_DART_SCORES: number[] = (() => {
    const scores = new Set<number>();
    for (let n = 1; n <= 20; n++) {
      scores.add(n);
      scores.add(n * 2);
      scores.add(n * 3);
    }
    scores.add(25);
    scores.add(50);
    return [...scores];
  })();

  /**
   * Nombre minimum de fléchettes pour finir un score
   * (le maximum est toujours 3 : on peut rater avant de toucher le double)
   */
  private minDartsToCheckout(score: number): number {
    if (this.isInDoublePosition(score)) return 1;
    if (ScoreInputComponent.SINGLE_DART_SCORES.some(v => this.isInDoublePosition(score - v))) return 2;
    return 3;
  }

  /**
   * Options de fléchettes utilisées pour le checkout en cours (cas possibles uniquement)
   */
  get checkoutDartsOptions(): number[] {
    const options: number[] = [];
    for (let i = this.minDartsToCheckout(this.currentPlayerScore); i <= 3; i++) {
      options.push(i);
    }
    return options;
  }

  /**
   * Options de doubles tentés hors checkout (3 fléchettes jouées) :
   * les fléchettes lancées avant d'atteindre une position de double
   * ne peuvent pas être des tentatives
   */
  get normalPlayDoublesOptions(): number[] {
    const max = this.isInDoublePosition(this.currentPlayerScore) ? 3 : 2;
    const options: number[] = [];
    for (let i = 0; i <= max; i++) {
      options.push(i);
    }
    return options;
  }

  // Checkout button functionality
  initiateCheckout() {
    this.openCheckoutFlow();
  }

  /**
   * Ouvre le flux checkout en sautant les étapes qui n'ont qu'une seule réponse possible
   */
  private openCheckoutFlow() {
    this.selectedDartsForCheckout = undefined;

    const dartsOptions = this.checkoutDartsOptions;
    if (dartsOptions.length === 1) {
      const darts = dartsOptions[0];

      if (!this.trackDoubles) {
        this.submitCheckout(darts);
        return;
      }

      const doublesOptions = this.getDoublesAttemptedOptions(darts);
      if (doublesOptions.length === 1) {
        this.submitCheckout(darts, doublesOptions[0]);
        return;
      }

      // Étape 1 inutile : passer directement à la question des doubles
      this.selectedDartsForCheckout = darts;
    }

    this.checkoutModalVisible.set(true);
  }

  selectDartsForCheckout(darts: number) {
    // Sans tracking des doubles, le nombre de fléchettes suffit (utile pour la moyenne)
    if (!this.trackDoubles) {
      this.submitCheckout(darts);
      return;
    }

    const doublesOptions = this.getDoublesAttemptedOptions(darts);
    if (doublesOptions.length === 1) {
      this.submitCheckout(darts, doublesOptions[0]);
    } else {
      this.selectedDartsForCheckout = darts;
    }
  }

  submitCheckout(dartsUsed: number, doublesAttempted?: number) {
    const checkoutScore = this.currentPlayerScore;

    if (!this.pendingThrowData) {
      // Cas du bouton CHECKOUT direct
      this.throwSubmit.emit({
        score: checkoutScore,
        dartsUsed,
        doublesAttempted
      });
    } else {
      // Cas de la popup normale qui déclenche un checkout
      this.throwSubmit.emit({
        ...this.pendingThrowData,
        dartsUsed,
        doublesAttempted
      });
      this.pendingThrowData = undefined;
    }

    this.checkoutModalVisible.set(false);
    this.selectedDartsForCheckout = undefined;
    this.resetInput();
  }

  cancelCheckout() {
    this.checkoutModalVisible.set(false);
    this.selectedDartsForCheckout = undefined;
    this.pendingThrowData = undefined;
  }

  /**
   * Retourne les options de doubles tentés pour un checkout
   * Minimum = 1 (la fléchette de checkout est forcément sur un double)
   * Maximum = nombre de fléchettes si on était en position de double au début
   * de la volée, sinon une fléchette au moins a servi à préparer le double
   */
  getDoublesAttemptedOptions(dartsUsed: number): number[] {
    const wasOnDouble = this.isInDoublePosition(this.currentPlayerScore);
    const max = wasOnDouble ? dartsUsed : Math.max(dartsUsed - 1, 1);
    const options: number[] = [];
    for (let i = 1; i <= max; i++) {
      options.push(i);
    }
    return options;
  }

  resetInput() {
    this.numpadDisplay = '';
    this.dart1 = '';
    this.dart2 = '';
    this.dart3 = '';
    this.selectedMultiplier = 'S';
  }
}
