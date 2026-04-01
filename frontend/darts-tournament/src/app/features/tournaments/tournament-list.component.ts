import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Tournament, TournamentFormat, TournamentStatus } from '../../core/models';

@Component({
  selector: 'app-tournament-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h2>Tournois</h2>

      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (authService.isAuthenticated()) {
        <div class="add-form">
          <h3>Créer un tournoi</h3>
          <form (ngSubmit)="createTournament()">
            <div class="form-row">
              <input type="text" [(ngModel)]="form.name" name="name" placeholder="Nom du tournoi" required>
              <select [(ngModel)]="form.format" name="format" (ngModelChange)="onFormatChange()">
                <option [value]="TournamentFormat.SingleElimination">Élimination directe</option>
                <option [value]="TournamentFormat.DoubleElimination">Double élimination</option>
                <option [value]="TournamentFormat.RoundRobin">Round Robin</option>
                <option [value]="TournamentFormat.GroupStage">Phase de groupes</option>
              </select>
              <input type="date" [(ngModel)]="form.startDate" name="startDate">
            </div>

            @if (form.format == TournamentFormat.GroupStage) {
              <div class="form-row group-config">
                <label>
                  Nombre de groupes
                  <input type="number" [(ngModel)]="form.numberOfGroups" name="numberOfGroups" min="2" max="8" placeholder="Auto">
                </label>
                <label>
                  Qualifiés par groupe
                  <input type="number" [(ngModel)]="form.qualifiersPerGroup" name="qualifiersPerGroup" min="1" max="4" [value]="2">
                </label>
                <label class="checkbox-label">
                  <input type="checkbox" [(ngModel)]="form.hasKnockoutPhase" name="hasKnockoutPhase">
                  Phase éliminatoire
                </label>
              </div>
            }

            <button type="submit">Créer</button>
          </form>
        </div>
      }

      <div class="tournament-list">
        @for (tournament of tournaments; track tournament.id) {
          <div class="tournament-card">
            <h3>
              <a [routerLink]="['/tournaments', tournament.id]">{{ tournament.name }}</a>
            </h3>
            <p>Format: {{ getFormatLabel(tournament.format) }}</p>
            <p>Status: <span [class]="'status-' + tournament.status">{{ getStatusLabel(tournament.status) }}</span></p>
            <p>Joueurs: {{ tournament.playerCount }}</p>
            @if (tournament.format === TournamentFormat.GroupStage && tournament.numberOfGroups) {
              <p>{{ tournament.numberOfGroups }} groupes, {{ tournament.qualifiersPerGroup || 2 }} qualifiés/groupe</p>
            }
            @if (tournament.startDate) {
              <p>Date: {{ tournament.startDate | date:'dd/MM/yyyy' }}</p>
            }
            @if (authService.isAuthenticated() && tournament.status === TournamentStatus.Draft) {
              <button (click)="deleteTournament(tournament.id)" class="delete">Supprimer</button>
            }
          </div>
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
    .group-config {
      background: #e9ecef;
      padding: 10px;
      border-radius: 4px;
      margin-top: 5px;
    }
    .group-config label {
      display: flex;
      flex-direction: column;
      gap: 4px;
      font-size: 0.9em;
    }
    .group-config label input[type="number"] {
      width: 80px;
    }
    .checkbox-label {
      flex-direction: row !important;
      align-items: center;
    }
    .checkbox-label input {
      width: auto !important;
      margin-right: 6px;
    }
    .add-form input, .add-form select {
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
    .tournament-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
    }
    .tournament-card {
      padding: 15px;
      border: 1px solid #ddd;
      border-radius: 8px;
    }
    .tournament-card h3 {
      margin-top: 0;
    }
    .tournament-card h3 a {
      color: #007bff;
      text-decoration: none;
    }
    .tournament-card h3 a:hover {
      text-decoration: underline;
    }
    .status-0 { color: #6c757d; }
    .status-1 { color: #28a745; }
    .status-2 { color: #007bff; }
    button.delete {
      background: #dc3545;
      color: white;
      border: none;
      padding: 5px 10px;
      border-radius: 4px;
      cursor: pointer;
    }
    .loading {
      text-align: center;
      padding: 20px;
      color: #666;
    }
  `]
})
export class TournamentListComponent implements OnInit {
  tournaments: Tournament[] = [];
  form = {
    name: '',
    format: TournamentFormat.SingleElimination,
    startDate: '',
    numberOfGroups: null as number | null,
    qualifiersPerGroup: 2,
    hasKnockoutPhase: true
  };
  loading = false;

  TournamentFormat = TournamentFormat;
  TournamentStatus = TournamentStatus;

  private destroyRef = inject(DestroyRef);

  private notificationService = inject(NotificationService);

  constructor(public authService: AuthService, private apiService: ApiService) {}

  ngOnInit() {
    this.loadTournaments();
  }

  loadTournaments() {
    this.loading = true;
    this.apiService.getTournaments().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(tournaments => {
      this.tournaments = tournaments;
      this.loading = false;
    });
  }

  onFormatChange() {
    if (this.form.format != TournamentFormat.GroupStage) {
      this.form.numberOfGroups = null;
      this.form.qualifiersPerGroup = 2;
      this.form.hasKnockoutPhase = true;
    }
  }

  createTournament() {
    const data: any = {
      name: this.form.name,
      format: Number(this.form.format),
      startDate: this.form.startDate ? new Date(this.form.startDate) : undefined
    };

    if (Number(this.form.format) === TournamentFormat.GroupStage) {
      if (this.form.numberOfGroups) {
        data.numberOfGroups = this.form.numberOfGroups;
      }
      data.qualifiersPerGroup = this.form.qualifiersPerGroup;
      data.hasKnockoutPhase = this.form.hasKnockoutPhase;
    }

    this.apiService.createTournament(data).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.notificationService.showSuccess('Tournoi créé avec succès');
      this.loadTournaments();
      this.form = {
        name: '',
        format: TournamentFormat.SingleElimination,
        startDate: '',
        numberOfGroups: null,
        qualifiersPerGroup: 2,
        hasKnockoutPhase: true
      };
    });
  }

  deleteTournament(id: number) {
    if (confirm('Voulez-vous vraiment supprimer ce tournoi ?')) {
      this.apiService.deleteTournament(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Tournoi supprimé');
        this.loadTournaments();
      });
    }
  }

  getFormatLabel(format: TournamentFormat): string {
    switch (format) {
      case TournamentFormat.SingleElimination: return 'Élimination directe';
      case TournamentFormat.DoubleElimination: return 'Double élimination';
      case TournamentFormat.RoundRobin: return 'Round Robin';
      case TournamentFormat.GroupStage: return 'Phase de groupes';
      default: return 'Inconnu';
    }
  }

  getStatusLabel(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Draft: return 'Brouillon';
      case TournamentStatus.InProgress: return 'En cours';
      case TournamentStatus.Completed: return 'Terminé';
      default: return 'Inconnu';
    }
  }
}
