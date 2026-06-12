# TODO - Darts Tournament

## 🚀 MVP - Points critiques à finaliser avant lancement

### ✅ TERMINÉ (Session 2026-06-05)

- [x] **SignalR broadcasting complet** : Broadcasting temps réel implémenté pour Cricket et 501
  - ✓ Backend : Événements CricketTurnRecorded, LegWon, MatchFinished émis correctement
  - ✓ Frontend : SignalRService écoute tous les événements
  - ✓ Vue spectateur Cricket : Affichage en temps réel du tableau des cibles
  - ✓ Backend compile sans erreur
  - ✓ Frontend compile sans erreur

- [x] **Mode Cricket fonctionnel** : Implémentation complète pour MVP
  - ✓ Saisie des visites avec validation
  - ✓ Calcul des règles de jeu (fermeture, scoring)
  - ✓ Broadcasting temps réel vers spectateurs
  - ✓ Vue spectateur avec tableau Cricket
  - ✓ Détection fin de leg et match
  - ⚠️ Stats Cricket détaillées reportées post-MVP (ligne 70)

### ✅ TERMINÉ (Session 2026-06-12)

- [x] **Revue critique des modes de jeu (501 + Cricket)** : corrections de bugs
  - ✓ dartsUsed/doublesAttempted enfin transmis au backend (stats checkout réparées)
  - ✓ Victoire Cricket à égalité de points (règle standard : fermé + score >= adversaire)
  - ✓ Événement LegWon Cricket : bon numéro de leg diffusé aux spectateurs
  - ✓ Garde-fou : volée 501 refusée sur une session Cricket
- [x] **Annuler la dernière volée (undo)** : endpoint DELETE + bouton UI (501 et Cricket, y compris depuis l'écran de fin avant validation), rollback complet (scores, legs, état Cricket rejoué), événement SignalR ThrowUndone
- [x] **Validation Cricket réaliste** : faisabilité en 3 fléchettes (max 3 marques/fléchette, 2 sur le Bull), backend + UI
- [x] **Checkout % précis** : basé sur les doubles réellement tentés quand le tracking est actif
- [x] **Projet de tests backend** : DartsTournament.Tests (xUnit, 23 tests sur la logique de jeu et les stats)

### 🔴 À FAIRE - Prochaine session

- [ ] **Tester Cricket end-to-end manuellement** : Valider le mode Cricket complet
  - Créer un tournoi
  - Lancer un match en mode Cricket
  - Jouer quelques legs
  - Vérifier vue spectateur en temps réel
  - Valider la fin du match

- [ ] **Externaliser les credentials** : Priorité HAUTE - Sécurité
  - Déplacer JWT Key vers variables d'environnement
  - Déplacer connection string PostgreSQL vers variables d'environnement
  - Mettre à jour appsettings.json et appsettings.Production.json

- [ ] **Tester déploiement Docker** : Valider les fichiers Docker créés
  - Build des images backend et frontend
  - Test du démarrage complet avec PostgreSQL
  - Vérifier les variables d'environnement
  - Tester l'accès via nginx

- [ ] **Tests manuels end-to-end complets** : Valider tous les formats de tournoi
  - Single Elimination : création → brackets → matchs → résultats
  - Double Elimination : brackets winners/losers → final
  - Round Robin : tous contre tous → classement final
  - Group Stage : phase de groupes → knockout → résultats
  - Cricket : match complet avec vue spectateur

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

- [x] Écrire des tests unitaires (Backend - Services) (projet DartsTournament.Tests : CricketService + MatchStatsService, 23 tests)
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
- [x] Formats de jeu supplémentaires : Cricket (saisie visite complète, scoring, victoire)
- [x] Format de jeu 301 (avec option Double Out / Straight Out)
- [ ] Gestion des sets en plus des legs
- [x] Statistiques en temps réel : moyenne, % doubles, checkout rate
- [ ] Historique : voir le détail d'un match terminé (toutes les volées)
- [ ] Vue joueur (match-play) branchée sur SignalR : synchronisation multi-écrans (deux appareils ouverts sur le même match divergent aujourd'hui)
- [x] Temps réel : SignalR pour refresh instantané du spectateur
- [x] Statistiques Cricket : calcul des stats spécifiques au Cricket (MPR, points marqués, meilleure visite)
- [x] Vue spectateur Cricket : affichage temps réel pour spectateurs (tableau des cibles + stats en direct)

**Amélioration interface de saisie :**
- [x] Pavé numérique visuel en mode score total (0-9, effacer, valider)
- [x] Tableau interactif : chiffres 1-20 + Bull, avec boutons Simple/Double/Triple
- [x] Bouton "il reste xxx" : saisir le score restant plutôt que le score fait
- [x] Bouton "Checkout" : valider directement un checkout quand le score le permet
- [x] Demander le nombre de fléchettes utilisées lors d'un checkout (1, 2 ou 3)
- [x] Demander le nombre de fléchettes tentées sur un double (pour stats précises)
- [x] Combiner les deux questions ci-dessus quand nécessaire (ex: "Checkout en 2 fléchettes, 1 tentée sur double")
- [ ] Interface Cricket : optimiser l'affichage pour tenir sans scroll (réduire hauteurs, espacements)

### Comptes Joueurs & Inscription
- [x] Lier un compte utilisateur à un profil joueur (User -> Player)
- [x] Interface de création de compte joueur (inscription publique)
- [x] Page profil joueur (édition de ses propres infos)
- [x] Auto-inscription aux tournois (pour les utilisateurs connectés avec profil joueur)
- [x] Gestion des inscriptions par l'admin (valider/refuser)

### Statistiques & Joueurs
- [x] Page de statistiques par joueur (moyenne, % victoires, historique)
- [x] Historique des confrontations directes entre joueurs
- [ ] Graphiques d'évolution des performances

### Circuit & Classement
- [ ] Système de circuit (regrouper plusieurs tournois)
- [ ] Classement global sur un circuit (points cumulés)
- [ ] Seeding automatique basé sur le classement circuit

### Fonctionnalités avancées
- [x] Support Cricket (visite complète avec validation, scoring, stats basiques)
- [x] Support format 301 (option Double Out / Straight Out, score de départ généralisé)
- [ ] Gestion des sets (en plus des legs)
- [ ] Mode équipes / doubles
- [ ] Export des résultats (PDF, CSV)
- [ ] Dashboard récapitulatif (stats globales, tournois récents)
