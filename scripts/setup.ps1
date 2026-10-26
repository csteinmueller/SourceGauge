#!/usr/bin/env pwsh
# One-time developer setup: enables repository git hooks and restores tools/packages.
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

git config core.hooksPath .githooks
Write-Host 'Git hooks enabled (.githooks).'

dotnet restore
Write-Host 'Setup complete.'
