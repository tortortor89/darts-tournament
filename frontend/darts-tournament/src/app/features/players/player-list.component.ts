import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Player } from '../../core/models';

@Component({
  selector: 'app-player-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
            @if (authService.isAdmin()) {
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
              @if (authService.isAdmin()) {
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
      margin-bottom: 20px;
      padding: 15px;
      background: #f5f5f5;
      border-radius: 8px;
    }
    .add-form form {
      display: flex;
      gap: 10px;
      flex-wrap: wrap;
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
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th, td {
      padding: 10px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }
    th {
      background: #f5f5f5;
    }
    button {
      padding: 5px 10px;
      margin-right: 5px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    button.delete {
      background: #dc3545;
      color: white;
    }
    .loading {
      text-align: center;
      padding: 20px;
      color: #666;
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
      this.apiService.updatePlayer(this.editingPlayer.id, this.form).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Joueur modifié');
        this.loadPlayers();
        this.cancelEdit();
      });
    } else {
      this.apiService.createPlayer(this.form).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Joueur créé');
        this.loadPlayers();
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
        this.loadPlayers();
      });
    }
  }
}
