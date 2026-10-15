import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GameService } from '../../core/services/game.service';

/** Shows participants who have authenticated and recently sent a heartbeat. */
@Component({
  standalone: true,
  imports: [RouterLink],
  templateUrl: './lobby.component.html',
})
export class LobbyComponent {
  readonly g = inject(GameService);
}
