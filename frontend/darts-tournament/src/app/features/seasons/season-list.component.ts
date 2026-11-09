import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Season, SeasonStatus } from '../../core/models';

@Component({
  selector: 'app-season-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h2>Saisons</h2>
      <p class="intro">Une saison regroupe tournois, circuits et championnats interclubs, tous joués en 501 double out.</p>

      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (authService.isAdmin()) {
        <div class="add-form">
          <h3>Créer une saison</h3>
          <form (ngSubmit)="createSeason()">
            <div class="form-row">
              <input type="text" [(ngModel)]="form.name" name="name" placeholder="Nom (ex. Saison 2026-2027)" required>
              <label>
                Début
                <input type="date" [(ngModel)]="form.startDate" name="startDate">
              </label>
              <label>
                Fin
                <input type="date" [(ngModel)]="form.endDate" name="endDate">
              </label>
              <label>
                Legs à gagner
                <input type="number" [(ngModel)]="form.legsToWin" name="legsToWin" min="1" max="10" class="legs-input">
              </label>
            </div>
            <button type="submit" [disabled]="!form.name">Créer</button>
          </form>
        </div>
      }

      <div class="season-list">
        @for (season of seasons; track season.id) {
          <div class="season-card">
            <h3>
              <a [routerLink]="['/seasons', season.id]">{{ season.name }}</a>
              <span class="badge" [class.closed]="season.status === SeasonStatus.Closed">
                {{ season.status === SeasonStatus.Closed ? 'Clôturée' : 'En cours' }}
              </span>
            </h3>
            @if (season.startDate || season.endDate) {
              <p>{{ season.startDate ? (season.startDate | date:'dd/MM/yyyy') : '…' }} → {{ season.endDate ? (season.endDate | date:'dd/MM/yyyy') : '…' }}</p>
            }
            <p>501 double out, {{ season.legsToWin }} leg{{ season.legsToWin > 1 ? 's' : '' }} gagnant{{ season.legsToWin > 1 ? 's' : '' }}</p>
            <p>
              {{ season.tournamentCount }} tournoi{{ season.tournamentCount > 1 ? 's' : '' }},
              {{ season.circuitCount }} circuit{{ season.circuitCount > 1 ? 's' : '' }},
              {{ season.championshipCount }} championnat{{ season.championshipCount > 1 ? 's' : '' }}
            </p>
            @if (authService.isAdmin()) {
              <button (click)="deleteSeason(season.id)" class="delete">Supprimer</button>
            }
          </div>
        } @empty {
          @if (!loading) {
            <p class="empty">Aucune saison pour le moment.</p>
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
    .intro {
      color: var(--text-muted);
      margin-top: 0;
    }
    .add-form {
      margin-bottom: 20px;
      padding: 15px;
      background: var(--surface-alt);
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
    .form-row label {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.9em;
      color: var(--text-muted);
    }
    .add-form input {
      padding: 8px;
      border: 1px solid var(--border);
      border-radius: 4px;
    }
    .legs-input {
      width: 60px;
    }
    .add-form button {
      padding: 8px 16px;
      background: var(--primary);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      align-self: flex-start;
    }
    .add-form button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .season-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
    }
    .season-card {
      padding: 15px;
      border: 1px solid var(--border);
      border-radius: 8px;
    }
    .season-card h3 {
      margin-top: 0;
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }
    .season-card h3 a {
      color: var(--link);
      text-decoration: none;
    }
    .season-card h3 a:hover {
      text-decoration: underline;
    }
    .badge {
      padding: 2px 10px;
      border-radius: 10px;
      font-size: 0.7em;
      color: white;
      background: var(--action);
    }
    .badge.closed {
      background: var(--secondary);
    }
    button.delete {
      background: var(--danger);
      color: white;
      border: none;
      padding: 5px 10px;
      border-radius: 4px;
      cursor: pointer;
    }
    .loading, .empty {
      text-align: center;
      padding: 20px;
      color: var(--text-muted);
    }
  `]
})
export class SeasonListComponent implements OnInit {
  seasons: Season[] = [];
  form = this.emptyForm();
  loading = false;

  SeasonStatus = SeasonStatus;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);

  constructor(public authService: AuthService, private apiService: ApiService) {}

  ngOnInit() {
    this.loadSeasons();
  }

  loadSeasons() {
    this.loading = true;
    this.apiService.getSeasons().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(seasons => {
      this.seasons = seasons;
      this.loading = false;
    });
  }

  createSeason() {
    this.apiService.createSeason({
      name: this.form.name,
      startDate: this.form.startDate || null,
      endDate: this.form.endDate || null,
      legsToWin: this.form.legsToWin
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(newSeason => {
      this.notificationService.showSuccess('Saison créée avec succès');
      this.seasons = [newSeason, ...this.seasons];
      this.form = this.emptyForm();
    });
  }

  deleteSeason(id: number) {
    if (confirm('Voulez-vous vraiment supprimer cette saison ? Les tournois, circuits et championnats ne seront pas supprimés, seulement détachés.')) {
      this.apiService.deleteSeason(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Saison supprimée');
        this.seasons = this.seasons.filter(s => s.id !== id);
      });
    }
  }

  private emptyForm() {
    return { name: '', startDate: '', endDate: '', legsToWin: 3 };
  }
}
