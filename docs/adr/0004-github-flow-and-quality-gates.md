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
- `main` protection (repository ruleset, configured by the maintainer): require PR, required status checks (CI, CodeQL, PR title), linear history, no force-push, no deletion. Required approvals stay at 0 while the project has a single maintainer (GitHub does not allow approving your own PR); raise to 1 with CODEOWNERS review once there are co-maintainers.

## Consequences

- `main` is always green and releasable.
- Conventional Commits enable automated versioning/changelog later (separate ADR).
