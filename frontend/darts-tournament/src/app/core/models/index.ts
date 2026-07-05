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
  clubId?: number;
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
  circuitId?: number;
  circuitName?: string;
  isDoubles: boolean;
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
  teams?: TournamentTeam[];
}

// Paire d'un tournoi en double
export interface TournamentTeam {
  id: number;
  player1Id: number;
  player1Name: string;
  player2Id: number;
  player2Name: string;
  name: string;
  seed?: number;
  groupId?: number;
}

export interface TeamMemberInfo {
  playerId: number;
  name: string;
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
  teams?: TournamentTeam[];
}

// En double : player1Id/player2Id/winnerId portent des ids d'ÉQUIPE et les noms
// le label de la paire. Ne pas utiliser ces ids pour naviguer vers un profil
// joueur quand isDoubles est vrai.
// Un match appartient soit à un tournoi (tournamentId), soit à une rencontre
// interclubs (encounterId + encounterLabel + valeurs par défaut du championnat).
export interface Match {
  id: number;
  tournamentId?: number;
  encounterId?: number;
  encounterLabel?: string;
  defaultLegsToWin?: number;
  defaultGameMode?: GameMode;
  defaultDoubleOut?: boolean;
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
  isDoubles: boolean;
  team1Members?: TeamMemberInfo[];
  team2Members?: TeamMemberInfo[];
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

// Circuits
export interface CircuitPointsRule {
  minRank: number;
  maxRank: number;
  points: number;
}

export interface Circuit {
  id: number;
  name: string;
  description?: string;
  participationPoints: number;
  createdAt: Date;
  tournamentCount: number;
  completedTournamentCount: number;
  pointsRules: CircuitPointsRule[];
}

export interface CircuitDetail {
  id: number;
  name: string;
  description?: string;
  participationPoints: number;
  createdAt: Date;
  pointsRules: CircuitPointsRule[];
  tournaments: Tournament[];
}

export interface CircuitTournamentPoints {
  tournamentId: number;
  tournamentName: string;
  finalRank: number;
  points: number;
}

export interface CircuitStanding {
  playerId: number;
  playerName: string;
  tournamentsPlayed: number;
  totalPoints: number;
  rank: number;
  details: CircuitTournamentPoints[];
}

// Interclubs
export interface Club {
  id: number;
  name: string;
  createdAt: Date;
  playerCount: number;
}

export interface ClubDetail {
  id: number;
  name: string;
  createdAt: Date;
  players: ClubPlayer[];
}

export interface ClubPlayer {
  playerId: number;
  name: string;
  nickname?: string;
}

export enum ChampionshipStatus {
  Draft = 0,
  InProgress = 1,
  Completed = 2
}

export enum EncounterStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2
}

export interface InterclubChampionship {
  id: number;
  name: string;
  status: ChampionshipStatus;
  singlesPerEncounter: number;
  doublesPerEncounter: number;
  legsToWin: number;
  gameMode: GameMode;
  doubleOut: boolean;
  pointsForWin: number;
  pointsForDraw: number;
  pointsForLoss: number;
  createdAt: Date;
  clubCount: number;
}

export interface InterclubChampionshipDetail extends Omit<InterclubChampionship, 'clubCount'> {
  clubs: ChampionshipClub[];
}

export interface ChampionshipClub {
  clubId: number;
  clubName: string;
  roster: ClubPlayer[];
}

export interface EncounterSummary {
  id: number;
  round: number;
  homeClubId: number;
  homeClubName: string;
  awayClubId: number;
  awayClubName: string;
  scheduledAt?: Date;
  status: EncounterStatus;
  homeScore: number;
  awayScore: number;
}

export interface CalendarRound {
  round: number;
  encounters: EncounterSummary[];
}

export interface EncounterDetail {
  id: number;
  championshipId: number;
  championshipName: string;
  round: number;
  homeClubId: number;
  homeClubName: string;
  awayClubId: number;
  awayClubName: string;
  scheduledAt?: Date;
  status: EncounterStatus;
  homeScore: number;
  awayScore: number;
  singlesPerEncounter: number;
  doublesPerEncounter: number;
  homeRoster: ClubPlayer[];
  awayRoster: ClubPlayer[];
  boards: EncounterBoard[];
}

export interface EncounterBoard {
  position: number;
  isDoubles: boolean;
  match: Match | null;
}

export interface BoardLineup {
  position: number;
  homePlayerIds: number[];
  awayPlayerIds: number[];
}

export interface InterclubStanding {
  clubId: number;
  clubName: string;
  played: number;
  wins: number;
  draws: number;
  losses: number;
  points: number;
  matchesWon: number;
  matchesLost: number;
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
  // Pour les modes x01, la valeur correspond au score de départ
  Cricket = 1,
  ThreeOhOne = 301,
  FiveOhOne = 501
}

export function isX01(mode: GameMode): boolean {
  return mode === GameMode.FiveOhOne || mode === GameMode.ThreeOhOne;
}

export function startingScore(mode: GameMode): number {
  return isX01(mode) ? mode : 0;
}

export enum MatchSessionStatus {
  Configuration = 0,
  InProgress = 1,
  Finished = 2,
  Cancelled = 3
}

