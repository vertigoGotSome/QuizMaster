import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { ToastService } from './toast.service';

import type { JeopardyCell, Player, Session, State } from '../models/game.models';

/**
 * Central source of truth for the authenticated player, lobby and current game.
 * The API owns all decisions; clients only request actions and render state.
 */
@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  private connection: HubConnection | null = null;

  readonly me = signal<Player | null>(null);
  readonly players = signal<Player[]>([]);
  readonly session = signal<Session | null>(null);
  readonly state = signal<State | null>(null);
  readonly online = signal(false);
  readonly connectionError = signal('');
  readonly jeopardyCells = signal<JeopardyCell[]>([]);
  private refreshing: Promise<void> | null = null;

  /** Read the logged-in player and the current authoritative game state. */
  async refresh(): Promise<void> {
    if (this.refreshing) return this.refreshing;
    this.refreshing = this.performRefresh();
    try {
      await this.refreshing;
    } finally {
      this.refreshing = null;
    }
  }

  private async performRefresh(): Promise<void> {
    try {
      const user = await firstValueFrom(this.http.get<Player>('/api/auth/me'));
      this.me.set(user);
      this.online.set(true);

      await firstValueFrom(this.http.post('/api/lobby/heartbeat', {}));
      const lobby = await firstValueFrom(
        this.http.get<{ players: Player[]; session: Session }>('/api/lobby'),
      );

      this.players.set(lobby.players);
      this.session.set(lobby.session);
      this.state.set(await firstValueFrom(this.http.get<State>('/api/game/state')));
      if (lobby.session.game === 'jeopardy')
        this.jeopardyCells.set(
          await firstValueFrom(this.http.get<JeopardyCell[]>('/api/jeopardy/board')),
        );
      else this.jeopardyCells.set([]);
      this.connectionError.set('');
    } catch (error) {
      if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
        this.me.set(null);
        this.players.set([]);
        this.session.set(null);
        this.state.set(null);
        this.jeopardyCells.set([]);
      } else {
        this.connectionError.set(
          'Verbindung zum Spielserver unterbrochen. Der letzte Stand bleibt sichtbar.',
        );
      }
      this.online.set(false);
    }
  }

  /** Invoke an action and refresh all derived client state. */
  async post(path: string, body: object = {}): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`/api/${path}`, body));
      await this.refresh();
      this.toast.show('Gespeichert.', 'success');
    } catch (error) {
      this.toast.show(describeApiError(error), 'error');
      throw error;
    }
  }

  /** Invoke an action which returns a response object. */
  async postResult<T>(path: string, body: object = {}): Promise<T> {
    const result = await firstValueFrom(this.http.post<T>(`/api/${path}`, body));
    await this.refresh();
    return result;
  }

  /** Send a host-only multipart upload, then refresh derived state. */
  async postForm<T>(path: string, body: FormData): Promise<T> {
    const result = await firstValueFrom(this.http.post<T>(`/api/${path}`, body));
    await this.refresh();
    return result;
  }

  /** Delete a host-owned content record. */
  async delete(path: string): Promise<void> {
    await firstValueFrom(this.http.delete(`/api/${path}`));
  }

  /** Request a typed resource. */
  async get<T>(path: string): Promise<T> {
    return firstValueFrom(this.http.get<T>(`/api/${path}`));
  }

  /** Subscribe to realtime changes; root component also polls as fallback. */
  async connectLive(): Promise<void> {
    if (this.connection) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hub/game')
      .withAutomaticReconnect()
      .build();

    connection.on('changed', () => void this.refresh());
    try {
      await connection.start();
      this.connection = connection;
    } catch (error) {
      console.warn('SignalR unavailable; polling remains active.', error);
      await connection.stop();
    }
  }

  /**
   * Start OAuth through the current origin. This works with the local proxy and
   * with the single-origin release build served by the API/VPN endpoint.
   */
  login(): void {
    window.location.assign('/api/auth/login');
  }

  /** End the server-side session and return to the homepage. */
  async logout(): Promise<void> {
    await this.post('auth/logout');
    window.location.assign('/');
  }
}

/** Extracts the server's validation message instead of rendering [object Object]. */
export function describeApiError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const payload: unknown = error.error;
    if (typeof payload === 'string' && payload.trim()) return payload;
    if (payload !== null && typeof payload === 'object') {
      const body = payload as Record<string, unknown>;
      for (const key of ['detail', 'message', 'title']) {
        if (typeof body[key] === 'string') return body[key];
      }
    }
    return `HTTP ${error.status}: ${error.statusText}`;
  }
  return error instanceof Error ? error.message : 'Unknown error';
}
