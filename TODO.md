# TODO - Darts Tournament

## Priorité Haute - Sécurité / Intégrité

- [ ] Ajouter validation des entrées sur les DTOs (password, username, noms)
- [ ] Externaliser les credentials (BDD, JWT) hors de appsettings.json
- [ ] Protéger les routes Angular avec authGuard
- [ ] Ajouter middleware global de gestion des erreurs (Backend)
- [ ] Corriger la race condition sur la génération knockout
- [x] Implémenter un intercepteur HTTP pour gérer les 401 (Angular)

## Priorité Moyenne - Qualité / UX

- [ ] Corriger les fuites mémoire (unsubscribe dans les composants)
- [ ] Ajouter des indicateurs de chargement (loading states)
- [ ] Améliorer la gestion d'erreurs avec messages informatifs
- [ ] Ajouter des notifications toast pour les actions utilisateur
- [ ] Optimiser le rechargement des données (éviter reload complet)
- [x] Ajouter vérification d'intégrité pour les scripts CDN (N/A - pas de CDN)

## Priorité Basse - Polish / Documentation

- [ ] Écrire des tests unitaires (Backend - Services)
- [ ] Écrire des tests unitaires (Frontend - Services/Components)
- [ ] Ajouter documentation Swagger avec commentaires XML
- [ ] Améliorer l'accessibilité (labels, ARIA)
- [x] Rendre l'URL API configurable par environnement
- [ ] Implémenter la concurrence optimiste (RowVersion)

## Nouvelles Fonctionnalités

### Affichage & Formats de tournoi
- [x] Améliorer l'affichage Round Robin (s'inspirer du format Group Stage)
- [x] Améliorer l'affichage Single Elimination (bracket visuel cohérent)
- [ ] Ajouter le format Double Elimination
- [ ] Écrans de gestion de match dédiés (saisie des scores, legs, sets)

### Statistiques & Joueurs
- [ ] Page de statistiques par joueur (moyenne, % victoires, historique)
- [ ] Historique des confrontations directes entre joueurs
- [ ] Graphiques d'évolution des performances

### Circuit & Classement
- [ ] Système de circuit (regrouper plusieurs tournois)
- [ ] Classement global sur un circuit (points cumulés)
- [ ] Seeding automatique basé sur le classement circuit

### Fonctionnalités avancées
- [ ] Support des formats de jeu (501, 301, Cricket)
- [ ] Gestion des legs et sets (pas juste un score simple)
- [ ] Mode équipes / doubles
- [ ] Export des résultats (PDF, CSV)
- [ ] Mode affichage public (écran spectateur sans contrôles)
- [ ] Dashboard récapitulatif (stats globales, tournois récents)
