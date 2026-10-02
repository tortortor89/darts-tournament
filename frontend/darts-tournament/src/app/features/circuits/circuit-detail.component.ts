import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { CircuitDetail, CircuitStanding, CircuitPointsRule, Tournament, TournamentFormat, TournamentStatus } from '../../core/models';

@Component({
  selector: 'app-circuit-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (circuit) {
        <div class="header">
          <h2>{{ circuit.name }}</h2>
          @if (circuit.description) {
            <p class="description">{{ circuit.description }}</p>
          }
        </div>

        <!-- Classement général -->
        <div class="section">
          <h3>Classement général</h3>
          @if (ranking.length > 0) {
            <table class="ranking-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Joueur</th>
                  <th>Tournois</th>
                  <th>Points</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                @for (standing of ranking; track standing.playerId) {
                  <tr [class.top-three]="standing.rank <= 3">
                    <td>{{ getMedal(standing.rank) }}</td>
                    <td>{{ standing.playerName }}</td>
                    <td>{{ standing.tournamentsPlayed }}</td>
                    <td><strong>{{ standing.totalPoints }}</strong></td>
                    <td>
                      <button class="detail-toggle" (click)="toggleDetails(standing.playerId)">
                        {{ expandedPlayerId === standing.playerId ? '▲' : '▼' }}
                      </button>
                    </td>
                  </tr>
                  @if (expandedPlayerId === standing.playerId) {
                    <tr class="details-row">
                      <td colspan="5">
                        <table class="details-table">
                          @for (detail of standing.details; track detail.tournamentId) {
                            <tr>
                              <td><a [routerLink]="['/tournaments', detail.tournamentId]">{{ detail.tournamentName }}</a></td>
                              <td>{{ detail.finalRank }}{{ detail.finalRank === 1 ? 'er' : 'e' }}</td>
                              <td>{{ detail.points }} pts</td>
                            </tr>
                          }
                        </table>
                      </td>
                    </tr>
                  }
                }
              </tbody>
            </table>
          } @else {
            <p class="empty">Aucun point attribué pour le moment. Le classement se remplira quand des tournois du circuit seront terminés.</p>
          }
        </div>

        <!-- Tournois du circuit -->
        <div class="section">
          <h3>Tournois du circuit</h3>
          @if (circuit.tournaments.length > 0) {
            <div class="tournament-rows">
              @for (tournament of circuit.tournaments; track tournament.id) {
                <div class="tournament-row">
                  <a [routerLink]="['/tournaments', tournament.id]">{{ tournament.name }}</a>
                  <span class="badge" [class]="'status-' + tournament.status">{{ getStatusLabel(tournament.status) }}</span>
                  @if (tournament.status !== TournamentStatus.Completed) {
                    <span class="pending-hint">Comptabilisé une fois terminé</span>
                  }
                  @if (authService.isAdmin()) {
                    <button class="detach" (click)="detachTournament(tournament.id)">Détacher</button>
                  }
                </div>
              }
            </div>
          } @else {
            <p class="empty">Aucun tournoi rattaché à ce circuit.</p>
          }

          @if (authService.isAdmin() && attachableTournaments.length > 0) {
            <div class="attach-form">
              <select [(ngModel)]="tournamentToAttach" name="tournamentToAttach">
                <option [ngValue]="null">Rattacher un tournoi existant...</option>
                @for (tournament of attachableTournaments; track tournament.id) {
                  <option [ngValue]="tournament.id">{{ tournament.name }}</option>
                }
              </select>
              <button (click)="attachTournament()" [disabled]="tournamentToAttach === null">Rattacher</button>
            </div>
          }
        </div>

        <!-- Barème -->
        <div class="section">
          <h3>Barème de points</h3>
          @if (!editingRules) {
            <table class="rules-table">
              <thead>
                <tr><th>Place</th><th>Points</th></tr>
              </thead>
              <tbody>
                @for (rule of circuit.pointsRules; track $index) {
                  <tr>
                    <td>{{ rule.minRank === rule.maxRank ? rule.minRank : rule.minRank + ' - ' + rule.maxRank }}</td>
                    <td>{{ rule.points }}</td>
                  </tr>
                }
                <tr class="participation-row">
                  <td>Participation</td>
                  <td>{{ circuit.participationPoints }}</td>
                </tr>
              </tbody>
            </table>
            @if (authService.isAdmin()) {
              <button class="edit-btn" (click)="startEditRules()">Modifier le barème</button>
            }
          } @else {
            <table class="rules-table editing">
              <thead>
                <tr><th>De la place</th><th>À la place</th><th>Points</th><th></th></tr>
              </thead>
              <tbody>
                @for (rule of editRules; track $index; let i = $index) {
                  <tr>
                    <td><input type="number" [(ngModel)]="rule.minRank" [name]="'min' + i" min="1"></td>
                    <td><input type="number" [(ngModel)]="rule.maxRank" [name]="'max' + i" min="1"></td>
                    <td><input type="number" [(ngModel)]="rule.points" [name]="'pts' + i" min="0"></td>
                    <td><button class="remove-rule" (click)="removeRule(i)">✕</button></td>
                  </tr>
                }
                <tr class="participation-row">
                  <td colspan="2">Participation (hors barème)</td>
                  <td><input type="number" [(ngModel)]="editParticipationPoints" name="participationPoints" min="0"></td>
                  <td></td>
                </tr>
              </tbody>
            </table>
            <div class="edit-actions">
              <button class="add-rule" (click)="addRule()">+ Ajouter une ligne</button>
              <button class="save" (click)="saveRules()">Enregistrer</button>
              <button class="cancel" (click)="editingRules = false">Annuler</button>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .container {
      max-width: 900px;
      margin: 20px auto;
      padding: 20px;
    }
    .header h2 {
      margin-bottom: 5px;
    }
    .description {
      color: #666;
      font-style: italic;
      margin-top: 0;
    }
    .section {
      margin-bottom: 30px;
      padding: 15px;
      background: #f8f9fa;
      border-radius: 8px;
    }
    .section h3 {
      margin-top: 0;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      background: white;
      border-radius: 4px;
    }
    th, td {
      padding: 8px 12px;
      text-align: left;
      border-bottom: 1px solid #eee;
    }
    th {
      background: #333;
      color: white;
    }
    .top-three {
      background: #fff8e1;
    }
    .detail-toggle {
      background: transparent;
      border: none;
      cursor: pointer;
      color: #007bff;
    }
    .details-row td {
      background: #f0f7ff;
      padding: 5px 20px;
    }
    .details-table {
      background: transparent;
    }
    .details-table td {
      border-bottom: none;
      padding: 3px 12px;
      font-size: 0.9em;
    }
    .details-table a {
      color: #007bff;
      text-decoration: none;
    }
    .tournament-rows {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .tournament-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px;
      background: white;
      border-radius: 4px;
      border: 1px solid #eee;
    }
    .tournament-row a {
      color: #007bff;
      text-decoration: none;
      font-weight: 600;
    }
    .badge {
      padding: 2px 10px;
      border-radius: 10px;
      font-size: 0.8em;
      color: white;
    }
    .badge.status-0 { background: #6c757d; }
    .badge.status-1 { background: #28a745; }
    .badge.status-2 { background: #007bff; }
    .pending-hint {
      font-size: 0.8em;
      color: #999;
      font-style: italic;
    }
    .detach {
      margin-left: auto;
      background: #dc3545;
      color: white;
      border: none;
      padding: 4px 10px;
      border-radius: 4px;
      cursor: pointer;
    }
    .attach-form {
      margin-top: 12px;
      display: flex;
      gap: 10px;
    }
    .attach-form select {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      min-width: 250px;
    }
    .attach-form button, .edit-btn, .save {
      padding: 8px 16px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .attach-form button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .edit-btn {
      margin-top: 10px;
    }
    .rules-table input {
      width: 80px;
      padding: 4px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    .participation-row td {
      color: #666;
      font-style: italic;
    }
    .remove-rule {
      background: transparent;
      border: none;
      color: #dc3545;
      cursor: pointer;
      font-weight: bold;
    }
    .edit-actions {
      margin-top: 10px;
      display: flex;
      gap: 10px;
    }
    .add-rule, .cancel {
      padding: 8px 16px;
      background: #6c757d;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .save {
      background: #28a745;
    }
    .loading, .empty {
      text-align: center;
      padding: 20px;
      color: #666;
    }
  `]
})
export class CircuitDetailComponent implements OnInit {
  circuit: CircuitDetail | null = null;
  ranking: CircuitStanding[] = [];
  allTournaments: Tournament[] = [];
  loading = false;
  expandedPlayerId: number | null = null;
  tournamentToAttach: number | null = null;

  editingRules = false;
  editRules: CircuitPointsRule[] = [];
  editParticipationPoints = 10;

  TournamentStatus = TournamentStatus;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  private circuitId = 0;

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.circuitId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadCircuit();
    this.loadRanking();
    if (this.authService.isAdmin()) {
      this.loadTournaments();
    }
  }

  get attachableTournaments(): Tournament[] {
    return this.allTournaments.filter(t => !t.circuitId);
  }

  loadCircuit() {
    this.loading = true;
    this.apiService.getCircuit(this.circuitId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(circuit => {
      this.circuit = circuit;
      this.loading = false;
    });
  }

  loadRanking() {
    this.apiService.getCircuitRanking(this.circuitId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(ranking => {
      this.ranking = ranking;
    });
  }

  loadTournaments() {
    this.apiService.getTournaments().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(tournaments => {
      this.allTournaments = tournaments;
    });
  }

  toggleDetails(playerId: number) {
    this.expandedPlayerId = this.expandedPlayerId === playerId ? null : playerId;
  }

  attachTournament() {
    if (this.tournamentToAttach === null) return;
    this.apiService.attachTournamentToCircuit(this.circuitId, this.tournamentToAttach)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Tournoi rattaché au circuit');
        this.tournamentToAttach = null;
        this.loadCircuit();
        this.loadRanking();
        this.loadTournaments();
      });
  }

  detachTournament(tournamentId: number) {
    if (confirm('Détacher ce tournoi du circuit ? Ses points ne compteront plus dans le classement.')) {
      this.apiService.detachTournamentFromCircuit(this.circuitId, tournamentId)
        .pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
          this.notificationService.showSuccess('Tournoi détaché du circuit');
          this.loadCircuit();
          this.loadRanking();
          this.loadTournaments();
        });
    }
  }

  startEditRules() {
    if (!this.circuit) return;
    this.editRules = this.circuit.pointsRules.map(r => ({ ...r }));
    this.editParticipationPoints = this.circuit.participationPoints;
    this.editingRules = true;
  }

  addRule() {
    const lastMax = this.editRules.length > 0
      ? Math.max(...this.editRules.map(r => r.maxRank))
      : 0;
    this.editRules.push({ minRank: lastMax + 1, maxRank: lastMax + 1, points: 0 });
  }

  removeRule(index: number) {
    this.editRules.splice(index, 1);
  }

  saveRules() {
    if (!this.circuit) return;
    this.apiService.updateCircuit(this.circuitId, {
      name: this.circuit.name,
      description: this.circuit.description,
      participationPoints: this.editParticipationPoints,
      pointsRules: this.editRules
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.notificationService.showSuccess('Barème mis à jour');
      this.editingRules = false;
      this.loadCircuit();
      this.loadRanking();
    });
  }

  getMedal(rank: number): string {
    switch (rank) {
      case 1: return '🥇';
      case 2: return '🥈';
      case 3: return '🥉';
      default: return rank.toString();
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
