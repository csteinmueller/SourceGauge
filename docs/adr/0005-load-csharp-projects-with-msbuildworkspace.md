# ADR-0005: Load C# projects with MSBuildWorkspace

- **Status:** Accepted
- **Date:** 2026-10-31
- **Deciders:** @csteinmueller
- **Related:** #17

## Context

SourceGauge's CLI must analyze C# projects and solutions independently of their builds and without requiring a SourceGauge package reference in the analyzed projects. Correct analysis depends on the project's evaluated references, configuration, and compilation context. The existing analyzer prototype targets `netstandard2.0` and does not provide the intended CLI loading architecture.

The CLI host can use the installed .NET SDK and MSBuild to load project and solution inputs. This requires workspace and MSBuild integration packages in addition to the existing Roslyn compiler package. Loading a project may evaluate MSBuild files and execute project-defined tasks; this is not a safe operation on untrusted input.

## Options considered

1. **Load with Roslyn `MSBuildWorkspace`** — preserves MSBuild project evaluation, references, and compilation context. Requires an installed and discoverable MSBuild toolset, additional dependencies, and care around project-loading diagnostics and executable build logic.
2. **Construct Roslyn compilations without MSBuild** — avoids MSBuild evaluation and may reduce integration dependencies. The CLI would need to reproduce project references, conditional properties, target frameworks, generated inputs, and other evaluated project state, making results incomplete or incorrect for ordinary projects.

## Decision

Use `MSBuildWorkspace` in a separate CLI host to load C# project and solution inputs. Register and select the installed MSBuild toolset with `Microsoft.Build.Locator` before using MSBuild APIs. Keep workspace and Roslyn-specific types behind the C# analysis boundary; the shared metric model must not depend on them.

Add `Microsoft.CodeAnalysis.Workspaces.MSBuild` and `Microsoft.CodeAnalysis.CSharp.Workspaces` at the repository's Roslyn version (`5.9.0`), and use `Microsoft.Build.Locator` version `1.11.2`. The CLI must reference the matching `Microsoft.Build.Framework` package (`17.11.48`) for compilation only (`ExcludeAssets="runtime"`, `PrivateAssets="all"`) so MSBuild assemblies are resolved from the registered SDK installation. Manage package versions centrally. These dependencies belong in the CLI project, not the existing `netstandard2.0` analyzer prototype.

The initial CLI should accept C# project (`.csproj`) and solution (`.sln`) inputs. It should report load failures and diagnostics as operational output, separate from any future machine-readable metrics payload. Failed project loading must be reported as an analysis failure; metric values themselves must not determine process success. The output schema and metric definitions remain outside this decision.

SourceGauge does not sandbox MSBuild evaluation. Users must only analyze projects they trust; documentation must make this limitation clear. The CLI should not claim that loading arbitrary projects is safe.

## Consequences

- Project analysis uses the same evaluated references and compilation context as the selected local MSBuild toolset, improving fidelity over manually assembled compilations.
- The CLI requires a compatible installed .NET SDK/MSBuild toolset, and the supported discovery and selection behavior must be documented and tested.
- Project loading can execute project-defined logic. Treating untrusted project analysis safely would require a separate security design and is not provided by this decision.
- Solution loading can produce diagnostics or partial results; implementation must define and test how those are surfaced without confusing them with metric output.
- The workspace integration remains specific to the C# backend, preserving a language-neutral shared metric model.
- The CLI loading dependencies are scoped to the new CLI host; the analyzer prototype remains unchanged.