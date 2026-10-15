# Deployment and game-night sharing

## Local development

Use the separate Angular and API processes described in [Development](DEVELOPMENT.md). This is the fastest workflow while changing the application.

## Single-host build

For a game night, build Angular into the API’s `wwwroot` directory:

```powershell
Set-Location <repository-root>
.\scripts\build-single-host.ps1

Set-Location src/backend/QuizMaster.Api
dotnet run --urls http://127.0.0.1:5222
```

The generated `wwwroot` directory is ignored by Git because it is reproducible.

## Private sharing with Tailscale

When every participant can install Tailscale and join the same tailnet, run the API on the loopback port above and expose it privately:

```powershell
tailscale serve --https=443 http://127.0.0.1:5222
```

Open the HTTPS address reported by Tailscale. Add that exact address plus `/signin-discord` to your Discord OAuth redirect URLs.

## Public temporary sharing with Tailscale Funnel

Funnel exposes the app publicly. Use it only for a short trusted playtest and stop it afterwards:

```powershell
tailscale funnel --https=443 http://127.0.0.1:5222
```

Add the exact Funnel URL with `/signin-discord` to Discord OAuth. Keep the API and Funnel terminal open for the entire session. Press `Ctrl+C` in the Funnel terminal to stop public access.

## HTTPS and cameras

Browsers require a secure context for camera access outside localhost. The HTTPS URLs provided by Tailscale Serve or Funnel meet this requirement. Camera permissions still require each player to approve access in their browser.

## Before sharing

- Verify the backend starts and the production frontend build succeeds.
- Confirm the Discord redirect URL exactly matches the active HTTPS address.
- Use a test Discord account to verify player login.
- Export content as a backup before a session.
- Never expose database credentials or the Discord client secret.
