import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { TournamentDetail, Player, TournamentFormat, TournamentStatus, MatchStatus, Match, GroupStanding, BracketType } from '../../core/models';
import { BracketViewerComponent } from '../../shared/components/bracket-viewer/bracket-viewer.component';
import { DoubleBracketViewerComponent } from '../../shared/components/double-bracket-viewer/double-bracket-viewer.component';

@Component({
  selector: 'app-tournament-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, BracketViewerComponent, DoubleBracketViewerComponent],
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
        @if (tournament.format === TournamentFormat.GroupStage) {
          <p>
            {{ tournament.numberOfGroups || 'Auto' }} groupes,
            {{ tournament.qualifiersPerGroup || 2 }} qualifiés/groupe
            @if (tournament.hasKnockoutPhase) { + phase éliminatoire }
          </p>
        }
      </div>

      @if (tournament.status === TournamentStatus.Draft && authService.isAdmin()) {
        <div class="players-section">
          <h3>Joueurs inscrits ({{ tournament.players.length }})</h3>

          <div class="add-player">
            <select [(ngModel)]="selectedPlayerId">
              <option value="">Sélectionner un joueur</option>
              @for (player of availablePlayers; track player.id) {
                <option [value]="player.id">{{ player.firstName }} {{ player.lastName }}</option>
              }
            </select>
            <input type="number" [(ngModel)]="selectedSeed" placeholder="Seed (optionnel)">
            <button (click)="addPlayer()" [disabled]="!selectedPlayerId">Ajouter</button>
          </div>

          <ul class="player-list">
            @for (player of tournament.players; track player.playerId) {
              <li>
                {{ player.firstName }} {{ player.lastName }}
                @if (player.nickname) { ({{ player.nickname }}) }
                @if (player.seed) { - Seed: {{ player.seed }} }
                <button (click)="removePlayer(player.playerId)" class="remove">X</button>
              </li>
            }
          </ul>

          @if (tournament.players.length >= 2) {
            <button (click)="generateBracket()" class="generate">Générer le bracket</button>
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
                          <tr [class.qualified]="player.rank <= (tournament.qualifiersPerGroup || 2)">
                            <td>{{ player.rank }}</td>
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
                        <div class="score-input">
                          <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                          <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                          <button (click)="updateScore(match)">Valider</button>
                        </div>
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
                            <div class="score-input">
                              <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                              <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                              <button (click)="updateScore(match)">Valider</button>
                            </div>
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
                          <div class="score-input">
                            <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                            <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                            <button (click)="updateScore(match)">Valider</button>
                          </div>
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
                      <div class="score-input">
                        <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                        <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                        <button (click)="updateScore(match)">Valider</button>
                      </div>
                    }
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
                      <div class="score-input">
                        <input type="number" [(ngModel)]="scoreInputs[match.id].player1" min="0" placeholder="Score 1">
                        <input type="number" [(ngModel)]="scoreInputs[match.id].player2" min="0" placeholder="Score 2">
                        <button (click)="updateScore(match)">Valider</button>
                      </div>
                    }
                  </div>
                }
              </div>
            </div>
          </div>
        }
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
      padding: 10px;
      background: #f5f5f5;
      border-radius: 8px;
    }
    .players-section, .matches-section, .standings-section, .knockout-section {
      margin-bottom: 30px;
    }
    .add-player {
      display: flex;
      gap: 10px;
      margin-bottom: 15px;
    }
    .add-player select, .add-player input {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    .add-player button {
      padding: 8px 16px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .player-list {
      list-style: none;
      padding: 0;
    }
    .player-list li {
      padding: 8px;
      border-bottom: 1px solid #eee;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .remove {
      background: #dc3545;
      color: white;
      border: none;
      padding: 2px 8px;
      border-radius: 4px;
      cursor: pointer;
    }
    .generate {
      padding: 10px 20px;
      background: #28a745;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 16px;
    }

    /* Standings */
    .standings-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
      gap: 20px;
    }
    .group-standings {
      border: 1px solid #ddd;
      border-radius: 8px;
      overflow: hidden;
    }
    .group-standings h4 {
      margin: 0;
      padding: 10px;
      background: #007bff;
      color: white;
    }
    .group-standings table {
      width: 100%;
      border-collapse: collapse;
    }
    .group-standings th, .group-standings td {
      padding: 8px;
      text-align: center;
      border-bottom: 1px solid #eee;
    }
    .group-standings th {
      background: #f5f5f5;
      font-weight: 600;
    }
    .group-standings td:nth-child(2) {
      text-align: left;
    }
    .group-standings tr.qualified {
      background: #e8f5e9;
    }

    /* Group Matches */
    .group-matches {
      margin-bottom: 20px;
    }
    .group-matches h4 {
      color: #007bff;
      border-bottom: 2px solid #007bff;
      padding-bottom: 5px;
    }

    /* Knockout Section */
    .knockout-section {
      background: #f8f9fa;
      padding: 20px;
      border-radius: 8px;
    }
    .knockout-section h3 {
      color: #dc3545;
    }
    .knockout-matches {
      margin-top: 20px;
    }

    /* Single Elimination Bracket Section */
    .bracket-section {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 20px;
      border-radius: 8px;
      color: white;
    }
    .bracket-section h3 {
      color: white;
      margin-bottom: 15px;
    }
    .bracket-section h4 {
      color: white;
      border-bottom: 2px solid rgba(255,255,255,0.3);
      padding-bottom: 8px;
      margin-bottom: 15px;
    }
    .bracket-matches {
      margin-top: 20px;
    }
    .bracket-section .match {
      background: rgba(255,255,255,0.95);
      color: #333;
    }
    .bracket-section .match.completed {
      background: rgba(255,255,255,1);
      border-left: 4px solid #28a745;
    }

    /* Round Robin Section */
    .roundrobin-section {
      background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
      padding: 20px;
      border-radius: 8px;
    }
    .roundrobin-section h3 {
      color: white;
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
      border-bottom: 1px solid #eee;
    }
    .roundrobin-standings th {
      background: #f8f9fa;
      font-weight: 600;
      color: #333;
    }
    .roundrobin-standings td:nth-child(2) {
      text-align: left;
    }
    .roundrobin-standings tr.top-3 {
      background: #e8f5e9;
    }
    .roundrobin-standings .rank {
      font-size: 1.1em;
    }
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
      border-bottom: 1px solid #eee;
      color: #333;
    }
    .de-standings-table th {
      background: #f8f9fa;
      font-weight: 600;
      color: #333;
    }
    .de-standings-table td:nth-child(2) {
      text-align: left;
    }
    .de-standings-table tr.top-3 {
      background: #e8f5e9;
    }
    .de-standings-table .rank {
      font-size: 1.1em;
    }
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
      color: #333;
    }
    .roundrobin-section .match.completed {
      border-left: 4px solid #28a745;
    }

    .round {
      margin-bottom: 20px;
    }
    .matches {
      display: flex;
      flex-wrap: wrap;
      gap: 15px;
    }
    .match {
      padding: 15px;
      border: 1px solid #ddd;
      border-radius: 8px;
      min-width: 250px;
      background: white;
    }
    .match.completed {
      background: #f0fff0;
    }
    .players {
      display: flex;
      flex-direction: column;
      gap: 5px;
    }
    .vs {
      text-align: center;
      color: #666;
      font-size: 12px;
    }
    .winner {
      font-weight: bold;
      color: #28a745;
    }
    .score-input {
      display: flex;
      gap: 5px;
      margin-top: 10px;
    }
    .score-input input {
      width: 60px;
      padding: 5px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    .score-input button {
      padding: 5px 10px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .loading {
      text-align: center;
      padding: 40px;
      color: #666;
      font-size: 1.1em;
    }

    /* Double Elimination Section */
    .double-elim-section {
      background: linear-gradient(135deg, #1e3a5f 0%, #2d5a87 100%);
      padding: 20px;
      border-radius: 8px;
      color: white;
    }
    .double-elim-section h3 {
      color: white;
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
      color: #333;
      padding: 15px;
      border-radius: 8px;
      min-width: 280px;
      border-left: 4px solid #ccc;
    }
    .match-input-card.winners {
      border-left-color: #0d6efd;
    }
    .match-input-card.losers {
      border-left-color: #fd7e14;
    }
    .match-input-card.grand-final {
      border-left-color: #ffc107;
      background: #fffbeb;
    }
    .match-input-card .bracket-label {
      font-size: 0.8em;
      font-weight: 600;
      text-transform: uppercase;
      color: #666;
      margin-bottom: 5px;
      display: block;
    }
    .match-input-card .match-info {
      margin-bottom: 10px;
    }
  `]
})
export class TournamentDetailComponent implements OnInit {
  tournament: TournamentDetail | null = null;
  standings: GroupStanding[] = [];
  availablePlayers: Player[] = [];
  selectedPlayerId = '';
  selectedSeed: number | null = null;
  scoreInputs: { [key: number]: { player1: number; player2: number } } = {};
  loading = false;

  TournamentFormat = TournamentFormat;
  TournamentStatus = TournamentStatus;
  MatchStatus = MatchStatus;
  BracketType = BracketType;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);

  constructor(
    private route: ActivatedRoute,
    public authService: AuthService,
    private apiService: ApiService
  ) {}

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
            seed: seed
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

  updateScore(match: Match) {
    const scores = this.scoreInputs[match.id];
    this.apiService.updateMatchScore(match.id, scores.player1, scores.player2).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.notificationService.showSuccess('Score enregistré');
      this.loadTournament(this.tournament!.id);
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
