# Copilot instructions

Follow [AGENTS.md](../AGENTS.md) — it is the authoritative rule set for this repository (workflow, guardrails, metrics and analysis rules, commands).

Key reminders:
- Read [docs/vision.md](../docs/vision.md), relevant [requirements](../docs/requirements/README.md) and [ADRs](../docs/adr/README.md) before designing anything.
- Use the `product-owner` agent to create or refine issues and the `developer` agent to implement. After quality gates pass, the developer automatically invokes the `architect` agent for read-only review, addresses `BLOCKER` and `MAJOR` findings, and requests follow-up review before opening a PR. `/request-review` remains available for manual review requests.
- Never push to `main`, merge, or bypass hooks/CI.
