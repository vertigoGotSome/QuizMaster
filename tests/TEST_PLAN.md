# Manual game smoke test

Run this checklist before a game night or a release candidate. Use two browser profiles or two Discord accounts for multiplayer checks.

## Setup

1. Configure Discord User Secrets and apply migrations.
2. Start the backend and frontend, then verify `/api/health` returns `status: ok`.
3. Sign in once as the configured host and once as a player.
4. Verify that both people appear in the lobby and that the host is not displayed as a playable participant when host participation is disabled.

## Imposter

1. Start a round with enough players and confirm each player sees only their own role information.
2. Confirm crew members receive the secret word while a blind Imposter does not receive the word catalogue.
3. Complete clue turns, open voting, and verify that a player cannot vote for themselves.
4. Reveal the result and verify the player ring, score state, and host camera layout.

## WWM

1. Start question 1 and answer correctly. Confirm the awarded score is 1.
2. Advance to a later question and confirm the normal award equals its round number.
3. Use the Risk joker once for a correct answer and once for a wrong answer in separate sessions. Confirm both use double the round value.
4. Verify 50/50 and Spy can each be used once and that the host can reveal and move to the next question without changing screens.

## Jeopardy

1. Configure six unique categories for board 1.
2. Confirm the active team is highlighted while selecting a cell.
3. Open a cell, verify the host can see answer and notes, then mark it correct and verify the full value is awarded.
4. Mark another answer wrong, open steal, buzz from another player, and verify a correct or wrong steal changes the score by half the cell value.
5. Finish the board and verify board 2 selects the lowest-scoring team to start.

## Content and sharing

1. Import and export one catalogue of each content type.
2. Use the custom confirmation dialog to remove a single entry and then clear a catalogue in a disposable test database.
3. Produce a single-host build and test the configured HTTPS URL from a second device.
4. Confirm the Discord redirect URL exactly matches the active HTTPS address.
