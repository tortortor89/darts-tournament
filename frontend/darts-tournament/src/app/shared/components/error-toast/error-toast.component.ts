import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ErrorService } from '../../../core/services/error.service';

@Component({
  selector: 'app-error-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (errorService.currentError(); as error) {
      <div class="error-toast" (click)="errorService.clearError()">
        <span class="error-icon">!</span>
        <span class="error-message">{{ error.message }}</span>
        <button class="close-btn">&times;</button>
      </div>
    }
  `,
  styles: [`
    .error-toast {
      position: fixed;
      bottom: 20px;
      right: 20px;
      background: #dc3545;
      color: white;
      padding: 12px 16px;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      display: flex;
      align-items: center;
      gap: 12px;
      max-width: 400px;
      cursor: pointer;
      z-index: 9999;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    .error-icon {
      background: white;
      color: #dc3545;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      flex-shrink: 0;
    }

    .error-message {
      flex: 1;
      font-size: 0.9em;
    }

    .close-btn {
      background: none;
      border: none;
      color: white;
      font-size: 1.2em;
      cursor: pointer;
      padding: 0;
      opacity: 0.8;
    }

    .close-btn:hover {
      opacity: 1;
    }
  `]
})
export class ErrorToastComponent {
  errorService = inject(ErrorService);
}
