import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { FeatureService } from '../../core/services/feature.service';
import { DartsEvent, Season, SeasonStatus } from '../../core/models';

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      <h2>Événements</h2>
      <p class="intro">Un événement regroupe plusieurs tableaux (simple, double) avec une inscription en ligne commune.</p>

      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (authService.isAdmin()) {
        <div class="add-form">
          <h3>Créer un événement</h3>
          <form (ngSubmit)="createEvent()">
            <div class="form-row">
              <input type="text" [(ngModel)]="form.name" name="name" placeholder="Nom (ex. Open de printemps)" required>
              <input type="text" [(ngModel)]="form.location" name="location" placeholder="Lieu (optionnel)">
              <label>
                Date
                <input type="date" [(ngModel)]="form.startDate" name="startDate">
              </label>
              <label>
                Clôture des inscriptions
                <input type="datetime-local" [(ngModel)]="form.registrationDeadline" name="registrationDeadline">
              </label>
              @if (features.isEnabled('seasons') && openSeasons.length > 0) {
                <select [(ngModel)]="form.seasonId" name="seasonId">
                  <option [ngValue]="null">Hors saison</option>
                  @for (season of openSeasons; track season.id) {
                    <option [ngValue]="season.id">{{ season.name }} (501 DO)</option>
                  }
                </select>
              }
            </div>
            <input type="text" [(ngModel)]="form.description" name="description" placeholder="Description (optionnelle)">
            <button type="submit" [disabled]="!form.name">Créer</button>
          </form>
        </div>
      }

      <div class="event-list">
        @for (event of events; track event.id) {
          <div class="event-card">
            <h3>
              <a [routerLink]="['/events', event.id]">{{ event.name }}</a>
              <span class="badge" [class.closed]="!event.registrationOpen">
                {{ event.registrationOpen ? 'Inscriptions ouvertes' : 'Inscriptions closes' }}
              </span>
            </h3>
            @if (event.startDate) {
              <p>{{ event.startDate | date:'dd/MM/yyyy' }}@if (event.location) { · {{ event.location }} }</p>
            } @else if (event.location) {
              <p>{{ event.location }}</p>
            }
            @if (event.registrationDeadline) {
              <p class="muted">Clôture : {{ event.registrationDeadline | date:'dd/MM/yyyy HH:mm' }}</p>
            }
            <p>{{ event.tableauCount }} tableau{{ event.tableauCount > 1 ? 'x' : '' }}</p>
            @if (event.seasonName && features.isEnabled('seasons')) {
              <p>Saison : <a [routerLink]="['/seasons', event.seasonId]">{{ event.seasonName }}</a></p>
            }
            @if (authService.isAdmin()) {
              <button (click)="deleteEvent(event.id)" class="delete">Supprimer</button>
            }
          </div>
        } @empty {
          @if (!loading) {
            <p class="empty">Aucun événement pour le moment.</p>
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
    .intro, .muted {
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
    .add-form input, .add-form select {
      padding: 8px;
      border: 1px solid var(--border);
      border-radius: 4px;
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
    .event-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 20px;
    }
    .event-card {
      padding: 15px;
      border: 1px solid var(--border);
      border-radius: 8px;
    }
    .event-card h3 {
      margin-top: 0;
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }
    .event-card a {
      color: var(--link);
      text-decoration: none;
    }
    .event-card a:hover {
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
export class EventListComponent implements OnInit {
  events: DartsEvent[] = [];
  openSeasons: Season[] = [];
  form = this.emptyForm();
  loading = false;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  features = inject(FeatureService);

  constructor(public authService: AuthService, private apiService: ApiService) {}

  ngOnInit() {
    this.loadEvents();
    if (this.authService.isAdmin() && this.features.isEnabled('seasons')) {
      this.apiService.getSeasons().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(seasons => {
        this.openSeasons = seasons.filter(s => s.status === SeasonStatus.InProgress);
      });
    }
  }

  loadEvents() {
    this.loading = true;
    this.apiService.getEvents().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(events => {
      this.events = events;
      this.loading = false;
    });
  }

  createEvent() {
    this.apiService.createEvent({
      name: this.form.name,
      description: this.form.description || null,
      location: this.form.location || null,
      startDate: this.form.startDate || null,
      // datetime-local = heure locale, envoyée en UTC
      registrationDeadline: this.form.registrationDeadline ? new Date(this.form.registrationDeadline).toISOString() : null,
      seasonId: this.form.seasonId
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(newEvent => {
      this.notificationService.showSuccess('Événement créé');
      this.events = [newEvent, ...this.events];
      this.form = this.emptyForm();
    });
  }

  deleteEvent(id: number) {
    if (confirm('Voulez-vous vraiment supprimer cet événement ? Ses tableaux ne seront pas supprimés, seulement détachés.')) {
      this.apiService.deleteEvent(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Événement supprimé');
        this.events = this.events.filter(e => e.id !== id);
      });
    }
  }

  private emptyForm() {
    return {
      name: '',
      description: '',
      location: '',
      startDate: '',
      registrationDeadline: '',
      seasonId: null as number | null
    };
  }
}
