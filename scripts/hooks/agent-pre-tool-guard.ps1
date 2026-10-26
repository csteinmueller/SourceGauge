#!/usr/bin/env pwsh
# Copilot agent PreToolUse hook: deterministically blocks or escalates risky tool calls (see AGENTS.md "Guardrails").
$ErrorActionPreference = 'Stop'

$payload = [Console]::In.ReadToEnd()

$deny = @(
    @{ Pattern = 'git\s+push\b[^"]*\b(origin\s+)?(main|master)\b'; Reason = 'Pushing to main is not allowed; open a PR.' }
    @{ Pattern = 'git\s+push\b[^"]*(--force\b|-f\b|--force-with-lease)'; Reason = 'Force-push is not allowed for agents.' }
    @{ Pattern = '--no-verify'; Reason = 'Bypassing git hooks is not allowed.' }
    @{ Pattern = 'git\s+reset\s+--hard'; Reason = 'Destructive reset is not allowed for agents.' }
    @{ Pattern = 'rm\s+-rf\s+(/|~|\.)(\s|"|$)|Remove-Item[^"]*-Recurse[^"]*-Force'; Reason = 'Recursive force delete is not allowed for agents.' }
    @{ Pattern = '(gh\s+pr\s+merge|gh\s+release\s+create|dotnet\s+nuget\s+push|gh\s+repo\s+(delete|edit))'; Reason = 'Merging, releasing, publishing and repo changes are reserved for maintainers.' }
    @{ Pattern = '(^|[\\/"\s])\.env(\.|"|\s|$)|secrets\.json|\.pfx\b'; Reason = 'Access to secret files is not allowed.' }
)

$ask = @(
    @{ Pattern = 'git\s+push\b'; Reason = 'Pushing a branch requires confirmation.' }
    @{ Pattern = 'dotnet\s+add\s+[^"]*package|PackageVersion\s+Include'; Reason = 'New dependencies require an ADR or maintainer approval.' }
    @{ Pattern = '\.github[\\/](workflows|hooks)[\\/]|scripts[\\/]hooks[\\/]|\.githooks[\\/]'; Reason = 'Changes to CI or hooks require confirmation.' }
)

function Write-Decision([string]$decision, [string]$reason) {
    @{
        hookSpecificOutput = @{
            hookEventName            = 'PreToolUse'
            permissionDecision       = $decision
            permissionDecisionReason = "SourceGauge guardrail: $reason"
        }
    } | ConvertTo-Json -Depth 3 -Compress
    exit 0
}

foreach ($rule in $deny) {
    if ($payload -match $rule.Pattern) { Write-Decision 'deny' $rule.Reason }
}
foreach ($rule in $ask) {
    if ($payload -match $rule.Pattern) { Write-Decision 'ask' $rule.Reason }
}

exit 0
