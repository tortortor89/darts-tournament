import { Injectable, signal } from '@angular/core';

export interface AppError {
  message: string;
  code?: string;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ErrorService {
  private _currentError = signal<AppError | null>(null);

  readonly currentError = this._currentError.asReadonly();

  showError(message: string, code?: string) {
    this._currentError.set({
      message,
      code,
      timestamp: new Date()
    });

    // Auto-clear after 5 seconds
    setTimeout(() => this.clearError(), 5000);
  }

  clearError() {
    this._currentError.set(null);
  }

  getErrorMessage(error: any): string {
    // Handle backend ErrorResponse format
    if (error?.error?.message) {
      return error.error.message;
    }

    // Handle string error
    if (typeof error?.error === 'string') {
      return error.error;
    }

    // Handle HTTP status codes
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
