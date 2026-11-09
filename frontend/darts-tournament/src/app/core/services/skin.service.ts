import { Injectable, Inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Branding } from '../models';
import { DEFAULT_SKIN, SKINS } from '../skins';
import { environment } from '../../../environments/environment';

/**
 * Skin de l'instance (GET /config/branding), chargé au démarrage : pose data-skin sur <html>
 * (les tokens CSS du skin s'appliquent alors), le titre, le favicon et les polices.
 * En cas d'échec de chargement ou de skin inconnu, le skin par défaut reste actif.
 */
@Injectable({
  providedIn: 'root'
})
export class SkinService {
  readonly skin = signal(DEFAULT_SKIN);
  readonly appName = signal(SKINS[DEFAULT_SKIN].appName);

  constructor(private http: HttpClient, @Inject(DOCUMENT) private document: Document) {}

  load(): Promise<void> {
    return firstValueFrom(this.http.get<Branding>(`${environment.apiUrl}/config/branding`))
      .then(branding => this.apply(branding))
      .catch(() => { /* garder le skin par défaut */ });
  }

  private apply(branding: Branding): void {
    const name = SKINS[branding.skin] ? branding.skin : DEFAULT_SKIN;
    const skin = SKINS[name];
    const appName = branding.appName?.trim() || skin.appName;

    this.skin.set(name);
    this.appName.set(appName);

    const root = this.document.documentElement;
    if (name === DEFAULT_SKIN) {
      root.removeAttribute('data-skin');
    } else {
      root.setAttribute('data-skin', name);
    }

    this.document.title = appName;
    this.document.querySelector<HTMLLinkElement>('link[rel="icon"]')?.setAttribute('href', skin.favicon);

    if (skin.fontsUrl) {
      const link = this.document.createElement('link');
      link.rel = 'stylesheet';
      link.href = skin.fontsUrl;
      this.document.head.appendChild(link);
    }
  }
}
