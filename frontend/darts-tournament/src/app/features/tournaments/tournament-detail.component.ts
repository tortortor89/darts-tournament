import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { TournamentDetail, Player, TournamentFormat, TournamentStatus, MatchStatus, Match, GroupStanding } from '../../core/models';
import { BracketViewerComponent } from '../../shared/components/bracket-viewer/bracket-viewer.component';

@Component({
  selector: 'app-tournament-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, BracketViewerComponent],
  template: `
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

      @if (tournament.status === TournamentStatus.Draft && authService.isAuthenticated()) {
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
                      @if (authService.isAuthenticated() && match.status !== MatchStatus.Completed && match.player1Id && match.player2Id) {
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
                          @if (authService.isAuthenticated() && match.status !== MatchStatus.Completed && match.player1Id && match.player2Id) {
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
        } @else {
          <!-- Non-GroupStage: Show all matches by round -->
          <div class="matches-section">
            <h3>Matchs</h3>
            @if (tournament.format === TournamentFormat.SingleElimination) {
              <app-bracket-viewer [tournament]="tournament"></app-bracket-viewer>
            }
            @for (round of getRounds(); track round) {
              <div class="round">
                <h4>Round {{ round }}</h4>
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
                      @if (authService.isAuthenticated() && match.status !== MatchStatus.Completed && match.player1Id && match.player2Id) {
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
  `]
})
export class TournamentDetailComponent implements OnInit {
  tournament: TournamentDetail | null = null;
  standings: GroupStanding[] = [];
  availablePlayers: Player[] = [];
  selectedPlayerId = '';
  selectedSeed: number | null = null;
  scoreInputs: { [key: number]: { player1: number; player2: number } } = {};

  TournamentFormat = TournamentFormat;
  TournamentStatus = TournamentStatus;
  MatchStatus = MatchStatus;

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
    this.apiService.getTournament(id).subscribe(tournament => {
      this.tournament = tournament;
      this.initScoreInputs();

      // Load standings for GroupStage tournaments
      if (tournament.format === TournamentFormat.GroupStage && tournament.status !== TournamentStatus.Draft) {
        this.loadStandings(id);
      }
    });
  }

  loadStandings(id: number) {
    this.apiService.getStandings(id).subscribe(standings => {
      this.standings = standings;
    });
  }

  loadPlayers() {
    this.apiService.getPlayers().subscribe(players => {
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
      this.apiService.addPlayerToTournament(
        this.tournament.id,
        Number(this.selectedPlayerId),
        this.selectedSeed || undefined
      ).subscribe(() => {
        this.loadTournament(this.tournament!.id);
        this.selectedPlayerId = '';
        this.selectedSeed = null;
        this.loadPlayers();
      });
    }
  }

  removePlayer(playerId: number) {
    if (this.tournament) {
      this.apiService.removePlayerFromTournament(this.tournament.id, playerId).subscribe(() => {
        this.loadTournament(this.tournament!.id);
        this.loadPlayers();
      });
    }
  }

  generateBracket() {
    if (this.tournament) {
      this.apiService.generateBracket(this.tournament.id).subscribe(() => {
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
    this.apiService.updateMatchScore(match.id, scores.player1, scores.player2).subscribe(() => {
      this.loadTournament(this.tournament!.id);
    });
  }

  getFormatLabel(format: TournamentFormat): string {
    switch (format) {
      case TournamentFormat.SingleElimination: return 'Élimination directe';
      case TournamentFormat.RoundRobin: return 'Round Robin';
      case TournamentFormat.GroupStage: return 'Phase de groupes';
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
