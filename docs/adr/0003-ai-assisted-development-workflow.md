# ADR-0003: AI-assisted development with developer and architect agents

- **Status:** Accepted
- **Date:** 2026-10-25
- **Deciders:** @csteinmueller

## Context

Much of the code will be produced with generative AI. Unchecked, this risks scope creep, hallucinated APIs, weakened tests, secrets leakage, licensing issues, and decisions nobody documented.

## Options considered

1. **Ad-hoc AI usage** — no overhead; inconsistent quality, no accountability.
2. **Single general-purpose agent** — simple; author reviews its own work.
3. **Separated roles (developer + architect) with written guardrails and deterministic hooks** — independent review from a different model family; more setup.

## Decision

- `AGENTS.md` is the binding rule set for all agents; `.github/copilot-instructions.md` points to it.
- **developer** agent (Claude Sonnet) implements; after quality gates pass, it automatically invokes the read-only **architect** agent (GPT) to review, using a different model family to reduce correlated blind spots. The developer addresses `BLOCKER` and `MAJOR` findings, reruns the gates, and requests follow-up reviews until none remain before preparing a PR. `/request-review` remains a manual entry point.
- Deterministic enforcement in addition to instructions:
  - Copilot agent `PreToolUse` hook (`.github/hooks/guardrails.json`) denies pushes to `main`, force-push, `--no-verify`, destructive deletes, publishing and secret-file access, and asks before pushes, dependency changes and CI/hook edits.
  - Git hooks (`.githooks/`) and CI apply to humans and agents equally.
- Humans approve and merge every PR; AI assistance is disclosed in the PR template.

## Consequences

- Clear accountability and traceability for AI-generated changes.
- Model names in agent files must be maintained as models evolve.
- Hooks require PowerShell 7 (`pwsh`) on the developer machine.
