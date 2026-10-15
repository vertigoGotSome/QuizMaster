import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GameService } from '../../core/services/game.service';

/** Landing page for the private, Discord-authenticated game night. */
@Component({
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
})
export class HomeComponent {
  readonly g = inject(GameService);
}
