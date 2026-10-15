import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

export interface CameraFeed {
  playerId: number;
  stream: MediaStream;
}
interface Peer {
  connection: RTCPeerConnection;
  makingOffer: boolean;
  ignoreOffer: boolean;
  candidates: RTCIceCandidateInit[];
  stream?: MediaStream;
}
interface CameraSignal {
  description?: RTCSessionDescriptionInit;
  ice?: RTCIceCandidateInit;
  cameraOff?: boolean;
  cameraOn?: boolean;
}

/** Voluntary, video-only WebRTC. Audio stays in Discord. */
@Injectable({ providedIn: 'root' })
export class CameraService {
  readonly enabled = signal(false);
  readonly pending = signal(false);
  readonly localStream = signal<MediaStream | null>(null);
  readonly feeds = signal<CameraFeed[]>([]);
  readonly error = signal('');
  private hub: HubConnection | null = null;
  private connecting: Promise<void> | null = null;
  private peers = new Map<number, Peer>();
  private knownPlayers = new Set<number>();
  private ownId = 0;
  private generation = 0;

  async connect(): Promise<void> {
    if (this.connecting) return this.connecting;
    if (this.hub?.state === HubConnectionState.Connected) return;
    this.connecting = this.startChannel();
    try {
      await this.connecting;
    } finally {
      this.connecting = null;
    }
  }

  private async startChannel(): Promise<void> {
    if (this.hub) await this.hub.stop();
    const hub = new HubConnectionBuilder().withUrl('/hub/game').withAutomaticReconnect().build();
    this.hub = hub;
    hub.on('camera-peers', (ids: number[], ownId: number) => {
      this.ownId = ownId;
      this.knownPlayers = new Set(ids);
      if (this.enabled()) for (const id of ids) this.attachLocalTracks(this.peer(id), id);
    });
    hub.on('camera-joined', (id: number) => {
      if (id === this.ownId) return;
      this.knownPlayers.add(id);
      this.removePeer(id);
      if (this.enabled()) this.attachLocalTracks(this.peer(id), id);
    });
    hub.on('camera-left', (id: number) => {
      this.knownPlayers.delete(id);
      this.removePeer(id);
    });
    hub.on(
      'camera-signal',
      (id: number, payload: CameraSignal) => void this.handleSignal(id, payload),
    );
    hub.onreconnecting(() => {
      this.clearPeers();
      this.knownPlayers.clear();
      this.error.set('Kamera-Verbindung wird wiederhergestellt.');
    });
    hub.onreconnected(async () => {
      try {
        await hub.invoke('JoinCamera');
        this.error.set('');
      } catch {
        this.error.set('Kamera-Verbindung konnte nicht wiederhergestellt werden.');
      }
    });
    hub.onclose(() => {
      this.clearPeers();
      this.knownPlayers.clear();
    });
    try {
      await hub.start();
      await hub.invoke('JoinCamera');
    } catch {
      this.error.set('Kamera-Verbindung nicht verfügbar.');
      await hub.stop();
      if (this.hub === hub) this.hub = null;
    }
  }

  async enable(): Promise<void> {
    if (this.enabled() || this.pending()) return;
    const generation = ++this.generation;
    this.pending.set(true);
    try {
      await this.connect();
      if (this.hub?.state !== HubConnectionState.Connected || generation !== this.generation)
        return;
      const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
      if (generation !== this.generation) {
        stream.getTracks().forEach((t) => t.stop());
        return;
      }
      this.localStream.set(stream);
      this.enabled.set(true);
      for (const id of this.knownPlayers) this.attachLocalTracks(this.peer(id), id);
      this.error.set('');
    } catch (error) {
      this.disable();
      this.error.set(this.cameraErrorMessage(error));
    } finally {
      this.pending.set(false);
    }
  }

  disable(): void {
    ++this.generation;
    this.localStream()
      ?.getTracks()
      .forEach((track) => track.stop());
    this.localStream.set(null);
    this.enabled.set(false);
    for (const [id, peer] of this.peers) {
      for (const sender of peer.connection.getSenders())
        if (sender.track) void sender.replaceTrack(null);
      void this.signal(id, { cameraOff: true }).catch(() => undefined);
    }
  }

  async disconnect(): Promise<void> {
    this.disable();
    await this.connecting;
    const hub = this.hub;
    this.hub = null;
    await hub?.stop();
    this.clearPeers();
    this.knownPlayers.clear();
  }

