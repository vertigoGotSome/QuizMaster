# Development

## Initial setup

```powershell
# From the repository root
Set-Location src/frontend/web
npm ci

Set-Location ../../backend/QuizMaster.Api
dotnet restore
```

Configure Discord credentials with .NET User Secrets as shown in the root README. The host account is identified by `Discord:HostUserId`.

## Database

Apply migrations from the repository root:

```powershell
dotnet ef database update --project src/backend/QuizMaster.Data --startup-project src/backend/QuizMaster.Api
```

Create a migration after changing data entities:

```powershell
dotnet ef migrations add MeaningfulMigrationName --project src/backend/QuizMaster.Data --startup-project src/backend/QuizMaster.Api
```

## Daily commands

```powershell
# Backend API
Set-Location src/backend/QuizMaster.Api
dotnet run

# Angular frontend
Set-Location src/frontend/web
npm start

# Rule checks
Set-Location <repository-root>
dotnet run --project tests/QuizMaster.RuleChecks/QuizMaster.RuleChecks.csproj
```

## Formatting and validation

```powershell
# C# formatting
dotnet format QuizMaster.slnx --no-restore

# Frontend formatting
Set-Location src/frontend/web
npm run format
npm run format:check
npm run typecheck
npm run build
```

Run backend build, rule checks, frontend typecheck, and frontend production build before opening a pull request.

## Conventions

- Keep repository source, comments, documentation, issue text, and code identifiers in English.
- German is appropriate for player-facing labels and German game content.
- Prefer small feature-local files over catch-all endpoint or utility files.
- Keep browser-only display logic in Angular and authoritative game rules in the backend.
- Do not commit credentials, User Secrets, local databases, generated frontend assets, or uploaded images.
