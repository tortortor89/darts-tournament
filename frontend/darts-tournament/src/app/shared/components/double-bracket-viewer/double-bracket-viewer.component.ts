import { Component, Input, OnChanges, SimpleChanges, AfterViewInit, OnDestroy, ElementRef, ViewChild, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Match, TournamentDetail, BracketType, MatchStatus } from '../../../core/models';

interface BracketRound {
  name: string;
  matches: Match[];
}

@Component({
  selector: 'app-double-bracket-viewer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="double-bracket-container">
      <!-- Winner's Bracket -->
      <div class="bracket-section winners-section">
        <h4 class="section-title winners-title">Tableau Principal (Winner's Bracket)</h4>
        <div class="bracket-wrapper" #winnersBracketWrapper>
          <div class="bracket-with-svg">
            <svg class="connector-svg" #winnersSvg></svg>
            <div class="bracket">
              @for (round of winnersRounds; track round.name; let roundIdx = $index) {
                <div class="round" [attr.data-round]="roundIdx">
                  <div class="round-header winners-header">{{ round.name }}</div>
                  <div class="round-matches">
                    @for (match of round.matches; track match.id; let matchIdx = $index) {
                      <div class="match-wrapper">
                        <div class="match"
                             [class.completed]="match.status === 2"
                             [attr.data-bracket]="'winners'"
                             [attr.data-round]="roundIdx"
                             [attr.data-position]="matchIdx">
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
        </div>
      </div>

      <!-- Loser's Bracket -->
      <div class="bracket-section losers-section">
        <h4 class="section-title losers-title">Tableau de Repechage (Loser's Bracket)</h4>
        <div class="bracket-wrapper" #losersBracketWrapper>
          <div class="bracket-with-svg">
            <svg class="connector-svg" #losersSvg></svg>
            <div class="bracket">
              @for (round of losersRounds; track round.name; let roundIdx = $index) {
                <div class="round" [attr.data-round]="roundIdx">
                  <div class="round-header losers-header">{{ round.name }}</div>
                  <div class="round-matches">
                    @for (match of round.matches; track match.id; let matchIdx = $index) {
                      <div class="match-wrapper">
                        <div class="match"
                             [class.completed]="match.status === 2"
                             [attr.data-bracket]="'losers'"
                             [attr.data-round]="roundIdx"
                             [attr.data-position]="matchIdx">
                          <div class="player" [class.winner]="match.winnerId === match.player1Id">
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
        </div>
      </div>

      <!-- Grand Final -->
      @if (grandFinalMatches.length > 0) {
        <div class="bracket-section grand-final-section">
          <h4 class="section-title grand-final-title">Grande Finale</h4>
          <div class="grand-final-matches">
            @for (match of grandFinalMatches; track match.id) {
              <div class="grand-final-match" [class.bracket-reset]="match.isBracketReset" [class.skipped]="match.isBracketReset && isResetSkipped(match)">
                <div class="match-label">
                  @if (match.isBracketReset) {
                    @if (isResetSkipped(match)) {
                      Match Decisif (non necessaire)
                    } @else {
                      Match Decisif
                    }
                  } @else {
                    Finale
                  }
                </div>
                <div class="match grand-final" [class.completed]="match.status === 2">
                  <div class="player" [class.winner]="match.winnerId === match.player1Id">
                    <span class="name">{{ match.player1Name || 'Champion W' }}</span>
                    <span class="score">{{ match.player1Score ?? '-' }}</span>
                  </div>
                  <div class="player" [class.winner]="match.winnerId === match.player2Id">
                    <span class="name">{{ match.player2Name || 'Champion L' }}</span>
                    <span class="score">{{ match.player2Score ?? '-' }}</span>
                  </div>
                </div>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .double-bracket-container {
      display: flex;
      flex-direction: column;
      gap: 30px;
    }

    .bracket-section {
      background: var(--surface-alt);
      border-radius: 8px;
      padding: 20px;
    }

    .section-title {
      margin: 0 0 15px 0;
      font-size: 1.1em;
      font-weight: 600;
    }

    .winners-title {
      color: var(--bracket-winners);
    }

    .losers-title {
      color: var(--bracket-losers);
    }

    .grand-final-title {
      color: var(--accent);
      text-shadow: 0 0 1px rgba(0,0,0,0.3);
    }

    .bracket-wrapper {
      overflow-x: auto;
      padding: 10px 0;
    }

    .bracket-with-svg {
      position: relative;
      display: inline-block;
    }

    .connector-svg {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
      z-index: 0;
    }

    .bracket {
      display: flex;
      gap: 30px;
      min-width: max-content;
      position: relative;
      z-index: 1;
    }

    .round {
      display: flex;
      flex-direction: column;
      min-width: 180px;
    }

    .round-header {
      text-align: center;
      font-weight: 600;
      padding: 6px 12px;
      border-radius: 4px;
      margin-bottom: 12px;
      font-size: 0.85em;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: white;
    }

    .winners-header {
      background: linear-gradient(135deg, var(--bracket-winners), color-mix(in srgb, var(--bracket-winners) 80%, black));
    }

    .losers-header {
      background: linear-gradient(135deg, var(--bracket-losers), color-mix(in srgb, var(--bracket-losers) 80%, black));
    }

    .round-matches {
      display: flex;
      flex-direction: column;
      justify-content: space-around;
      flex: 1;
      gap: 8px;
    }

    .match-wrapper {
      display: flex;
      align-items: center;
      flex: 1;
      min-height: 60px;
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
      border-color: var(--success);
    }

    .player {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 6px 10px;
      border-bottom: 1px solid var(--border-light);
      background: var(--surface-alt);
      transition: background 0.2s;
    }

    .player:last-child {
      border-bottom: none;
    }

    .player.winner {
      background: color-mix(in srgb, var(--success) 15%, transparent);
      font-weight: 600;
    }

    .player.bye {
      background: var(--accent-soft);
    }

    .player .name {
      flex: 1;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      color: var(--text);
      font-size: 0.9em;
    }

    .player .score {
      min-width: 28px;
      text-align: center;
      font-weight: 600;
      color: var(--text-muted);
      background: rgba(0,0,0,0.05);
      padding: 2px 6px;
      border-radius: 3px;
      margin-left: 8px;
      font-size: 0.9em;
    }

    .player.winner .score {
      background: var(--action);
      color: white;
    }

    /* Grand Final Section */
    .grand-final-section {
      background: linear-gradient(135deg, var(--accent-soft), var(--accent-soft));
      border: 2px solid var(--accent);
    }

    .grand-final-matches {
      display: flex;
      gap: 20px;
      justify-content: center;
      flex-wrap: wrap;
    }

    .grand-final-match {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    .match-label {
      font-weight: 600;
      font-size: 0.9em;
      color: var(--accent-text);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .grand-final-match .match {
      min-width: 220px;
    }

    .grand-final-match .match.grand-final {
      border-color: var(--accent);
      box-shadow: 0 4px 12px color-mix(in srgb, var(--accent) 30%, transparent);
    }

    .grand-final-match.bracket-reset .match-label {
      color: var(--text-muted);
    }

    .grand-final-match.bracket-reset .match {
      border-style: dashed;
      border-color: var(--secondary);
    }

    .grand-final-match.skipped {
      opacity: 0.5;
    }

    .grand-final-match.skipped .match-label {
      font-style: italic;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .bracket {
        gap: 15px;
      }
      .round {
        min-width: 150px;
      }
      .player {
        padding: 5px 8px;
        font-size: 0.85em;
      }
    }

    @media (min-width: 1200px) {
      .double-bracket-container {
        flex-direction: row;
        flex-wrap: wrap;
      }

      .winners-section,
      .losers-section {
        flex: 1;
        min-width: 45%;
      }

      .grand-final-section {
        flex: 0 0 100%;
      }
    }
  `]
})
export class DoubleBracketViewerComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input() tournament!: TournamentDetail;

  @ViewChild('winnersBracketWrapper') winnersBracketWrapper!: ElementRef<HTMLDivElement>;
  @ViewChild('losersBracketWrapper') losersBracketWrapper!: ElementRef<HTMLDivElement>;
  @ViewChild('winnersSvg') winnersSvg!: ElementRef<SVGSVGElement>;
  @ViewChild('losersSvg') losersSvg!: ElementRef<SVGSVGElement>;

  winnersRounds: BracketRound[] = [];
  losersRounds: BracketRound[] = [];
  grandFinalMatches: Match[] = [];

  private resizeObserver: ResizeObserver | null = null;
  private isViewInitialized = false;

  constructor(private ngZone: NgZone) {}

  ngOnChanges(changes: SimpleChanges) {
    if (changes['tournament']) {
      this.buildBrackets();
      if (this.isViewInitialized) {
        setTimeout(() => this.drawAllConnectors(), 0);
      }
    }
  }

  ngAfterViewInit() {
    this.isViewInitialized = true;
    setTimeout(() => this.drawAllConnectors(), 0);

    this.ngZone.runOutsideAngular(() => {
      this.resizeObserver = new ResizeObserver(() => {
        this.drawAllConnectors();
      });

      if (this.winnersBracketWrapper?.nativeElement) {
        this.resizeObserver.observe(this.winnersBracketWrapper.nativeElement);
      }
      if (this.losersBracketWrapper?.nativeElement) {
        this.resizeObserver.observe(this.losersBracketWrapper.nativeElement);
      }
    });
  }

  ngOnDestroy() {
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  private drawAllConnectors() {
    if (this.winnersSvg?.nativeElement && this.winnersBracketWrapper?.nativeElement) {
      this.drawConnectors(this.winnersBracketWrapper.nativeElement, this.winnersSvg.nativeElement, 'var(--bracket-winners)');
    }
    if (this.losersSvg?.nativeElement && this.losersBracketWrapper?.nativeElement) {
      this.drawConnectors(this.losersBracketWrapper.nativeElement, this.losersSvg.nativeElement, 'var(--bracket-losers)');
    }
  }

  private drawConnectors(wrapper: HTMLElement, svg: SVGSVGElement, color: string) {
    // Clear existing paths
    svg.innerHTML = '';

    const bracketContainer = wrapper.querySelector('.bracket-with-svg');
    if (!bracketContainer) return;

    const containerRect = bracketContainer.getBoundingClientRect();

    // Set SVG dimensions
    svg.setAttribute('width', containerRect.width.toString());
    svg.setAttribute('height', containerRect.height.toString());

    const rounds = wrapper.querySelectorAll('.round');
    if (rounds.length < 2) return;

    // Draw connectors between consecutive rounds
    for (let i = 0; i < rounds.length - 1; i++) {
      const currentRoundMatches = rounds[i].querySelectorAll('.match');
      const nextRoundMatches = rounds[i + 1].querySelectorAll('.match');

      // In a standard bracket, 2 matches from current round feed into 1 match in next round
      for (let j = 0; j < nextRoundMatches.length; j++) {
        const targetMatch = nextRoundMatches[j];
        const sourceMatch1 = currentRoundMatches[j * 2];
        const sourceMatch2 = currentRoundMatches[j * 2 + 1];

        if (sourceMatch1) {
          this.drawConnectorPath(svg, containerRect, sourceMatch1, targetMatch, color);
        }
        if (sourceMatch2) {
          this.drawConnectorPath(svg, containerRect, sourceMatch2, targetMatch, color);
        }
      }
    }
  }

  private drawConnectorPath(svg: SVGSVGElement, containerRect: DOMRect, source: Element, target: Element, color: string) {
    const sourceRect = source.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();

    // Calculate positions relative to the SVG container
    const x1 = sourceRect.right - containerRect.left;
    const y1 = sourceRect.top + sourceRect.height / 2 - containerRect.top;
    const x2 = targetRect.left - containerRect.left;
    const y2 = targetRect.top + targetRect.height / 2 - containerRect.top;

    // Calculate midpoint for the horizontal-vertical-horizontal path
    const midX = x1 + (x2 - x1) / 2;

    // Create path: horizontal from source, vertical connector, horizontal to target
    const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    path.setAttribute('d', `M ${x1} ${y1} H ${midX} V ${y2} H ${x2}`);
    path.style.stroke = color; // via style (et non attribut) pour accepter var(--...)
    path.setAttribute('stroke-width', '2');
    path.setAttribute('fill', 'none');
    path.setAttribute('stroke-opacity', '0.6');

    svg.appendChild(path);
  }

  private buildBrackets() {
    if (!this.tournament) {
      this.winnersRounds = [];
      this.losersRounds = [];
      this.grandFinalMatches = [];
      return;
    }

    const matches = this.tournament.matches;

    // Separate by bracket type
    const winnersMatches = matches.filter(m => m.bracketType === BracketType.Winners);
    const losersMatches = matches.filter(m => m.bracketType === BracketType.Losers);
    this.grandFinalMatches = matches
      .filter(m => m.bracketType === BracketType.GrandFinal)
      .sort((a, b) => a.round - b.round);

    // Build winner's rounds
    this.winnersRounds = this.groupByRound(winnersMatches, 'Winners');

    // Build loser's rounds
    this.losersRounds = this.groupByRound(losersMatches, 'Losers');
  }

  private groupByRound(matches: Match[], bracketName: string): BracketRound[] {
    if (matches.length === 0) return [];

    const roundNumbers = [...new Set(matches.map(m => m.round))].sort((a, b) => a - b);

    return roundNumbers.map(roundNum => {
      const roundMatches = matches
        .filter(m => m.round === roundNum)
        .sort((a, b) => a.position - b.position);

      return {
        name: this.getRoundName(roundNum, roundNumbers, roundMatches.length, bracketName),
        matches: roundMatches
      };
    });
  }

  private getRoundName(round: number, allRounds: number[], matchCount: number, bracket: string): string {
    const maxRound = Math.max(...allRounds);
    const suffix = bracket === 'Winners' ? ' W' : ' L';

    if (round === maxRound) {
      return 'Finale' + suffix;
    }
    if (matchCount === 1) return 'Finale' + suffix;
    if (matchCount === 2) return 'Demi-finales' + suffix;
    if (matchCount === 4) return 'Quarts' + suffix;

    return `Tour ${round}` + suffix;
  }

  isResetSkipped(match: Match): boolean {
    // Bracket reset is skipped if it's completed but has no real scores (winner's bracket champion won GF1)
    return match.status === MatchStatus.Completed &&
           match.player1Score === 0 &&
           match.player2Score === 0;
  }
}
