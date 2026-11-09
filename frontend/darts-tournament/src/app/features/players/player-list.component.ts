import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Player } from '../../core/models';

@Component({
  selector: 'app-player-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="container">
      <h2>Joueurs</h2>

      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (authService.isAdmin()) {
        <div class="add-form">
          <h3>{{ editingPlayer ? 'Modifier' : 'Ajouter' }} un joueur</h3>
          <form (ngSubmit)="savePlayer()">
            <input type="text" [(ngModel)]="form.firstName" name="firstName" placeholder="Prénom" required>
            <input type="text" [(ngModel)]="form.lastName" name="lastName" placeholder="Nom" required>
            <input type="text" [(ngModel)]="form.nickname" name="nickname" placeholder="Surnom (optionnel)">
            <button type="submit">{{ editingPlayer ? 'Modifier' : 'Ajouter' }}</button>
            @if (editingPlayer) {
              <button type="button" (click)="cancelEdit()">Annuler</button>
            }
          </form>
        </div>
      }

      <table>
        <thead>
          <tr>
            <th>Nom</th>
            <th>Prénom</th>
            <th>Surnom</th>
            <th>Stats</th>
            @if (authService.isAdmin()) {
              <th>Utilisateur Lié</th>
              <th>Actions</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (player of players; track player.id) {
            <tr>
              <td>{{ player.lastName }}</td>
              <td>{{ player.firstName }}</td>
              <td>{{ player.nickname || '-' }}</td>
              <td>
                <a [routerLink]="['/players', player.id, 'stats']" class="btn-stats">📊 Stats</a>
              </td>
              @if (authService.isAdmin()) {
                <td>{{ player.linkedUsername || '-' }}</td>
                <td>
                  <button (click)="editPlayer(player)">Modifier</button>
                  <button (click)="deletePlayer(player.id)" class="delete">Supprimer</button>
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .container {
      max-width: 800px;
      margin: 20px auto;
      padding: 20px;
    }
    .add-form {
      margin-bottom: 24px;
      padding: 20px;
      background: white;
      border: 2px solid var(--hd-border);
      border-top: 4px solid var(--hd-green);
      border-radius: 8px;
    }
    .add-form h3 {
      margin-bottom: 12px;
    }
    .add-form form {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
    }
    .add-form input {
      padding: 8px 10px;
      border: 1.5px solid var(--hd-border);
      border-radius: 4px;
      background: var(--hd-cream);
      color: var(--hd-text);
    }
    .add-form input:focus {
      outline: none;
      border-color: var(--hd-amber);
    }
    .add-form button {
      padding: 8px 18px;
      background: var(--hd-amber);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .add-form button:hover {
      background: var(--hd-amber-light);
    }
    .add-form button[type="button"] {
      background: var(--hd-text-muted);
    }
    table {
      width: 100%;
      border-collapse: collapse;
      background: white;
      border: 1px solid var(--hd-border);
      border-radius: 8px;
      overflow: hidden;
    }
    th, td {
      padding: 12px 10px;
      text-align: left;
      border-bottom: 1px solid var(--hd-border);
    }
    th {
      background: var(--hd-green);
      color: white;
      font-family: 'Barlow Condensed', sans-serif;
      font-weight: 600;
      letter-spacing: 0.03em;
      font-size: 0.95em;
    }
    tr:last-child td {
      border-bottom: none;
    }
    tr:hover td {
      background: var(--hd-cream);
    }
    button {
      padding: 5px 10px;
      margin-right: 5px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      background: var(--hd-green-mid);
      color: white;
      font-size: 0.85em;
    }
    button:hover {
      background: var(--hd-green);
    }
    button.delete {
      background: var(--hd-danger);
      color: white;
    }
    button.delete:hover {
      background: var(--hd-danger-dark);
    }
    .loading {
      text-align: center;
      padding: 20px;
      color: var(--hd-text-muted);
    }
    .btn-stats {
      display: inline-block;
      padding: 6px 12px;
      background: var(--hd-amber);
      color: white;
      text-decoration: none;
      border-radius: 4px;
      font-size: 0.9em;
      font-weight: 500;
      transition: all 0.2s;
    }
    .btn-stats:hover {
      background: var(--hd-amber-light);
      transform: translateY(-1px);
      box-shadow: 0 3px 8px rgba(232,149,10,0.3);
    }
  `]
})
export class PlayerListComponent implements OnInit {
  players: Player[] = [];
  form = { firstName: '', lastName: '', nickname: '' };
  editingPlayer: Player | null = null;
  loading = false;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);

  constructor(public authService: AuthService, private apiService: ApiService) {}

  ngOnInit() {
    this.loadPlayers();
  }

  loadPlayers() {
    this.loading = true;
    this.apiService.getPlayers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(players => {
      this.players = players;
      this.loading = false;
    });
  }

  savePlayer() {
    if (this.editingPlayer) {
      const playerId = this.editingPlayer.id;
      this.apiService.updatePlayer(playerId, this.form).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Joueur modifié');
        this.players = this.players.map(p => p.id === playerId
          ? { ...p, firstName: this.form.firstName, lastName: this.form.lastName, nickname: this.form.nickname }
          : p
        );
        this.cancelEdit();
      });
    } else {
      this.apiService.createPlayer(this.form).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(newPlayer => {
        this.notificationService.showSuccess('Joueur créé');
        this.players = [...this.players, newPlayer];
        this.form = { firstName: '', lastName: '', nickname: '' };
      });
    }
  }

  editPlayer(player: Player) {
    this.editingPlayer = player;
    this.form = {
      firstName: player.firstName,
      lastName: player.lastName,
      nickname: player.nickname || ''
    };
  }

  cancelEdit() {
    this.editingPlayer = null;
    this.form = { firstName: '', lastName: '', nickname: '' };
  }

  deletePlayer(id: number) {
    if (confirm('Voulez-vous vraiment supprimer ce joueur ?')) {
      this.apiService.deletePlayer(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Joueur supprimé');
        this.players = this.players.filter(p => p.id !== id);
      });
    }
  }
}
