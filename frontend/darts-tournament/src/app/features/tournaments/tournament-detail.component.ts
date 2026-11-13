import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { FeatureService } from '../../core/services/feature.service';
import { TournamentDetail, TournamentTeam, Player, TournamentFormat, TournamentStatus, MatchStatus, Match, GroupStanding, BracketType, RegistrationStatus } from '../../core/models';
import { BracketViewerComponent } from '../../shared/components/bracket-viewer/bracket-viewer.component';
import { DoubleBracketViewerComponent } from '../../shared/components/double-bracket-viewer/double-bracket-viewer.component';
import { MatchExploitsDialogComponent } from '../matches/components/match-exploits-dialog.component';

@Component({
  selector: 'app-tournament-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, BracketViewerComponent, DoubleBracketViewerComponent, MatchExploitsDialogComponent],
  template: `
    @if (loading) {
      <div class="container">
        <div class="loading">Chargement...</div>
      </div>
    }
    <div class="container" *ngIf="tournament">
      <h2>{{ tournament.name }}</h2>
      <div class="info">
        <p>Format: {{ getFormatLabel(tournament.format) }}</p>
        <p>Status: {{ getStatusLabel(tournament.status) }}</p>
        @if (tournament.circuitName && features.isEnabled('circuits')) {
          <p>Circuit: <a [routerLink]="['/circuits', tournament.circuitId]">{{ tournament.circuitName }}</a></p>
        }
        @if (tournament.eventName && features.isEnabled('events')) {
          <p>Événement: <a [routerLink]="['/events', tournament.eventId]">{{ tournament.eventName }}</a></p>
        }
        @if (tournament.seasonName && features.isEnabled('seasons')) {
          <p>Saison: <a [routerLink]="['/seasons', tournament.seasonId]">{{ tournament.seasonName }}</a> (501 double out)</p>
        }
        @if (tournament.format === TournamentFormat.GroupStage && tournament.useFsgtFormat) {
          <p>Format FSGT : poules et tableau final selon le nombre d'inscrits</p>
        } @else if (tournament.format === TournamentFormat.GroupStage) {
          <p>
            {{ tournament.numberOfGroups || 'Auto' }} groupes,
            {{ tournament.qualifiersPerGroup || 2 }} qualifiés/groupe
            @if (tournament.hasKnockoutPhase) { + phase éliminatoire }
          </p>
        }
      </div>

      <!-- Self-registration section (for authenticated users with player profile) -->
      <!-- Masquée en double : les paires sont composées par l'administrateur -->
      @if (tournament.status === TournamentStatus.Draft && authService.isAuthenticated() && !authService.isAdmin() && !tournament.isDoubles && features.isEnabled('onlineRegistration')) {
        <div class="self-registration-section">
          @if (authService.hasLinkedPlayer()) {
            @if (isUserRegistered()) {
              @let userPlayer = tournament.players.find(p => p.playerId === authService.linkedPlayerId());
              @if (userPlayer) {
                @if (userPlayer.status === RegistrationStatus.Pending) {
                  <div class="status-pending">
                    <p>⏳ Votre inscription est en attente d'approbation par l'administrateur</p>
                  </div>
                  <button class="btn-unregister" (click)="unregisterFromTournament()">
                    Annuler mon inscription
                  </button>
                } @else if (userPlayer.status === RegistrationStatus.Approved) {
                  <div class="status-approved">
                    <p>✓ Votre inscription a été approuvée</p>
                  </div>
                  <button class="btn-unregister" (click)="unregisterFromTournament()">
                    Se désinscrire du tournoi
                  </button>
                }
              }
            } @else {
              <button class="btn-register" (click)="registerToTournament()">
                S'inscrire au tournoi
              </button>
            }
          } @else {
            <div class="warning">
              <p>Vous devez avoir un profil joueur pour vous inscrire.</p>
              <a routerLink="/profile">Créer ou lier mon profil joueur</a>
            </div>
          }
        </div>
      }

      <!-- Players/teams list visible to everyone in Draft status -->
      @if (tournament.status === TournamentStatus.Draft) {
        <div class="players-section">
          @if (tournament.isDoubles) {
            <h3>Paires inscrites ({{ approvedTeams().length }})</h3>
            @if (!authService.isAdmin()) {
              <ul class="player-list">
                @for (team of tournament.teams; track team.id) {
                  <li>
                    {{ team.name }}
                    @if (team.status === RegistrationStatus.Pending) { <em>(en attente)</em> }
                  </li>
                }
              </ul>
            }
          } @else {
            <h3>Joueurs inscrits ({{ tournament.players.length }})</h3>
            @if (!authService.isAdmin()) {
              <ul class="player-list">
                @for (player of tournament.players; track player.playerId) {
                  <li>
                    {{ player.firstName }} {{ player.lastName }}
                    @if (player.nickname) { ({{ player.nickname }}) }
                  </li>
                }
              </ul>
            }
          }
        </div>
      }

      <!-- Admin : gestion des paires (tournoi en double) -->
      @if (tournament.status === TournamentStatus.Draft && authService.isAdmin() && tournament.isDoubles) {
        <div class="players-section">
          <h3>Gestion des paires (Admin)</h3>

          <div class="add-player">
            <h4>Ajouter une paire</h4>
            <select [(ngModel)]="selectedTeamPlayer1Id">
              <option value="">Joueur 1</option>
              @for (player of getUnpairedPlayers(); track player.id) {
                <option [value]="player.id">{{ player.firstName }} {{ player.lastName }}</option>
              }
            </select>
            <select [(ngModel)]="selectedTeamPlayer2Id">
              <option value="">Joueur 2</option>
              @for (player of getUnpairedPlayers(); track player.id) {
                @if (player.id !== Number(selectedTeamPlayer1Id)) {
                  <option [value]="player.id">{{ player.firstName }} {{ player.lastName }}</option>
                }
              }
            </select>
            <input type="number" [(ngModel)]="selectedTeamSeed" placeholder="Seed (optionnel)">
            <button (click)="addTeam()" [disabled]="!selectedTeamPlayer1Id || !selectedTeamPlayer2Id">Ajouter la paire</button>
          </div>

          @if (pendingTeams().length > 0) {
            <div class="pending-section">
              <h4>Paires en attente de validation ({{ pendingTeams().length }})</h4>
              <ul class="player-list">
                @for (team of pendingTeams(); track team.id) {
                  <li>
                    <span>{{ team.name }}</span>
                    <div class="registration-actions">
                      <button (click)="approveTeam(team.id)" class="approve">Valider</button>
                      <button (click)="rejectTeam(team.id)" class="reject">Refuser</button>
                    </div>
                  </li>
                }
              </ul>
            </div>
          }

          <div class="approved-section">
            <h4>Paires ({{ approvedTeams().length }})</h4>
            <ul class="player-list">
              @for (team of approvedTeams(); track team.id) {
                <li>
                  <span>
                    {{ team.name }}
                    @if (team.seed) { - Seed: {{ team.seed }} }
                  </span>
                  <button (click)="removeTeam(team.id)" class="remove">X</button>
                </li>
              }
            </ul>
          </div>

          @if (approvedTeams().length >= 2) {
            <button (click)="generateBracket()" class="generate">Générer le bracket ({{ approvedTeams().length }} paires)</button>
          } @else {
            <p class="info-text">Au moins 2 paires sont nécessaires pour générer le bracket</p>
          }
        </div>
      }

      @if (tournament.status === TournamentStatus.Draft && authService.isAdmin() && !tournament.isDoubles) {
        <div class="players-section">
          <h3>Gestion des joueurs (Admin)</h3>

          <!-- Pending registrations -->
          @if (getPendingRegistrations().length > 0) {
            <div class="pending-section">
              <h4>Inscriptions en attente ({{ getPendingRegistrations().length }})</h4>
              <ul class="player-list pending">
                @for (player of getPendingRegistrations(); track player.playerId) {
                  <li>
                    <span>
                      {{ player.firstName }} {{ player.lastName }}
                      @if (player.nickname) { ({{ player.nickname }}) }
                    </span>
                    <div class="registration-actions">
                      <button (click)="approveRegistration(player.playerId)" class="approve">✓ Approuver</button>
                      <button (click)="rejectRegistration(player.playerId)" class="reject">✗ Refuser</button>
                    </div>
                  </li>
                }
              </ul>
            </div>
          }

          <!-- Add player manually -->
          <div class="add-player">
            <h4>Ajouter un joueur manuellement</h4>
            <select [(ngModel)]="selectedPlayerId">
              <option value="">Sélectionner un joueur</option>
              @for (player of availablePlayers; track player.id) {
                <option [value]="player.id">{{ player.firstName }} {{ player.lastName }}</option>
              }
            </select>
            <input type="number" [(ngModel)]="selectedSeed" placeholder="Seed (optionnel)">
            <button (click)="addPlayer()" [disabled]="!selectedPlayerId">Ajouter</button>
          </div>

          <!-- Approved players -->
          <div class="approved-section">
            <h4>Joueurs approuvés ({{ getApprovedRegistrations().length }})</h4>
            <ul class="player-list">
              @for (player of getApprovedRegistrations(); track player.playerId) {
                <li>
                  <span>
                    {{ player.firstName }} {{ player.lastName }}
                    @if (player.nickname) { ({{ player.nickname }}) }
                    @if (player.seed) { - Seed: {{ player.seed }} }
                  </span>
                  <button (click)="removePlayer(player.playerId)" class="remove">X</button>
                </li>
              }
            </ul>
          </div>

          @if (getApprovedRegistrations().length >= 2) {
            <button (click)="generateBracket()" class="generate">Générer le bracket ({{ getApprovedRegistrations().length }} joueurs)</button>
          } @else {
            <p class="info-text">Au moins 2 joueurs approuvés sont nécessaires pour générer le bracket</p>
          }
        </div>
      }

      @if (tournament.status !== TournamentStatus.Draft) {
        @if (tournament.format === TournamentFormat.GroupStage) {
          <!-- Standings Tables -->
          @if (standings.length > 0) {
            <div class="standings-section">
              <h3>Classements</h3>
              <div class="standings-grid">
                @for (group of standings; track group.groupId) {
                  <div class="group-standings">
                    <h4>{{ group.groupName }}</h4>
                    <table>
                      <thead>
                        <tr>
                          <th>#</th>
                          <th>Joueur</th>
                          <th>J</th>
                          <th>V</th>
                          <th>D</th>
                          <th>+/-</th>
                          <th>Pts</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (player of group.standings; track player.playerId) {
                          <tr [class.qualified]="player.rank <= qualifiersFor(group.groupId)">
                            <td>{{ player.rank }}{{ player.tieUnresolved ? '=' : '' }}</td>
                            <td>{{ player.playerName }}</td>
                            <td>{{ player.played }}</td>
                            <td>{{ player.won }}</td>
                            <td>{{ player.lost }}</td>
                            <td>{{ player.pointsDiff > 0 ? '+' : '' }}{{ player.pointsDiff }}</td>
                            <td><strong>{{ player.points }}</strong></td>
                          </tr>
                        }
                      </tbody>
                    </table>
                    <!-- Égalité parfaite sur une place qualificative : la phase finale attend le départage -->
                    @for (block of blockingTies(group); track block.key) {
                      <div class="tie-break">
                        <p>
                          Égalité parfaite à départager (victoires, différence de manches, confrontation directe) :
                          la phase finale sera générée une fois l'ordre saisi.
                        </p>
                        @if (authService.isAdmin() && !hasKnockoutMatches()) {
                          <ol>
                            @for (sideId of tieOrder(block); track sideId; let i = $index, last = $last) {
                              <li>
                                <span>{{ standingName(group, sideId) }}</span>
                                <button type="button" class="tie-move" [disabled]="i === 0" (click)="moveTie(block, i, -1)" title="Monter">▲</button>
                                <button type="button" class="tie-move" [disabled]="last" (click)="moveTie(block, i, 1)" title="Descendre">▼</button>
                              </li>
                            }
                          </ol>
                          <button type="button" (click)="saveTieBreak(group.groupId, block)">Valider l'ordre</button>
                        }
                      </div>
                    }
                  </div>
                }
              </div>
            </div>
          }

          <!-- Group Matches -->
          <div class="matches-section">
            <h3>Matchs de groupes</h3>
            @for (group of tournament.groups; track group.id) {
              <div class="group-matches">
                <h4>{{ group.name }}</h4>
                <div class="matches">
                  @for (match of getGroupMatches(group.id); track match.id) {
                    <div class="match" [class.completed]="match.status === MatchStatus.Completed">
                      <div class="players">
                        <span [class.winner]="match.winnerId === match.player1Id">
                          {{ match.player1Name || 'TBD' }}
                          @if (match.player1Score !== null) { ({{ match.player1Score }}) }
                        </span>
                        <span class="vs">vs</span>
                        <span [class.winner]="match.winnerId === match.player2Id">
                          {{ match.player2Name || 'TBD' }}
                          @if (match.player2Score !== null) { ({{ match.player2Score }}) }
                        </span>
                      </div>
                      @if (authService.isAdmin() && match.status !== MatchStatus.Completed && match.player1Id && match.player2Id) {
                        <div class="match-actions">
                          <a [routerLink]="['/matches', match.id, 'play']" class="play-btn">Jouer</a>
                          <div class="score-input">
                            <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                            <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                            <button (click)="updateScore(match)">Valider</button>
                          </div>
                        </div>
                      }
                      @if (authService.isAdmin() && match.status === MatchStatus.Completed && match.player1Id && match.player2Id) {
                        <div class="match-actions">
                          @if (correctingMatchId === match.id) {
                            <div class="score-input">
                              <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                              <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                              <button (click)="updateScore(match)">Valider</button>
                              <button class="cancel-correct" (click)="correctingMatchId = null">Annuler</button>
                            </div>
                          } @else {
                            <button class="correct-btn" (click)="startCorrection(match)">Corriger</button>
                            @if (canTrackExploits) {
                              <button class="exploits-btn" (click)="exploitsMatchId = match.id">Exploits</button>
                            }
                          }
                        </div>
                      }
                      @if (match.player1Id && match.player2Id && match.status !== MatchStatus.Completed) {
                        <a [routerLink]="['/matches', match.id, 'spectate']" class="spectate-btn">Spectateur</a>
                      }
                    </div>
                  }
                </div>
              </div>
            }
          </div>

          <!-- Knockout Phase -->
          @if (hasKnockoutMatches()) {
            <div class="knockout-section">
              <h3>Phase éliminatoire</h3>
              <app-bracket-viewer [tournament]="tournament" [knockoutOnly]="true"></app-bracket-viewer>

              <!-- Fallback match input for knockout -->
              <div class="knockout-matches">
                @for (round of getKnockoutRounds(); track round) {
                  <div class="round">
                    <h4>{{ getKnockoutRoundName(round) }}</h4>
                    <div class="matches">
                      @for (match of getKnockoutMatchesByRound(round); track match.id) {
                        <div class="match" [class.completed]="match.status === MatchStatus.Completed">
                          <div class="players">
                            <span [class.winner]="match.winnerId === match.player1Id">
                              {{ match.player1Name || 'TBD' }}
                              @if (match.player1Score !== null) { ({{ match.player1Score }}) }
                            </span>
                            <span class="vs">vs</span>
                            <span [class.winner]="match.winnerId === match.player2Id">
                              {{ match.player2Name || 'TBD' }}
                              @if (match.player2Score !== null) { ({{ match.player2Score }}) }
                            </span>
                          </div>
                          @if (authService.isAdmin() && match.status !== MatchStatus.Completed && match.player1Id && match.player2Id) {
                            <div class="match-actions">
                              <a [routerLink]="['/matches', match.id, 'play']" class="play-btn">Jouer</a>
                              <div class="score-input">
                                <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                                <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                                <button (click)="updateScore(match)">Valider</button>
                              </div>
                            </div>
                          }
                          @if (authService.isAdmin() && match.status === MatchStatus.Completed && match.player1Id && match.player2Id) {
                            <div class="match-actions">
                              @if (correctingMatchId === match.id) {
                                <div class="score-input">
                                  <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                                  <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                                  <button (click)="updateScore(match)">Valider</button>
                                  <button class="cancel-correct" (click)="correctingMatchId = null">Annuler</button>
                                </div>
                              } @else {
                                <button class="correct-btn" (click)="startCorrection(match)">Corriger</button>
                                @if (canTrackExploits) {
                                  <button class="exploits-btn" (click)="exploitsMatchId = match.id">Exploits</button>
                                }
                              }
                            </div>
                          }
                          @if (match.player1Id && match.player2Id && match.status !== MatchStatus.Completed) {
                            <a [routerLink]="['/matches', match.id, 'spectate']" class="spectate-btn">Spectateur</a>
                          }
                        </div>
                      }
                    </div>
                  </div>
                }
              </div>
            </div>
          }
        } @else if (tournament.format === TournamentFormat.SingleElimination) {
          <!-- Single Elimination -->
          <div class="bracket-section">
            <h3>Tableau</h3>
            <app-bracket-viewer [tournament]="tournament"></app-bracket-viewer>

            <!-- Match details by round -->
            <div class="bracket-matches">
              @for (round of getRounds(); track round) {
                <div class="round">
                  <h4>{{ getSingleElimRoundName(round) }}</h4>
                  <div class="matches">
                    @for (match of getMatchesByRound(round); track match.id) {
                      <div class="match" [class.completed]="match.status === MatchStatus.Completed">
                        <div class="players">
                          <span [class.winner]="match.winnerId === match.player1Id">
                            {{ match.player1Name || 'TBD' }}
                            @if (match.player1Score !== null) { ({{ match.player1Score }}) }
                          </span>
                          <span class="vs">vs</span>
                          <span [class.winner]="match.winnerId === match.player2Id">
                            {{ match.player2Name || 'TBD' }}
                            @if (match.player2Score !== null) { ({{ match.player2Score }}) }
                          </span>
                        </div>
                        @if (authService.isAdmin() && match.status !== MatchStatus.Completed && match.player1Id && match.player2Id) {
                          <div class="match-actions">
                            <a [routerLink]="['/matches', match.id, 'play']" class="play-btn">Jouer</a>
                            <div class="score-input">
                              <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                              <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                              <button (click)="updateScore(match)">Valider</button>
                            </div>
                          </div>
                        }
                        @if (authService.isAdmin() && match.status === MatchStatus.Completed && match.player1Id && match.player2Id) {
                          <div class="match-actions">
                            @if (correctingMatchId === match.id) {
                              <div class="score-input">
                                <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                                <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                                <button (click)="updateScore(match)">Valider</button>
                                <button class="cancel-correct" (click)="correctingMatchId = null">Annuler</button>
                              </div>
                            } @else {
                              <button class="correct-btn" (click)="startCorrection(match)">Corriger</button>
                              @if (canTrackExploits) {
                                <button class="exploits-btn" (click)="exploitsMatchId = match.id">Exploits</button>
                              }
                            }
                          </div>
                        }
                        @if (match.player1Id && match.player2Id && match.status !== MatchStatus.Completed) {
                          <a [routerLink]="['/matches', match.id, 'spectate']" class="spectate-btn">Spectateur</a>
                        }
                      </div>
                    }
                  </div>
                </div>
              }
            </div>
          </div>
        } @else if (tournament.format === TournamentFormat.DoubleElimination) {
          <!-- Double Elimination -->
          <div class="double-elim-section">
            <h3>Tableau Double Élimination</h3>
            <app-double-bracket-viewer [tournament]="tournament"></app-double-bracket-viewer>

            <!-- Standings for Double Elimination (only when completed) -->
            @if (tournament.status === TournamentStatus.Completed && standings.length > 0 && standings[0].standings.length > 0) {
              <div class="standings-section de-standings">
                <h3>Classement</h3>
                <div class="de-standings-table">
                  <table>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Joueur</th>
                        <th>J</th>
                        <th>V</th>
                        <th>D</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (player of standings[0].standings; track player.playerId) {
                        <tr [class.top-3]="player.rank <= 3">
                          <td class="rank">
                            @if (player.rank === 1) { 🥇 }
                            @else if (player.rank === 2) { 🥈 }
                            @else if (player.rank === 3) { 🥉 }
                            @else { {{ player.rank }} }
                          </td>
                          <td>{{ player.playerName }}</td>
                          <td>{{ player.played }}</td>
                          <td>{{ player.won }}</td>
                          <td>{{ player.lost }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              </div>
            }

            <!-- Match input for Double Elimination -->
            <div class="double-elim-matches">
              @for (match of getDoubleElimMatches(); track match.id) {
                @if (match.player1Id && match.player2Id && match.status !== MatchStatus.Completed) {
                  <div class="match-input-card" [class.winners]="match.bracketType === BracketType.Winners" [class.losers]="match.bracketType === BracketType.Losers" [class.grand-final]="match.bracketType === BracketType.GrandFinal">
                    <div class="match-info">
                      <span class="bracket-label">{{ getBracketLabel(match) }}</span>
                      <div class="players">
                        <span>{{ match.player1Name }}</span>
                        <span class="vs">vs</span>
                        <span>{{ match.player2Name }}</span>
                      </div>
                    </div>
                    @if (authService.isAdmin()) {
                      <div class="match-actions">
                        <a [routerLink]="['/matches', match.id, 'play']" class="play-btn">Jouer</a>
                        <div class="score-input">
                          <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                          <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                          <button (click)="updateScore(match)">Valider</button>
                        </div>
                      </div>
                    }
                    <a [routerLink]="['/matches', match.id, 'spectate']" class="spectate-btn">Spectateur</a>
                  </div>
                }
              }
            </div>
          </div>
        } @else {
          <!-- Round Robin -->
          <div class="roundrobin-section">
            <!-- Standings Table -->
            @if (standings.length > 0) {
              <div class="standings-section">
                <h3>Classement</h3>
                <div class="roundrobin-standings">
                  <table>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Joueur</th>
                        <th>J</th>
                        <th>V</th>
                        <th>D</th>
                        <th>+/-</th>
                        <th>Pts</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (player of standings[0].standings; track player.playerId) {
                        <tr [class.top-3]="player.rank <= 3">
                          <td class="rank">
                            @if (player.rank === 1) { 🥇 }
                            @else if (player.rank === 2) { 🥈 }
                            @else if (player.rank === 3) { 🥉 }
                            @else { {{ player.rank }} }
                          </td>
                          <td>{{ player.playerName }}</td>
                          <td>{{ player.played }}</td>
                          <td>{{ player.won }}</td>
                          <td>{{ player.lost }}</td>
                          <td>{{ player.pointsDiff > 0 ? '+' : '' }}{{ player.pointsDiff }}</td>
                          <td><strong>{{ player.points }}</strong></td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              </div>
            }

            <!-- Matches -->
            <div class="matches-section">
              <h3>Matchs</h3>
              <div class="matches-grid">
                @for (match of tournament.matches; track match.id) {
                  <div class="match" [class.completed]="match.status === MatchStatus.Completed">
                    <div class="players">
                      <span [class.winner]="match.winnerId === match.player1Id">
                        {{ match.player1Name || 'TBD' }}
                        @if (match.player1Score !== null) { ({{ match.player1Score }}) }
                      </span>
                      <span class="vs">vs</span>
                      <span [class.winner]="match.winnerId === match.player2Id">
                        {{ match.player2Name || 'TBD' }}
                        @if (match.player2Score !== null) { ({{ match.player2Score }}) }
                      </span>
                    </div>
                    @if (authService.isAdmin() && match.status !== MatchStatus.Completed && match.player1Id && match.player2Id) {
                      <div class="match-actions">
                        <a [routerLink]="['/matches', match.id, 'play']" class="play-btn">Jouer</a>
                        <div class="score-input">
                          <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                          <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                          <button (click)="updateScore(match)">Valider</button>
                        </div>
                      </div>
                    }
                    @if (authService.isAdmin() && match.status === MatchStatus.Completed && match.player1Id && match.player2Id) {
                      <div class="match-actions">
                        @if (correctingMatchId === match.id) {
                          <div class="score-input">
                            <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                            <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                            <button (click)="updateScore(match)">Valider</button>
                            <button class="cancel-correct" (click)="correctingMatchId = null">Annuler</button>
                          </div>
                        } @else {
                          <button class="correct-btn" (click)="startCorrection(match)">Corriger</button>
                          @if (canTrackExploits) {
                            <button class="exploits-btn" (click)="exploitsMatchId = match.id">Exploits</button>
                          }
                        }
                      </div>
                    }
                    @if (match.player1Id && match.player2Id && match.status !== MatchStatus.Completed) {
                      <a [routerLink]="['/matches', match.id, 'spectate']" class="spectate-btn">Spectateur</a>
                    }
                  </div>
                }
              </div>
            </div>
          </div>
        }
      }

      @if (exploitsMatchId !== null) {
        <app-match-exploits-dialog [matchId]="exploitsMatchId" (closed)="exploitsMatchId = null" />
      }
    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 20px auto;
      padding: 20px;
    }
    .info {
      margin-bottom: 20px;
      padding: 12px 16px;
      background: white;
      border: 1px solid var(--border);
      border-left: 4px solid var(--primary);
      border-radius: 6px;
      color: var(--text-muted);
    }
    .info p { margin: 4px 0; }
    .players-section, .matches-section, .standings-section, .knockout-section {
      margin-bottom: 30px;
    }
    .add-player {
      display: flex;
      gap: 10px;
      margin-bottom: 15px;
    }
    .add-player select, .add-player input {
      padding: 8px 10px;
      border: 1.5px solid var(--border);
      border-radius: 4px;
      background: var(--input-bg);
      color: var(--text);
    }
    .add-player button {
      padding: 8px 16px;
      background: var(--primary);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .add-player button:hover {
      background: var(--primary-hover);
    }
    .player-list {
      list-style: none;
      padding: 0;
      background: white;
      border: 1px solid var(--border);
      border-radius: 6px;
      overflow: hidden;
    }
    .player-list li {
      padding: 10px 12px;
      border-bottom: 1px solid var(--border-light);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .player-list li:last-child { border-bottom: none; }
    .remove {
      background: var(--danger);
      color: white;
      border: none;
      padding: 3px 10px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.85em;
    }
    .remove:hover { background: var(--danger-hover); }
    .generate {
      padding: 10px 24px;
      background: var(--action);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 1em;
      font-weight: 600;
      letter-spacing: 0.02em;
    }
    .generate:hover { background: var(--action-hover); }

    /* Self-registration section */
    .self-registration-section {
      margin-bottom: 20px;
      padding: 24px;
      background: var(--hero-gradient);
      border-radius: 8px;
      text-align: center;
      color: white;
    }
    .btn-register {
      padding: 12px 30px;
      background: var(--cta);
      color: white;
      border: none;
      border-radius: 4px;
      font-size: 1.1em;
      font-weight: 700;
      cursor: pointer;
      transition: all 0.2s;
      letter-spacing: 0.03em;
    }
    .btn-register:hover {
      background: var(--cta-hover);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0,0,0,0.2);
    }
    .btn-unregister {
      padding: 10px 20px;
      background: var(--danger);
      color: white;
      border: none;
      border-radius: 4px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }
    .btn-unregister:hover {
      background: var(--danger-hover);
      transform: translateY(-1px);
    }
    .self-registration-section .info-text {
      margin-top: 10px;
      font-size: 0.9em;
      opacity: 0.85;
    }
    .self-registration-section .warning {
      background: rgba(255,255,255,0.1);
      padding: 15px;
      border-radius: 4px;
      border-left: 4px solid var(--accent);
    }
    .self-registration-section .warning p { margin: 0 0 10px 0; }
    .self-registration-section .warning a {
      color: var(--hero-accent);
      font-weight: 600;
    }

    /* Standings */
    .standings-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
      gap: 20px;
    }
    .group-standings {
      border: 2px solid var(--border);
      border-radius: 8px;
      overflow: hidden;
    }
    .group-standings h4 {
      margin: 0;
      padding: 10px 14px;
      background: var(--header-bg);
      color: var(--hero-accent);
      font-family: var(--font-heading);
      letter-spacing: 0.04em;
    }
    .group-standings table {
      width: 100%;
      border-collapse: collapse;
      background: white;
    }
    .group-standings th, .group-standings td {
      padding: 8px;
      text-align: center;
      border-bottom: 1px solid var(--border-light);
    }
    .group-standings th {
      background: var(--surface-alt);
      font-weight: 600;
      color: var(--heading-color);
      font-size: 0.85em;
    }
    .group-standings td:nth-child(2) { text-align: left; }
    .group-standings tr.qualified { background: color-mix(in srgb, var(--success) 8%, transparent); }
    .tie-break {
      padding: 10px 14px;
      background: var(--accent-soft);
      border-top: 1px solid var(--border-light);
      font-size: 0.9em;
    }
    .tie-break p { margin: 0 0 8px; }
    .tie-break ol { margin: 0 0 8px; padding-left: 20px; }
    .tie-break li { display: flex; align-items: center; gap: 6px; margin-bottom: 4px; }
    .tie-break li span { flex: 1; }
    .tie-move { padding: 2px 8px; font-size: 0.8em; }

    /* Group Matches */
    .group-matches { margin-bottom: 20px; }
    .group-matches h4 {
      color: var(--link);
      border-bottom: 2px solid var(--primary);
      padding-bottom: 5px;
    }

    /* Knockout Section */
    .knockout-section {
      background: white;
      padding: 20px;
      border: 2px solid var(--border);
      border-top: 4px solid var(--danger);
      border-radius: 8px;
    }
    .knockout-section h3 { color: var(--danger); }
    .knockout-matches { margin-top: 20px; }

    /* Single Elimination Bracket Section */
    .bracket-section {
      background: var(--hero-gradient);
      padding: 20px;
      border-radius: 8px;
      color: white;
    }
    .bracket-section h3 {
      color: var(--hero-accent);
      margin-bottom: 15px;
    }
    .bracket-section h4 {
      color: white;
      border-bottom: 2px solid color-mix(in srgb, var(--hero-accent) 40%, transparent);
      padding-bottom: 8px;
      margin-bottom: 15px;
    }
    .bracket-matches { margin-top: 20px; }
    .bracket-section .match {
      background: rgba(255,255,255,0.96);
      color: var(--text);
    }
    .bracket-section .match.completed {
      background: white;
      border-left: 4px solid var(--success);
    }

    /* Round Robin Section */
    .roundrobin-section {
      background: var(--success-gradient);
      padding: 20px;
      border-radius: 8px;
    }
    .roundrobin-section h3 {
      color: var(--hero-accent);
      margin-bottom: 15px;
    }
    .roundrobin-standings {
      background: white;
      border-radius: 8px;
      overflow: hidden;
      margin-bottom: 20px;
    }
    .roundrobin-standings table {
      width: 100%;
      border-collapse: collapse;
    }
    .roundrobin-standings th, .roundrobin-standings td {
      padding: 12px 8px;
      text-align: center;
      border-bottom: 1px solid var(--border-light);
    }
    .roundrobin-standings th {
      background: var(--surface-alt);
      font-weight: 600;
      color: var(--heading-color);
      font-size: 0.85em;
    }
    .roundrobin-standings td:nth-child(2) { text-align: left; }
    .roundrobin-standings tr.top-3 { background: color-mix(in srgb, var(--status-active) 10%, transparent); }
    .roundrobin-standings .rank { font-size: 1.1em; }
    .de-standings {
      margin-top: 20px;
      margin-bottom: 20px;
    }
    .de-standings-table {
      background: white;
      border-radius: 8px;
      overflow: hidden;
    }
    .de-standings-table table {
      width: 100%;
      border-collapse: collapse;
    }
    .de-standings-table th, .de-standings-table td {
      padding: 12px 8px;
      text-align: center;
      border-bottom: 1px solid var(--border-light);
      color: var(--text);
    }
    .de-standings-table th {
      background: var(--surface-alt);
      font-weight: 600;
      color: var(--heading-color);
      font-size: 0.85em;
    }
    .de-standings-table td:nth-child(2) { text-align: left; }
    .de-standings-table tr.top-3 { background: color-mix(in srgb, var(--status-active) 10%, transparent); }
    .de-standings-table .rank { font-size: 1.1em; }
    .roundrobin-section .matches-section {
      background: rgba(255,255,255,0.1);
      padding: 15px;
      border-radius: 8px;
    }
    .roundrobin-section .matches-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 15px;
    }
    .roundrobin-section .match {
      background: white;
      color: var(--text);
    }
    .roundrobin-section .match.completed {
      border-left: 4px solid var(--success);
    }

    .round { margin-bottom: 20px; }
    .matches {
      display: flex;
      flex-wrap: wrap;
      gap: 15px;
    }
    .match {
      padding: 15px;
      border: 1.5px solid var(--border);
      border-radius: 8px;
      min-width: 250px;
      background: white;
    }
    .match.completed { background: color-mix(in srgb, var(--success) 6%, transparent); }
    .players {
      display: flex;
      flex-direction: column;
      gap: 5px;
    }
    .vs {
      text-align: center;
      color: var(--text-muted);
      font-size: 12px;
      font-weight: 600;
    }
    .winner {
      font-weight: 700;
      color: var(--success);
    }
    .score-input {
      display: flex;
      gap: 5px;
      margin-top: 10px;
    }
    .score-input input {
      width: 60px;
      padding: 5px;
      border: 1.5px solid var(--border);
      border-radius: 4px;
      background: var(--input-bg);
    }
    .score-input button {
      padding: 5px 10px;
      background: var(--primary);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .score-input button:hover { background: var(--primary-hover); }
    .match-actions {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-top: 10px;
    }
    .play-btn {
      display: inline-block;
      padding: 8px 16px;
      background: var(--action);
      color: white;
      text-decoration: none;
      border-radius: 4px;
      text-align: center;
      font-weight: 600;
      transition: background 0.2s;
    }
    .correct-btn {
      padding: 6px 14px;
      background: transparent;
      color: var(--accent-text);
      border: 1px solid var(--accent);
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.85em;
    }
    .correct-btn:hover {
      background: var(--accent-soft);
    }
    .exploits-btn {
      padding: 6px 14px;
      background: transparent;
      color: var(--special);
      border: 1px solid var(--special);
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.85em;
    }
    .exploits-btn:hover {
      background: color-mix(in srgb, var(--special) 10%, transparent);
    }
    .cancel-correct {
      padding: 6px 12px;
      background: var(--secondary);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .play-btn:hover {
      background: var(--action-hover);
      color: white;
    }
    .spectate-btn {
      display: inline-block;
      padding: 6px 12px;
      background: var(--secondary);
      color: white;
      text-decoration: none;
      border-radius: 4px;
      font-size: 0.85em;
      margin-top: 8px;
    }
    .spectate-btn:hover {
      background: var(--secondary-hover);
      color: white;
    }
    .loading {
      text-align: center;
      padding: 40px;
      color: var(--text-muted);
      font-size: 1.1em;
    }

    /* Double Elimination Section */
    .double-elim-section {
      background: var(--info-gradient);
      padding: 20px;
      border-radius: 8px;
      color: white;
    }
    .double-elim-section h3 {
      color: var(--hero-accent);
      margin-bottom: 20px;
    }
    .double-elim-matches {
      margin-top: 25px;
      display: flex;
      flex-wrap: wrap;
      gap: 15px;
    }
    .match-input-card {
      background: white;
      color: var(--text);
      padding: 15px;
      border-radius: 8px;
      min-width: 280px;
      border-left: 4px solid var(--border);
    }
    .match-input-card.winners { border-left-color: var(--bracket-winners); }
    .match-input-card.losers { border-left-color: var(--bracket-losers); }
    .match-input-card.grand-final {
      border-left-color: var(--accent);
      background: var(--accent-soft);
    }
    .match-input-card .bracket-label {
      font-size: 0.78em;
      font-weight: 700;
      text-transform: uppercase;
      color: var(--text-muted);
      margin-bottom: 5px;
      display: block;
      letter-spacing: 0.05em;
    }
    .match-input-card .match-info { margin-bottom: 10px; }

    /* Registration Management */
    .pending-section, .approved-section { margin-bottom: 20px; }
    .pending-section h4, .approved-section h4, .add-player h4 {
      margin-bottom: 10px;
      color: var(--heading-color);
    }
    .player-list.pending li {
      background: var(--accent-soft);
      border-left: 4px solid var(--accent);
    }
    .registration-actions { display: flex; gap: 8px; }
    .registration-actions .approve {
      background: var(--success);
      color: white;
      border: none;
      padding: 6px 12px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .registration-actions .approve:hover { background: var(--action-hover); }
    .registration-actions .reject {
      background: var(--danger);
      color: white;
      border: none;
      padding: 6px 12px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .registration-actions .reject:hover { background: var(--danger-hover); }
    .status-pending {
      padding: 15px;
      background: rgba(255,255,255,0.15);
      border-radius: 4px;
      margin-bottom: 15px;
    }
    .status-pending p { margin: 0; font-size: 1.05em; }
    .status-approved {
      padding: 15px;
      background: rgba(255,255,255,0.15);
      border-radius: 4px;
      margin-bottom: 15px;
    }
    .status-approved p {
      margin: 0;
      font-size: 1.05em;
      font-weight: 600;
    }
    .info-text { color: var(--text-muted); font-size: 0.9em; }
  `]
})
export class TournamentDetailComponent implements OnInit {
  tournament: TournamentDetail | null = null;
  standings: GroupStanding[] = [];
  availablePlayers: Player[] = [];
  selectedPlayerId = '';
  selectedSeed: number | null = null;
  selectedTeamPlayer1Id = '';
  selectedTeamPlayer2Id = '';
  selectedTeamSeed: number | null = null;
  Number = Number;
  scoreInputs: { [key: number]: { player1: number; player2: number } } = {};
  correctingMatchId: number | null = null;
  // Match dont le dialogue des exploits est ouvert
  exploitsMatchId: number | null = null;
  loading = false;

