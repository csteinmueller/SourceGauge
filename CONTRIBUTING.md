# Contributing to SourceGauge

Thanks for your interest! This project follows a lightweight GitHub flow with documented decisions. See [AGENTS.md](AGENTS.md) for the complete rule set (it applies to humans and AI agents alike).

## Setup

Prerequisites: [.NET SDK 10](https://dotnet.microsoft.com/download), Git, PowerShell 7 (`pwsh`) for agent hooks.

```bash
./scripts/setup.sh      # or: pwsh ./scripts/setup.ps1  — enables git hooks
dotnet build -c Release
dotnet test -c Release
```

## Workflow

1. **Issue first** — pick or open an issue (bug, feature, requirement, or tech debt).
2. **Branch** — `<type>/<issue#>-<slug>`, e.g. `feat/12-cyclomatic-complexity`.
3. **Implementation** — the developer agent implements the issue and passes the required quality gates.
4. **Review** — the developer agent automatically invokes the architect agent. Address `BLOCKER` and `MAJOR` findings, rerun the gates, and request follow-up review until none remain; do not wait for a manual handoff.
5. **Commit** — after review is clear, the developer agent creates local Conventional Commit(s) for only the issue-scoped changes. Preserve unrelated work and never bypass a failed hook.
6. **PR** — fill in the template, link the issue, requirement and ADRs. CI must be green. A maintainer approves and squash-merges.

## Where things live

| What | Where |
|------|-------|
| Product vision | [docs/vision.md](docs/vision.md) |
| Requirements (FR/NFR) | [docs/requirements/](docs/requirements/README.md) |
| Architecture decisions | [docs/adr/](docs/adr/README.md) |
| Technical debt | GitHub issues labelled [`tech-debt`](https://github.com/csteinmueller/SourceGauge/labels/tech-debt) |

## Technical debt

Shortcuts are fine when they are **visible**. Open a *Technical debt* issue describing the debt, its impact and a remediation idea, and reference it in code as `// TODO(#123): …`. Untracked TODOs are rejected in review.

## AI-assisted contributions

AI assistance is welcome. You remain responsible for every line: review it, test it, ensure it is MIT-compatible, and disclose AI use in the PR template.

## Code of conduct

Participation is governed by our [Code of Conduct](CODE_OF_CONDUCT.md).
