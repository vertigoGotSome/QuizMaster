import type { Routes } from '@angular/router';

/** Lazy-load features so new game modules can be added independently. */
export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'lobby',
    loadComponent: () => import('./features/lobby/lobby.component').then((m) => m.LobbyComponent),
  },
  {
    path: 'host',
    loadComponent: () => import('./features/host/host.component').then((m) => m.HostComponent),
  },
  {
    path: 'storage',
    loadComponent: () =>
      import('./features/storage/storage.component').then((m) => m.StorageComponent),
  },
  {
    path: 'play',
    loadComponent: () => import('./features/play/play.component').then((m) => m.PlayComponent),
  },
  { path: '**', redirectTo: '' },
];
