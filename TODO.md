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
  - ⚠️ Stats Cricket détaillées reportées post-MVP (fait depuis, voir « Gestion de Match »)

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
- [x] **Format 301** : score de départ généralisé, option Double Out / Straight Out

### ✅ TERMINÉ (Sessions 2026-06-24 → 2026-07-05)

- [x] **TV Lobby** : route /tv, écran d'accueil pour le bar (matchs en cours / à venir)
- [x] **Circuit de tournois** : barème de points configurable, classement global calculé à la demande
- [x] **Doubles / équipes** : paires par tournoi, alternance stricte A1→B1→A2→B2, tous formats + 501/301/Cricket
- [x] **Interclubs** : clubs, championnats, effectifs, calendrier des rencontres, validation des compositions, classement (+ tests)
- [x] Affichage du match Cricket amélioré

### 🔴 À FAIRE - Prochaine session

- [x] **Externaliser les credentials** (2026-10-13)
  - ✓ appsettings.json sans secrets, désormais versionné
  - ✓ Dev : dotnet user-secrets ; Prod : variables d'environnement (Jwt__Key, ConnectionStrings__DefaultConnection, CORS__Origins)
  - ✓ Démarrage refusé si secret absent / clé JWT < 32 octets / clé d'exemple hors dev
  - [ ] Branche `docker` : retirer les secrets par défaut de docker-compose.yml et vider appsettings.Production.json

- [ ] **Tester déploiement Docker** : Valider les fichiers Docker créés (sur la branche `docker`, pas encore mergée)
  - Build des images backend et frontend
  - Test du démarrage complet avec PostgreSQL
  - Vérifier les variables d'environnement
  - Tester l'accès via nginx

- [ ] **Tests manuels end-to-end complets** : Valider tous les formats de tournoi
  - Single Elimination : création → brackets → matchs → résultats
  - Double Elimination : brackets winners/losers → final
  - Round Robin : tous contre tous → classement final
  - Group Stage : phase de groupes → knockout → résultats
  - Cricket : match complet (plusieurs legs) avec vue spectateur en temps réel
  - Doubles : tournoi en paires, alternance des lanceurs
  - Circuit : plusieurs tournois → classement cumulé
  - Interclubs : championnat → rencontres → classement
  - TV Lobby : affichage sur l'écran du bar

## 🧭 Prochaines grosses fonctionnalités (analyse du 2026-10-19, plan validé ; A et B codées)

