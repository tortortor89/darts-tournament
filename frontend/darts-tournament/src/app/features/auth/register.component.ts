import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <h2>Inscription</h2>
      <form (ngSubmit)="onSubmit()">
        <div class="form-group">
          <label for="username">Nom d'utilisateur</label>
          <input type="text" id="username" [(ngModel)]="username" name="username" required>
        </div>
        <div class="form-group">
          <label for="password">Mot de passe</label>
          <input type="password" id="password" [(ngModel)]="password" name="password" required>
        </div>
        <div class="form-group">
          <label for="confirmPassword">Confirmer le mot de passe</label>
          <input type="password" id="confirmPassword" [(ngModel)]="confirmPassword" name="confirmPassword" required>
        </div>
        @if (error) {
          <div class="error">{{ error }}</div>
        }
        <button type="submit" [disabled]="loading">
          {{ loading ? 'Inscription...' : 'S\\'inscrire' }}
        </button>
      </form>
      <p>Déjà un compte ? <a routerLink="/login">Se connecter</a></p>
    </div>
  `,
  styles: [`
    .auth-container {
      max-width: 400px;
      margin: 60px auto;
      padding: 30px;
      border: 2px solid var(--border);
      border-radius: 8px;
      background: white;
      box-shadow: 0 4px 16px color-mix(in srgb, var(--shadow-color) 10%, transparent);
    }
    .form-group {
      margin-bottom: 15px;
    }
    label {
      display: block;
      margin-bottom: 5px;
      font-weight: 500;
      color: var(--text-muted);
    }
    input {
      width: 100%;
      padding: 10px;
      border: 1.5px solid var(--border);
      border-radius: 4px;
      box-sizing: border-box;
      background: var(--input-bg);
      color: var(--text);
    }
    input:focus {
      outline: none;
      border-color: var(--primary);
    }
    button {
      width: 100%;
      padding: 12px;
      background: var(--action);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
      font-size: 1em;
      letter-spacing: 0.03em;
    }
    button:hover {
      background: var(--action-hover);
    }
    button:disabled {
      background: var(--disabled);
      cursor: not-allowed;
    }
    .error {
      color: var(--danger);
      margin-bottom: 15px;
      font-size: 0.9em;
    }
    p {
      text-align: center;
      margin-top: 15px;
      color: var(--text-muted);
    }
  `]
})
export class RegisterComponent {
  username = '';
  password = '';
  confirmPassword = '';
  error = '';
  loading = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit() {
    if (this.password !== this.confirmPassword) {
      this.error = 'Les mots de passe ne correspondent pas';
      return;
    }

    this.loading = true;
    this.error = '';

    this.authService.register(this.username, this.password).subscribe({
      next: () => {
        this.router.navigate(['/tournaments']);
      },
      error: (err) => {
        this.error = err.error || 'Erreur lors de l\'inscription';
        this.loading = false;
      }
    });
  }
}