  TournamentFormat = TournamentFormat;
  TournamentStatus = TournamentStatus;
  MatchStatus = MatchStatus;
  BracketType = BracketType;
  RegistrationStatus = RegistrationStatus;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  features = inject(FeatureService);

  constructor(
    private route: ActivatedRoute,
    public authService: AuthService,
    private apiService: ApiService
  ) {}

  // Exploits suivis sur les matchs de saison (tournoi rattaché ou circuit de saison)
  get canTrackExploits(): boolean {
    return !!this.tournament?.seasonId && this.features.isEnabled('seasons') && this.features.isEnabled('exploits');
  }

  // Ordres de départage en cours d'édition, par bloc d'ex-aequo
  private tieOrders: Record<string, number[]> = {};

  // Blocs d'ex-aequo non tranchés qui touchent une place qualificative
  blockingTies(group: GroupStanding): { key: string; sideIds: number[] }[] {
    const qualifiers = this.qualifiersFor(group.groupId);
    const byRank = new Map<number, number[]>();
    for (const s of group.standings) {
      if (s.tieUnresolved && s.rank <= qualifiers) {
        byRank.set(s.rank, [...(byRank.get(s.rank) ?? []), s.playerId]);
      }
    }
    return [...byRank.entries()].map(([rank, sideIds]) => ({ key: `${group.groupId}-${rank}`, sideIds }));
  }

