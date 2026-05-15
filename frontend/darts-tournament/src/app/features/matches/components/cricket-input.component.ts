import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecordCricketThrowRequest } from '../../../core/models';

@Component({
  selector: 'app-cricket-input',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="cricket-input">
      <h4>Saisie Cricket</h4>

      <!-- Grille de cibles -->
      <div class="target-grid">
        @for (target of targets; track target) {
          <button
            class="target-btn"
            [class.selected]="selectedTarget === target"
            (click)="selectTarget(target)">
            {{ target === 25 ? 'BULL' : target }}
          </button>
        }
      </div>

      @if (selectedTarget !== null) {
        <div class="hits-selector">
          <p>Cible sélectionnée: <strong>{{ selectedTarget === 25 ? 'BULL' : selectedTarget }}</strong></p>
          <div class="hits-buttons">
            <button (click)="submitHits(1)" class="simple">Simple (1 hit)</button>
            <button (click)="submitHits(2)" class="double">Double (2 hits)</button>
            <button (click)="submitHits(3)" class="triple">Triple (3 hits)</button>
          </div>
          <button class="cancel-btn" (click)="cancel()">Annuler</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .cricket-input {
      padding: 20px;
      background: #f8f9fa;
      border-radius: 8px;
      max-width: 600px;
      margin: 0 auto;
    }

    h4 {
      text-align: center;
      margin-bottom: 20px;
      color: #333;
    }

    .target-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 10px;
      margin-bottom: 20px;
    }

    .target-btn {
      padding: 20px;
      font-size: 1.5em;
      font-weight: bold;
      border: 2px solid #007bff;
      background: white;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .target-btn:hover {
      background: #e7f3ff;
    }

    .target-btn.selected {
      background: #007bff;
      color: white;
      transform: scale(1.05);
    }

    .hits-selector {
      text-align: center;
      padding: 20px;
      background: white;
      border-radius: 8px;
      border: 2px solid #007bff;
    }

    .hits-selector p {
      margin-bottom: 15px;
      font-size: 1.1em;
    }

    .hits-buttons {
      display: flex;
      gap: 10px;
      justify-content: center;
      margin: 20px 0;
      flex-wrap: wrap;
    }

    .hits-buttons button {
      padding: 15px 25px;
      font-size: 1.1em;
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
      font-weight: 600;
    }

    .hits-buttons button.simple {
      background: #28a745;
    }

    .hits-buttons button.double {
      background: #ffc107;
      color: #000;
    }

    .hits-buttons button.triple {
      background: #dc3545;
    }

    .hits-buttons button:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    .cancel-btn {
      padding: 10px 20px;
      background: #6c757d;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 1em;
    }

    .cancel-btn:hover {
      background: #5a6268;
    }

    @media (max-width: 768px) {
      .target-grid {
        grid-template-columns: repeat(3, 1fr);
      }

      .target-btn {
        padding: 15px;
        font-size: 1.3em;
      }

      .hits-buttons {
        flex-direction: column;
      }

      .hits-buttons button {
        width: 100%;
      }
    }
  `]
})
export class CricketInputComponent {
  @Output() throwSubmit = new EventEmitter<RecordCricketThrowRequest>();

  targets = [20, 19, 18, 17, 16, 15, 25];  // 25 = Bull
  selectedTarget: number | null = null;

  selectTarget(target: number) {
    this.selectedTarget = target;
  }

  submitHits(hits: number) {
    if (this.selectedTarget === null) return;

    this.throwSubmit.emit({
      target: this.selectedTarget,
      hits: hits
    });

    this.selectedTarget = null;
  }

  cancel() {
    this.selectedTarget = null;
  }
}
