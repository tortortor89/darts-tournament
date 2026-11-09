import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { FeatureService } from '../../core/services/feature.service';
import { NotificationService } from '../../core/services/notification.service';
import { SeasonDetail, SeasonStatus, SeasonItemKind, Tournament, Circuit, InterclubChampionship, TournamentStatus, SeasonExploitRankings, ExploitStanding } from '../../core/models';

@Component({
  selector: 'app-season-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (season) {
        <div class="header">
          <h2>
            {{ season.name }}
            <span class="badge" [class.closed]="isClosed">{{ isClosed ? 'Clôturée' : 'En cours' }}</span>
          </h2>
          @if (season.startDate || season.endDate) {
            <p>{{ season.startDate ? (season.startDate | date:'dd/MM/yyyy') : '…' }} → {{ season.endDate ? (season.endDate | date:'dd/MM/yyyy') : '…' }}</p>
          }
          <p class="rules">
            Règlement : 501 double out, {{ season.legsToWin }} leg{{ season.legsToWin > 1 ? 's' : '' }} gagnant{{ season.legsToWin > 1 ? 's' : '' }} par défaut
            (les championnats interclubs gardent leur propre nombre de legs).
          </p>
          @if (isClosed) {
            <p class="closed-hint">Saison clôturée : plus aucun rattachement possible.</p>
          }

          @if (authService.isAdmin()) {
            @if (!editing) {
              <div class="header-actions">
                <button class="edit-btn" (click)="startEdit()">Modifier</button>
                <button class="status-btn" (click)="toggleStatus()">{{ isClosed ? 'Rouvrir la saison' : 'Clôturer la saison' }}</button>
              </div>
            } @else {
              <form class="edit-form" (ngSubmit)="saveEdit()">
                <input type="text" [(ngModel)]="editForm.name" name="name" required>
                <label>Début <input type="date" [(ngModel)]="editForm.startDate" name="startDate"></label>
                <label>Fin <input type="date" [(ngModel)]="editForm.endDate" name="endDate"></label>
                <label>Legs à gagner <input type="number" [(ngModel)]="editForm.legsToWin" name="legsToWin" min="1" max="10" class="legs-input"></label>
                <button type="submit" class="save" [disabled]="!editForm.name">Enregistrer</button>
                <button type="button" class="cancel" (click)="editing = false">Annuler</button>
              </form>
            }
          }
        </div>

        <!-- Tournois hors circuit -->
        <div class="section">
          <h3>Tournois</h3>
          @if (features.isEnabled('circuits')) {
            <p class="hint">Les tournois d'un circuit suivent la saison du circuit.</p>
          }
          @for (tournament of season.tournaments; track tournament.id) {
            <div class="item-row">
              <a [routerLink]="['/tournaments', tournament.id]">{{ tournament.name }}</a>
              <span class="item-badge" [class]="'status-' + tournament.status">{{ getTournamentStatusLabel(tournament.status) }}</span>
              @if (authService.isAdmin()) {
                <button class="detach" (click)="detach('tournaments', tournament.id)">Détacher</button>
              }
            </div>
          } @empty {
            <p class="empty">Aucun tournoi rattaché directement.</p>
          }
          @if (canAttach && attachableTournaments.length > 0) {
            <div class="attach-form">
              <select [(ngModel)]="toAttach.tournaments" name="tournamentToAttach">
                <option [ngValue]="null">Rattacher un tournoi existant...</option>
                @for (tournament of attachableTournaments; track tournament.id) {
                  <option [ngValue]="tournament.id">{{ tournament.name }}</option>
                }
              </select>
              <button (click)="attach('tournaments')" [disabled]="toAttach.tournaments === null">Rattacher</button>
            </div>
          }
        </div>

        @if (features.isEnabled('circuits')) {
          <div class="section">
            <h3>Circuits</h3>
            @for (circuit of season.circuits; track circuit.id) {
              <div class="item-row">
                <a [routerLink]="['/circuits', circuit.id]">{{ circuit.name }}</a>
                <span class="meta">{{ circuit.tournamentCount }} tournoi{{ circuit.tournamentCount > 1 ? 's' : '' }}</span>
                @if (authService.isAdmin()) {
                  <button class="detach" (click)="detach('circuits', circuit.id)">Détacher</button>
                }
              </div>
            } @empty {
              <p class="empty">Aucun circuit rattaché.</p>
            }
            @if (canAttach && attachableCircuits.length > 0) {
              <div class="attach-form">
                <select [(ngModel)]="toAttach.circuits" name="circuitToAttach">
                  <option [ngValue]="null">Rattacher un circuit existant...</option>
                  @for (circuit of attachableCircuits; track circuit.id) {
                    <option [ngValue]="circuit.id">{{ circuit.name }}</option>
                  }
                </select>
                <button (click)="attach('circuits')" [disabled]="toAttach.circuits === null">Rattacher</button>
              </div>
            }
          </div>
        }

        @if (features.isEnabled('interclubs')) {
          <div class="section">
            <h3>Championnats interclubs</h3>
            @for (championship of season.championships; track championship.id) {
              <div class="item-row">
                <a [routerLink]="['/interclubs', championship.id]">{{ championship.name }}</a>
                <span class="meta">{{ championship.clubCount }} club{{ championship.clubCount > 1 ? 's' : '' }}, {{ championship.legsToWin }} legs</span>
                @if (authService.isAdmin()) {
                  <button class="detach" (click)="detach('championships', championship.id)">Détacher</button>
                }
              </div>
            } @empty {
              <p class="empty">Aucun championnat rattaché.</p>
            }
            @if (canAttach && attachableChampionships.length > 0) {
              <div class="attach-form">
                <select [(ngModel)]="toAttach.championships" name="championshipToAttach">
                  <option [ngValue]="null">Rattacher un championnat existant...</option>
                  @for (championship of attachableChampionships; track championship.id) {
                    <option [ngValue]="championship.id">{{ championship.name }}</option>
                  }
                </select>
                <button (click)="attach('championships')" [disabled]="toAttach.championships === null">Rattacher</button>
              </div>
            }
          </div>
        }

        @if (features.isEnabled('exploits')) {
          <div class="section">
            <div class="section-header">
              <h3>Exploits</h3>
              @if (authService.isAdmin()) {
                <button class="recompute" (click)="recomputeExploits()" [disabled]="recomputing"
                  title="Recalcule les exploits des matchs déjà joués dans l'application">
                  {{ recomputing ? 'Recalcul...' : 'Recalculer' }}
                </button>
              }
            </div>
            <p class="hint">
              Détectés automatiquement en 501 double out, ou saisis sur les matchs (bouton « Exploits »).
              En double, la manche est attribuée à la paire, le checkout au finisseur.
            </p>
            @if (exploits) {
              <div class="rankings">
                @for (ranking of exploitRankings; track ranking.title) {
                  <div class="ranking">
                    <h4>{{ ranking.title }}</h4>
                    @if (ranking.rows.length > 0) {
                      <table>
                        <thead>
                          <tr>
                            <th>#</th>
                            <th>Joueur</th>
                            @if (ranking.bestLabel) {
                              <th>{{ ranking.bestLabel }}</th>
                            }
                            <th>Nb</th>
                          </tr>
                        </thead>
                        <tbody>
                          @for (row of ranking.rows; track row.playerIds.join('-')) {
                            <tr>
                              <td>{{ row.rank }}</td>
                              <td>{{ row.name }}</td>
                              @if (ranking.bestLabel) {
                                <td class="best">{{ row.best }}</td>
                              }
                              <td>{{ row.count }}</td>
                            </tr>
                          }
                        </tbody>
                      </table>
                    } @else {
                      <p class="empty">Aucun pour l'instant.</p>
                    }
                  </div>
                }
              </div>
            }
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .container {
      max-width: 1000px;
      margin: 20px auto;
      padding: 20px;
    }
    .header {
      margin-bottom: 20px;
    }
    .header h2 {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .badge {
      padding: 2px 10px;
      border-radius: 10px;
      font-size: 0.55em;
      color: white;
      background: var(--action);
    }
    .badge.closed {
      background: var(--secondary);
    }
    .rules {
      color: var(--text-muted);
    }
    .closed-hint {
      color: var(--accent-text);
      background: var(--accent-soft);
      padding: 8px 12px;
      border-radius: 4px;
    }
    .header-actions, .edit-form {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      align-items: center;
    }
    .edit-form label {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.9em;
      color: var(--text-muted);
    }
    .edit-form input {
      padding: 8px;
      border: 1px solid var(--border);
      border-radius: 4px;
    }
    .legs-input {
      width: 60px;
    }
    .section {
      margin-bottom: 30px;
      padding: 15px;
      background: var(--surface-alt);
      border-radius: 8px;
    }
    .section h3 {
      margin-top: 0;
    }
    .hint {
      margin-top: 0;
      font-size: 0.85em;
      color: var(--text-muted);
    }
    .item-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px;
      margin-bottom: 8px;
      background: white;
      border-radius: 4px;
      border: 1px solid var(--border-light);
    }
    .item-row a {
      color: var(--link);
      text-decoration: none;
      font-weight: 600;
    }
    .meta {
      font-size: 0.85em;
      color: var(--text-muted);
    }
    .item-badge {
      padding: 2px 10px;
      border-radius: 10px;
      font-size: 0.8em;
      color: white;
    }
    .item-badge.status-0 { background: var(--status-draft); }
    .item-badge.status-1 { background: var(--status-active); }
    .item-badge.status-2 { background: var(--status-done); }
    .detach {
      margin-left: auto;
      background: var(--danger);
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
      border: 1px solid var(--border);
      border-radius: 4px;
      min-width: 250px;
    }
    .attach-form button, .edit-btn, .save, .status-btn, .cancel {
      padding: 8px 16px;
      background: var(--primary);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .attach-form button:disabled, .save:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .status-btn, .cancel {
      background: var(--secondary);
    }
    .save {
      background: var(--action);
    }
    .section-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .recompute {
      padding: 6px 14px;
      background: var(--secondary);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .recompute:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .rankings {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 16px;
    }
    .ranking {
      background: white;
      border: 1px solid var(--border-light);
      border-radius: 4px;
      padding: 10px;
    }
    .ranking h4 {
      margin: 0 0 8px 0;
    }
    .ranking table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.9em;
    }
    .ranking th, .ranking td {
      text-align: left;
      padding: 4px 6px;
      border-bottom: 1px solid var(--border-light);
    }
    .ranking .best {
      font-weight: 600;
    }
    .loading, .empty {
      text-align: center;
      padding: 20px;
      color: var(--text-muted);
    }
  `]
})
export class SeasonDetailComponent implements OnInit {
  season: SeasonDetail | null = null;
  loading = false;
  editing = false;
  editForm = { name: '', startDate: '', endDate: '', legsToWin: 3 };

  allTournaments: Tournament[] = [];
  allCircuits: Circuit[] = [];
  allChampionships: InterclubChampionship[] = [];
  toAttach: Record<SeasonItemKind, number | null> = { tournaments: null, circuits: null, championships: null };
  exploits: SeasonExploitRankings | null = null;
  recomputing = false;

  features = inject(FeatureService);
  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  private seasonId = 0;

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.seasonId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadSeason();
    this.loadExploits();
    if (this.authService.isAdmin()) {
      this.loadAttachables();
    }
  }

  get isClosed(): boolean {
    return this.season?.status === SeasonStatus.Closed;
  }

  get canAttach(): boolean {
    return this.authService.isAdmin() && !this.isClosed;
  }

  // Tournois hors circuit et hors saison (un tournoi de circuit suit la saison du circuit)
  get attachableTournaments(): Tournament[] {
    return this.allTournaments.filter(t => !t.circuitId && !t.seasonId);
  }

  get attachableCircuits(): Circuit[] {
    return this.allCircuits.filter(c => !c.seasonId);
  }

  get attachableChampionships(): InterclubChampionship[] {
    return this.allChampionships.filter(c => !c.seasonId);
  }

  loadSeason() {
    this.loading = true;
    this.apiService.getSeason(this.seasonId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(season => {
      this.season = season;
      this.loading = false;
    });
  }

  // Quatre classements : 180 et 171 au nombre, checkouts et manches à la performance
  get exploitRankings(): { title: string; bestLabel?: string; rows: ExploitStanding[] }[] {
    if (!this.exploits) return [];
    return [
      { title: '180', rows: this.exploits.oneEighties },
      { title: '171', rows: this.exploits.oneSeventyOnes },
      { title: 'Checkouts 100+', bestLabel: 'Meilleur', rows: this.exploits.highCheckouts },
      { title: 'Manches en 15 fléchettes ou moins', bestLabel: 'Fléchettes', rows: this.exploits.shortLegs }
    ];
  }

  loadExploits() {
    if (!this.features.isEnabled('exploits')) return;
    this.apiService.getSeasonExploits(this.seasonId).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(exploits => this.exploits = exploits);
  }

  recomputeExploits() {
    this.recomputing = true;
    this.apiService.recomputeSeasonExploits(this.seasonId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ matchCount }) => {
        this.recomputing = false;
        this.notificationService.showSuccess(`Exploits recalculés sur ${matchCount} match${matchCount > 1 ? 's' : ''}`);
        this.loadExploits();
      },
      error: () => this.recomputing = false
    });
  }

  loadAttachables() {
    this.apiService.getTournaments().pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(tournaments => this.allTournaments = tournaments);
    if (this.features.isEnabled('circuits')) {
      this.apiService.getCircuits().pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(circuits => this.allCircuits = circuits);
    }
    if (this.features.isEnabled('interclubs')) {
      this.apiService.getChampionships().pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(championships => this.allChampionships = championships);
    }
  }

  attach(kind: SeasonItemKind) {
    const id = this.toAttach[kind];
    if (id === null) return;
    this.apiService.attachToSeason(this.seasonId, kind, id)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Rattaché à la saison');
        this.toAttach[kind] = null;
        this.loadSeason();
        this.loadAttachables();
        this.loadExploits();
      });
  }

  detach(kind: SeasonItemKind, id: number) {
    if (confirm('Détacher de la saison ? Le règlement 501 double out ne sera plus imposé.')) {
      this.apiService.detachFromSeason(this.seasonId, kind, id)
        .pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
          this.notificationService.showSuccess('Détaché de la saison');
          this.loadSeason();
          this.loadAttachables();
          this.loadExploits();
        });
    }
  }

  startEdit() {
    if (!this.season) return;
    this.editForm = this.formFromSeason(this.season);
    this.editing = true;
  }

  saveEdit() {
    if (!this.season) return;
    this.update(this.season.status, () => this.editing = false);
  }

  toggleStatus() {
    if (!this.season) return;
    const closing = !this.isClosed;
    if (closing && !confirm('Clôturer la saison ? Plus aucun tournoi, circuit ou championnat ne pourra y être rattaché.')) {
      return;
    }
    this.editForm = this.formFromSeason(this.season);
    this.update(closing ? SeasonStatus.Closed : SeasonStatus.InProgress);
  }

  getTournamentStatusLabel(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Draft: return 'Brouillon';
      case TournamentStatus.InProgress: return 'En cours';
      case TournamentStatus.Completed: return 'Terminé';
      default: return '';
    }
  }

  // Dates ISO tronquées au format attendu par <input type="date">
  private formFromSeason(season: SeasonDetail) {
    return {
      name: season.name,
      startDate: season.startDate?.substring(0, 10) ?? '',
      endDate: season.endDate?.substring(0, 10) ?? '',
      legsToWin: season.legsToWin
    };
  }

  private update(status: SeasonStatus, onSuccess?: () => void) {
    this.apiService.updateSeason(this.seasonId, {
      name: this.editForm.name,
      startDate: this.editForm.startDate || null,
      endDate: this.editForm.endDate || null,
      legsToWin: this.editForm.legsToWin,
      status
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.notificationService.showSuccess('Saison mise à jour');
      onSuccess?.();
      this.loadSeason();
    });
  }
}
