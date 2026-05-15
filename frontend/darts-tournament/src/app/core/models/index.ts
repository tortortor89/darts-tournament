export interface User {
  id: number;
  username: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  username: string;
  role: string;
  linkedPlayerId?: number;
  linkedPlayerName?: string;
}

export enum UserRole {
  User = 'User',
  Admin = 'Admin'
}

export interface Player {
  id: number;
  firstName: string;
  lastName: string;
  nickname?: string;
  createdAt: Date;
  userId?: number;
  linkedUsername?: string;
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
  status: RegistrationStatus;
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

export enum RegistrationStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

// Match Session (Live Game)
export enum GameMode {
  FiveOhOne = 501
}

export enum MatchSessionStatus {
  Configuration = 0,
  InProgress = 1,
  Finished = 2,
  Cancelled = 3
}

export interface MatchSession {
  id: number;
  matchId: number;
  legsToWin: number;
  gameMode: GameMode;
  status: MatchSessionStatus;
  player1: PlayerSessionInfo;
  player2: PlayerSessionInfo;
  currentPlayerId: number;
  currentLeg: number;
  currentLegThrows: ThrowInfo[];
  createdAt: Date;
  startedAt?: Date;
  finishedAt?: Date;
  trackDoubles: boolean;
}

export interface PlayerSessionInfo {
  playerId: number;
  name: string;
  legsWon: number;
  currentScore: number;
  isStarting: boolean;
}

export interface ThrowInfo {
  id: number;
  playerId: number;
  playerName: string;
  legNumber: number;
  throwNumber: number;
  score: number;
  dart1?: string;
  dart2?: string;
  dart3?: string;
  remainingScore: number;
  isCheckout: boolean;
  isBust: boolean;
  createdAt: Date;
}

export interface MatchSessionSpectator {
  matchId: number;
  tournamentName: string;
  legsToWin: number;
  status: MatchSessionStatus;
  player1: PlayerSpectatorInfo;
  player2: PlayerSpectatorInfo;
  currentPlayerId: number;
  currentLeg: number;
  legsHistory: LegSummary[];
}

export interface PlayerSpectatorInfo {
  playerId: number;
  name: string;
  legsWon: number;
  currentScore: number;
}

export interface LegSummary {
  legNumber: number;
  winnerId: number;
  winnerName: string;
  winnerDartsThrown: number;
  winnerAverage?: number;
}

export interface StartMatchSessionRequest {
  legsToWin: number;
  startingPlayerId: number;
  trackDoubles?: boolean;
}

export interface RecordThrowRequest {
  score: number;
  dart1?: string;
  dart2?: string;
  dart3?: string;
}

// Statistics
export interface MatchStats {
  player1Stats: PlayerStats;
  player2Stats: PlayerStats;
}

export interface PlayerStats {
  playerId: number;
  name: string;
  threeDartAverage: number;
  checkoutPercentage?: number;
  first9Average?: number;
  highestCheckout?: number;
  totalDartsThrown: number;
  totalScore: number;
  legsWon: number;
  checkoutAttempts: number;
  checkoutSuccesses: number;
  highestScore?: number;
  oneEighties: number;
}

// SignalR Events
export interface ThrowRecordedEvent {
  matchId: number;
  throw: ThrowInfo;
  player1CurrentScore: number;
  player2CurrentScore: number;
  currentPlayerId: number;
  stats: MatchStats;
}

export interface LegWonEvent {
  matchId: number;
  legNumber: number;
  winnerId: number;
  winnerName: string;
  player1LegsWon: number;
  player2LegsWon: number;
  newCurrentLeg: number;
  legSummary: LegSummary;
}

export interface MatchFinishedEvent {
  matchId: number;
  winnerId: number;
  winnerName: string;
  player1LegsWon: number;
  player2LegsWon: number;
  finalStats: MatchStats;
}

export interface SessionStartedEvent {
  matchId: number;
  session: MatchSession;
}
