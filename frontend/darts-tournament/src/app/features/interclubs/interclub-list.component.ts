import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { InterclubChampionship, ChampionshipStatus, Club, ClubDetail, Player, GameMode } from '../../core/models';

@Component({
  selector: 'app-interclub-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h2>Interclubs</h2>

      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (authService.isAdmin()) {
        <div class="add-form">
          <h3>Créer un championnat</h3>
          <form (ngSubmit)="createChampionship()">
            <div class="form-row">
              <input type="text" [(ngModel)]="form.name" name="name" placeholder="Nom du championnat" required>
            </div>
            <div class="form-row config">
              <label>
                Simples / rencontre
                <input type="number" [(ngModel)]="form.singlesPerEncounter" name="singles" min="0" max="20">
              </label>
              <label>
                Doubles / rencontre
                <input type="number" [(ngModel)]="form.doublesPerEncounter" name="doubles" min="0" max="10">
              </label>
              <label>
                Legs à gagner
                <input type="number" [(ngModel)]="form.legsToWin" name="legs" min="1" max="10">
              </label>
              <label>
                Mode
                <select [(ngModel)]="form.gameMode" name="gameMode">
                  <option [ngValue]="GameMode.FiveOhOne">501</option>
                  <option [ngValue]="GameMode.ThreeOhOne">301</option>
                  <option [ngValue]="GameMode.Cricket">Cricket</option>
                </select>
              </label>
              <label class="checkbox-label">
                <input type="checkbox" [(ngModel)]="form.doubleOut" name="doubleOut">
                Double Out
              </label>
            </div>
            <button type="submit">Créer</button>
          </form>
        </div>
      }

      <div class="championship-list">
        @for (championship of championships; track championship.id) {
          <div class="championship-card">
            <h3>
              <a [routerLink]="['/interclubs', championship.id]">{{ championship.name }}</a>
            </h3>
            <p>Statut: <span [class]="'status-' + championship.status">{{ getStatusLabel(championship.status) }}</span></p>
            <p>{{ championship.clubCount }} clubs</p>
            <p>{{ championship.singlesPerEncounter }} simples + {{ championship.doublesPerEncounter }} doubles par rencontre</p>
            @if (authService.isAdmin() && championship.status === ChampionshipStatus.Draft) {
              <button (click)="deleteChampionship(championship.id)" class="delete">Supprimer</button>
            }
          </div>
        } @empty {
          @if (!loading) {
            <p class="empty">Aucun championnat pour le moment.</p>
          }
        }
      </div>

      <!-- Clubs -->
      <div class="clubs-section">
        <h2>Clubs</h2>

        @if (authService.isAdmin()) {
          <div class="add-form">
            <form (ngSubmit)="createClub()">
              <div class="form-row">
                <input type="text" [(ngModel)]="newClubName" name="newClubName" placeholder="Nom du club" required>
                <button type="submit">Créer le club</button>
              </div>
            </form>
          </div>
        }

        <div class="club-list">
          @for (club of clubs; track club.id) {
            <div class="club-card">
              <div class="club-header" (click)="toggleClub(club.id)">
                <strong>{{ club.name }}</strong>
                <span class="player-count">{{ club.playerCount }} joueur(s)</span>
                <span class="expand">{{ expandedClubId === club.id ? '▲' : '▼' }}</span>
              </div>
              @if (expandedClubId === club.id && expandedClub) {
                <div class="club-body">
                  <ul class="member-list">
                    @for (member of expandedClub.players; track member.playerId) {
                      <li>
                        {{ member.name }}
                        @if (authService.isAdmin()) {
                          <button class="remove" (click)="removeClubPlayer(club.id, member.playerId)">X</button>
                        }
                      </li>
                    } @empty {
                      <li class="empty">Aucun joueur</li>
                    }
                  </ul>
                  @if (authService.isAdmin()) {
                    <div class="assign-row">
                      <select [(ngModel)]="playerToAssign">
                        <option [ngValue]="null">Ajouter un joueur...</option>
                        @for (player of getClubLessPlayers(); track player.id) {
                          <option [ngValue]="player.id">{{ player.firstName }} {{ player.lastName }}</option>
                        }
                      </select>
                      <button (click)="assignClubPlayer(club.id)" [disabled]="playerToAssign === null">Ajouter</button>
                      <button class="delete" (click)="deleteClub(club.id)">Supprimer le club</button>
                    </div>
                  }
                </div>
              }
            </div>
          } @empty {
            <p class="empty">Aucun club pour le moment.</p>
          }
        </div>
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
    .form-row.config label {
      display: flex;
      flex-direction: column;
      gap: 4px;
      font-size: 0.9em;
    }
    .form-row.config input[type="number"] {
      width: 80px;
    }
    .checkbox-label {
      flex-direction: row !important;
      align-items: center;
      gap: 6px !important;
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
    .championship-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
      margin-bottom: 40px;
    }
    .championship-card, .club-card {
      padding: 15px;
      border: 1px solid #ddd;
      border-radius: 8px;
    }
    .championship-card h3 {
      margin-top: 0;
    }
    .championship-card h3 a {
      color: #007bff;
      text-decoration: none;
    }
    .championship-card h3 a:hover {
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
    .clubs-section h2 {
      margin-top: 20px;
    }
    .club-list {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .club-card {
      padding: 0;
      overflow: hidden;
    }
    .club-header {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 15px;
      cursor: pointer;
      background: #f8f9fa;
    }
    .club-header .player-count {
      color: #666;
      font-size: 0.9em;
    }
    .club-header .expand {
      margin-left: auto;
      color: #007bff;
    }
    .club-body {
      padding: 12px 15px;
    }
    .member-list {
      list-style: none;
      padding: 0;
      margin: 0 0 10px;
    }
    .member-list li {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 4px 0;
      border-bottom: 1px solid #eee;
    }
    button.remove {
      background: transparent;
      border: none;
      color: #dc3545;
      cursor: pointer;
      font-weight: bold;
    }
    .assign-row {
      display: flex;
      gap: 10px;
      align-items: center;
    }
    .assign-row select {
      padding: 6px;
      border: 1px solid #ddd;
      border-radius: 4px;
      min-width: 200px;
    }
    .assign-row button {
      padding: 6px 12px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .assign-row button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .loading, .empty {
      text-align: center;
      padding: 20px;
      color: #666;
    }
  `]
})
export class InterclubListComponent implements OnInit {
  championships: InterclubChampionship[] = [];
  clubs: Club[] = [];
  allPlayers: Player[] = [];
  expandedClubId: number | null = null;
  expandedClub: ClubDetail | null = null;
  playerToAssign: number | null = null;
  newClubName = '';
  loading = false;

  form = {
    name: '',
    singlesPerEncounter: 4,
    doublesPerEncounter: 2,
    legsToWin: 3,
    gameMode: GameMode.FiveOhOne,
    doubleOut: true
  };

  ChampionshipStatus = ChampionshipStatus;
  GameMode = GameMode;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);

  constructor(public authService: AuthService, private apiService: ApiService) {}

  ngOnInit() {
    this.loading = true;
    this.apiService.getChampionships().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(championships => {
      this.championships = championships;
      this.loading = false;
    });
    this.loadClubs();
    if (this.authService.isAdmin()) {
      this.apiService.getPlayers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(players => {
        this.allPlayers = players;
      });
    }
  }

  loadClubs() {
    this.apiService.getClubs().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(clubs => {
      this.clubs = clubs;
    });
  }

  createChampionship() {
    this.apiService.createChampionship({
      name: this.form.name,
      singlesPerEncounter: Number(this.form.singlesPerEncounter),
      doublesPerEncounter: Number(this.form.doublesPerEncounter),
      legsToWin: Number(this.form.legsToWin),
      gameMode: this.form.gameMode,
      doubleOut: this.form.doubleOut
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (championship) => {
        this.notificationService.showSuccess('Championnat créé');
        this.championships = [championship, ...this.championships];
        this.form = { name: '', singlesPerEncounter: 4, doublesPerEncounter: 2, legsToWin: 3, gameMode: GameMode.FiveOhOne, doubleOut: true };
      },
      error: (err) => this.showError(err)
    });
  }

  deleteChampionship(id: number) {
    if (confirm('Supprimer ce championnat ? Les rencontres et matchs associés seront supprimés.')) {
      this.apiService.deleteChampionship(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Championnat supprimé');
        this.championships = this.championships.filter(c => c.id !== id);
      });
    }
  }

  toggleClub(clubId: number) {
    if (this.expandedClubId === clubId) {
      this.expandedClubId = null;
      this.expandedClub = null;
      return;
    }
    this.expandedClubId = clubId;
    this.expandedClub = null;
    this.playerToAssign = null;
    this.apiService.getClub(clubId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(club => {
      this.expandedClub = club;
    });
  }

  // Joueurs sans club (candidats à l'affectation)
  getClubLessPlayers(): Player[] {
    return this.allPlayers.filter(p => !p.clubId);
  }

  createClub() {
    if (!this.newClubName) return;
    this.apiService.createClub(this.newClubName).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Club créé');
        this.newClubName = '';
        this.loadClubs();
      },
      error: (err) => this.showError(err)
    });
  }

  deleteClub(clubId: number) {
    if (confirm('Supprimer ce club ?')) {
      this.apiService.deleteClub(clubId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Club supprimé');
          this.expandedClubId = null;
          this.expandedClub = null;
          this.loadClubs();
        },
        error: (err) => this.showError(err)
      });
    }
  }

  assignClubPlayer(clubId: number) {
    if (this.playerToAssign === null) return;
    this.apiService.assignPlayerToClub(clubId, this.playerToAssign).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.playerToAssign = null;
        this.refreshExpandedClub(clubId);
      },
      error: (err) => this.showError(err)
    });
  }

  removeClubPlayer(clubId: number, playerId: number) {
    this.apiService.removePlayerFromClub(clubId, playerId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.refreshExpandedClub(clubId);
    });
  }

  private refreshExpandedClub(clubId: number) {
    this.apiService.getClub(clubId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(club => {
      this.expandedClub = club;
      this.loadClubs();
    });
    // Rafraîchir les clubId des joueurs (filtre "sans club")
    this.apiService.getPlayers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(players => {
      this.allPlayers = players;
    });
  }

  private showError(err: any) {
    this.notificationService.showError(
      typeof err.error === 'string' ? err.error : (err.error?.message || 'Une erreur est survenue'));
  }

  getStatusLabel(status: ChampionshipStatus): string {
    switch (status) {
      case ChampionshipStatus.Draft: return 'Brouillon';
      case ChampionshipStatus.InProgress: return 'En cours';
      case ChampionshipStatus.Completed: return 'Terminé';
      default: return 'Inconnu';
    }
  }
}