// En double : player1/player2 sont les ÉQUIPES (playerId = id d'équipe, name =
// label de paire). currentPlayerId = lanceur individuel, currentSideId = côté au trait.
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
  cricketState?: CricketDisplayState;
  doubleOut: boolean;
  isDoubles: boolean;
  currentSideId: number;
  currentThrowerName?: string;
}

export interface PlayerSessionInfo {
  playerId: number;
  name: string;
  legsWon: number;
  currentScore: number;
  isStarting: boolean;
  members?: TeamMemberInfo[];
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
  gameMode: GameMode;
  status: MatchSessionStatus;
  player1: PlayerSpectatorInfo;
  player2: PlayerSpectatorInfo;
  currentPlayerId: number;
  currentLeg: number;
  legsHistory: LegSummary[];
  cricketState?: CricketDisplayState;
  isDoubles: boolean;
  currentSideId: number;
  currentThrowerName?: string;
}

export interface PlayerSpectatorInfo {
  playerId: number;
  name: string;
  legsWon: number;
  currentScore: number;
  members?: TeamMemberInfo[];
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
  startingPlayerId?: number;   // simple uniquement
  trackDoubles?: boolean;
  gameMode?: GameMode;
  doubleOut?: boolean;
  // Double uniquement : équipe qui commence + ordre de passage de chaque paire
  startingTeamId?: number;
  side1PlayerOrder?: number[];
  side2PlayerOrder?: number[];
}

export interface RecordThrowRequest {
  score: number;
  dart1?: string;
  dart2?: string;
  dart3?: string;
  dartsUsed?: number;
  doublesAttempted?: number;
}

// Statistics
// En double : player1Stats/player2Stats = stats agrégées de chaque équipe,
// memberStats = détail individuel de chaque lanceur
export interface MatchStats {
  player1Stats: PlayerStats;
  player2Stats: PlayerStats;
  player1MemberStats?: PlayerStats[];
  player2MemberStats?: PlayerStats[];
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
  marksPerRound?: number;  // Cricket uniquement
}

// SignalR Events
export interface ThrowRecordedEvent {
  matchId: number;
  throw: ThrowInfo;
  player1CurrentScore: number;
  player2CurrentScore: number;
  currentPlayerId: number;
  stats: MatchStats;
  currentSideId: number;
}

export interface ThrowUndoneEvent {
  matchId: number;
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

export interface CricketTurnRecordedEvent {
  matchId: number;
  turn: CricketTurnResponse;
  player1CurrentScore: number;
  player2CurrentScore: number;
  currentPlayerId: number;
  currentSideId: number;
}

// Player Statistics
export interface PlayerCareerStats {
  playerId: number;
  playerName: string;
  totalMatches: number;
  matchesWon: number;
  matchesLost: number;
  winPercentage: number;
  detailedStats?: PlayerStatsAggregated;
  tournamentsPlayed: number;
  tournamentsWon: number;
  firstMatchDate?: Date;
  lastMatchDate?: Date;
}

export interface PlayerStatsAggregated {
  threeDartAverage: number;
  checkoutPercentage?: number;
  first9Average?: number;
  highestCheckout?: number;
  totalDartsThrown: number;
  totalScore: number;
  totalLegsWon: number;
  totalCheckoutAttempts: number;
  totalCheckoutSuccesses: number;
  highestScore?: number;
  totalOneEighties: number;
  matchesWithStats: number;
}

export interface PlayerTournamentHistoryItem {
  tournamentId: number;
  tournamentName: string;
  format: TournamentFormat;
  status: TournamentStatus;
  startDate?: Date;
  matchesPlayed: number;
  matchesWon: number;
  matchesLost: number;
  result: string;
  groupId?: number;
  groupName?: string;
  groupRank?: number;
}

export interface HeadToHeadRecord {
  opponentId: number;
  opponentName: string;
  matchesPlayed: number;
  matchesWon: number;
  matchesLost: number;
  winPercentage: number;
  totalLegsWon: number;
  totalLegsLost: number;
  lastMatchDate?: Date;
  lastMatchTournament?: string;
}

// TV Lobby
export interface ActiveSessionSummary {
  matchId: number;
  tournamentName: string;
  player1Name: string;
  player2Name: string;
  player1LegsWon: number;
  player2LegsWon: number;
  legsToWin: number;
  gameMode: GameMode;
  currentLeg: number;
  startedAt?: Date;
}

// Cricket interfaces
export interface CricketTargetState {
  target: number;
  hits: number;
  closed: boolean;
}

export interface CricketDisplayState {
  player1Targets: { [target: number]: CricketTargetState };
  player2Targets: { [target: number]: CricketTargetState };
  player1Score: number;
  player2Score: number;
}

export interface CricketHit {
  target: number;
  marks: number;
}

export interface CricketHitResult {
  target: number;
  marks: number;
  pointsScored: number;
  closedTarget: boolean;
}

export interface CricketTurnResponse {
  playerId: number;
  playerName: string;
  hitResults: CricketHitResult[];
  totalPointsScored: number;
  currentState: CricketDisplayState;
}
