# Copilot instructions

Follow [AGENTS.md](../AGENTS.md) — it is the authoritative rule set for this repository (workflow, guardrails, analyzer rules, commands).

Key reminders:
- Read [docs/vision.md](../docs/vision.md), relevant [requirements](../docs/requirements/README.md) and [ADRs](../docs/adr/README.md) before designing anything.
- Use the `developer` agent to implement and the `architect` agent to review. Request a review with `/request-review` before opening a PR.
- Never push to `main`, merge, or bypass hooks/CI.