Ordre de réalisation : **A** (cadre) → **B** (s'appuie sur la saison) → **C** (indépendante, peut être décalée).

### A. Deux usages : mode « Bar » (one-shot) / mode « Ligue » (compétitions liées)
Bar : tournois isolés, tous formats et modes de jeu. Ligue : saison regroupant tournois, circuits, interclubs, exploits, toujours en 501 double out.
- [x] Modules activables par instance : section `Features` dans appsettings (surchargeable par env `Features__Interclubs=false`) (2026-10-20)
  - [x] Backend : endpoint `GET /api/config/features` + filtre `[RequiresFeature(Feature.X)]` (404 si désactivé, données conservées)
  - [x] Front : `FeatureService` (signal) chargé via `APP_INITIALIZER`, `featureGuard` sur les routes, `@if` dans la nav et les formulaires
  - Modules : Circuits, Interclubs, Doubles, Tv, Cricket, ThreeOhOne, OnlineRegistration, Seasons, Exploits
- [x] Entité `Season` (mode Ligue) : regroupe circuits, championnats interclubs, tournois ; porte le règlement (501 DO, legs par défaut) appliqué aux tournois/sessions rattachés (2026-10-20)
  - Statut En cours / Clôturée (clôturée = plus aucun rattachement), module `Seasons`
  - Tournoi de circuit = saison du circuit ; championnat de saison forcé en 501 DO (garde son nombre de legs)
  - Session refusée hors 501 DO pour un match de saison ; écran de config verrouillé (`SeasonRulesLocked`)
  - Pages /seasons et /seasons/:id (rattacher / détacher), choix de la saison à la création tournoi / circuit / championnat
  - [ ] Tests manuels (admin) : créer une saison, rattacher, clôturer, démarrer un match de saison
- [ ] Module `PublicScoring` (mode Bar) : bouton « Jouer » visible sans être admin (ni connecté) sur les matchs non terminés, pour que les joueurs saisissent eux-mêmes leurs scores (téléphone, tablette au pas de tir) (idée du 2026-11-03)
  - Aujourd'hui : endpoints de session déjà anonymes (choix assumé), seul le front réserve « Jouer » à l'admin (`tournament-detail`, `encounter-detail`)
  - Désactivé = comportement actuel ; la correction d'un match terminé reste admin
  - Lié à « restreindre l'écran de jeu aux joueurs du match » (Gestion de Match) : les deux options devront cohabiter
- Pas de multi-tenant (une instance Docker par client)
- ❓ En attente de réponse :
  - [x] A1. Flags en config de déploiement (appsettings/env), pas d'écran d'admin pour l'instant
  - [x] A2. Une instance par client, pas de multi-tenant

### B. Exploits (mode Ligue)
Exploit = manche en ≤ 15 fléchettes, checkout ≥ 100, 180, 171. Uniquement en 501 double out.
- [x] Modèle `Exploit` : MatchId, PlayerId, PartnerId? (manche en double), Type (ShortLeg / HighCheckout / OneEighty / OneSeventyOne), Value, LegNumber?, Source (Auto/Manual) ; migration AddExploits (2026-10-23)
  - Rattaché au match ; la saison est résolue au calcul (match → tournoi / circuit / championnat → saison)
- [x] Module `Exploits` (flag d'instance) au lieu d'un flag par saison : les exploits sont détectés sur tout match 501 DO, le classement ne prend que les matchs de la saison (rattacher un tournoi après coup suffit)
- [x] Détection auto : `ExploitDetector` (fonction pure) appelé dans `ValidateMatchAsync`, régénère les exploits Auto du match (idempotent)
- [x] Saisie manuelle (admin) : `GET/POST/DELETE /matches/{id}/exploits`, réservée aux matchs de saison (joueur du match, checkout 100-170 finissable, manche 9-15) ; dialogue `match-exploits-dialog`, bouton « Exploits » à côté de « Corriger » (tournoi + rencontre interclubs)
- [x] Recalcul : `POST /seasons/{id}/exploits/recompute` (bouton « Recalculer » sur la page saison) pour les matchs validés avant l'activation
- [x] Classements de saison `GET /seasons/{id}/exploits` (`ExploitRankingCalculator`), section « Exploits » de /seasons/:id
- [x] Interclubs : exploits aussi sur les matchs de rencontres
- [x] Tests unitaires du détecteur et du classement
- [ ] Tests manuels : jouer un match de saison avec un 180 / checkout 100+ / manche courte, vérifier le classement ; saisie manuelle sur un match en double
- [ ] Bonus : événement SignalR « 180 ! » sur spectateur / TV
- Réponses :
  - [x] B1. Checkout → finisseur ; manche ≤ 15 → la paire (fléchettes de la paire) ; 180/171 → le lanceur
  - [x] B2. 4 classements séparés : 180 et 171 au nombre, checkouts (meilleur) et manches (la plus courte) à la performance
  - [x] B3. Pas de départage : les ex-aequo partagent le rang

### C. Événements multi-tableaux + inscription en ligne simple/double
- [ ] Entité `Event` (nom, date, lieu, ouverture/clôture des inscriptions) + `Tournament.EventId` nullable ; chaque tableau reste un `Tournament` autonome (bracket, matchs inchangés), tournoi sans événement = comportement actuel
- [ ] `TournamentTeam.Status` (PendingPartner → Pending → Approved / Rejected), Approved par défaut pour les paires créées par l'admin et en interclubs
  - [ ] Filtrer les paires sur Approved partout où les joueurs le sont déjà (`MatchSideAccessor`, génération, `FinalPlacementCalculator`, stats)
- [ ] Endpoint transactionnel `POST /events/{id}/register` `{ singles: bool, doubles: { partnerId } }` (lever le refus actuel de l'auto-inscription en double)
- [ ] Vue admin « Inscriptions en attente » par événement (simples + paires)
- [ ] Profil : section « Mes inscriptions » (statut, désinscription) ; si un partenaire se désinscrit, la paire est annulée
- ❓ En attente de réponse :
  - [ ] C1. Le partenaire doit-il confirmer (exige un compte) ou l'inscription part directement à l'admin ? Statut « cherche un partenaire » ?
  - [ ] C2. Capacité max par tableau + liste d'attente ? Date limite d'inscription ?

## Priorité Haute - Sécurité / Intégrité

- [x] Ajouter validation des entrées sur les DTOs (password, username, noms)
- [x] Externaliser les credentials (BDD, JWT) hors de appsettings.json
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
- [x] Interface Cricket : optimiser l'affichage pour tenir sans scroll (mode compact du tableau, saisie sur une rangée, suppression du score en doublon)

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
- [x] Système de circuit (regrouper plusieurs tournois, barème de points configurable par circuit)
- [x] Classement global sur un circuit (points cumulés selon la place finale, calcul à la demande)
- [ ] Seeding automatique basé sur le classement circuit

### Fonctionnalités avancées
- [ ] Gestion des sets (en plus des legs)
- [x] Mode équipes / doubles (paires par tournoi, alternance stricte A1→B1→A2→B2, tous formats + 501/301/Cricket)
- [x] Interclubs : clubs, championnats (composition configurable X simples + Y doubles), calendrier aller-retour, effectifs déclarés, feuille de rencontre avec composition rapide (ordre des joueurs → confrontations déduites), classement V2/N1/D0
- [x] TV Lobby (écran /tv pour le bar)
- [ ] Export des résultats (PDF, CSV)
- [ ] Dashboard récapitulatif (stats globales, tournois récents)
