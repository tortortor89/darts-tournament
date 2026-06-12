import { Injectable, inject, signal } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import {
  MatchSession,
  ThrowRecordedEvent,
  ThrowUndoneEvent,
  CricketTurnRecordedEvent,
  LegWonEvent,
  MatchFinishedEvent,
  SessionStartedEvent
} from '../models';

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  private authService = inject(AuthService);

  // Event subjects
  private sessionStarted$ = new Subject<SessionStartedEvent>();
  private throwRecorded$ = new Subject<ThrowRecordedEvent>();
  private throwUndone$ = new Subject<ThrowUndoneEvent>();
  private cricketTurnRecorded$ = new Subject<CricketTurnRecordedEvent>();
  private legWon$ = new Subject<LegWonEvent>();
  private matchFinished$ = new Subject<MatchFinishedEvent>();
  private sessionCancelled$ = new Subject<number>();

  // Connection status as signal
  connectionStatus = signal<ConnectionStatus>('disconnected');

  // Public observables
  get onSessionStarted(): Observable<SessionStartedEvent> {
    return this.sessionStarted$.asObservable();
  }

  get onThrowRecorded(): Observable<ThrowRecordedEvent> {
    return this.throwRecorded$.asObservable();
  }

  get onThrowUndone(): Observable<ThrowUndoneEvent> {
    return this.throwUndone$.asObservable();
  }

  get onCricketTurnRecorded(): Observable<CricketTurnRecordedEvent> {
    return this.cricketTurnRecorded$.asObservable();
  }

  get onLegWon(): Observable<LegWonEvent> {
    return this.legWon$.asObservable();
  }

  get onMatchFinished(): Observable<MatchFinishedEvent> {
    return this.matchFinished$.asObservable();
  }

  get onSessionCancelled(): Observable<number> {
    return this.sessionCancelled$.asObservable();
  }

  async startConnection(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connectionStatus.set('connecting');

    const token = this.authService.getToken();

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/hubs/match`, {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerEventHandlers();
    this.registerConnectionHandlers();

    try {
      await this.hubConnection.start();
      this.connectionStatus.set('connected');
      console.log('SignalR Connected');
    } catch (err) {
      console.error('SignalR Connection Error:', err);
      this.connectionStatus.set('disconnected');
      throw err;
    }
  }

  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.connectionStatus.set('disconnected');
    }
  }

  async joinMatch(matchId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('JoinMatch', matchId);
      console.log(`Joined match group: ${matchId}`);
    }
  }

  async leaveMatch(matchId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('LeaveMatch', matchId);
      console.log(`Left match group: ${matchId}`);
    }
  }

  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('SessionStarted', (event: SessionStartedEvent) => {
      this.sessionStarted$.next(event);
    });

    this.hubConnection.on('ThrowRecorded', (event: ThrowRecordedEvent) => {
      this.throwRecorded$.next(event);
    });

    this.hubConnection.on('ThrowUndone', (event: ThrowUndoneEvent) => {
      this.throwUndone$.next(event);
    });

    this.hubConnection.on('CricketTurnRecorded', (event: CricketTurnRecordedEvent) => {
      this.cricketTurnRecorded$.next(event);
    });

    this.hubConnection.on('LegWon', (event: LegWonEvent) => {
      this.legWon$.next(event);
    });

    this.hubConnection.on('MatchFinished', (event: MatchFinishedEvent) => {
      this.matchFinished$.next(event);
    });

    this.hubConnection.on('SessionCancelled', (matchId: number) => {
      this.sessionCancelled$.next(matchId);
    });
  }

  private registerConnectionHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.onreconnecting(() => {
      console.log('SignalR Reconnecting...');
      this.connectionStatus.set('reconnecting');
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR Reconnected');
      this.connectionStatus.set('connected');
    });

    this.hubConnection.onclose(() => {
      console.log('SignalR Disconnected');
      this.connectionStatus.set('disconnected');
    });
  }
}
