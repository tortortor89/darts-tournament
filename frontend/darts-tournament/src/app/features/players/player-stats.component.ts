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
      color: var(--hd-text-muted);
    }

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
      font-size: 2em;
    }

    .btn-back {
      padding: 10px 20px;
      background: var(--hd-text-muted);
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 1em;
      font-weight: 500;
      transition: all 0.2s;
    }

    .btn-back:hover {
      background: var(--hd-green);
      transform: translateY(-1px);
    }

    section {
      background: white;
      border: 1px solid var(--hd-border);
      border-radius: 8px;
      padding: 25px;
      margin-bottom: 25px;
      box-shadow: 0 2px 8px rgba(26,60,42,0.08);
    }

    section h3 {
      margin: 0 0 20px 0;
      font-size: 1.4em;
      border-bottom: 2px solid var(--hd-amber);
      padding-bottom: 10px;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 20px;
    }

    .stat-card {
      background: var(--hd-green);
      padding: 25px;
      border-radius: 8px;
      text-align: center;
      color: white;
      box-shadow: 0 4px 8px rgba(26,60,42,0.2);
      transition: transform 0.2s;
    }

    .stat-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 16px rgba(26,60,42,0.25);
    }

    .stat-card.success {
      background: var(--hd-success);
    }

    .stat-card.danger {
      background: var(--hd-danger);
    }

    .stat-card.highlight {
      background: var(--hd-amber);
    }

    .stat-card .stat-value {
      font-size: 3em;
      font-weight: 800;
      font-family: 'Barlow Condensed', sans-serif;
      margin-bottom: 8px;
      color: white;
    }

    .stat-label {
      font-size: 0.9em;
      opacity: 0.9;
    }

    table {
      width: 100%;
      border-collapse: collapse;
    }

    th {
      background: var(--hd-cream-dark);
      padding: 12px;
      text-align: left;
      font-weight: 600;
      color: var(--hd-green);
      border-bottom: 2px solid var(--hd-border);
      font-size: 0.9em;
    }

    td {
      padding: 12px;
      border-bottom: 1px solid var(--hd-border);
      color: var(--hd-text);
    }

    tbody tr:hover { background: var(--hd-cream); }

    .stat-value {
      font-weight: 600;
      color: var(--hd-amber);
    }

    .note {
      margin-top: 15px;
      font-size: 0.9em;
      color: var(--hd-text-muted);
      font-style: italic;
    }

    .no-data {
      text-align: center;
      padding: 40px 20px;
      color: var(--hd-text-muted);
    }
    .no-data p { margin: 0 0 8px 0; font-size: 1.1em; }
    .no-data small { font-size: 0.9em; }

    .tournament-history a {
      color: var(--hd-green);
      text-decoration: none;
      font-weight: 500;
    }
    .tournament-history a:hover { color: var(--hd-amber); }

    .status-badge {
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 0.85em;
      font-weight: 600;
    }
    .status-badge.draft { background: rgba(232,149,10,0.2); color: var(--hd-amber); }
    .status-badge.inprogress { background: var(--hd-green); color: white; }
    .status-badge.completed { background: var(--hd-success); color: white; }

    .opponent-name { font-weight: 500; }
    .win { color: var(--hd-success); font-weight: 600; }
    .loss { color: var(--hd-danger); font-weight: 600; }

    .win-pct {
      padding: 4px 8px;
      border-radius: 4px;
      font-weight: 600;
    }
    .win-pct.high { background: rgba(39,100,58,0.12); color: var(--hd-success); }
    .win-pct.medium { background: rgba(232,149,10,0.12); color: var(--hd-amber); }
    .win-pct.low { background: rgba(192,57,43,0.12); color: var(--hd-danger); }

    @media (max-width: 768px) {
      .header { flex-direction: column; align-items: stretch; }
      .btn-back { width: 100%; }
      .stats-grid { grid-template-columns: 1fr; }
      table { font-size: 0.9em; }
      th, td { padding: 8px; }
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
