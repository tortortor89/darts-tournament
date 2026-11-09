/**
 * Skins disponibles. La palette de chaque skin est dans src/skins/_<nom>.scss
 * (tokens CSS redéfinis sous [data-skin="<nom>"]) ; ici, ce qui ne passe pas par le CSS.
 */
export interface SkinDefinition {
  /** Nom affiché dans la navbar et l'onglet */
  appName: string;
  /** Favicon (chemin relatif à la racine du site) */
  favicon: string;
  /** Feuille Google Fonts à charger, si le skin utilise des polices web */
  fontsUrl?: string;
}

export const DEFAULT_SKIN = 'default';

export const SKINS: Record<string, SkinDefinition> = {
  default: {
    appName: 'Darts Tournament',
    favicon: 'favicon.ico'
  },
  hacienda: {
    appName: "Hacien'Darts Cup",
    favicon: 'assets/skins/hacienda/logo.png',
    fontsUrl: 'https://fonts.googleapis.com/css2?family=Barlow+Condensed:wght@600;700;800&family=Barlow:wght@400;500;600&display=swap'
  }
};
