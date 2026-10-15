# Content management

The Storage page manages three independent catalogues:

- **Imposter words**: a category, secret word, and deliberately broad hint.
- **WWM questions**: four answers, the correct option, and a difficulty from 1 to 30.
- **Jeopardy questions**: board round, category, value, clue, answer, accepted answers, optional host note, and optional image URL.

## Import and export

Choose a content type on the Storage page, paste or select a JSON file, then validate and save it. Export creates a backup in the same format. Duplicate entries are skipped by the application.

## Clearing content

Each catalogue has its own destructive clear action. Use it only after exporting a backup when you want to start with a new data set. Clearing one game mode never removes content from another.

## Question design

- WWM difficulty should increase steadily from 1 to 30.
- Jeopardy values should become harder as the value increases.
- Use accepted answers for fair host moderation, including common alternative wording.
- Avoid overly direct Imposter hints; a good hint helps an Imposter participate without revealing the secret word.
- Keep image URLs stable and use images only when they are necessary to solve the clue.

Question text and player-facing content may remain German for a German-speaking game night.

<<<<<<< HEAD
### Jeopardy difficulty calibration

Jeopardy cells carry a `difficulty` from **1** (introductory) to **10** (expert).
If omitted or set to `0`, QuizMaster derives a baseline from the board round and
visible value: board one maps from 1–5 and board two maps from 6–10. Authors may
supply a different value, but the importer reports a warning when it departs from
that baseline. Every selected category must increase strictly by difficulty from
its lowest to highest value before it can be used on a board.
=======
### WWM difficulty scale

WWM uses a 1–30 difficulty scale. The host's next-question action targets the
same value as the round number. The selector randomizes only between unused
questions at the nearest available difficulty. The host panel flags empty
levels, and explicitly warns when a selected fallback is more than two levels
away from its target.
>>>>>>> 00ff367 (feat: audit WWM question difficulty coverage)
