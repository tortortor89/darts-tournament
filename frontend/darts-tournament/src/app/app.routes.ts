import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { PlayerListComponent } from './features/players/player-list.component';
import { TournamentListComponent } from './features/tournaments/tournament-list.component';
import { TournamentDetailComponent } from './features/tournaments/tournament-detail.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/tournaments', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'players', component: PlayerListComponent, canActivate: [authGuard] },
  { path: 'tournaments', component: TournamentListComponent, canActivate: [authGuard] },
  { path: 'tournaments/:id', component: TournamentDetailComponent, canActivate: [authGuard] }
];
