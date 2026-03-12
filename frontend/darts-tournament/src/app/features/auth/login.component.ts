import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <h2>Connexion</h2>
      <form (ngSubmit)="onSubmit()">
        <div class="form-group">
          <label for="username">Nom d'utilisateur</label>
          <input type="text" id="username" [(ngModel)]="username" name="username" required>
        </div>
        <div class="form-group">
          <label for="password">Mot de passe</label>
          <input type="password" id="password" [(ngModel)]="password" name="password" required>
        </div>
        @if (error) {
          <div class="error">{{ error }}</div>
        }
        <button type="submit" [disabled]="loading">
          {{ loading ? 'Connexion...' : 'Se connecter' }}
        </button>
      </form>
      <p>Pas de compte ? <a routerLink="/register">S'inscrire</a></p>
    </div>
  `,
  styles: [`
    .auth-container {
      max-width: 400px;
      margin: 50px auto;
      padding: 20px;
      border: 1px solid #ddd;
      border-radius: 8px;
    }
    .form-group {
      margin-bottom: 15px;
    }
    label {
      display: block;
      margin-bottom: 5px;
    }
    input {
      width: 100%;
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      box-sizing: border-box;
    }
    button {
      width: 100%;
      padding: 10px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    button:disabled {
      background: #ccc;
    }
    .error {
      color: red;
      margin-bottom: 15px;
    }
    p {
      text-align: center;
      margin-top: 15px;
    }
  `]
})
export class LoginComponent {
  username = '';
  password = '';
  error = '';
  loading = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit() {
    this.loading = true;
    this.error = '';

    this.authService.login(this.username, this.password).subscribe({
      next: () => {
        this.router.navigate(['/tournaments']);
      },
      error: (err) => {
        this.error = err.error || 'Erreur de connexion';
        this.loading = false;
      }
    });
  }
}
