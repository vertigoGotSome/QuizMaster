# Security policy

## Supported versions

The `main` branch is the supported development version.

## Reporting a vulnerability

Please do not open a public issue for a security vulnerability. Contact the repository owner privately through GitHub with a clear description, impact, and reproduction steps. The maintainer will acknowledge the report and coordinate a fix.

## Operational guidance

- Store Discord OAuth credentials in .NET User Secrets or deployment secrets.
- Do not commit `appsettings.Development.json`, `.env` files, local databases, or generated uploads.
- Treat Tailscale Funnel URLs as public internet endpoints and use them only for short trusted sessions.