  tieOrder(block: { key: string; sideIds: number[] }): number[] {
    const current = this.tieOrders[block.key];
    // Réinitialise si le bloc a changé (correction de score)
    if (!current || current.length !== block.sideIds.length || !current.every(id => block.sideIds.includes(id))) {
      this.tieOrders[block.key] = [...block.sideIds];
    }
    return this.tieOrders[block.key];
  }

  moveTie(block: { key: string; sideIds: number[] }, index: number, delta: number) {
    const order = this.tieOrder(block);
    [order[index], order[index + delta]] = [order[index + delta], order[index]];
  }

  standingName(group: GroupStanding, sideId: number): string {
    return group.standings.find(s => s.playerId === sideId)?.playerName ?? '';
  }

  saveTieBreak(groupId: number, block: { key: string; sideIds: number[] }) {
    const tournamentId = this.tournament!.id;
    this.apiService.setGroupTieBreak(tournamentId, groupId, this.tieOrder(block))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          delete this.tieOrders[block.key];
          this.notificationService.showSuccess('Départage enregistré');
          this.loadTournament(tournamentId);
        },
        error: (err) => {
          this.notificationService.showError(err.error?.message || 'Erreur lors du départage');
        }
      });
  }

  // Qualifiés d'une poule : imposés par la grille en format FSGT, sinon réglage du tournoi
  qualifiersFor(groupId: number): number {
    const group = this.tournament?.groups.find(g => g.id === groupId);
    return group?.qualifierCount ?? this.tournament?.qualifiersPerGroup ?? 2;
  }

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loadTournament(id);
    this.loadPlayers();
  }

  loadTournament(id: number) {
    this.loading = true;
    this.apiService.getTournament(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(tournament => {
      this.tournament = tournament;
      this.initScoreInputs();
      this.loading = false;

      // Load standings for GroupStage, RoundRobin and DoubleElimination tournaments
      if ((tournament.format === TournamentFormat.GroupStage ||
           tournament.format === TournamentFormat.RoundRobin ||
           tournament.format === TournamentFormat.DoubleElimination)
          && tournament.status !== TournamentStatus.Draft) {
        this.loadStandings(id);
      }
    });
  }

  loadStandings(id: number) {
    this.apiService.getStandings(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(standings => {
      this.standings = standings;
    });
  }

  loadPlayers() {
    this.apiService.getPlayers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(players => {
      this.updateAvailablePlayers(players);
    });
  }

  updateAvailablePlayers(allPlayers: Player[]) {
    if (this.tournament) {
      const registeredIds = this.tournament.players.map(p => p.playerId);
      this.availablePlayers = allPlayers.filter(p => !registeredIds.includes(p.id));
    } else {
      this.availablePlayers = allPlayers;
    }
  }

  initScoreInputs() {
    if (this.tournament) {
      this.tournament.matches.forEach(match => {
        this.scoreInputs[match.id] = { player1: 0, player2: 0 };
      });
    }
  }

  // Joueurs pas encore membres d'une paire de ce tournoi (doubles)
  getUnpairedPlayers(): Player[] {
    const pairedIds = new Set(
      (this.tournament?.teams ?? []).flatMap(t => [t.player1Id, t.player2Id])
    );
    return this.availablePlayers.filter(p => !pairedIds.has(p.id));
  }

  addTeam() {
    if (this.tournament && this.selectedTeamPlayer1Id && this.selectedTeamPlayer2Id) {
      this.apiService.addTeamToTournament(
        this.tournament.id,
        Number(this.selectedTeamPlayer1Id),
        Number(this.selectedTeamPlayer2Id),
        this.selectedTeamSeed || undefined
      ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(team => {
        this.notificationService.showSuccess('Paire ajoutée');
        if (this.tournament) {
          this.tournament.teams = [...(this.tournament.teams ?? []), team];
        }
        this.selectedTeamPlayer1Id = '';
        this.selectedTeamPlayer2Id = '';
        this.selectedTeamSeed = null;
      });
    }
  }

  approvedTeams(): TournamentTeam[] {
    return (this.tournament?.teams ?? []).filter(t => t.status !== RegistrationStatus.Pending);
  }

  pendingTeams(): TournamentTeam[] {
    return (this.tournament?.teams ?? []).filter(t => t.status === RegistrationStatus.Pending);
  }

  approveTeam(teamId: number) {
    if (!this.tournament) return;
    this.apiService.approveTeam(this.tournament.id, teamId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Paire validée');
        this.loadTournament(this.tournament!.id);
      },
      error: err => this.notificationService.showError(err.error?.message || 'Erreur lors de la validation')
    });
  }

  rejectTeam(teamId: number) {
    if (!this.tournament || !confirm('Refuser cette paire ?')) return;
    this.apiService.rejectTeam(this.tournament.id, teamId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Paire refusée');
        this.loadTournament(this.tournament!.id);
      },
      error: err => this.notificationService.showError(err.error?.message || 'Erreur lors du refus')
    });
  }

  removeTeam(teamId: number) {
    if (this.tournament) {
      this.apiService.removeTeamFromTournament(this.tournament.id, teamId)
        .pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
          this.notificationService.showSuccess('Paire retirée');
          if (this.tournament) {
            this.tournament.teams = (this.tournament.teams ?? []).filter(t => t.id !== teamId);
          }
        });
    }
  }

  addPlayer() {
    if (this.tournament && this.selectedPlayerId) {
      const playerId = Number(this.selectedPlayerId);
      const player = this.availablePlayers.find(p => p.id === playerId);
      const seed = this.selectedSeed || undefined;

      this.apiService.addPlayerToTournament(
        this.tournament.id,
        playerId,
        seed
      ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Joueur ajouté');
        if (player && this.tournament) {
          const tournamentPlayer = {
            playerId: player.id,
            firstName: player.firstName,
            lastName: player.lastName,
            nickname: player.nickname,
            seed: seed,
            status: RegistrationStatus.Approved
          };
          this.tournament.players = [...this.tournament.players, tournamentPlayer];
          this.availablePlayers = this.availablePlayers.filter(p => p.id !== playerId);
        }
        this.selectedPlayerId = '';
        this.selectedSeed = null;
      });
    }
  }

  removePlayer(playerId: number) {
    if (this.tournament) {
      const removedPlayer = this.tournament.players.find(p => p.playerId === playerId);

      this.apiService.removePlayerFromTournament(this.tournament.id, playerId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Joueur retiré');
        if (this.tournament) {
          this.tournament.players = this.tournament.players.filter(p => p.playerId !== playerId);
          if (removedPlayer) {
            const playerToAdd = {
              id: removedPlayer.playerId,
              firstName: removedPlayer.firstName,
              lastName: removedPlayer.lastName,
              nickname: removedPlayer.nickname,
              createdAt: new Date()
            };
            this.availablePlayers = [...this.availablePlayers, playerToAdd];
          }
        }
      });
    }
  }

  isUserRegistered(): boolean {
    if (!this.tournament || !this.authService.linkedPlayerId()) {
      return false;
    }
    const linkedPlayerId = this.authService.linkedPlayerId();
    return this.tournament.players.some(p => p.playerId === linkedPlayerId);
  }

  registerToTournament() {
    if (!this.tournament) return;

    this.apiService.registerToTournament(this.tournament.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Inscription réussie');
          this.loadTournament(this.tournament!.id);
        },
        error: (error) => {
          const message = error.error?.message || 'Erreur lors de l\'inscription';
          this.notificationService.showError(message);
        }
      });
  }

  unregisterFromTournament() {
    if (!this.tournament) return;

    if (!confirm('Voulez-vous vraiment vous désinscrire de ce tournoi ?')) {
      return;
    }

    this.apiService.unregisterFromTournament(this.tournament.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Désinscription réussie');
          this.loadTournament(this.tournament!.id);
        },
        error: (error) => {
          const message = error.error?.message || 'Erreur lors de la désinscription';
          this.notificationService.showError(message);
        }
      });
  }

  approveRegistration(playerId: number) {
    if (!this.tournament) return;

    this.apiService.approveRegistration(this.tournament.id, playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Inscription approuvée');
          this.loadTournament(this.tournament!.id);
        },
        error: (error) => {
          const message = error.error?.message || 'Erreur lors de l\'approbation';
          this.notificationService.showError(message);
        }
      });
  }

  rejectRegistration(playerId: number) {
    if (!this.tournament) return;

    if (!confirm('Voulez-vous vraiment refuser cette inscription ?')) {
      return;
    }

    this.apiService.rejectRegistration(this.tournament.id, playerId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Inscription refusée');
          this.loadTournament(this.tournament!.id);
        },
        error: (error) => {
          const message = error.error?.message || 'Erreur lors du refus';
          this.notificationService.showError(message);
        }
      });
  }

  getRegistrationStatusLabel(status: RegistrationStatus): string {
    switch (status) {
      case RegistrationStatus.Pending:
        return 'En attente';
      case RegistrationStatus.Approved:
        return 'Approuvé';
      case RegistrationStatus.Rejected:
        return 'Refusé';
      default:
        return '';
    }
  }

  getPendingRegistrations() {
    if (!this.tournament) return [];
    return this.tournament.players.filter(p => p.status === RegistrationStatus.Pending);
  }

  getApprovedRegistrations() {
    if (!this.tournament) return [];
    return this.tournament.players.filter(p => p.status === RegistrationStatus.Approved);
  }

  generateBracket() {
    if (this.tournament) {
      this.apiService.generateBracket(this.tournament.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
        this.notificationService.showSuccess('Bracket généré');
        this.loadTournament(this.tournament!.id);
      });
    }
  }

  // Group matches (non-knockout)
  getGroupMatches(groupId: number): Match[] {
    if (!this.tournament) return [];
    return this.tournament.matches
      .filter(m => m.groupId === groupId && !m.isKnockoutMatch)
      .sort((a, b) => a.position - b.position);
  }

  // Knockout matches
  hasKnockoutMatches(): boolean {
    return this.tournament?.matches.some(m => m.isKnockoutMatch) ?? false;
  }

  getKnockoutRounds(): number[] {
    if (!this.tournament) return [];
    const knockoutMatches = this.tournament.matches.filter(m => m.isKnockoutMatch);
    const rounds = [...new Set(knockoutMatches.map(m => m.round))];
    return rounds.sort((a, b) => a - b);
  }

  getKnockoutMatchesByRound(round: number): Match[] {
    if (!this.tournament) return [];
    return this.tournament.matches
      .filter(m => m.isKnockoutMatch && m.round === round)
      .sort((a, b) => a.position - b.position);
  }

  getKnockoutRoundName(round: number): string {
    const knockoutMatches = this.tournament?.matches.filter(m => m.isKnockoutMatch) ?? [];
    const maxRound = Math.max(...knockoutMatches.map(m => m.round));
    const matchesInRound = knockoutMatches.filter(m => m.round === round).length;

    if (round === maxRound) return 'Finale';
    if (round === maxRound - 1 && matchesInRound <= 2) return 'Demi-finales';
    if (round === maxRound - 2 && matchesInRound <= 4) return 'Quarts de finale';
    return `Round ${round}`;
  }

  getSingleElimRoundName(round: number): string {
    if (!this.tournament) return `Round ${round}`;
    const rounds = this.getRounds();
    const maxRound = Math.max(...rounds);
    const matchesInRound = this.getMatchesByRound(round).length;

    if (round === maxRound) return 'Finale';
    if (matchesInRound === 1) return 'Finale';
    if (matchesInRound === 2) return 'Demi-finales';
    if (matchesInRound === 4) return 'Quarts de finale';
    if (matchesInRound === 8) return 'Huitièmes de finale';
    if (matchesInRound === 16) return 'Seizièmes de finale';
    return `Round ${round}`;
  }

  // All rounds (for non-GroupStage formats)
  getRounds(): number[] {
    if (!this.tournament) return [];
    const rounds = [...new Set(this.tournament.matches.map(m => m.round))];
    return rounds.sort((a, b) => a - b);
  }

  getMatchesByRound(round: number): Match[] {
    if (!this.tournament) return [];
    return this.tournament.matches
      .filter(m => m.round === round)
      .sort((a, b) => a.position - b.position);
  }

  startCorrection(match: Match) {
    this.scoreInputs[match.id] = {
      player1: match.player1Score ?? 0,
      player2: match.player2Score ?? 0
    };
    this.correctingMatchId = match.id;
  }

  updateScore(match: Match) {
    const scores = this.scoreInputs[match.id];

    // Correction d'un score déjà validé : avertir avant d'écraser
    if (match.status === MatchStatus.Completed) {
      const warning = 'Corriger le score de ce match ?\n\n'
        + 'Si ce match a été joué via l\'interface de jeu, les statistiques détaillées '
        + '(volées, moyennes) ne seront pas modifiées : seul le résultat le sera.';
      if (!confirm(warning)) {
        return;
      }
    }

    this.apiService.updateMatchScore(match.id, scores.player1, scores.player2).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Score enregistré');
        this.correctingMatchId = null;
        this.loadTournament(this.tournament!.id);
      },
      error: (err) => {
        this.notificationService.showError(
          typeof err.error === 'string' ? err.error : (err.error?.message || 'Erreur lors de l\'enregistrement du score'));
      }
    });
  }

  // Double Elimination helpers
  getDoubleElimMatches(): Match[] {
    if (!this.tournament) return [];
    return this.tournament.matches.sort((a, b) => {
      // Sort by bracket type first, then by round, then by position
      if (a.bracketType !== b.bracketType) return a.bracketType - b.bracketType;
      if (a.round !== b.round) return a.round - b.round;
      return a.position - b.position;
    });
  }

  getBracketLabel(match: Match): string {
    switch (match.bracketType) {
      case BracketType.Winners:
        return `Winner's Bracket - Tour ${match.round}`;
      case BracketType.Losers:
        return `Loser's Bracket - Tour ${match.round}`;
      case BracketType.GrandFinal:
        return match.isBracketReset ? 'Grande Finale - Match Décisif' : 'Grande Finale';
      default:
        return `Tour ${match.round}`;
    }
  }

  getFormatLabel(format: TournamentFormat): string {
    switch (format) {
      case TournamentFormat.SingleElimination: return 'Élimination directe';
      case TournamentFormat.RoundRobin: return 'Round Robin';
      case TournamentFormat.GroupStage: return 'Phase de groupes';
      case TournamentFormat.DoubleElimination: return 'Double Élimination';
      default: return 'Inconnu';
    }
  }

  getStatusLabel(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Draft: return 'Brouillon';
      case TournamentStatus.InProgress: return 'En cours';
      case TournamentStatus.Completed: return 'Terminé';
      default: return 'Inconnu';
    }
  }
}
