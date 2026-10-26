# ADR-0001: Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-10-25
- **Deciders:** @csteinmueller

## Context

SourceGauge is developed in public, by humans and AI agents. Agents have no memory between sessions and contributors come and go; both need the reasoning behind decisions, not only the code.

## Options considered

1. **No formal records** — zero overhead; reasoning is lost and agents re-litigate decisions.
2. **Wiki / external docs** — easy editing; drifts from code, not reviewed in PRs.
3. **ADRs in the repository** — versioned and reviewed with code, readable by agents.

## Decision

We will keep lightweight ADRs in `docs/adr/` using the template in `0000-template.md`, reviewed via pull requests.

## Consequences

- Decisions are traceable and discoverable by agents via `AGENTS.md`.
- Small overhead per significant decision.
