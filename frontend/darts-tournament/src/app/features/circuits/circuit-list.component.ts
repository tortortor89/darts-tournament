import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Circuit } from '../../core/models';

@Component({
  selector: 'app-circuit-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h2>Circuits</h2>

      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (authService.isAdmin()) {
        <div class="add-form">
          <h3>Créer un circuit</h3>
          <form (ngSubmit)="createCircuit()">
            <div class="form-row">
              <input type="text" [(ngModel)]="form.name" name="name" placeholder="Nom du circuit" required>
              <input type="text" [(ngModel)]="form.description" name="description" placeholder="Description (optionnelle)" class="description-input">
            </div>
            <p class="hint">Le barème de points par défaut sera appliqué (modifiable ensuite sur la page du circuit).</p>
            <button type="submit">Créer</button>
          </form>
        </div>
      }

      <div class="circuit-list">
        @for (circuit of circuits; track circuit.id) {
          <div class="circuit-card">
            <h3>
              <a [routerLink]="['/circuits', circuit.id]">{{ circuit.name }}</a>
            </h3>
            @if (circuit.description) {
              <p class="description">{{ circuit.description }}</p>
            }
            <p>Tournois: {{ circuit.tournamentCount }} ({{ circuit.completedTournamentCount }} terminé{{ circuit.completedTournamentCount > 1 ? 's' : '' }})</p>
            <p>Créé le {{ circuit.createdAt | date:'dd/MM/yyyy' }}</p>
            @if (authService.isAdmin()) {
              <button (click)="deleteCircuit(circuit.id)" class="delete">Supprimer</button>
            }
          </div>
        } @empty {
          @if (!loading) {
            <p class="empty">Aucun circuit pour le moment.</p>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    .container {
      max-width: 1000px;
      margin: 20px auto;
      padding: 20px;
    }
    .add-form {
      margin-bottom: 20px;
      padding: 15px;
      background: #f5f5f5;
      border-radius: 8px;
    }
    .add-form form {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .form-row {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      align-items: center;
    }
    .description-input {
      flex: 1;
      min-width: 250px;
    }
    .hint {
      margin: 0;
      font-size: 0.85em;
      color: #666;
    }
    .add-form input {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    .add-form button {
      padding: 8px 16px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      align-self: flex-start;
    }
    .circuit-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
    }
    .circuit-card {
      padding: 15px;
      border: 1px solid #ddd;
      border-radius: 8px;
    }
    .circuit-card h3 {
      margin-top: 0;
    }
    .circuit-card h3 a {
      color: #007bff;
      text-decoration: none;
    }
    .circuit-card h3 a:hover {
      text-decoration: underline;
    }
    .circuit-card .description {
      color: #666;
      font-style: italic;
    }
    button.delete {
      background: #dc3545;
      color: white;
      border: none;
      padding: 5px 10px;
      border-radius: 4px;
      cursor: pointer;
    }
    .loading, .empty {
      text-align: center;
      padding: 20px;
      color: #666;
    }
  `]
})
export class CircuitListComponent implements OnInit {
  circuits: Circuit[] = [];
  form = {
    name: '',
    description: ''
  };
  loading = false;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);

  constructor(public authService: AuthService, private apiService: ApiService) {}

  ngOnInit() {
    this.loadCircuits();
  }

  loadCircuits() {
    this.loading = true;
    this.apiService.getCircuits().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(circuits => {
      this.circuits = circuits;
      this.loading = false;
    });
  }

  createCircuit() {
    this.apiService.createCircuit({
      name: this.form.name,
      description: this.form.description || undefined
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(newCircuit => {
      this.notificationService.showSuccess('Circuit créé avec succès');
      this.circuits = [newCircuit, ...this.circuits];
      this.form = { name: '', description: '' };
    });
  }

  deleteCircuit(id: number) {
    if (confirm('Voulez-vous vraiment supprimer ce circuit ? Les tournois ne seront pas supprimés, seulement détachés du circuit.')) {
      this.apiService.deleteCircuit(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Circuit supprimé');
        this.circuits = this.circuits.filter(c => c.id !== id);
      });
    }
  }
}
