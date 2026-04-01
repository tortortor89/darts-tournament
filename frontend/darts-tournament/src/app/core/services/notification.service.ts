import { Injectable, signal } from '@angular/core';

export type NotificationType = 'success' | 'error';

export interface AppNotification {
  message: string;
  type: NotificationType;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private _current = signal<AppNotification | null>(null);

  readonly current = this._current.asReadonly();

  showSuccess(message: string) {
    this._current.set({
      message,
      type: 'success',
      timestamp: new Date()
    });
    setTimeout(() => this.clear(), 3000);
  }

  showError(message: string) {
    this._current.set({
      message,
      type: 'error',
      timestamp: new Date()
    });
    setTimeout(() => this.clear(), 5000);
  }

  clear() {
    this._current.set(null);
  }

  getErrorMessage(error: any): string {
    if (error?.error?.message) {
      return error.error.message;
    }
    if (typeof error?.error === 'string') {
      return error.error;
    }
    switch (error?.status) {
      case 0:
        return 'Impossible de contacter le serveur. Vérifiez votre connexion.';
      case 400:
        return 'Requête invalide. Vérifiez les données saisies.';
      case 401:
        return 'Session expirée. Veuillez vous reconnecter.';
      case 403:
        return 'Accès non autorisé.';
      case 404:
        return 'Ressource non trouvée.';
      case 409:
        return 'Conflit de données. Veuillez rafraîchir la page.';
      case 500:
        return 'Erreur serveur. Veuillez réessayer plus tard.';
      default:
        return 'Une erreur inattendue s\'est produite.';
    }
  }
}
