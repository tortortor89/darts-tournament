import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CricketDisplayState, CricketTargetState } from '../../../core/models';

@Component({
  selector: 'app-cricket-display',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="cricket-display" [class.compact]="compact">
      <div class="cricket-grid">
        <!-- Header -->
        <div class="header player1">{{ player1Name }}</div>
        <div class="header target-col">Cible</div>
        <div class="header player2">{{ player2Name }}</div>

        <!-- Rows -->
        @for (target of targets; track target) {
          <div class="cell marks">
            {{ getMarks(cricketState.player1Targets[target]) }}
          </div>
          <div class="cell target">
            {{ target === 25 ? 'BULL' : target }}
          </div>
          <div class="cell marks">
            {{ getMarks(cricketState.player2Targets[target]) }}
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .cricket-display {
      max-width: 600px;
      margin: 0 auto 20px;
    }

    .cricket-grid {
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      gap: 1px;
      background: #ddd;
      border: 2px solid #333;
    }

    .cell {
      background: white;
      padding: 12px;
      text-align: center;
      font-size: 1.2em;
    }

    .cell.target {
      font-weight: bold;
      background: #007bff;
      color: white;
      font-size: 1.5em;
    }

    .cell.marks {
      font-size: 2em;
      font-weight: bold;
      color: #28a745;
    }

    .header {
      background: #333;
      color: white;
      padding: 10px;
      font-weight: bold;
      text-align: center;
    }

    /* Mode compact : réduit hauteurs et espacements pour l'écran de saisie */
    .compact {
      margin-bottom: 12px;
    }

    .compact .cell {
      padding: 3px 10px;
      font-size: 1em;
    }

    .compact .cell.target {
      font-size: 1.1em;
    }

    .compact .cell.marks {
      font-size: 1.3em;
      line-height: 1.2;
    }

    .compact .header {
      padding: 5px;
    }

    @media (max-width: 768px) {
      .cricket-grid {
        font-size: 0.9em;
      }

      .cell {
        padding: 8px;
      }
    }
  `]
})
export class CricketDisplayComponent {
  @Input() cricketState!: CricketDisplayState;
  @Input() player1Name!: string;
  @Input() player2Name!: string;
  @Input() compact = false;

  targets = [20, 19, 18, 17, 16, 15, 25];

  getMarks(targetState: CricketTargetState): string {
    if (!targetState) return '';

    const hits = targetState.hits;
    if (hits === 0) return '';
    if (hits === 1) return '/';
    if (hits === 2) return 'X';
    return '⊗';  // Fermé
  }
}
