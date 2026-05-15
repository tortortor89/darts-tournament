import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { PlayerCareerStats, PlayerTournamentHistoryItem, HeadToHeadRecord, TournamentFormat, TournamentStatus } from '../../core/models';

@Component({
  selector: 'app-player-stats',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container">
      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (playerStats) {
        <!-- Header -->
        <div class="header">
          <h2>📊 Statistiques de {{ playerStats.playerName }}</h2>
          <button class="btn-back" routerLink="/players">← Retour aux joueurs</button>
        </div>

        <!-- Section 1: Stats globales -->
        <section class="career-stats">
          <h3>Statistiques de carrière</h3>
          <div class="stats-grid">
            <div class="stat-card">
              <div class="stat-value">{{ playerStats.totalMatches }}</div>
              <div class="stat-label">Matchs joués</div>
            </div>
            <div class="stat-card success">
              <div class="stat-value">{{ playerStats.matchesWon }}</div>
              <div class="stat-label">Victoires</div>
            </div>
            <div class="stat-card danger">
              <div class="stat-value">{{ playerStats.matchesLost }}</div>
              <div class="stat-label">Défaites</div>
            </div>
            <div class="stat-card highlight">
              <div class="stat-value">{{ playerStats.winPercentage.toFixed(1) }}%</div>
              <div class="stat-label">% Victoires</div>
            </div>
            <div class="stat-card">
              <div class="stat-value">{{ playerStats.tournamentsPlayed }}</div>
              <div class="stat-label">Tournois</div>
            </div>
          </div>
        </section>

        <!-- Section 2: Stats de jeu détaillées -->
        @if (playerStats.detailedStats) {
          <section class="game-stats">
            <h3>Statistiques de jeu</h3>
            <table>
              <tbody>
                <tr>
                  <td>Moyenne 3 fléchettes</td>
                  <td class="stat-value">{{ playerStats.detailedStats.threeDartAverage.toFixed(2) }}</td>
                </tr>
                <tr>
                  <td>Taux de checkout</td>
                  <td class="stat-value">
                    @if (playerStats.detailedStats.checkoutPercentage !== null && playerStats.detailedStats.checkoutPercentage !== undefined) {
                      {{ playerStats.detailedStats.checkoutPercentage.toFixed(1) }}%
                    } @else {
                      N/A
                    }
                  </td>
                </tr>
                <tr>
                  <td>Moyenne 9 premières fléchettes</td>
                  <td class="stat-value">
                    @if (playerStats.detailedStats.first9Average !== null && playerStats.detailedStats.first9Average !== undefined) {
                      {{ playerStats.detailedStats.first9Average.toFixed(2) }}
                    } @else {
                      N/A
                    }
                  </td>
                </tr>
                <tr>
                  <td>Plus haut checkout</td>
                  <td class="stat-value">
                    {{ playerStats.detailedStats.highestCheckout ?? 'N/A' }}
                  </td>
                </tr>
                <tr>
                  <td>Nombre de 180</td>
                  <td class="stat-value">{{ playerStats.detailedStats.totalOneEighties }}</td>
                </tr>
                <tr>
                  <td>Plus haut score</td>
                  <td class="stat-value">
                    {{ playerStats.detailedStats.highestScore ?? 'N/A' }}
                  </td>
                </tr>
                <tr>
                  <td>Legs gagnés</td>
                  <td class="stat-value">{{ playerStats.detailedStats.totalLegsWon }}</td>
                </tr>
                <tr>
                  <td>Fléchettes lancées</td>
                  <td class="stat-value">{{ playerStats.detailedStats.totalDartsThrown }}</td>
                </tr>
              </tbody>
            </table>
            <p class="note">Basé sur {{ playerStats.detailedStats.matchesWithStats }} match(s) avec données détaillées</p>
          </section>
        } @else {
          <section class="game-stats">
            <h3>Statistiques de jeu</h3>
            <div class="no-data">
              <p>Aucune donnée de jeu détaillée disponible</p>
              <small>Les statistiques détaillées sont disponibles uniquement pour les matchs joués via l'interface de jeu</small>
            </div>
          </section>
        }

        <!-- Section 3: Historique des tournois -->
        @if (tournamentHistory && tournamentHistory.length > 0) {
          <section class="tournament-history">
            <h3>Historique des tournois</h3>
            <table>
              <thead>
                <tr>
                  <th>Tournoi</th>
                  <th>Format</th>
                  <th>Statut</th>
                  <th>Matchs</th>
                  <th>V-D</th>
                  <th>Résultat</th>
                </tr>
              </thead>
              <tbody>
                @for (item of tournamentHistory; track item.tournamentId) {
                  <tr>
                    <td>
                      <a [routerLink]="['/tournaments', item.tournamentId]">
                        {{ item.tournamentName }}
                      </a>
                    </td>
                    <td>{{ formatTournamentFormat(item.format) }}</td>
                    <td>
                      <span [class]="'status-badge ' + getStatusClass(item.status)">
                        {{ formatTournamentStatus(item.status) }}
                      </span>
                    </td>
                    <td>{{ item.matchesPlayed }}</td>
                    <td>{{ item.matchesWon }}-{{ item.matchesLost }}</td>
                    <td>{{ item.result }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        } @else {
          <section class="tournament-history">
            <h3>Historique des tournois</h3>
            <div class="no-data">
              <p>Aucun tournoi joué</p>
            </div>
          </section>
        }

        <!-- Section 4: Head-to-head -->
        @if (headToHead && headToHead.length > 0) {
          <section class="head-to-head">
            <h3>Confrontations directes</h3>
            <table>
              <thead>
                <tr>
                  <th>Adversaire</th>
                  <th>Matchs</th>
                  <th>V</th>
                  <th>D</th>
                  <th>% Victoires</th>
                  <th>Legs</th>
                </tr>
              </thead>
              <tbody>
                @for (record of headToHead; track record.opponentId) {
                  <tr>
                    <td class="opponent-name">{{ record.opponentName }}</td>
                    <td>{{ record.matchesPlayed }}</td>
                    <td class="win">{{ record.matchesWon }}</td>
                    <td class="loss">{{ record.matchesLost }}</td>
                    <td>
                      <span [class]="'win-pct ' + getWinPercentageClass(record.winPercentage)">
                        {{ record.winPercentage.toFixed(1) }}%
                      </span>
                    </td>
                    <td>{{ record.totalLegsWon }}-{{ record.totalLegsLost }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        } @else {
          <section class="head-to-head">
            <h3>Confrontations directes</h3>
            <div class="no-data">
              <p>Aucune confrontation</p>
            </div>
          </section>
        }
      }
    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }

    .loading {
      text-align: center;
      padding: 60px 20px;
      font-size: 1.2em;
      color: #666;
    }

    /* Header */
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 30px;
      flex-wrap: wrap;
      gap: 15px;
    }

    .header h2 {
      margin: 0;
      color: #333;
      font-size: 2em;
    }

    .btn-back {
      padding: 10px 20px;
      background: #6c757d;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 1em;
      transition: all 0.2s;
    }

    .btn-back:hover {
      background: #5a6268;
      transform: translateY(-2px);
    }

    /* Sections */
    section {
      background: white;
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 25px;
      margin-bottom: 25px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    section h3 {
      margin: 0 0 20px 0;
      color: #007bff;
      font-size: 1.5em;
      border-bottom: 2px solid #007bff;
      padding-bottom: 10px;
    }

    /* Stats Grid */
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 20px;
    }

    .stat-card {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 25px;
      border-radius: 8px;
      text-align: center;
      color: white;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      transition: transform 0.2s;
    }

    .stat-card:hover {
      transform: translateY(-5px);
      box-shadow: 0 6px 12px rgba(0,0,0,0.15);
    }

    .stat-card.success {
      background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
    }

    .stat-card.danger {
      background: linear-gradient(135deg, #eb3349 0%, #f45c43 100%);
    }

    .stat-card.highlight {
      background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
    }

    .stat-value {
      font-size: 3em;
      font-weight: bold;
      margin-bottom: 8px;
    }

    .stat-label {
      font-size: 0.95em;
      opacity: 0.95;
    }

    /* Tables */
    table {
      width: 100%;
      border-collapse: collapse;
    }

    th {
      background: #f8f9fa;
      padding: 12px;
      text-align: left;
      font-weight: 600;
      color: #495057;
      border-bottom: 2px solid #dee2e6;
    }

    td {
      padding: 12px;
      border-bottom: 1px solid #dee2e6;
    }

    tbody tr:hover {
      background: #f8f9fa;
    }

    .stat-value {
      font-weight: 600;
      color: #007bff;
    }

    .note {
      margin-top: 15px;
      font-size: 0.9em;
      color: #6c757d;
      font-style: italic;
    }

    /* No data */
    .no-data {
      text-align: center;
      padding: 40px 20px;
      color: #6c757d;
    }

    .no-data p {
      margin: 0 0 8px 0;
      font-size: 1.1em;
    }

    .no-data small {
      font-size: 0.9em;
    }

    /* Tournament history */
    .tournament-history a {
      color: #007bff;
      text-decoration: none;
      font-weight: 500;
    }

    .tournament-history a:hover {
      text-decoration: underline;
    }

    .status-badge {
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 0.85em;
      font-weight: 600;
    }

    .status-badge.draft {
      background: #ffc107;
      color: #000;
    }

    .status-badge.inprogress {
      background: #17a2b8;
      color: white;
    }

    .status-badge.completed {
      background: #28a745;
      color: white;
    }

    /* Head-to-head */
    .opponent-name {
      font-weight: 500;
    }

    .win {
      color: #28a745;
      font-weight: 600;
    }

    .loss {
      color: #dc3545;
      font-weight: 600;
    }

    .win-pct {
      padding: 4px 8px;
      border-radius: 4px;
      font-weight: 600;
    }

    .win-pct.high {
      background: #d4edda;
      color: #155724;
    }

    .win-pct.medium {
      background: #fff3cd;
      color: #856404;
    }

    .win-pct.low {
      background: #f8d7da;
      color: #721c24;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .header {
        flex-direction: column;
        align-items: stretch;
      }

      .btn-back {
        width: 100%;
      }

      .stats-grid {
        grid-template-columns: 1fr;
      }

      table {
        font-size: 0.9em;
      }

      th, td {
        padding: 8px;
      }
    }
  `]
})
export class PlayerStatsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private apiService = inject(ApiService);

  playerId!: number;
  playerStats?: PlayerCareerStats;
  tournamentHistory?: PlayerTournamentHistoryItem[];
  headToHead?: HeadToHeadRecord[];
  loading = false;

  TournamentFormat = TournamentFormat;
  TournamentStatus = TournamentStatus;

  ngOnInit() {
    this.playerId = +this.route.snapshot.params['id'];
    this.loadStats();
  }

  loadStats() {
    this.loading = true;

    // 3 appels API en parallèle
    forkJoin({
      career: this.apiService.getPlayerCareerStats(this.playerId),
      history: this.apiService.getPlayerTournamentHistory(this.playerId),
      h2h: this.apiService.getPlayerHeadToHead(this.playerId)
    }).subscribe({
      next: (result) => {
        this.playerStats = result.career;
        this.tournamentHistory = result.history;
        this.headToHead = result.h2h;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading player stats:', error);
        this.loading = false;
      }
    });
  }

  formatTournamentFormat(format: TournamentFormat): string {
    switch (format) {
      case TournamentFormat.SingleElimination:
        return 'Simple élimination';
      case TournamentFormat.DoubleElimination:
        return 'Double élimination';
      case TournamentFormat.RoundRobin:
        return 'Round Robin';
      case TournamentFormat.GroupStage:
        return 'Poules';
      default:
        return 'Inconnu';
    }
  }

  formatTournamentStatus(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Draft:
        return 'Brouillon';
      case TournamentStatus.InProgress:
        return 'En cours';
      case TournamentStatus.Completed:
        return 'Terminé';
      default:
        return 'Inconnu';
    }
  }

  getStatusClass(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Draft:
        return 'draft';
      case TournamentStatus.InProgress:
        return 'inprogress';
      case TournamentStatus.Completed:
        return 'completed';
      default:
        return '';
    }
  }

  getWinPercentageClass(winPct: number): string {
    if (winPct >= 60) return 'high';
    if (winPct >= 40) return 'medium';
    return 'low';
  }
}
