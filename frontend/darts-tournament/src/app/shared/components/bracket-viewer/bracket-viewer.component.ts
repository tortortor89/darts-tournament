import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Match, TournamentDetail } from '../../../core/models';

interface BracketMatch {
  match: Match;
  roundName: string;
}

interface BracketRound {
  name: string;
  matches: Match[];
}

@Component({
  selector: 'app-bracket-viewer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bracket-wrapper">
      <div class="bracket">
        @for (round of rounds; track round.name) {
          <div class="round">
            <div class="round-header">{{ round.name }}</div>
            <div class="round-matches">
              @for (match of round.matches; track match.id) {
                <div class="match-wrapper">
                  <div class="match" [class.completed]="match.status === 2">
                    <div class="player" [class.winner]="match.winnerId === match.player1Id" [class.bye]="!match.player2Id && match.winnerId === match.player1Id">
                      <span class="name">{{ match.player1Name || 'TBD' }}</span>
                      <span class="score">{{ match.player1Score ?? '-' }}</span>
                    </div>
                    <div class="player" [class.winner]="match.winnerId === match.player2Id">
                      <span class="name">{{ match.player2Name || 'TBD' }}</span>
                      <span class="score">{{ match.player2Score ?? '-' }}</span>
                    </div>
                  </div>
                </div>
              }
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .bracket-wrapper {
      overflow-x: auto;
      padding: 20px 0;
    }

    .bracket {
      display: flex;
      gap: 40px;
      min-width: max-content;
    }

    .round {
      display: flex;
      flex-direction: column;
      min-width: 200px;
    }

    .round-header {
      text-align: center;
      font-weight: 600;
      padding: 8px 16px;
      background: rgba(0,0,0,0.1);
      border-radius: 4px;
      margin-bottom: 15px;
      font-size: 0.9em;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .round-matches {
      display: flex;
      flex-direction: column;
      justify-content: space-around;
      flex: 1;
      gap: 10px;
    }

    .match-wrapper {
      display: flex;
      align-items: center;
      flex: 1;
      min-height: 70px;
    }

    .match {
      width: 100%;
      border: 2px solid rgba(0,0,0,0.15);
      border-radius: 6px;
      overflow: hidden;
      background: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .match:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.15);
    }

    .match.completed {
      border-color: #28a745;
    }

    .player {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 12px;
      border-bottom: 1px solid #eee;
      background: #fafafa;
      transition: background 0.2s;
    }

    .player:last-child {
      border-bottom: none;
    }

    .player.winner {
      background: #d4edda;
      font-weight: 600;
    }

    .player.bye {
      background: #fff3cd;
    }

    .player .name {
      flex: 1;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      color: #333;
    }

    .player .score {
      min-width: 30px;
      text-align: center;
      font-weight: 600;
      color: #666;
      background: rgba(0,0,0,0.05);
      padding: 2px 8px;
      border-radius: 3px;
      margin-left: 10px;
    }

    .player.winner .score {
      background: #28a745;
      color: white;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .bracket {
        gap: 20px;
      }
      .round {
        min-width: 160px;
      }
      .player {
        padding: 6px 8px;
        font-size: 0.9em;
      }
    }
  `]
})
export class BracketViewerComponent implements OnChanges {
  @Input() tournament!: TournamentDetail;
  @Input() knockoutOnly = false;

  rounds: BracketRound[] = [];

  ngOnChanges(changes: SimpleChanges) {
    if (changes['tournament']) {
      this.buildBracket();
    }
  }

  private buildBracket() {
    if (!this.tournament) {
      this.rounds = [];
      return;
    }

    const matches = this.knockoutOnly
      ? this.tournament.matches.filter(m => m.isKnockoutMatch)
      : this.tournament.matches.filter(m => !m.groupId || m.isKnockoutMatch);

    if (matches.length === 0) {
      this.rounds = [];
      return;
    }

    // Group by round
    const roundNumbers = [...new Set(matches.map(m => m.round))].sort((a, b) => a - b);

    this.rounds = roundNumbers.map(roundNum => {
      const roundMatches = matches
        .filter(m => m.round === roundNum)
        .sort((a, b) => a.position - b.position);

      return {
        name: this.getRoundName(roundNum, roundNumbers, roundMatches.length),
        matches: roundMatches
      };
    });
  }

  private getRoundName(round: number, allRounds: number[], matchCount: number): string {
    const maxRound = Math.max(...allRounds);

    if (round === maxRound) return 'Finale';
    if (matchCount === 1) return 'Finale';
    if (matchCount === 2) return 'Demi-finales';
    if (matchCount === 4) return 'Quarts';
    if (matchCount === 8) return '8èmes';
    if (matchCount === 16) return '16èmes';
    return `Tour ${round}`;
  }
}
