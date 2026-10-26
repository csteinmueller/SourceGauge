# ADR-0002: Ship as a Roslyn analyzer targeting netstandard2.0

- **Status:** Accepted
- **Date:** 2026-10-25
- **Deciders:** @csteinmueller

## Context

The vision (G1, G4) requires metrics in the IDE, CLI and CI without extra tooling. Roslyn analyzers run inside the compiler host (Visual Studio on .NET Framework, `dotnet build`, Rider, VS Code C# Dev Kit).

## Options considered

1. **Standalone CLI tool** — flexible output; separate pipeline step, no in-editor feedback.
2. **Roslyn analyzer (NuGet)** — native diagnostics everywhere; constrained runtime (`netstandard2.0`, no dependencies, must be fast).
3. **IDE extension** — rich UI; per-IDE implementations, no CI.

## Decision

- Implement metrics as `DiagnosticAnalyzer`s in `SourceGauge.Analyzers`, target `netstandard2.0`.
- Reference `Microsoft.CodeAnalysis.CSharp` **4.8.0** (VS 2022 17.8 / .NET 8 SDK) as the minimum supported compiler; raise only via a new ADR.
- Package as a development dependency with the assembly under `analyzers/dotnet/cs`.
- Diagnostic IDs: `SG` + 4 digits, category `Metrics`; tracked with `AnalyzerReleases.*.md`.
- Tests run on the current .NET SDK (`net10.0`).

## Consequences

- Works out of the box wherever the compiler runs.
- No runtime dependencies allowed in the analyzer assembly; newer Roslyn APIs are unavailable.
- Potential future CLI/report tooling can reuse the metric engine but would need its own ADR.
