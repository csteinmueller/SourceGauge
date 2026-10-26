# Vision

> Status: **Draft** — owned by the maintainers. Changes via PR with architect review.

## Problem

.NET teams want objective, continuous feedback on code health — complexity, size, coupling, cohesion, maintainability. Existing options are either IDE-only (Visual Studio Code Metrics), separate commercial tools with their own pipelines, or unmaintained. Metrics rarely show up *where developers already work*: in the compiler output, the IDE, and the pull request.

## Vision statement

**SourceGauge makes software metrics a first-class part of the .NET build.** Install one NuGet package and every build — in the IDE, on the CLI, and in CI — measures your C# code and tells you, with familiar diagnostics, where it exceeds the thresholds your team agreed on.

## Target users

- **Developers** who want immediate, in-editor feedback on code they are writing.
- **Tech leads / architects** who want to enforce agreed quality gates in CI.
- **Open-source maintainers** who want a free, transparent, zero-infrastructure quality signal.

## Goals

| ID | Goal |
|----|------|
| G1 | **Zero friction** — one package reference; works in Visual Studio, Rider, VS Code, and `dotnet build`. |
| G2 | **Trustworthy metrics** — every metric has a documented, cited definition and is verified by tests. |
| G3 | **Configurable** — thresholds and severities configurable per repo/folder via `.editorconfig` / `.globalconfig`. |
| G4 | **Fast** — negligible impact on build and IDE responsiveness. |
| G5 | **Open** — MIT licensed, developed in public, contributions welcome. |

## Non-goals (for now)

- Languages other than C# (VB.NET, F#).
- Dashboards, historical trend storage, or a hosted service.
- Automatic code fixes / refactorings.
- Replacing general-purpose style or bug-finding analyzers.

## Success indicators

- Metric values match the documented definitions on a reference test corpus.
- Analyzer overhead stays within the budget defined in the NFRs.
- Adoption: NuGet downloads, GitHub stars, external contributions.

## Guiding principles

1. Correctness over number of metrics.
2. Sensible defaults, everything overridable.
3. Decisions are documented ([ADRs](adr/README.md)); requirements are traceable ([requirements](requirements/README.md)).
4. Humans own decisions; AI agents assist under explicit [guardrails](../AGENTS.md).
