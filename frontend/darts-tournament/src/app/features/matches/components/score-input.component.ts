import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type InputMode = 'total' | 'darts';

export interface ThrowData {
  score: number;
  dart1?: string;
  dart2?: string;
  dart3?: string;
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

      <button class="submit-throw" (click)="submitThrow()" [disabled]="!canSubmit()">
        Valider
      </button>
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
  `]
})
export class ScoreInputComponent {
  @Input() quickScores: number[] = [0, 26, 41, 45, 60, 85, 100, 120, 140, 180];
  @Input() currentPlayerScore: number = 501;

  @Output() throwSubmit = new EventEmitter<ThrowData>();

  inputMode: InputMode = 'total';
  isRemainingMode: boolean = false;
  numpadDisplay: string = '';
  dart1: string = '';
  dart2: string = '';
  dart3: string = '';
  selectedMultiplier: 'S' | 'D' | 'T' = 'S';

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

    const throwData: ThrowData = {
      score,
      dart1: this.dart1 || undefined,
      dart2: this.dart2 || undefined,
      dart3: this.dart3 || undefined
    };

    this.throwSubmit.emit(throwData);
    this.resetInput();
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

  resetInput() {
    this.numpadDisplay = '';
    this.dart1 = '';
    this.dart2 = '';
    this.dart3 = '';
    this.selectedMultiplier = 'S';
  }
}
