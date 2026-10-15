$ErrorActionPreference = 'Stop'

# Builds the Angular app and embeds it in the API's static web root. This is
# intended for a game night or private VPN release, not hot-reload development.
$root = Join-Path $PSScriptRoot '..'
$frontend = Join-Path $root 'src/frontend/web'
$api = Join-Path $root 'src/backend/QuizMaster.Api'
$webRoot = Join-Path $api 'wwwroot'
$browserOutput = Join-Path $frontend 'dist/web/browser'

Push-Location $frontend
try {
  if (-not (Test-Path 'node_modules/.bin/ng.cmd')) { npm ci }
  npm run build
  if ($LASTEXITCODE -ne 0) { throw 'Angular production build failed.' }
}
finally { Pop-Location }

New-Item -ItemType Directory -Force -Path $webRoot | Out-Null
# Keep host-uploaded Jeopardy pictures when refreshing the frontend assets.
Get-ChildItem -Force $webRoot | Where-Object { $_.Name -ne 'uploads' } | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $browserOutput '*') -Destination $webRoot -Recurse -Force

Write-Host "Single-host build ready in $webRoot"
