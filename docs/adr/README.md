# Architecture Decision Records

We record architecturally significant decisions as lightweight ADRs, following [Michael Nygard's format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).

## When to write an ADR

- Adding a dependency or changing target frameworks / Roslyn version.
- Defining how a metric is calculated or configured.
- Changing public API, diagnostic ID scheme, packaging, or build/CI architecture.
- Anything a future contributor would ask "why did we do it this way?"

## Process

1. Copy [0000-template.md](0000-template.md) to `NNNN-kebab-title.md` (next free number) — or run `/new-adr`.
2. Status `Proposed`; open a PR (label `adr`). The architect agent reviews; a maintainer accepts.
3. On merge set status `Accepted`. ADRs are immutable afterwards — to change a decision, write a new ADR and mark the old one `Superseded by ADR-NNNN`.

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-roslyn-analyzer-netstandard20.md) | Ship as a Roslyn analyzer targeting netstandard2.0 | Accepted |
| [0003](0003-ai-assisted-development-workflow.md) | AI-assisted development with developer and architect agents | Accepted |
| [0004](0004-github-flow-and-quality-gates.md) | GitHub flow with PRs, Conventional Commits and quality gates | Accepted |