  private attachLocalTracks(peer: Peer, id: number): void {
    const stream = this.localStream();
    if (!stream) return;
    for (const track of stream.getTracks()) {
      const transceiver = peer.connection
        .getTransceivers()
        .find((t) => t.receiver.track.kind === track.kind);
      if (transceiver) {
        void transceiver.sender.replaceTrack(track);
        if (transceiver.direction !== 'sendrecv') transceiver.direction = 'sendrecv';
      } else peer.connection.addTrack(track, stream);
    }
    void this.negotiate(peer, id);
    void this.signal(id, { cameraOn: true }).catch(() => undefined);
  }

  private peer(id: number): Peer {
    const existing = this.peers.get(id);
    if (existing) return existing;
    const connection = new RTCPeerConnection({
      iceServers: [{ urls: 'stun:stun.l.google.com:19302' }],
    });
    const peer: Peer = { connection, makingOffer: false, ignoreOffer: false, candidates: [] };
    this.peers.set(id, peer);
    connection.onicecandidate = (event) => {
      if (event.candidate)
        void this.signal(id, { ice: event.candidate.toJSON() }).catch(() => undefined);
    };
    connection.ontrack = (event) => {
      const stream = event.streams[0] ?? new MediaStream([event.track]);
      peer.stream = stream;
      this.feeds.update((feeds) => [
        ...feeds.filter((f) => f.playerId !== id),
        { playerId: id, stream },
      ]);
    };
    connection.onnegotiationneeded = () => void this.negotiate(peer, id);
    return peer;
  }

  private async negotiate(peer: Peer, id: number): Promise<void> {
    if (peer.makingOffer || peer.connection.signalingState !== 'stable') return;
    try {
      peer.makingOffer = true;
      await peer.connection.setLocalDescription();
      await this.signal(id, { description: peer.connection.localDescription! });
    } catch {
      this.error.set('Kamera-Verbindung konnte nicht aufgebaut werden.');
    } finally {
      peer.makingOffer = false;
    }
  }

  private async handleSignal(id: number, data: CameraSignal): Promise<void> {
    try {
      if (data.cameraOff) {
        this.feeds.update((feeds) => feeds.filter((f) => f.playerId !== id));
        return;
      }
      const peer = this.peer(id);
      if (data.cameraOn) {
        if (peer.stream)
          this.feeds.update((feeds) => [
            ...feeds.filter((f) => f.playerId !== id),
            { playerId: id, stream: peer.stream! },
          ]);
        return;
      }
      const connection = peer.connection;
      if (data.description) {
        const collision =
          data.description.type === 'offer' &&
          (peer.makingOffer || connection.signalingState !== 'stable');
        peer.ignoreOffer = this.ownId < id && collision;
        if (peer.ignoreOffer) return;
        await connection.setRemoteDescription(data.description);
        for (const ice of peer.candidates.splice(0)) await connection.addIceCandidate(ice);
        if (data.description.type === 'offer') {
          if (this.enabled()) this.attachLocalTracks(peer, id);
          await connection.setLocalDescription();
          await this.signal(id, { description: connection.localDescription! });
        }
      } else if (data.ice && !peer.ignoreOffer) {
        if (connection.remoteDescription) await connection.addIceCandidate(data.ice);
        else peer.candidates.push(data.ice);
      }
    } catch {
      this.error.set('Kamera-Signalisierung fehlgeschlagen.');
    }
  }

  private async signal(id: number, payload: CameraSignal): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected)
      await this.hub.invoke('SignalCamera', id, payload);
  }

  private removePeer(id: number): void {
    this.peers.get(id)?.connection.close();
    this.peers.delete(id);
    this.feeds.update((feeds) => feeds.filter((f) => f.playerId !== id));
  }
  private clearPeers(): void {
    for (const id of [...this.peers.keys()]) this.removePeer(id);
  }
  private cameraErrorMessage(error: unknown): string {
    if (error instanceof DOMException) {
      if (error.name === 'NotAllowedError')
        return 'Kamerazugriff wurde im Browser blockiert. Erlaube die Kamera für localhost und versuche es erneut.';
      if (error.name === 'NotFoundError') return 'Keine verwendbare Kamera gefunden.';
      if (error.name === 'NotReadableError')
        return 'Die Kamera wird bereits von einer anderen App oder einem anderen Browser-Tab verwendet.';
      if (error.name === 'SecurityError')
        return 'Kamera benötigt eine sichere Seite; nutze localhost oder HTTPS.';
    }
    return error instanceof Error && error.message
      ? `Kamera konnte nicht gestartet werden: ${error.message}`
      : 'Kamera konnte nicht gestartet werden.';
  }
}
