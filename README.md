# QuizMaster

QuizMaster is a self-hosted, host-moderated game-night application built with ASP.NET Core, SQL Server, Angular, SignalR, and Discord OAuth. It currently offers three German-language game modes: Imposter, Who Wants to Be a Millionaire (WWM), and Jeopardy.

The host controls rounds, answers, scoring, timers, and content. Players join from a browser and receive only the state required to play.

## Highlights

- **Imposter**: private roles and secret words, clue rounds, moderated voting, and a reveal.
- **WWM**: difficulty-based question selection, progressive scoring, and 50/50, Spy, and Risk jokers.
- **Jeopardy**: six-column boards, host-selected categories, turn rotation, host decisions, optional steals, and two boards.
- **Live game state**: SignalR synchronizes rounds, player cards, scores, and cameras.
- **Content library**: import, export, remove, and reuse separate Imposter, WWM, and Jeopardy content.

## Technology

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core / .NET 10, EF Core, SQL Server |
| Frontend | Angular 21, TypeScript, CSS |
| Realtime | ASP.NET Core SignalR |
| Identity | Discord OAuth |
| Testing | Rule-check console project, Angular typecheck and production build |

## Quick start

### Prerequisites

- .NET 10 SDK
- Node.js 22 or newer
- SQL Server LocalDB or SQL Server Express
- A Discord OAuth application

### Configure Discord secrets

Use .NET User Secrets; do not put credentials into `appsettings.json`.

```powershell
Set-Location src/backend/QuizMaster.Api
dotnet user-secrets set "Discord:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Discord:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet user-secrets set "Discord:HostUserId" "YOUR_DISCORD_USER_ID"
```

Add this redirect URL to the Discord application:

```text
https://localhost:7222/signin-discord
```

### Run locally

```powershell
# Terminal 1
.\scripts\start-backend.ps1

# Terminal 2
.\scripts\start-frontend.ps1
```

Open `http://localhost:4200`.

For database setup, content management, validation, a shared game-night setup, and release direction, see [Development](docs/DEVELOPMENT.md), [Content](docs/CONTENT.md), [Deployment](docs/DEPLOYMENT.md), and the [Roadmap](docs/ROADMAP.md).

## Project layout

```text
src/
  backend/
    QuizMaster.Api/
      Features/
        Game/       # GameController, GameService, GameModels
        Jeopardy/   # JeopardyController, JeopardyModels
        Storage/    # StorageController, StorageService, StorageModels
    QuizMaster.Data/
  frontend/web/
    src/app/
      core/         # shared API, game-state, camera, and UI services
      features/     # one folder per screen or game feature
```

The backend uses a pragmatic feature-first organization: each feature keeps its controller, service, and models together. See [Architecture](docs/ARCHITECTURE.md).

## Validation

```powershell
dotnet build src/backend/QuizMaster.Api/QuizMaster.Api.csproj
dotnet run --project tests/QuizMaster.RuleChecks/QuizMaster.RuleChecks.csproj

Set-Location src/frontend/web
npm ci
npm run typecheck
npm run build
npm run format:check
```

## Contributing and security

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening an issue or pull request. Report security concerns using [SECURITY.md](SECURITY.md). QuizMaster is released under the [MIT License](LICENSE).
