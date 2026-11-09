import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CricketHit } from '../../../core/models';

interface CricketInputState {
  [target: number]: number; // target -> nombre de marques
}

@Component({
  selector: 'app-cricket-input',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="cricket-input">
      <div class="info-bar">
        <strong>Marques: {{ totalMarks }}</strong>
        <span class="targets-count"> • Fléchettes: {{ usedDarts }}/3</span>
      </div>

      <!-- Grille de cibles -->
      <div class="target-grid">
        @for (target of targets; track target) {
          <button
            class="target-btn"
            [class.has-marks]="targetMarks[target] > 0"
            [class.disabled]="!canAddMark(target)"
            [disabled]="!canAddMark(target)"
            (click)="addMark(target)">
            <div class="target-label">{{ target === 25 ? 'BULL' : target }}</div>
            @if (targetMarks[target] > 0) {
              <div class="marks-display">{{ getMarkSymbol(targetMarks[target]) }}</div>
            }
          </button>
        }
      </div>

      <!-- Boutons d'action -->
      <div class="action-buttons">
        <button
          class="clear-btn"
          (click)="clear()"
          [disabled]="totalMarks === 0">
          Effacer
        </button>
        <button
          class="submit-btn"
          (click)="submit()">
          Valider visite
        </button>
      </div>

      @if (errorMessage) {
        <div class="error-message">{{ errorMessage }}</div>
      }
    </div>
  `,
  styles: [`
    .cricket-input {
      padding: 12px;
      background: var(--surface-alt);
      border-radius: 8px;
      max-width: 600px;
      margin: 0 auto;
    }

    .info-bar {
      text-align: center;
      margin-bottom: 10px;
      padding: 6px;
      background: white;
      border-radius: 6px;
      border: 1px solid var(--border);
      font-size: 1em;
      color: var(--text-muted);
    }

    .targets-count {
      color: var(--link);
    }

    .target-grid {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
      gap: 6px;
      margin-bottom: 10px;
    }

    .target-btn {
      padding: 6px 4px;
      font-size: 1em;
      font-weight: bold;
      border: 2px solid var(--primary);
      background: white;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
      position: relative;
      min-height: 56px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 2px;
    }

    .target-btn:hover:not(:disabled) {
      background: color-mix(in srgb, var(--primary) 10%, transparent);
      transform: scale(1.05);
    }

    .target-btn.has-marks {
      background: var(--primary);
      color: white;
      border-color: var(--primary-hover);
    }

    .target-btn.has-marks:hover:not(:disabled) {
      background: var(--primary-hover);
    }

    .target-btn.disabled,
    .target-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
      transform: none !important;
    }

    .target-label {
      font-size: 1.1em;
    }

    .marks-display {
      font-size: 1.2em;
      font-weight: bold;
      color: white;
      line-height: 1;
    }

    .action-buttons {
      display: flex;
      gap: 10px;
      justify-content: center;
      margin-top: 10px;
    }

    .action-buttons button {
      padding: 10px 24px;
      font-size: 1.1em;
      font-weight: 600;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
      flex: 1;
      max-width: 200px;
    }

    .action-buttons button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .clear-btn {
      background: var(--secondary);
      color: white;
    }

    .clear-btn:hover:not(:disabled) {
      background: var(--secondary-hover);
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    .submit-btn {
      background: var(--action);
      color: white;
    }

    .submit-btn:hover {
      background: var(--action-hover);
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    .error-message {
      margin-top: 10px;
      padding: 8px;
      background: color-mix(in srgb, var(--danger) 15%, transparent);
      color: var(--danger);
      border: 1px solid color-mix(in srgb, var(--danger) 30%, transparent);
      border-radius: 4px;
      text-align: center;
    }

    @media (max-width: 768px) {
      .target-grid {
        grid-template-columns: repeat(4, 1fr);
      }

      .target-btn {
        min-height: 52px;
      }

      .action-buttons button {
        max-width: 100%;
      }
    }
  `]
})
export class CricketInputComponent {
  @Output() turnSubmit = new EventEmitter<CricketHit[]>();

  targets = [20, 19, 18, 17, 16, 15, 25];  // 25 = Bull
  targetMarks: CricketInputState = {
    15: 0, 16: 0, 17: 0, 18: 0, 19: 0, 20: 0, 25: 0
  };

  errorMessage = '';

  get totalMarks(): number {
    return Object.values(this.targetMarks).reduce((sum, marks) => sum + marks, 0);
  }

  // Nombre minimum de fléchettes pour réaliser les marques saisies
  // (1 fléchette = 1 cible, max 3 marques par fléchette, 2 sur le Bull)
  get usedDarts(): number {
    return this.minDartsNeeded(this.targetMarks);
  }

  private minDartsNeeded(marks: CricketInputState): number {
    return Object.entries(marks).reduce((sum, [target, m]) =>
      sum + Math.ceil(m / (Number(target) === 25 ? 2 : 3)), 0);
  }

  canAddMark(target: number): boolean {
    const simulated = { ...this.targetMarks, [target]: this.targetMarks[target] + 1 };
    return this.minDartsNeeded(simulated) <= 3;
  }

  addMark(target: number): void {
    this.errorMessage = '';

    if (!this.canAddMark(target)) {
      this.errorMessage = 'Impossible avec 3 fléchettes (max 3 marques par fléchette, 2 sur le Bull)';
      return;
    }

    this.targetMarks[target]++;
  }

  getMarkSymbol(count: number): string {
    // Afficher le symbole en fonction du nombre de marques
    switch (count) {
      case 1: return '/';
      case 2: return 'X';
      case 3: return '⊗';
      default: return count.toString();
    }
  }

  clear(): void {
    this.targetMarks = {
      15: 0, 16: 0, 17: 0, 18: 0, 19: 0, 20: 0, 25: 0
    };
    this.errorMessage = '';
  }

  submit(): void {
    // Convertir l'état en liste de CricketHit
    const hits: CricketHit[] = [];

    for (const target of this.targets) {
      const marks = this.targetMarks[target];
      if (marks > 0) {
        hits.push({ target, marks });
      }
    }

    // Émettre la visite (peut être vide pour 3 ratés)
    this.turnSubmit.emit(hits);

    // Réinitialiser après soumission
    this.clear();
  }
}
