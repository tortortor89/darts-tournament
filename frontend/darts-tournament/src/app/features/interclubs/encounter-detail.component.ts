import { Component, OnInit, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { EncounterDetail, EncounterStatus, MatchStatus, BoardLineup, ClubPlayer, Match } from '../../core/models';

interface BoardEdit {
  position: number;
  isDoubles: boolean;
  homePlayerIds: (number | null)[];
  awayPlayerIds: (number | null)[];
}

@Component({
  selector: 'app-encounter-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="container">
      @if (loading) {
        <div class="loading">Chargement...</div>
      }

      @if (encounter) {
        <a class="back" [routerLink]="['/interclubs', encounter.championshipId]">← {{ encounter.championshipName }}</a>

        <div class="scoreboard">
          <span class="club home">{{ encounter.homeClubName }}</span>
          <span class="big-score">{{ encounter.homeScore }} - {{ encounter.awayScore }}</span>
          <span class="club away">{{ encounter.awayClubName }}</span>
        </div>
        <p class="meta">
          Journée {{ encounter.round }}
          · <span class="badge" [class]="'status-badge-' + encounter.status">{{ getStatusLabel() }}</span>
        </p>

        <div class="boards">
          @for (board of encounter.boards; track board.position) {
            <div class="board-card" [class.doubles]="board.isDoubles">
              <div class="board-header">
                <span class="board-label">
                  {{ board.isDoubles ? 'Double' : 'Simple' }} {{ getBoardNumber(board.position, board.isDoubles) }}
                </span>
                @if (board.match?.status === MatchStatus.Completed) {
                  <span class="board-done">✓</span>
                }
              </div>

              @if (isEditingBoard(board.position)) {
                <!-- Composition admin -->
                <div class="lineup-edit">
                  <div class="side-select">
                    <span class="side-label">{{ encounter.homeClubName }}</span>
                    @for (slot of getEdit(board.position).homePlayerIds; track $index; let i = $index) {
                      <select [(ngModel)]="getEdit(board.position).homePlayerIds[i]">
                        <option [ngValue]="null">Joueur...</option>
                        @for (player of encounter.homeRoster; track player.playerId) {
                          <option [ngValue]="player.playerId">{{ player.name }}</option>
                        }
                      </select>
                    }
                  </div>
                  <span class="vs">vs</span>
                  <div class="side-select">
                    <span class="side-label">{{ encounter.awayClubName }}</span>
                    @for (slot of getEdit(board.position).awayPlayerIds; track $index; let i = $index) {
                      <select [(ngModel)]="getEdit(board.position).awayPlayerIds[i]">
                        <option [ngValue]="null">Joueur...</option>
                        @for (player of encounter.awayRoster; track player.playerId) {
                          <option [ngValue]="player.playerId">{{ player.name }}</option>
                        }
                      </select>
                    }
                  </div>
                </div>
              } @else if (board.match) {
                <div class="board-match">
                  <span class="player" [class.winner]="board.match.winnerId != null && board.match.winnerId === board.match.player1Id">
                    {{ board.match.player1Name }}
                    @if (board.match.player1Score !== null && board.match.player1Score !== undefined) { ({{ board.match.player1Score }}) }
                  </span>
                  <span class="vs">vs</span>
                  <span class="player" [class.winner]="board.match.winnerId != null && board.match.winnerId === board.match.player2Id">
                    {{ board.match.player2Name }}
                    @if (board.match.player2Score !== null && board.match.player2Score !== undefined) { ({{ board.match.player2Score }}) }
                  </span>
                </div>

                @if (authService.isAdmin()) {
                  <div class="board-actions">
                    @if (board.match.status !== MatchStatus.Completed) {
                      <a [routerLink]="['/matches', board.match.id, 'play']" class="play-btn">Jouer</a>
                      <div class="score-input">
                        <input type="number" [(ngModel)]="scoreInputs[board.match.id].player1" min="0" placeholder="S1">
                        <input type="number" [(ngModel)]="scoreInputs[board.match.id].player2" min="0" placeholder="S2">
                        <button (click)="updateScore(board.match)">Valider</button>
                      </div>
                    } @else {
                      @if (correctingMatchId === board.match.id) {
                        <div class="score-input">
                          <input type="number" [(ngModel)]="scoreInputs[board.match.id].player1" min="0">
                          <input type="number" [(ngModel)]="scoreInputs[board.match.id].player2" min="0">
                          <button (click)="updateScore(board.match)">Valider</button>
                          <button class="cancel" (click)="correctingMatchId = null">Annuler</button>
                        </div>
                      } @else {
                        <button class="correct-btn" (click)="startCorrection(board.match)">Corriger</button>
                      }
                    }
                  </div>
                }
                @if (board.match.status !== MatchStatus.Completed) {
                  <a [routerLink]="['/matches', board.match.id, 'spectate']" class="spectate-btn">Spectateur</a>
                }
              } @else {
                <p class="not-composed">Non composé</p>
              }
            </div>
          }
        </div>

        @if (authService.isAdmin() && hasEditableBoards()) {
          <div class="lineup-actions">
            @if (editing) {
              <button class="save" (click)="saveLineup()">Enregistrer la composition</button>
              <button class="cancel" (click)="cancelEdit()">Annuler</button>
            } @else {
              <button class="edit" (click)="startEdit()">Composer la rencontre</button>
            }
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .container {
      max-width: 900px;
      margin: 20px auto;
      padding: 20px;
    }
    .back {
      color: #007bff;
      text-decoration: none;
      display: inline-block;
      margin-bottom: 12px;
    }
    .scoreboard {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 24px;
      padding: 18px;
      background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
      color: white;
      border-radius: 10px;
    }
    .scoreboard .club {
      font-size: 1.3em;
      font-weight: 600;
      flex: 1;
    }
    .scoreboard .home { text-align: right; }
    .scoreboard .big-score {
      font-size: 2.2em;
      font-weight: bold;
      color: #ffc107;
    }
    .meta {
      text-align: center;
      color: #666;
      margin: 10px 0 20px;
    }
    .badge {
      padding: 2px 10px;
      border-radius: 10px;
      font-size: 0.85em;
      color: white;
    }
    .status-badge-0 { background: #6c757d; }
    .status-badge-1 { background: #28a745; }
    .status-badge-2 { background: #007bff; }
    .boards {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .board-card {
      background: #f8f9fa;
      border: 1px solid #eee;
      border-left: 4px solid #007bff;
      border-radius: 6px;
      padding: 12px 15px;
    }
    .board-card.doubles { border-left-color: #6f42c1; }
    .board-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 6px;
    }
    .board-label {
      font-size: 0.8em;
      font-weight: 700;
      text-transform: uppercase;
      color: #666;
    }
    .board-done { color: #28a745; font-weight: bold; }
    .board-match {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .board-match .player { font-weight: 600; }
    .board-match .player.winner { color: #28a745; }
    .board-match .vs { color: #999; font-size: 0.85em; }
    .not-composed { color: #999; font-style: italic; margin: 0; }
    .lineup-edit {
      display: flex;
      gap: 15px;
      align-items: flex-start;
      flex-wrap: wrap;
    }
    .side-select {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }
    .side-label { font-size: 0.8em; color: #666; }
    .side-select select {
      padding: 6px;
      border: 1px solid #ddd;
      border-radius: 4px;
      min-width: 180px;
    }
    .lineup-edit .vs { margin-top: 24px; color: #999; }
    .board-actions {
      display: flex;
      gap: 10px;
      align-items: center;
      margin-top: 8px;
    }
    .play-btn {
      padding: 6px 14px;
      background: #28a745;
      color: white;
      text-decoration: none;
      border-radius: 4px;
      font-size: 0.85em;
    }
    .spectate-btn {
      display: inline-block;
      margin-top: 6px;
      color: #007bff;
      font-size: 0.85em;
      text-decoration: none;
    }
    .score-input {
      display: flex;
      gap: 6px;
      align-items: center;
    }
    .score-input input {
      width: 55px;
      padding: 5px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    .score-input button {
      padding: 5px 12px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .score-input .cancel { background: #6c757d; }
    .correct-btn {
      padding: 5px 12px;
      background: transparent;
      color: #856404;
      border: 1px solid #ffc107;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.85em;
    }
    .lineup-actions {
      margin-top: 15px;
      display: flex;
      gap: 10px;
    }
    .lineup-actions .edit, .lineup-actions .save {
      padding: 10px 20px;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .lineup-actions .save { background: #28a745; }
    .lineup-actions .cancel {
      padding: 10px 20px;
      background: #6c757d;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
    .loading { text-align: center; color: #666; padding: 20px; }
  `]
})
export class EncounterDetailComponent implements OnInit {
  encounter: EncounterDetail | null = null;
  scoreInputs: { [matchId: number]: { player1: number; player2: number } } = {};
  correctingMatchId: number | null = null;
  editing = false;
  boardEdits: BoardEdit[] = [];
  loading = false;

  MatchStatus = MatchStatus;
  EncounterStatus = EncounterStatus;

  private destroyRef = inject(DestroyRef);
  private notificationService = inject(NotificationService);
  private encounterId = 0;

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.encounterId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  load() {
    this.loading = true;
    this.apiService.getEncounter(this.encounterId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(encounter => {
      this.encounter = encounter;
      this.loading = false;
      this.editing = false;
      this.boardEdits = [];
      encounter.boards.forEach(b => {
        if (b.match) {
          this.scoreInputs[b.match.id] = { player1: b.match.player1Score ?? 0, player2: b.match.player2Score ?? 0 };
        }
      });
    });
  }

  getBoardNumber(position: number, isDoubles: boolean): number {
    if (!this.encounter) return position;
    return isDoubles ? position - this.encounter.singlesPerEncounter : position;
  }

  getStatusLabel(): string {
    switch (this.encounter?.status) {
      case EncounterStatus.Pending: return 'À venir';
      case EncounterStatus.InProgress: return 'En cours';
      case EncounterStatus.Completed: return 'Terminée';
      default: return '';
    }
  }

  // ----- Composition -----

  hasEditableBoards(): boolean {
    return (this.encounter?.boards ?? []).some(b => !b.match || b.match.status !== MatchStatus.Completed);
  }

  isEditingBoard(position: number): boolean {
    return this.editing && this.boardEdits.some(e => e.position === position);
  }

  getEdit(position: number): BoardEdit {
    return this.boardEdits.find(e => e.position === position)!;
  }

  startEdit() {
    if (!this.encounter) return;
    this.boardEdits = this.encounter.boards
      .filter(b => !b.match || b.match.status !== MatchStatus.Completed)
      .map(b => {
        const slots = b.isDoubles ? 2 : 1;
        // Préremplir avec la composition existante (membres pour les doubles)
        const home: (number | null)[] = [];
        const away: (number | null)[] = [];
        for (let i = 0; i < slots; i++) {
          home.push(b.match?.isDoubles
            ? b.match.team1Members?.[i]?.playerId ?? null
            : (i === 0 ? b.match?.player1Id ?? null : null));
          away.push(b.match?.isDoubles
            ? b.match.team2Members?.[i]?.playerId ?? null
            : (i === 0 ? b.match?.player2Id ?? null : null));
        }
        return { position: b.position, isDoubles: b.isDoubles, homePlayerIds: home, awayPlayerIds: away };
      });
    this.editing = true;
  }

  cancelEdit() {
    this.editing = false;
    this.boardEdits = [];
  }

  saveLineup() {
    // N'envoyer que les boards entièrement composés
    const boards: BoardLineup[] = this.boardEdits
      .filter(e => e.homePlayerIds.every(id => id !== null) && e.awayPlayerIds.every(id => id !== null))
      .map(e => ({
        position: e.position,
        homePlayerIds: e.homePlayerIds as number[],
        awayPlayerIds: e.awayPlayerIds as number[]
      }));

    if (boards.length === 0) {
      this.notificationService.showError('Aucun board entièrement composé');
      return;
    }

    this.apiService.setEncounterLineup(this.encounterId, boards)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Composition enregistrée');
          this.load();
        },
        error: (err) => this.showError(err)
      });
  }

  // ----- Scores -----

  startCorrection(match: Match) {
    this.scoreInputs[match.id] = { player1: match.player1Score ?? 0, player2: match.player2Score ?? 0 };
    this.correctingMatchId = match.id;
  }

  updateScore(match: Match) {
    const scores = this.scoreInputs[match.id];

    if (match.status === MatchStatus.Completed) {
      const warning = 'Corriger le score de ce match ?\n\n'
        + 'Si ce match a été joué via l\'interface de jeu, les statistiques détaillées '
        + 'ne seront pas modifiées : seul le résultat le sera.';
      if (!confirm(warning)) {
        return;
      }
    }

    this.apiService.updateMatchScore(match.id, scores.player1, scores.player2)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Score enregistré');
          this.correctingMatchId = null;
          this.load();
        },
        error: (err) => this.showError(err)
      });
  }

  private showError(err: any) {
    this.notificationService.showError(
      typeof err.error === 'string' ? err.error : (err.error?.message || 'Une erreur est survenue'));
  }
}
