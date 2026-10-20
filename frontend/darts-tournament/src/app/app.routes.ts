import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { PlayerListComponent } from './features/players/player-list.component';
import { PlayerStatsComponent } from './features/players/player-stats.component';
import { TournamentListComponent } from './features/tournaments/tournament-list.component';
import { TournamentDetailComponent } from './features/tournaments/tournament-detail.component';
import { CircuitListComponent } from './features/circuits/circuit-list.component';
import { CircuitDetailComponent } from './features/circuits/circuit-detail.component';
import { InterclubListComponent } from './features/interclubs/interclub-list.component';
import { InterclubDetailComponent } from './features/interclubs/interclub-detail.component';
import { EncounterDetailComponent } from './features/interclubs/encounter-detail.component';
import { MatchPlayComponent } from './features/matches/match-play.component';
import { MatchSpectateComponent } from './features/matches/match-spectate.component';
import { ProfileComponent } from './features/profile/profile.component';
import { TvLobbyComponent } from './features/tv/tv-lobby.component';
import { SeasonListComponent } from './features/seasons/season-list.component';
import { SeasonDetailComponent } from './features/seasons/season-detail.component';
import { featureGuard } from './core/guards/feature.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/tournaments', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'profile', component: ProfileComponent },
  { path: 'players', component: PlayerListComponent },
  { path: 'players/:id/stats', component: PlayerStatsComponent },
  { path: 'tournaments', component: TournamentListComponent },
  { path: 'tournaments/:id', component: TournamentDetailComponent },
  { path: 'seasons', component: SeasonListComponent, canActivate: [featureGuard('seasons')] },
  { path: 'seasons/:id', component: SeasonDetailComponent, canActivate: [featureGuard('seasons')] },
  { path: 'circuits', component: CircuitListComponent, canActivate: [featureGuard('circuits')] },
  { path: 'circuits/:id', component: CircuitDetailComponent, canActivate: [featureGuard('circuits')] },
  { path: 'interclubs', component: InterclubListComponent, canActivate: [featureGuard('interclubs')] },
  { path: 'interclubs/encounters/:id', component: EncounterDetailComponent, canActivate: [featureGuard('interclubs')] },
  { path: 'interclubs/:id', component: InterclubDetailComponent, canActivate: [featureGuard('interclubs')] },
  { path: 'matches/:id/play', component: MatchPlayComponent },
  { path: 'matches/:id/spectate', component: MatchSpectateComponent },
  { path: 'tv', component: TvLobbyComponent, canActivate: [featureGuard('tv')] }
];
