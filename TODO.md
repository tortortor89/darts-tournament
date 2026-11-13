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

Ordre de réalisation : **A** (cadre) → **B** (s'appuie sur la saison) → **C** (indépendante, peut être décalée). A, B, C et D codées.

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
- [x] Entité `Event` (nom, description, lieu, date, clôture des inscriptions, saison) + `Tournament.EventId` ; module `Events` ; pages /events et /events/:id (2026-11-13)
  - Saison de l'événement appliquée à ses tableaux hors circuit (à la création du tableau et en cas de changement)
  - Admin : créer / modifier l'événement, ajouter des tableaux (simple / double) depuis la page, choix de l'événement aussi dans le formulaire de création de tournoi
- [x] `TournamentTeam.Status` (Pending / Approved, refus = suppression), Approved par défaut (paires admin et interclubs, défaut SQL + sentinelle EF) ; paires en attente exclues de `MatchSideAccessor.GetSides` et `FinalPlacementCalculator`
- [x] `POST /events/{id}/register` `{ entries: [{ tournamentId, partnerId? }] }` tout ou rien ; clôture vérifiée aussi sur l'auto-inscription d'un tournoi d'événement
- [x] Validation admin : liste « Inscriptions en attente » sur la page de l'événement, et section « Paires en attente » sur la page du tournoi (`POST tournaments/{id}/teams/{teamId}/approve|reject`) ; l'ajout direct par l'admin reste possible
- [x] Profil : « Mes inscriptions » (`GET players/me/registrations`, tournois non terminés, avec ou sans événement) ; désinscription d'une paire = paire annulée
- [ ] Tests manuels : créer un événement (simple + double) rattaché à une saison, s'inscrire en joueur (simple + double avec partenaire), valider / refuser, générer, clôture dépassée
- ❓ En attente de réponse :
  - [x] C1. Pas de confirmation du partenaire : le joueur choisit son partenaire dans la liste, la paire part en attente chez l'admin (le partenaire n'a pas besoin de compte). Pas de statut « cherche un partenaire » pour l'instant (2026-11-13)
  - [x] C2. Date de clôture des inscriptions seule, pas de capacité max ni liste d'attente (2026-11-13)
  - [x] C3. Un événement peut être rattaché à une saison, appliquée à ses tableaux (2026-11-13)

### D. Formats de tableaux optimisés selon le nombre d'inscrits (mode Ligue) (idée du 2026-11-09)
Format proposé à l'utilisateur : la structure du tableau dépend du nombre d'inscrits (détails à récupérer).
- [x] Étape 1 : grille transcrite (2026-11-11) : `tools/fsgt/extract_fsgt_formats.py` → `backend/DartsTournament.Api/Data/fsgt-formats.json` (2-80 joueurs, variantes sans barrage, `default` modifiable à la main, barème inclus) ; corrections de saisie (43, 46) dans `CELL_OVERRIDES` d'après les classeurs détaillés ; recoupée avec les onglets Tableau des classeurs « A N inscrits »
- [x] Étape 2 (2026-11-13) : `FsgtFormatCatalog` (JSON chargé et validé au démarrage, singleton) + `FsgtDrawCalculator` (serpentin sur poules de tailles imposées, affiches « 1p1 ») ; `Tournament.UseFsgtFormat` (défaut : tournoi en poules de saison, propre ou via le circuit ; contournable à la création), qualifiés par poule (`Group.QualifierCount`) et affiches (`Tournament.KnockoutPairings`) figés à la génération ; 2-5 inscrits = poule unique sans phase finale ; migration AddFsgtFormat
  - Reste à tester à la main (création en saison, génération 10 / 24 / 46 inscrits, passage en phase finale)
- [x] Étape 3 (2026-11-13) : `GroupRankingCalculator` (critères ordonnés, confrontation directe en mini-championnat ré-appliqué aux sous-blocs) ; critères dans `tieBreakers` de `fsgt-formats.json` (`wins`, `legDifference`, `headToHead`, `legsWon` dispo) ; égalité parfaite = rang partagé « 2= » une fois la poule terminée ; phase finale bloquée si l'égalité touche une place qualificative ; l'admin saisit l'ordre (▲▼ sous la poule, `PUT tournaments/{id}/groups/{groupId}/tiebreak`, `Group.TieBreakOrder`) puis la phase finale est générée ; migration AddGroupTieBreakOrder. Hors FSGT : comportement inchangé
  - [ ] Règle exacte après confrontation directe à confirmer (manches gagnées ?) : pour l'instant non ; il suffit d'ajouter `"legsWon"` dans `tieBreakers` (et `TIE_BREAKERS` du script)
