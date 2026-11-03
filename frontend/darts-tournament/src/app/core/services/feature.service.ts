import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { FeatureFlags, GameMode } from '../models';
import { environment } from '../../../environments/environment';

const ALL_ENABLED: FeatureFlags = {
  circuits: true,
  interclubs: true,
  doubles: true,
  tv: true,
  cricket: true,
  threeOhOne: true,
  onlineRegistration: true,
  seasons: true,
  exploits: true
};

/**
 * Modules activés sur cette instance (GET /config/features), chargés au démarrage.
 * En cas d'échec de chargement, tout reste activé : le backend refuse de toute façon
 * les endpoints des modules désactivés.
 */
@Injectable({
  providedIn: 'root'
})
export class FeatureService {
  readonly features = signal<FeatureFlags>(ALL_ENABLED);

  constructor(private http: HttpClient) {}

  load(): Promise<void> {
    return firstValueFrom(this.http.get<FeatureFlags>(`${environment.apiUrl}/config/features`))
      .then(features => this.features.set({ ...ALL_ENABLED, ...features }))
      .catch(() => { /* garder les valeurs par défaut */ });
  }

  isEnabled(feature: keyof FeatureFlags): boolean {
    return this.features()[feature];
  }

  isGameModeEnabled(mode: GameMode): boolean {
    switch (mode) {
      case GameMode.Cricket: return this.features().cricket;
      case GameMode.ThreeOhOne: return this.features().threeOhOne;
      default: return true;
    }
  }
}
