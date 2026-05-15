import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { PlayerListComponent } from './features/players/player-list.component';
import { PlayerStatsComponent } from './features/players/player-stats.component';
import { TournamentListComponent } from './features/tournaments/tournament-list.component';
import { TournamentDetailComponent } from './features/tournaments/tournament-detail.component';
import { MatchPlayComponent } from './features/matches/match-play.component';
import { MatchSpectateComponent } from './features/matches/match-spectate.component';
import { ProfileComponent } from './features/profile/profile.component';

export const routes: Routes = [
  { path: '', redirectTo: '/tournaments', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'profile', component: ProfileComponent },
  { path: 'players', component: PlayerListComponent },
  { path: 'players/:id/stats', component: PlayerStatsComponent },
  { path: 'tournaments', component: TournamentListComponent },
  { path: 'tournaments/:id', component: TournamentDetailComponent },
  { path: 'matches/:id/play', component: MatchPlayComponent },
  { path: 'matches/:id/spectate', component: MatchSpectateComponent }
];
