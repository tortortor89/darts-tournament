import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
import { Player } from '../../core/models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  showCreateForm = signal(false);
  showLinkList = signal(false);
  showPasswordForm = signal(false);
  showEditPlayerForm = signal(false);
  availablePlayers = signal<Player[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Form data
  newPlayer = {
    firstName: '',
    lastName: '',
    nickname: ''
  };

  editPlayerForm = {
    firstName: '',
    lastName: '',
    nickname: ''
  };

  passwordForm = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check if user is authenticated
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
    }
  }

  toggleCreateForm(): void {
    this.showCreateForm.set(!this.showCreateForm());
    this.showLinkList.set(false);
    this.showPasswordForm.set(false);
    this.showEditPlayerForm.set(false);
    this.clearMessages();
  }

  toggleEditPlayerForm(): void {
    if (!this.showEditPlayerForm()) {
      // Load current player data when opening the form
      this.loadCurrentPlayerData();
    }
    this.showEditPlayerForm.set(!this.showEditPlayerForm());
    this.showCreateForm.set(false);
    this.showLinkList.set(false);
    this.showPasswordForm.set(false);
    this.clearMessages();
  }

  loadCurrentPlayerData(): void {
    const playerId = this.authService.linkedPlayerId();
    if (!playerId) return;

    this.apiService.getPlayerDetail(playerId).subscribe({
      next: (player) => {
        this.editPlayerForm = {
          firstName: player.firstName,
          lastName: player.lastName,
          nickname: player.nickname || ''
        };
      },
      error: (error) => {
        console.error('Error loading player data:', error);
        this.errorMessage.set('Erreur lors du chargement des données');
      }
    });
  }

  toggleLinkList(): void {
    this.showLinkList.set(!this.showLinkList());
    this.showCreateForm.set(false);
    this.showPasswordForm.set(false);
    this.showEditPlayerForm.set(false);
    this.clearMessages();

    if (this.showLinkList()) {
      this.loadAvailablePlayers();
    }
  }

  togglePasswordForm(): void {
    this.showPasswordForm.set(!this.showPasswordForm());
    this.showCreateForm.set(false);
    this.showLinkList.set(false);
    this.showEditPlayerForm.set(false);
    this.clearMessages();
    this.resetPasswordForm();
  }

  loadAvailablePlayers(): void {
    this.loading.set(true);
    this.apiService.getAvailablePlayers().subscribe({
      next: (players) => {
        this.availablePlayers.set(players);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading available players:', error);
        this.errorMessage.set('Erreur lors du chargement des joueurs disponibles');
        this.loading.set(false);
      }
    });
  }

  createOwnPlayer(): void {
    if (!this.newPlayer.firstName || !this.newPlayer.lastName) {
      this.errorMessage.set('Le prénom et le nom sont requis');
      return;
    }

    this.loading.set(true);
    this.clearMessages();

    this.apiService.createOwnPlayer(this.newPlayer).subscribe({
      next: (player) => {
        const playerName = `${player.firstName} ${player.lastName}`;
        this.authService.updateLinkedPlayer(player.id, playerName);
        this.successMessage.set('Profil joueur créé avec succès !');
        this.showCreateForm.set(false);
        this.resetForm();
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error creating player:', error);
        this.errorMessage.set(error.error?.message || 'Erreur lors de la création du profil');
        this.loading.set(false);
      }
    });
  }

  linkToPlayer(playerId: number): void {
    this.loading.set(true);
    this.clearMessages();

    this.apiService.linkToPlayer(playerId).subscribe({
      next: () => {
        // Find the player to get their name
        const player = this.availablePlayers().find(p => p.id === playerId);
        if (player) {
          const playerName = `${player.firstName} ${player.lastName}`;
          this.authService.updateLinkedPlayer(player.id, playerName);
        }
        this.successMessage.set('Profil joueur lié avec succès !');
        this.showLinkList.set(false);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error linking player:', error);
        this.errorMessage.set(error.error?.message || 'Erreur lors de la liaison du profil');
        this.loading.set(false);
      }
    });
  }

  unlinkPlayer(): void {
    if (!confirm('Êtes-vous sûr de vouloir délier ce profil joueur ?')) {
      return;
    }

    const playerId = this.authService.linkedPlayerId();
    if (!playerId) return;

    this.loading.set(true);
    this.clearMessages();

    this.apiService.unlinkPlayer(playerId).subscribe({
      next: () => {
        this.authService.clearLinkedPlayer();
        this.successMessage.set('Profil joueur délié avec succès');
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error unlinking player:', error);
        this.errorMessage.set(error.error?.message || 'Erreur lors du déliage du profil');
        this.loading.set(false);
      }
    });
  }

  updatePlayerProfile(): void {
    if (!this.editPlayerForm.firstName || !this.editPlayerForm.lastName) {
      this.errorMessage.set('Le prénom et le nom sont requis');
      return;
    }

    this.loading.set(true);
    this.clearMessages();

    this.apiService.updateOwnPlayer(this.editPlayerForm).subscribe({
      next: (player) => {
        const playerName = `${player.firstName} ${player.lastName}`;
        this.authService.updateLinkedPlayer(player.id, playerName);
        this.successMessage.set('Profil joueur mis à jour avec succès');
        this.showEditPlayerForm.set(false);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error updating player:', error);
        this.errorMessage.set(error.error?.message || 'Erreur lors de la mise à jour du profil');
        this.loading.set(false);
      }
    });
  }

  changePassword(): void {
    // Validation
    if (!this.passwordForm.currentPassword || !this.passwordForm.newPassword || !this.passwordForm.confirmPassword) {
      this.errorMessage.set('Tous les champs sont requis');
      return;
    }

    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.errorMessage.set('Les mots de passe ne correspondent pas');
      return;
    }

    if (this.passwordForm.newPassword.length < 8) {
      this.errorMessage.set('Le nouveau mot de passe doit contenir au moins 8 caractères');
      return;
    }

    this.loading.set(true);
    this.clearMessages();

    this.apiService.changePassword(this.passwordForm.currentPassword, this.passwordForm.newPassword).subscribe({
      next: () => {
        this.successMessage.set('Mot de passe changé avec succès');
        this.showPasswordForm.set(false);
        this.resetPasswordForm();
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error changing password:', error);
        this.errorMessage.set(error.error?.message || 'Erreur lors du changement de mot de passe');
        this.loading.set(false);
      }
    });
  }

  private resetForm(): void {
    this.newPlayer = {
      firstName: '',
      lastName: '',
      nickname: ''
    };
  }

  private resetPasswordForm(): void {
    this.passwordForm = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    };
  }

  private clearMessages(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }
}
