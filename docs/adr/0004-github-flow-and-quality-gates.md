# ADR-0004: GitHub flow with PRs, Conventional Commits and quality gates

- **Status:** Accepted
- **Date:** 2026-10-25
- **Deciders:** @csteinmueller

## Context

The project is public on GitHub under MIT. We need a lightweight, widely understood process that keeps `main` releasable and supports automated changelogs later.

## Options considered

1. **Git-flow** (develop/release branches) — heavy for a small library.
2. **Trunk-based with direct commits** — fast; no review gate.
3. **GitHub flow** — short-lived branches, PRs into protected `main`.

## Decision

- GitHub flow: branch `<type>/<issue#>-<slug>` → PR → squash-merge into `main`.
- Conventional Commits for commits and PR titles (enforced by `commit-msg` hook and the `PR hygiene` workflow).
- Required checks: CI (format, build, test on Linux + Windows), CodeQL, PR title.
- Work is tracked in GitHub issues: features, bugs, requirements, and **technical debt** (label `tech-debt`).
- Dependabot keeps NuGet packages and GitHub Actions current.
- `main` protection (configured by maintainer in repository settings): require PR, 1 approval, CODEOWNERS review, passing checks, linear history; no force-push.

## Consequences

- `main` is always green and releasable.
- Conventional Commits enable automated versioning/changelog later (separate ADR).
