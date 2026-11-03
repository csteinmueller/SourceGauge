# ADR-0009: CLI and C# Backend Project Boundaries

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** @csteinmueller
- **Related:** #28, ADR-0002, ADR-0005, ADR-0006, ADR-0007, ADR-0008, REQ-0001, REQ-0002

## Context

The accepted product is a CLI that loads C# projects and solutions and exports structured metric data. At the time of this decision, the analyzer prototype remained in the solution and CI, while the initial FanIn collector and the versioned result model and JSON exporter lived in `SourceGauge.Cli`.

The repository needs explicit project boundaries before retiring the prototype and moving metric code. ADR-0005 assigns `MSBuildWorkspace` loading to the CLI and requires Roslyn-specific types to remain outside the shared metric model. This ADR clarifies that boundary: the CLI may pass Roslyn workspace project objects to the C# backend, but Roslyn types must not enter the shared model or JSON contract.

Metric meaning is already governed by requirements and metric-specific ADRs. This decision does not redefine FanIn or the versioned JSON contract.

## Options considered

1. **Keep loading in the CLI and pass Roslyn projects to the C# backend** — Preserves ADR-0005's loading owner and keeps command-line and MSBuild concerns in the host. Roslyn types cross the CLI/backend implementation boundary, but remain isolated from the shared model. The CLI and backend must use compatible Roslyn APIs.
2. **Move project loading into the C# backend** — Keeps Roslyn types behind the backend API, but changes the loading owner and dependency placement accepted by ADR-0005.
3. **Keep loading and metric generation together in the CLI** — Minimizes project changes, but leaves language-specific collection coupled to CLI orchestration and weakens the backend boundary.

## Decision

We will use the following project responsibilities and dependency directions:

- **`SourceGauge.Core`** owns the language-neutral metric result model and shared contract types. It has no Roslyn, MSBuild, CLI, or language-backend dependencies.
- **`SourceGauge.CSharp`** owns Roslyn-backed C# metric collectors, including FanIn generation. It depends on `SourceGauge.Core` and `Microsoft.CodeAnalysis.CSharp.Workspaces` for analysis. Its collector boundary may accept Roslyn workspace project objects supplied by the CLI; those types are implementation details and are not represented in Core.
- **`SourceGauge.Cli`** owns command-line parsing, `MSBuildWorkspace` registration and project/solution loading, target-framework orchestration, backend invocation, and JSON serialization/export. It depends on Core and the C# backend, and owns `Microsoft.CodeAnalysis.Workspaces.MSBuild`, `Microsoft.Build.Locator`, and the matching `Microsoft.Build.Framework` reference required for MSBuild integration.

The project dependency graph is `SourceGauge.Cli -> SourceGauge.CSharp -> SourceGauge.Core`, with `SourceGauge.Cli -> SourceGauge.Core` for the result model. Core has no dependencies on the other projects, and the C# backend does not depend on the CLI. Keep Roslyn package versions centrally managed and aligned across the CLI and backend.

Metric definitions remain documented in `docs/requirements/` as testable functional requirements and in metric-specific ADRs for normative calculation, eligibility, scope, and edge-case rules. For FanIn, REQ-0002 and ADR-0008 are authoritative. Implementations and their behavior tests live with the C# backend; CLI integration tests verify loading, orchestration, and unchanged export behavior.

The obsolete analyzer prototype and its tests are not part of the target architecture. Issue #28 removes their solution entries, source projects, analyzer-only configuration, active CI packaging, and active documentation references. ADR-0002 remains as historical context for the decision to use a CLI rather than an analyzer package.

This ADR supersedes only ADR-0005's placement of `Microsoft.CodeAnalysis.CSharp.Workspaces` in the CLI: that analysis dependency belongs to `SourceGauge.CSharp`. ADR-0005 remains authoritative for CLI-owned MSBuild loading and its MSBuild integration dependencies. All package versions remain centrally managed. This decision does not change REQ-0001, REQ-0002, ADR-0007, or ADR-0008.

## Consequences

- Metric definitions, language-neutral data, C# analysis, and command-line/export responsibilities have separate, discoverable owners.
- The Core model can be reused by future language backends without Roslyn dependencies.
- Roslyn project objects cross the CLI-to-CSharp implementation boundary, so the two projects must remain compatible with the same Roslyn workspace version.
- The C# collector moves into its own project and test boundary; CLI tests continue to cover end-to-end behavior.
- The CLI and backend each own the dependencies for their boundary: MSBuild integration in the CLI, Roslyn C# analysis APIs in the backend.
- Removing the analyzer prototype reduces duplicate project, test, and packaging maintenance.
- The architect reviewed this ADR before maintainer acceptance; implementation must preserve the recorded project boundaries and metric/output contracts.