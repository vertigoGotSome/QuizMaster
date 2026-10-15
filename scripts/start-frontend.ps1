$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '../src/frontend/web')
if (-not (Test-Path node_modules/.bin/ng.cmd)) { npm ci }
npm start
