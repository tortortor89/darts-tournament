# TODO - Darts Tournament

## Priorité Haute - Sécurité / Intégrité

- [x] Ajouter validation des entrées sur les DTOs (password, username, noms)
- [ ] Externaliser les credentials (BDD, JWT) hors de appsettings.json
- [x] Protéger les routes Angular avec authGuard
- [x] Ajouter middleware global de gestion des erreurs (Backend)
- [x] Corriger la race condition sur la génération knockout
- [x] Implémenter un intercepteur HTTP pour gérer les 401 (Angular)
- [x] Gestion des rôles et permissions (Admin, Joueur, Non connecté)
  - Admin : création/suppression tournois, gestion joueurs, saisie scores
  - Joueur : consultation, inscription aux tournois
  - Non connecté : consultation seule

## Priorité Moyenne - Qualité / UX

- [x] Corriger les fuites mémoire (unsubscribe dans les composants)
- [x] Ajouter des indicateurs de chargement (loading states)
- [x] Améliorer la gestion d'erreurs avec messages informatifs
- [x] Ajouter des notifications toast pour les actions utilisateur
- [x] Optimiser le rechargement des données (éviter reload complet)
- [x] Ajouter vérification d'intégrité pour les scripts CDN (N/A - pas de CDN)

## Priorité Basse - Polish / Documentation

- [ ] Écrire des tests unitaires (Backend - Services)
- [ ] Écrire des tests unitaires (Frontend - Services/Components)
- [x] Ajouter documentation Swagger avec commentaires XML
- [ ] Améliorer l'accessibilité (labels, ARIA)
- [x] Rendre l'URL API configurable par environnement
- [ ] Implémenter la concurrence optimiste (RowVersion)

## Nouvelles Fonctionnalités

### Affichage & Formats de tournoi
- [x] Améliorer l'affichage Round Robin (s'inspirer du format Group Stage)
- [x] Améliorer l'affichage Single Elimination (bracket visuel cohérent)
- [x] Ajouter le format Double Elimination (bracket viewer avec lignes SVG, classement par élimination)

### Gestion de Match
**Backend :**
- [x] Modèle MatchSession (config: legsToWin, startingPlayer, gameMode)
- [x] Modèle état partie (legs gagnés, points restants, historique volées)
- [x] Endpoint POST /matches/{id}/start - démarrer un match avec config
- [x] Endpoint GET /matches/{id}/live - récupérer l'état en cours
- [x] Endpoint POST /matches/{id}/throw - enregistrer une volée/score
- [x] Endpoint POST /matches/{id}/validate - valider et clôturer le match

**Frontend :**
- [x] Route /matches/{id}/play - écran de jeu
- [x] Écran configuration match (legs à gagner, joueur qui commence)
- [x] Écran de jeu : affichage scores (legs, points restants 501)
- [x] Saisie score : mode volée (total) avec switch vers mode fléchette par fléchette
- [x] Logique 501 straight in, double out
- [x] Écran validation fin de match + mise à jour score tournoi
- [x] Route /matches/{id}/spectate - vue spectateur (lecture seule, pour projection)

**Optionnel (stats futures) :**
- [x] Persistance BDD des volées/fléchettes pour statistiques (table Throws créée)

**Évolutions futures :**
- [ ] Authentification : restreindre l'écran de jeu aux joueurs du match (nécessite lien User -> Player)
- [ ] Formats de jeu supplémentaires : 301, Cricket
- [ ] Gestion des sets en plus des legs
- [ ] Statistiques en temps réel : moyenne, % doubles, checkout rate
- [ ] Historique : voir le détail d'un match terminé (toutes les volées)
- [ ] Temps réel : SignalR pour refresh instantané du spectateur

### Comptes Joueurs & Inscription
- [ ] Lier un compte utilisateur à un profil joueur (User -> Player)
- [ ] Interface de création de compte joueur (inscription publique)
- [ ] Page profil joueur (édition de ses propres infos)
- [ ] Auto-inscription aux tournois (pour les utilisateurs connectés avec profil joueur)
- [ ] Gestion des inscriptions par l'admin (valider/refuser)

### Statistiques & Joueurs
- [ ] Page de statistiques par joueur (moyenne, % victoires, historique)
- [ ] Historique des confrontations directes entre joueurs
- [ ] Graphiques d'évolution des performances

### Circuit & Classement
- [ ] Système de circuit (regrouper plusieurs tournois)
- [ ] Classement global sur un circuit (points cumulés)
- [ ] Seeding automatique basé sur le classement circuit

### Fonctionnalités avancées
- [ ] Support des formats de jeu supplémentaires (301, Cricket) - 501 couvert dans Gestion de Match
- [ ] Gestion des sets (en plus des legs)
- [ ] Mode équipes / doubles
- [ ] Export des résultats (PDF, CSV)
- [ ] Dashboard récapitulatif (stats globales, tournois récents)