- [x] Étape 4 (2026-11-13) : `Circuit.UseFsgtPoints` (défaut : circuit de saison ; case à la création, modifiable dans « Modifier le barème ») ; `FsgtPointsCalculator` : tour atteint déduit de la place finale (1 / 2 / 3-4 / 5-8 / 9-16 / 17-32), ligne selon le nombre de côtés, éliminés en poules = 2 + 2/victoire en poule ; tour absent de la ligne (format contourné) = points de poule ; hors grille = ligne la plus proche ; barème lu dans `points` de `fsgt-formats.json` ; migration AddCircuitFsgtPoints
- [ ] Étape 5 : tests manuels de bout en bout (circuit de saison, 2-3 tournois FSGT, classement)
- Réponses :
  - [x] D1. Document complet fourni par l'utilisateur, un format par nombre de joueurs de 5 à 80 (à transcrire)
  - [x] D2. Format optimisé appliqué par défaut aux tournois de saison, l'admin peut le contourner si besoin
  - [x] D3. Format déterminé à la génération du tableau (nombre d'inscrits définitif)
  - [x] D4. Grille fixe pour toute l'instance (pas par saison) dans un premier temps
- Analyse du document (2026-11-11) : `docs-fsgt-flechettes/2 Répartition des Joueurs par poules.xlsx` (onglets Feuil1 5-80, Extention 81-100, Point Tournois) + un classeur détaillé par nombre d'inscrits (`A N inscrits...xlsx` : tirage au sort, poules, tableau, feuille de route)
  - Poules de tailles mélangées (3 à 6) avec numéros de poule précisés
  - Nombre de qualifiés par poule variable selon la taille (ex. 10 joueurs : 2 poules de 3 → 2Q, 1 poule de 4 → 3Q)
  - Barrages « prélim » : 1 x 701 entre les 3es (ou 4es) de certaines poules
  - Tableau final avec affiches imposées (1 p 1 contre 2 p 2...), demi / 1/4 / 8es / 16es / 32es
  - Plusieurs variantes pour un même nombre de joueurs, selon le nombre de cibles (8, 8 à 15, 16)
  - Barème de points par tour atteint selon le nombre de joueurs (onglet Point Tournois)
  - Écarts avec le générateur actuel : `QualifiersPerGroup` unique, qualifiés replacés par seeding générique (`ArrangeSidesForKnockout`), pas de barrage, pas de match à 3 joueurs
- Réponses :
  - [x] D5. Une seule variante par nombre de joueurs pour commencer ; choix parmi plusieurs options plus tard
  - [x] D6. Privilégier les variantes sans barrage 701 (il en existe toujours une)
  - [x] D7. Barème « Point Tournois » pour le classement du circuit quand le tournoi est rattaché à un circuit
  - [x] D8. Pas d'extension (5-80 seulement) ; s'applique aux doubles en comptant les paires (10 paires → grille à 10)
- Règle de choix de la variante : celle dont la sortie de poules correspond à l'onglet Point Tournois (6-10 demies, 11-20 quarts, 21-40 8es, 41-80 16es) et sans barrage
  - Conflit pour 41 et 42 : la variante 16es a un barrage, celle sans barrage sort en 8es
- Barème actuel du circuit : par rang final (`CircuitPointsRule` MinRank-MaxRank) + `ParticipationPoints` ; le barème FSGT est par tour atteint et dépend du nombre de joueurs
  - [x] D9. 41 et 42 : sortie en 8es (sans barrage), mais la grille doit rester facile à ajuster → fichier de données versionné, pas de code en dur
  - [x] D10. Valeurs = tours atteints (16es → vainqueur) ; probable erreur sur la ligne « demies » (4 valeurs pour 3 tours) ; éliminés en poules = 2 pts de participation + 2 pts par match gagné en poule
  - [x] D11. Option « barème FSGT » sur le circuit (le barème personnalisable reste pour les circuits de bar)
- [ ] Départage manuel des égalités en poule : aujourd'hui `GetGroupStageStandings` trie points > diff > marqués et départage arbitrairement les ex-aequo parfaits ; il faut détecter l'égalité, bloquer la génération du tableau final (`CheckAndGenerateKnockoutAsync` est automatique) et laisser l'admin saisir l'ordre (résultat du 701 à plusieurs joué hors appli)
  - [x] D12. Sortie en demies : demi 6 / finale 8 / vainqueur 10 ; barème dans le même fichier de données, donc modifiable
  - [x] D13. Qualifiés : uniquement les points du tour atteint (ils incluent la participation)
  - [x] D14. Poule unique (2-5) : 2 pts + 2 par victoire pour tous
  - [x] D15. Départage en poule : victoires > différence de manches > confrontation directe > départage manuel (701 hors appli). Classement du circuit : on garde les ex-aequo au même rang (comportement actuel)

### E. Joueurs de la saison + pointage par tournoi (mode Ligue) (idée du 2026-11-13)
Une liste de joueurs inscrits à la saison ; pour chaque tournoi de la saison, une page où l'admin coche simplement qui participe (au lieu d'ajouter les joueurs un par un).
- Aujourd'hui : `Season` n'a pas de liste de joueurs, l'ajout au tournoi se fait joueur par joueur (`TournamentPlayer`)
- Questions :
  - [ ] E1. Liste de la saison remplie à la main par l'admin, ou aussi depuis les joueurs ayant déjà joué / l'inscription en ligne ? Infos en plus (n° de licence, club) ?
  - [ ] E2. Un joueur hors liste peut-il participer à un tournoi de saison (invité) ? Si oui, est-il ajouté automatiquement à la liste ?
  - [ ] E3. Tournois en double : on coche les joueurs puis on forme les paires sur la même page, ou on coche des paires ?
  - [ ] E4. Seeding des joueurs cochés : aucun (ordre de pointage / tirage), ou d'après un classement de la saison ?

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
