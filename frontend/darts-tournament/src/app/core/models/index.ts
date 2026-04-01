export interface User {
  id: number;
  username: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  username: string;
}

export interface Player {
  id: number;
  firstName: string;
  lastName: string;
  nickname?: string;
  createdAt: Date;
}

export interface Tournament {
  id: number;
  name: string;
  format: TournamentFormat;
  status: TournamentStatus;
  startDate?: Date;
  createdAt: Date;
  playerCount: number;
  numberOfGroups?: number;
  playersPerGroup?: number;
  qualifiersPerGroup?: number;
  hasKnockoutPhase: boolean;
  allowBracketReset: boolean;
}

export interface TournamentDetail extends Omit<Tournament, 'playerCount'> {
  numberOfGroups?: number;
  playersPerGroup?: number;
  qualifiersPerGroup?: number;
  hasKnockoutPhase: boolean;
  allowBracketReset: boolean;
  players: TournamentPlayer[];
  groups: Group[];
  matches: Match[];
}

export interface TournamentPlayer {
  playerId: number;
  firstName: string;
  lastName: string;
  nickname?: string;
  seed?: number;
  groupId?: number;
}

export interface Group {
  id: number;
  name: string;
  players: TournamentPlayer[];
}

export interface Match {
  id: number;
  tournamentId: number;
  groupId?: number;
  round: number;
  position: number;
  player1Id?: number;
  player1Name?: string;
  player2Id?: number;
  player2Name?: string;
  player1Score?: number;
  player2Score?: number;
  winnerId?: number;
  status: MatchStatus;
  scheduledAt?: Date;
  isKnockoutMatch: boolean;
  bracketType: BracketType;
  isBracketReset: boolean;
}

export interface GroupStanding {
  groupId: number;
  groupName: string;
  standings: PlayerStanding[];
}

export interface PlayerStanding {
  playerId: number;
  playerName: string;
  played: number;
  won: number;
  lost: number;
  pointsFor: number;
  pointsAgainst: number;
  pointsDiff: number;
  points: number;
  rank: number;
}

export enum TournamentFormat {
  SingleElimination = 0,
  RoundRobin = 1,
  GroupStage = 2,
  DoubleElimination = 3
}

export enum BracketType {
  None = 0,
  Winners = 1,
  Losers = 2,
  GrandFinal = 3
}

export enum TournamentStatus {
  Draft = 0,
  InProgress = 1,
  Completed = 2
}

export enum MatchStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2
}
