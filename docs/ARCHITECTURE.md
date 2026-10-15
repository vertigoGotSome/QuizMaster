# Architecture

QuizMaster is a single-host web application. The browser frontend communicates with the ASP.NET Core API over HTTP and receives live updates through SignalR. SQL Server stores sessions, players, game content, and round state.

## Backend

The API follows a feature-first structure. A feature folder contains the code that changes together:

```text
Features/
  FeatureName/
    FeatureController.cs
    FeatureService.cs
    FeatureModels.cs
```

Controllers expose HTTP endpoints and enforce host/player access. Services contain reusable rules or feature operations. Models define request and response contracts. Not every feature needs all three file types; Jeopardy currently has no separate service because its orchestration stays cohesive in its controller.

| Feature | Responsibility |
| --- | --- |
| `Game` | Sessions, players, Imposter, WWM, scoring, camera state, and shared game rules |
| `Jeopardy` | Board configuration, category selection, question flow, steal flow, and turn rotation |
| `Storage` | Question/word import, export, removal, and catalog access rules |

`Infrastructure/Realtime/GameHub.cs` is intentionally outside a feature because it transports state changes used by every game mode.

## Frontend

```text
src/app/
  core/       # app-wide state, HTTP, SignalR, camera, dialogs, and models
  features/   # screen and game-specific UI
```

Each frontend feature keeps its component, template, API adapter, and local models close together. `play` owns the shared live-game canvas used by all modes so that camera and SignalR subscriptions are not duplicated. `jeopardy` owns the host’s board configuration and moderation controls.

## Game state and trust boundary

The server is authoritative. The frontend renders state and asks the API to perform an action. Host-only routes decide scores, answers, board configuration, question visibility, and round progression. Player routes expose only information needed by that player.

## Content lifecycle

Imposter words, WWM questions, and Jeopardy questions are stored separately. Storage import/export works per content type. This prevents accidental mixing of game-mode content and allows a host to clear or reuse a single catalogue.
