import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (notificationService.current(); as notif) {
      <div class="toast" [class.success]="notif.type === 'success'" [class.error]="notif.type === 'error'" (click)="notificationService.clear()">
        <span class="icon">{{ notif.type === 'success' ? '✓' : '!' }}</span>
        <span class="message">{{ notif.message }}</span>
        <button class="close-btn">&times;</button>
      </div>
    }
  `,
  styles: [`
    .toast {
      position: fixed;
      bottom: 20px;
      right: 20px;
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

    .toast.success {
      background: #28a745;
    }

    .toast.error {
      background: #dc3545;
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

    .icon {
      background: white;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: bold;
      flex-shrink: 0;
    }

    .toast.success .icon {
      color: #28a745;
    }

    .toast.error .icon {
      color: #dc3545;
    }

    .message {
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
export class ToastComponent {
  notificationService = inject(NotificationService);
}
