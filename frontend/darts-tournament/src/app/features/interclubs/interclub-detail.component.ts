import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { InterclubChampionshipDetail, ChampionshipStatus, EncounterStatus, CalendarRound, InterclubStanding, Club, Player, GameMode } from '../../core/models';

@Component({
  selector: 'app-interclub-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (championship) {
        <div class="header">
          <h2>{{ championship.name }}</h2>
          <p class="config">
            {{ championship.singlesPerEncounter }} simples + {{ championship.doublesPerEncounter }} doubles par rencontre
            · {{ getModeLabel() }} · premier à {{ championship.legsToWin }} legs
            · <span [class]="'status-' + championship.status">{{ getStatusLabel(championship.status) }}</span>
          </p>
        </div>

        <!-- Classement -->
        @if (standings.length > 0 && championship.status !== ChampionshipStatus.Draft) {
          <div class="section">
            <h3>Classement</h3>
            <table class="standings-table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Club</th>
                  <th>J</th>
                  <th>V</th>
                  <th>N</th>
                  <th>D</th>
                  <th>+/-</th>
                  <th>Pts</th>
                </tr>
              </thead>
              <tbody>
                @for (row of standings; track row.clubId) {
                  <tr [class.top-3]="row.rank <= 3">
                    <td>{{ getMedal(row.rank) }}</td>
                    <td>{{ row.clubName }}</td>
                    <td>{{ row.played }}</td>
                    <td>{{ row.wins }}</td>
                    <td>{{ row.draws }}</td>
                    <td>{{ row.losses }}</td>
                    <td>{{ row.matchesWon - row.matchesLost > 0 ? '+' : '' }}{{ row.matchesWon - row.matchesLost }}</td>
                    <td><strong>{{ row.points }}</strong></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }

        <!-- Calendrier -->
        <div class="section">
          <h3>Calendrier</h3>
          @if (calendar.length > 0) {
            @for (round of calendar; track round.round) {
              <div class="round-block">
                <h4>Journée {{ round.round }}</h4>
                @for (encounter of round.encounters; track encounter.id) {
                  <a class="encounter-row" [routerLink]="['/interclubs/encounters', encounter.id]"
                     [class.completed]="encounter.status === EncounterStatus.Completed">
                    <span class="home">{{ encounter.homeClubName }}</span>
                    <span class="score">
                      @if (encounter.status === EncounterStatus.Pending) { — }
                      @else { {{ encounter.homeScore }} - {{ encounter.awayScore }} }
                    </span>
                    <span class="away">{{ encounter.awayClubName }}</span>
                    <span class="badge" [class]="'status-badge-' + encounter.status">{{ getEncounterStatusLabel(encounter.status) }}</span>
                  </a>
                }
              </div>
            }
          } @else {
            <p class="empty">Le calendrier n'a pas encore été généré.</p>
            @if (authService.isAdmin() && championship.status === ChampionshipStatus.Draft) {
              @if (championship.clubs.length >= 2) {
                <button class="generate" (click)="generateCalendar()">
                  Générer le calendrier ({{ championship.clubs.length }} clubs, aller-retour)
                </button>
              } @else {
                <p class="info-text">Engagez au moins 2 clubs pour générer le calendrier.</p>
              }
            }
          }
        </div>

        <!-- Clubs engagés & effectifs -->
        <div class="section">
          <h3>Clubs engagés & effectifs</h3>

          @if (authService.isAdmin() && championship.status === ChampionshipStatus.Draft && attachableClubs.length > 0) {
            <div class="attach-row">
              <select [(ngModel)]="clubToAttach">
                <option [ngValue]="null">Engager un club...</option>
                @for (club of attachableClubs; track club.id) {
                  <option [ngValue]="club.id">{{ club.name }}</option>
                }
              </select>
              <button (click)="attachClub()" [disabled]="clubToAttach === null">Engager</button>
            </div>
          }

          <div class="clubs-grid">
            @for (club of championship.clubs; track club.clubId) {
              <div class="club-roster">
                <div class="club-roster-header">
                  <strong>{{ club.clubName }}</strong>
                  <span class="count">{{ club.roster.length }} joueur(s) déclarés</span>
                  @if (authService.isAdmin() && championship.status === ChampionshipStatus.Draft) {
                    <button class="detach" (click)="detachClub(club.clubId)">Retirer</button>
                  }
                </div>
                <ul>
                  @for (player of club.roster; track player.playerId) {
                    <li>{{ player.name }}</li>
                  } @empty {
                    <li class="empty">Effectif non déclaré</li>
                  }
                </ul>
                @if (authService.isAdmin()) {
                  @if (editingRosterClubId === club.clubId) {
                    <div class="roster-edit">
                      @for (player of getRosterCandidates(club.clubId); track player.id) {
                        <label class="roster-candidate">
                          <input type="checkbox"
                            [checked]="rosterSelection.has(player.id)"
                            (change)="toggleRosterPlayer(player.id)">
                          {{ player.firstName }} {{ player.lastName }}
                          @if (player.clubId !== club.clubId) { <em>(hors club)</em> }
                        </label>
                      }
                      <div class="roster-actions">
                        <button class="save" (click)="saveRoster(club.clubId)">Enregistrer l'effectif</button>
                        <button class="cancel" (click)="editingRosterClubId = null">Annuler</button>
                      </div>
                    </div>
                  } @else {
                    <button class="edit-roster" (click)="startEditRoster(club.clubId)">Modifier l'effectif</button>
                  }
                }
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .container {
      max-width: 1000px;
      margin: 20px auto;
      padding: 20px;
    }
    .header h2 { margin-bottom: 5px; }
    .config { color: #666; }
    .status-0 { color: #6c757d; }
    .status-1 { color: #28a745; }
    .status-2 { color: #007bff; }
    .section {
      margin-bottom: 30px;
      padding: 15px;
      background: #f8f9fa;
      border-radius: 8px;
    }
    .section h3 { margin-top: 0; }
    .standings-table {
      width: 100%;
      border-collapse: collapse;
      background: white;
      border-radius: 4px;
    }
    .standings-table th, .standings-table td {
      padding: 8px 12px;
      text-align: center;
      border-bottom: 1px solid #eee;
    }
    .standings-table th { background: #333; color: white; }
    .standings-table td:nth-child(2) { text-align: left; }
    .standings-table tr.top-3 { background: #fff8e1; }
    .round-block { margin-bottom: 15px; }
    .round-block h4 { margin: 10px 0 6px; color: #555; }
    .encounter-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 12px;
      background: white;
      border: 1px solid #eee;
      border-radius: 4px;
      margin-bottom: 6px;
      text-decoration: none;
      color: inherit;
    }
    .encounter-row:hover { border-color: #007bff; }
    .encounter-row .home { flex: 1; text-align: right; font-weight: 600; }
    .encounter-row .away { flex: 1; font-weight: 600; }
    .encounter-row .score {
      min-width: 60px;
      text-align: center;
      font-weight: bold;
      color: #007bff;
    }
    .badge {
      padding: 2px 10px;
      border-radius: 10px;
      font-size: 0.75em;
      color: white;
    }
    .status-badge-0 { background: #6c757d; }
    .status-badge-1 { background: #28a745; }
    .status-badge-2 { background: #007bff; }
    .generate {
      padding: 10px 20px;
      background: #28a745;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .attach-row {
      display: flex;
      gap: 10px;
      margin-bottom: 15px;
    }
    .attach-row select {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      min-width: 220px;
    }
    .attach-row button {
      padding: 8px 16px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .attach-row button:disabled { opacity: 0.5; cursor: not-allowed; }
    .clubs-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 15px;
    }
    .club-roster {
      background: white;
      border: 1px solid #eee;
      border-radius: 6px;
      padding: 12px;
    }
    .club-roster-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 8px;
    }
    .club-roster-header .count {
      color: #666;
      font-size: 0.85em;
    }
    .club-roster ul {
      list-style: none;
      padding: 0;
      margin: 0 0 8px;
    }
    .club-roster li {
      padding: 3px 0;
      border-bottom: 1px solid #f5f5f5;
    }
    .detach {
      margin-left: auto;
      background: #dc3545;
      color: white;
      border: none;
      padding: 3px 8px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.8em;
    }
    .edit-roster, .roster-actions .save {
      padding: 6px 12px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.85em;
    }
    .roster-actions .save { background: #28a745; }
    .roster-actions .cancel {
      padding: 6px 12px;
      background: #6c757d;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.85em;
    }
    .roster-edit {
      max-height: 240px;
      overflow-y: auto;
      border-top: 1px solid #eee;
      padding-top: 8px;
    }
    .roster-candidate {
      display: block;
      padding: 3px 0;
      font-size: 0.9em;
    }
    .roster-candidate em { color: #999; font-size: 0.85em; }
    .roster-actions {
      display: flex;
      gap: 8px;
      margin-top: 8px;
    }
    .loading, .empty { color: #666; }
    .info-text { color: #856404; }
  `]
})
export class InterclubDetailComponent implements OnInit {
  championship: InterclubChampionshipDetail | null = null;
  calendar: CalendarRound[] = [];
  standings: InterclubStanding[] = [];
  allClubs: Club[] = [];
  allPlayers: Player[] = [];
  clubToAttach: number | null = null;
  editingRosterClubId: number | null = null;
  rosterSelection = new Set<number>();
  loading = false;

  ChampionshipStatus = ChampionshipStatus;
  EncounterStatus = EncounterStatus;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  private championshipId = 0;

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.championshipId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAll();
    if (this.authService.isAdmin()) {
      this.apiService.getClubs().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(clubs => {
        this.allClubs = clubs;
      });
      this.apiService.getPlayers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(players => {
        this.allPlayers = players;
      });
    }
  }

  loadAll() {
    this.loading = true;
    this.apiService.getChampionship(this.championshipId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(championship => {
      this.championship = championship;
      this.loading = false;
    });
    this.apiService.getInterclubCalendar(this.championshipId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(calendar => {
      this.calendar = calendar;
    });
    this.apiService.getInterclubStandings(this.championshipId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(standings => {
      this.standings = standings;
    });
  }

  get attachableClubs(): Club[] {
    const engaged = new Set((this.championship?.clubs ?? []).map(c => c.clubId));
    return this.allClubs.filter(c => !engaged.has(c.id));
  }

  attachClub() {
    if (this.clubToAttach === null) return;
    this.apiService.attachClubToChampionship(this.championshipId, this.clubToAttach)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Club engagé');
          this.clubToAttach = null;
          this.loadAll();
        },
        error: (err) => this.showError(err)
      });
  }

  detachClub(clubId: number) {
    if (confirm('Retirer ce club du championnat ? Son effectif déclaré sera supprimé.')) {
      this.apiService.detachClubFromChampionship(this.championshipId, clubId)
        .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            this.notificationService.showSuccess('Club retiré');
            this.loadAll();
          },
          error: (err) => this.showError(err)
        });
    }
  }

  // Candidats à l'effectif : membres du club en priorité, mais tout joueur est éligible
  getRosterCandidates(clubId: number): Player[] {
    return [...this.allPlayers].sort((a, b) => {
      const aInClub = a.clubId === clubId ? 0 : 1;
      const bInClub = b.clubId === clubId ? 0 : 1;
      return aInClub - bInClub || a.lastName.localeCompare(b.lastName);
    });
  }

  startEditRoster(clubId: number) {
    const club = this.championship?.clubs.find(c => c.clubId === clubId);
    this.rosterSelection = new Set((club?.roster ?? []).map(p => p.playerId));
    // Préremplir avec les membres du club si l'effectif est vide
    if (this.rosterSelection.size === 0) {
      this.allPlayers.filter(p => p.clubId === clubId).forEach(p => this.rosterSelection.add(p.id));
    }
    this.editingRosterClubId = clubId;
  }

  toggleRosterPlayer(playerId: number) {
    if (this.rosterSelection.has(playerId)) {
      this.rosterSelection.delete(playerId);
    } else {
      this.rosterSelection.add(playerId);
    }
  }

  saveRoster(clubId: number) {
    this.apiService.setChampionshipRoster(this.championshipId, clubId, [...this.rosterSelection])
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Effectif enregistré');
          this.editingRosterClubId = null;
          this.loadAll();
        },
        error: (err) => this.showError(err)
      });
  }

  generateCalendar() {
    this.apiService.generateInterclubCalendar(this.championshipId)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Calendrier généré');
          this.loadAll();
        },
        error: (err) => this.showError(err)
      });
  }

  private showError(err: any) {
    this.notificationService.showError(
      typeof err.error === 'string' ? err.error : (err.error?.message || 'Une erreur est survenue'));
  }

  getMedal(rank: number): string {
    switch (rank) {
      case 1: return '🥇';
      case 2: return '🥈';
      case 3: return '🥉';
      default: return rank.toString();
    }
  }

  getModeLabel(): string {
    if (!this.championship) return '';
    if (this.championship.gameMode === GameMode.Cricket) return 'Cricket';
    return `${this.championship.gameMode} ${this.championship.doubleOut ? 'Double Out' : 'Straight Out'}`;
  }

  getStatusLabel(status: ChampionshipStatus): string {
    switch (status) {
      case ChampionshipStatus.Draft: return 'Brouillon';
      case ChampionshipStatus.InProgress: return 'En cours';
      case ChampionshipStatus.Completed: return 'Terminé';
      default: return 'Inconnu';
    }
  }

  getEncounterStatusLabel(status: EncounterStatus): string {
    switch (status) {
      case EncounterStatus.Pending: return 'À venir';
      case EncounterStatus.InProgress: return 'En cours';
      case EncounterStatus.Completed: return 'Terminée';
      default: return '';
    }
  }
}
