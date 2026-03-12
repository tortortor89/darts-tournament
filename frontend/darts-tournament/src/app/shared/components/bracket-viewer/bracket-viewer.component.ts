import { Component, Input, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Match, TournamentDetail } from '../../../core/models';

declare const window: any;

@Component({
  selector: 'app-bracket-viewer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div #bracketContainer class="bracket-container"></div>
  `,
  styles: [`
    .bracket-container {
      width: 100%;
      overflow-x: auto;
      padding: 1rem 0;
    }

    :host ::ng-deep .bracket {
      font-family: inherit;
    }

    :host ::ng-deep .match {
      background: #fff;
      border: 1px solid #ddd;
      border-radius: 4px;
    }

    :host ::ng-deep .participant {
      padding: 0.5rem;
    }

    :host ::ng-deep .participant.winner {
      background: #e8f5e9;
      font-weight: bold;
    }

    :host ::ng-deep .score {
      background: #f5f5f5;
      padding: 0.25rem 0.5rem;
      min-width: 2rem;
      text-align: center;
    }
  `]
})
export class BracketViewerComponent implements AfterViewInit, OnChanges {
  @Input() tournament!: TournamentDetail;
  @Input() knockoutOnly = false;

  @ViewChild('bracketContainer') containerRef!: ElementRef<HTMLDivElement>;

  private viewerLoaded = false;

  ngAfterViewInit() {
    this.loadBracketsViewer();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['tournament'] && this.viewerLoaded) {
      this.renderBracket();
    }
  }

  private loadBracketsViewer() {
    // Load CSS
    if (!document.querySelector('link[href*="brackets-viewer"]')) {
      const link = document.createElement('link');
      link.rel = 'stylesheet';
      link.href = 'https://cdn.jsdelivr.net/npm/brackets-viewer@latest/dist/brackets-viewer.min.css';
      document.head.appendChild(link);
    }

    // Load JS
    if (!window.bracketsViewer) {
      const script = document.createElement('script');
      script.src = 'https://cdn.jsdelivr.net/npm/brackets-viewer@latest/dist/brackets-viewer.min.js';
      script.onload = () => {
        this.viewerLoaded = true;
        this.renderBracket();
      };
      document.body.appendChild(script);
    } else {
      this.viewerLoaded = true;
      this.renderBracket();
    }
  }

  private renderBracket() {
    if (!this.tournament || !this.containerRef?.nativeElement || !window.bracketsViewer) {
      return;
    }

    const container = this.containerRef.nativeElement;
    container.innerHTML = '';

    const matches = this.knockoutOnly
      ? this.tournament.matches.filter(m => m.isKnockoutMatch)
      : this.tournament.matches.filter(m => !m.groupId || m.isKnockoutMatch);

    if (matches.length === 0) {
      container.innerHTML = '<p>Aucun match à afficher</p>';
      return;
    }

    const data = this.transformToViewerFormat(matches);

    try {
      window.bracketsViewer.render(data, {
        selector: container,
        participantOriginPlacement: 'before',
        separatedChildCountLabel: true,
        showSlotsOrigin: true,
        showLowerBracketSlotsOrigin: true,
        highlightParticipantOnHover: true,
      });
    } catch (error) {
      console.error('Error rendering bracket:', error);
      this.renderFallback(container, matches);
    }
  }

  private transformToViewerFormat(matches: Match[]) {
    // Get unique participants
    const participantIds = new Set<number>();
    matches.forEach(m => {
      if (m.player1Id) participantIds.add(m.player1Id);
      if (m.player2Id) participantIds.add(m.player2Id);
    });

    const participants = Array.from(participantIds).map(id => {
      const match = matches.find(m => m.player1Id === id || m.player2Id === id);
      const name = match?.player1Id === id ? match.player1Name : match?.player2Name;
      return {
        id,
        name: name || 'TBD',
        tournament_id: this.tournament.id,
      };
    });

    // Determine rounds
    const rounds = [...new Set(matches.map(m => m.round))].sort((a, b) => a - b);
    const minRound = Math.min(...rounds);

    // Transform matches to viewer format
    const transformedMatches = matches.map(m => ({
      id: m.id,
      stage_id: 0,
      group_id: 0,
      round_id: m.round - minRound,
      number: m.position,
      child_count: 0,
      status: m.status === 2 ? 4 : (m.player1Id && m.player2Id ? 2 : 1), // 4=completed, 2=ready, 1=waiting
      opponent1: m.player1Id ? {
        id: m.player1Id,
        score: m.player1Score,
        result: m.winnerId === m.player1Id ? 'win' : (m.winnerId === m.player2Id ? 'loss' : undefined),
      } : null,
      opponent2: m.player2Id ? {
        id: m.player2Id,
        score: m.player2Score,
        result: m.winnerId === m.player2Id ? 'win' : (m.winnerId === m.player1Id ? 'loss' : undefined),
      } : null,
    }));

    return {
      participants,
      stages: [{
        id: 0,
        tournament_id: this.tournament.id,
        name: this.knockoutOnly ? 'Phase éliminatoire' : this.tournament.name,
        type: 'single_elimination',
        number: 1,
        settings: {},
      }],
      matches: transformedMatches,
      match_game: [],
    };
  }

  private renderFallback(container: HTMLElement, matches: Match[]) {
    // Simple fallback rendering if brackets-viewer fails
    const rounds = [...new Set(matches.map(m => m.round))].sort((a, b) => a - b);

    let html = '<div class="fallback-bracket">';
    for (const round of rounds) {
      const roundMatches = matches.filter(m => m.round === round);
      html += `<div class="round"><h4>Round ${round}</h4>`;
      for (const match of roundMatches) {
        const p1Class = match.winnerId === match.player1Id ? 'winner' : '';
        const p2Class = match.winnerId === match.player2Id ? 'winner' : '';
        html += `
          <div class="match-card">
            <div class="player ${p1Class}">${match.player1Name || 'TBD'} ${match.player1Score ?? ''}</div>
            <div class="player ${p2Class}">${match.player2Name || 'TBD'} ${match.player2Score ?? ''}</div>
          </div>
        `;
      }
      html += '</div>';
    }
    html += '</div>';

    container.innerHTML = html;
  }
}
