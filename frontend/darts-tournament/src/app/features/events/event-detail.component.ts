import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { FeatureService } from '../../core/services/feature.service';
import {
  DartsEventDetail, EventTableau, MyRegistration, PendingRegistration, Player,
  RegistrationStatus, Season, TournamentFormat, TournamentStatus
} from '../../core/models';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      @if (event) {
        <div class="header">
          <h2>
            {{ event.name }}
            <span class="badge" [class.closed]="!event.registrationOpen">
              {{ event.registrationOpen ? 'Inscriptions ouvertes' : 'Inscriptions closes' }}
            </span>
          </h2>
          <p>
            @if (event.startDate) { {{ event.startDate | date:'dd/MM/yyyy' }} }
            @if (event.startDate && event.location) { · }
            @if (event.location) { {{ event.location }} }
          </p>
          @if (event.description) {
            <p class="description">{{ event.description }}</p>
          }
          @if (event.registrationDeadline) {
            <p class="muted">Clôture des inscriptions : {{ event.registrationDeadline | date:'dd/MM/yyyy HH:mm' }}</p>
          }
          @if (event.seasonName && features.isEnabled('seasons')) {
            <p>Saison : <a [routerLink]="['/seasons', event.seasonId]">{{ event.seasonName }}</a> (501 double out)</p>
          }
          @if (authService.isAdmin() && !editing) {
            <button class="secondary" (click)="startEdit()">Modifier</button>
          }
        </div>

        @if (editing) {
          <div class="section form-box">
            <h3>Modifier l'événement</h3>
            <div class="form-row">
              <input type="text" [(ngModel)]="editForm.name" name="editName" placeholder="Nom">
              <input type="text" [(ngModel)]="editForm.location" name="editLocation" placeholder="Lieu">
              <label>Date <input type="date" [(ngModel)]="editForm.startDate" name="editStartDate"></label>
              <label>Clôture <input type="datetime-local" [(ngModel)]="editForm.registrationDeadline" name="editDeadline"></label>
              @if (features.isEnabled('seasons')) {
                <select [(ngModel)]="editForm.seasonId" name="editSeasonId">
                  <option [ngValue]="null">Hors saison</option>
                  @for (season of seasons; track season.id) {
                    <option [ngValue]="season.id">{{ season.name }}</option>
                  }
                </select>
              }
            </div>
            <input type="text" class="full" [(ngModel)]="editForm.description" name="editDescription" placeholder="Description">
            <div class="actions">
              <button (click)="saveEdit()" [disabled]="!editForm.name">Enregistrer</button>
              <button class="secondary" (click)="editing = false">Annuler</button>
            </div>
          </div>
        }

        <!-- Tableaux -->
        <div class="section">
          <h3>Tableaux</h3>
          <table class="tableaux">
            <thead>
              <tr><th>Tableau</th><th>Type</th><th>Format</th><th>Statut</th><th>Inscrits</th></tr>
            </thead>
            <tbody>
              @for (tableau of event.tableaux; track tableau.tournamentId) {
                <tr>
                  <td><a [routerLink]="['/tournaments', tableau.tournamentId]">{{ tableau.name }}</a></td>
                  <td>{{ tableau.isDoubles ? 'Double' : 'Simple' }}</td>
                  <td>{{ formatLabel(tableau.format) }}</td>
                  <td>{{ statusLabel(tableau.status) }}</td>
                  <td>
                    {{ tableau.approvedCount }} {{ tableau.isDoubles ? 'paire' : 'joueur' }}{{ tableau.approvedCount > 1 ? 's' : '' }}
                    @if (tableau.pendingCount > 0) { <span class="muted">(+{{ tableau.pendingCount }} en attente)</span> }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="5" class="muted">Aucun tableau pour le moment.</td></tr>
              }
            </tbody>
          </table>

          @if (authService.isAdmin()) {
            <div class="form-row add-tableau">
              <input type="text" [(ngModel)]="tableauForm.name" name="tableauName" placeholder="Nom du tableau (ex. Simple)">
              <select [(ngModel)]="tableauForm.format" name="tableauFormat">
                <option [ngValue]="TournamentFormat.SingleElimination">Élimination directe</option>
                <option [ngValue]="TournamentFormat.DoubleElimination">Double élimination</option>
                <option [ngValue]="TournamentFormat.RoundRobin">Round Robin</option>
                <option [ngValue]="TournamentFormat.GroupStage">Phase de groupes</option>
              </select>
              @if (features.isEnabled('doubles')) {
                <label class="checkbox-label">
                  <input type="checkbox" [(ngModel)]="tableauForm.isDoubles" name="tableauDoubles"> Double
                </label>
              }
              <button (click)="addTableau()" [disabled]="!tableauForm.name">Ajouter un tableau</button>
            </div>
            @if (event.seasonName) {
              <p class="hint">Les tableaux en phase de groupes de saison utilisent le format FSGT par défaut (réglable sur la page des tournois).</p>
            }
          }
        </div>

        <!-- Validation des inscriptions en ligne (admin) -->
        @if (authService.isAdmin() && event.pendingRegistrations && event.pendingRegistrations.length > 0) {
          <div class="section">
            <h3>Inscriptions en attente ({{ event.pendingRegistrations.length }})</h3>
            <ul class="pending">
              @for (pending of event.pendingRegistrations; track pending.tournamentId + '-' + pending.sideId) {
                <li>
                  <span><strong>{{ pending.tournamentName }}</strong> : {{ pending.label }}</span>
                  <button (click)="approve(pending)">Valider</button>
                  <button class="danger" (click)="reject(pending)">Refuser</button>
                </li>
              }
            </ul>
          </div>
        }

        <!-- Inscription en ligne (joueur) -->
        @if (authService.isAuthenticated() && features.isEnabled('onlineRegistration')) {
          <div class="section">
            <h3>Mon inscription</h3>
            @if (!authService.linkedPlayerId()) {
              <p class="muted">Créez ou liez votre profil joueur (page Profil) pour vous inscrire.</p>
            } @else {
              @if (myRegistrations.length > 0) {
                <ul class="mine">
                  @for (reg of myRegistrations; track reg.tournamentId) {
                    <li>
                      <span>
                        <strong>{{ reg.tournamentName }}</strong>
                        @if (reg.partnerName) { avec {{ reg.partnerName }} }
                        <span class="status" [class.approved]="reg.status === RegistrationStatus.Approved">
                          {{ reg.status === RegistrationStatus.Approved ? 'Validée' : 'En attente' }}
                        </span>
                      </span>
                      @if (reg.canUnregister) {
                        <button class="danger" (click)="unregister(reg)">Se désinscrire</button>
                      }
                    </li>
                  }
                </ul>
              }

              @if (!event.registrationOpen) {
                <p class="muted">Les inscriptions sont closes.</p>
              } @else if (availableTableaux.length > 0) {
                <div class="register">
                  @for (tableau of availableTableaux; track tableau.tournamentId) {
                    <div class="register-row">
                      <label class="checkbox-label">
                        <input type="checkbox" [(ngModel)]="selection[tableau.tournamentId].checked" [name]="'sel' + tableau.tournamentId">
                        {{ tableau.name }} ({{ tableau.isDoubles ? 'double' : 'simple' }})
                      </label>
                      @if (tableau.isDoubles && selection[tableau.tournamentId].checked) {
                        <select [(ngModel)]="selection[tableau.tournamentId].partnerId" [name]="'partner' + tableau.tournamentId">
                          <option [ngValue]="null">Choisir mon partenaire…</option>
                          @for (player of partnerCandidates; track player.id) {
                            <option [ngValue]="player.id">{{ player.firstName }} {{ player.lastName }}</option>
                          }
                        </select>
                      }
                    </div>
                  }
                  <button (click)="register()" [disabled]="!canSubmit()">M'inscrire</button>
                  <p class="hint">Votre inscription sera validée par l'organisateur. Votre partenaire n'a pas besoin de compte.</p>
                </div>
              } @else if (myRegistrations.length === 0) {
                <p class="muted">Aucun tableau ouvert aux inscriptions.</p>
              }
            }
          </div>
        }
      } @else if (loading) {
        <div class="loading">Chargement...</div>
      }
    </div>
  `,
  styles: [`
    .container {
      max-width: 1000px;
      margin: 20px auto;
      padding: 20px;
    }
    .header h2 {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
      margin-bottom: 5px;
    }
    .description {
      font-style: italic;
      color: var(--text-muted);
    }
    .muted, .hint {
      color: var(--text-muted);
    }
    .hint {
      font-size: 0.85em;
    }
    a {
      color: var(--link);
    }
    .badge {
      padding: 2px 10px;
      border-radius: 10px;
      font-size: 0.5em;
      color: white;
      background: var(--action);
    }
    .badge.closed {
      background: var(--secondary);
    }
    .section {
      margin-top: 25px;
    }
    .form-box {
      padding: 15px;
      background: var(--surface-alt);
      border-radius: 8px;
    }
    .form-row {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
      align-items: center;
      margin-bottom: 10px;
    }
    .form-row label {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 0.9em;
      color: var(--text-muted);
    }
    input, select {
      padding: 8px;
      border: 1px solid var(--border);
      border-radius: 4px;
    }
    input.full {
      width: 100%;
      box-sizing: border-box;
      margin-bottom: 10px;
    }
    .checkbox-label input {
      width: auto;
    }
    button {
      padding: 6px 14px;
      background: var(--primary);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    button.secondary {
      background: var(--secondary);
    }
    button.danger {
      background: var(--danger);
    }
    .actions {
      display: flex;
      gap: 10px;
    }
    .tableaux {
      width: 100%;
      border-collapse: collapse;
      margin-bottom: 12px;
    }
    .tableaux th, .tableaux td {
      padding: 8px;
      text-align: left;
      border-bottom: 1px solid var(--border-light);
    }
    .tableaux th {
      background: var(--surface-alt);
      font-size: 0.85em;
    }
    .add-tableau {
      padding: 10px;
      background: var(--surface-alt);
      border-radius: 8px;
    }
    .pending, .mine {
      list-style: none;
      padding: 0;
    }
    .pending li, .mine li {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 0;
      border-bottom: 1px solid var(--border-light);
    }
    .pending li span, .mine li span {
      flex: 1;
    }
    .status {
      margin-left: 8px;
      padding: 1px 8px;
      border-radius: 10px;
      font-size: 0.8em;
      background: var(--accent-soft);
    }
    .status.approved {
      background: color-mix(in srgb, var(--success) 20%, transparent);
    }
    .register {
      display: flex;
      flex-direction: column;
      gap: 10px;
      align-items: flex-start;
    }
    .register-row {
      display: flex;
      gap: 10px;
      align-items: center;
      flex-wrap: wrap;
    }
    .loading {
      text-align: center;
      padding: 20px;
      color: var(--text-muted);
    }
    @media (max-width: 600px) {
      .tableaux th:nth-child(3), .tableaux td:nth-child(3) { display: none; }
    }
  `]
})
export class EventDetailComponent implements OnInit {
  event: DartsEventDetail | null = null;
  loading = false;
  eventId = 0;

  players: Player[] = [];
  seasons: Season[] = [];
  selection: Record<number, { checked: boolean; partnerId: number | null }> = {};

  editing = false;
  editForm = { name: '', description: '', location: '', startDate: '', registrationDeadline: '', seasonId: null as number | null };
  tableauForm = { name: '', format: TournamentFormat.SingleElimination, isDoubles: false };

  TournamentFormat = TournamentFormat;
  RegistrationStatus = RegistrationStatus;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  features = inject(FeatureService);

  constructor(
    private route: ActivatedRoute,
    public authService: AuthService,
    private apiService: ApiService
  ) {}

  ngOnInit() {
    this.eventId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadEvent();
    if (this.authService.isAuthenticated()) {
      this.apiService.getPlayers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(players => {
        this.players = players;
      });
    }
    if (this.authService.isAdmin() && this.features.isEnabled('seasons')) {
      this.apiService.getSeasons().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(seasons => {
        this.seasons = seasons;
      });
    }
  }

  loadEvent() {
    this.loading = true;
    this.apiService.getEvent(this.eventId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
      this.event = event;
      this.loading = false;
      this.selection = {};
      for (const tableau of this.availableTableaux) {
        this.selection[tableau.tournamentId] = { checked: false, partnerId: null };
      }
    });
  }

  get myRegistrations(): MyRegistration[] {
    return this.event?.myRegistrations ?? [];
  }

  // Tableaux pas encore lancés où le joueur n'est pas inscrit
  get availableTableaux(): EventTableau[] {
    const registered = new Set(this.myRegistrations.map(r => r.tournamentId));
    return (this.event?.tableaux ?? [])
      .filter(t => t.status === TournamentStatus.Draft && !registered.has(t.tournamentId));
  }

  get partnerCandidates(): Player[] {
    const me = this.authService.linkedPlayerId();
    return this.players.filter(p => p.id !== me);
  }

  canSubmit(): boolean {
    const chosen = this.availableTableaux.filter(t => this.selection[t.tournamentId]?.checked);
    return chosen.length > 0
      && chosen.every(t => !t.isDoubles || this.selection[t.tournamentId].partnerId !== null);
  }

  register() {
    const entries = this.availableTableaux
      .filter(t => this.selection[t.tournamentId]?.checked)
      .map(t => t.isDoubles
        ? { tournamentId: t.tournamentId, partnerId: this.selection[t.tournamentId].partnerId! }
        : { tournamentId: t.tournamentId });

    this.apiService.registerToEvent(this.eventId, entries).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.notificationService.showSuccess(res.message);
        this.loadEvent();
      },
      error: err => this.notificationService.showError(err.error?.message || "Erreur lors de l'inscription")
    });
  }

  unregister(reg: MyRegistration) {
    const message = reg.isDoubles
      ? `Se désinscrire de « ${reg.tournamentName} » ? La paire avec ${reg.partnerName} sera annulée.`
      : `Se désinscrire de « ${reg.tournamentName} » ?`;
    if (!confirm(message)) return;

    this.apiService.unregisterFromTournament(reg.tournamentId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Désinscription enregistrée');
        this.loadEvent();
      },
      error: err => this.notificationService.showError(err.error?.message || 'Erreur lors de la désinscription')
    });
  }

  approve(pending: PendingRegistration) {
    const call: Observable<unknown> = pending.isDoubles
      ? this.apiService.approveTeam(pending.tournamentId, pending.sideId)
      : this.apiService.approveRegistration(pending.tournamentId, pending.sideId);
    this.handleAdminAction(call, 'Inscription validée');
  }

  reject(pending: PendingRegistration) {
    if (!confirm(`Refuser l'inscription de ${pending.label} ?`)) return;
    const call: Observable<unknown> = pending.isDoubles
      ? this.apiService.rejectTeam(pending.tournamentId, pending.sideId)
      : this.apiService.rejectRegistration(pending.tournamentId, pending.sideId);
    this.handleAdminAction(call, 'Inscription refusée');
  }

  addTableau() {
    this.apiService.createTournament({
      name: this.tableauForm.name,
      format: this.tableauForm.format,
      isDoubles: this.tableauForm.isDoubles,
      eventId: this.eventId
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Tableau ajouté');
        this.tableauForm = { name: '', format: TournamentFormat.SingleElimination, isDoubles: false };
        this.loadEvent();
      },
      error: err => this.notificationService.showError(err.error?.message || "Erreur lors de l'ajout du tableau")
    });
  }

  startEdit() {
    if (!this.event) return;
    this.editForm = {
      name: this.event.name,
      description: this.event.description ?? '',
      location: this.event.location ?? '',
      startDate: this.event.startDate ? this.event.startDate.substring(0, 10) : '',
      registrationDeadline: this.event.registrationDeadline ? this.toLocalInput(this.event.registrationDeadline) : '',
      seasonId: this.event.seasonId ?? null
    };
    this.editing = true;
  }

  saveEdit() {
    this.apiService.updateEvent(this.eventId, {
      name: this.editForm.name,
      description: this.editForm.description || null,
      location: this.editForm.location || null,
      startDate: this.editForm.startDate || null,
      registrationDeadline: this.editForm.registrationDeadline ? new Date(this.editForm.registrationDeadline).toISOString() : null,
      seasonId: this.editForm.seasonId
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Événement modifié');
        this.editing = false;
        this.loadEvent();
      },
      error: err => this.notificationService.showError(err.error?.message || 'Erreur lors de la modification')
    });
  }

  formatLabel(format: TournamentFormat): string {
    switch (format) {
      case TournamentFormat.SingleElimination: return 'Élimination directe';
      case TournamentFormat.DoubleElimination: return 'Double élimination';
      case TournamentFormat.RoundRobin: return 'Round Robin';
      case TournamentFormat.GroupStage: return 'Phase de groupes';
      default: return 'Inconnu';
    }
  }

  statusLabel(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Draft: return 'Inscriptions';
      case TournamentStatus.InProgress: return 'En cours';
      case TournamentStatus.Completed: return 'Terminé';
      default: return 'Inconnu';
    }
  }

  private handleAdminAction(call: Observable<unknown>, success: string) {
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess(success);
        this.loadEvent();
      },
      error: err => this.notificationService.showError(err.error?.message || 'Erreur')
    });
  }

  // ISO UTC -> valeur d'un input datetime-local (heure locale)
  private toLocalInput(iso: string): string {
    const d = new Date(iso);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
