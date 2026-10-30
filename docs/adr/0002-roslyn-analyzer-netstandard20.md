# ADR-0002: Use Roslyn for initial C#/.NET analysis

- **Status:** Accepted
- **Date:** 2026-10-29
- **Deciders:** @csteinmueller

## Context

SourceGauge's purpose is to extract code metrics into structured output for third-party consumers. The analyzed project should not need a SourceGauge `PackageReference`, and metric values should not create rule violations or fail a build. C#/.NET is the first target, and Roslyn provides compiler-grade syntax and semantic information for that ecosystem. The architecture should also leave room for future language backends without forcing other languages into Roslyn-specific abstractions.

## Options considered

1. **Roslyn analyzer package** — convenient editor/compiler diagnostics, but requires project integration and is designed around diagnostics rather than exporting a complete metrics dataset.
2. **Command-line analysis tool using Roslyn** — can analyze projects and solutions and emit structured data without adding a SourceGauge reference to each analyzed project; runs as an explicit analysis step.
3. **IDE extension** — rich UI, but requires separate implementations and does not provide a neutral data contract by itself.

## Decision

- Build SourceGauge as a command-line analysis tool, using Roslyn as the initial C#/.NET backend. Analysis is initiated independently of builds and consumer project references.
- Make structured, machine-readable metric data the product output. Keep its schema versioned; define consistent metric identities, scopes, values and units; include analysis/source provenance; and prefer established standards when they represent metric data adequately.
- Keep the shared metric model independent of Roslyn so future language backends can contribute to a consistent output contract.
- Do not use analyzer diagnostics, thresholds, or build status as the primary output or enforcement mechanism.
- The repository's current analyzer project is a prototype and does not define the intended product packaging or integration model.

## Consequences

- Analysis is an explicit tool step, which can be added to CI without making metric values fail the build.
- Roslyn offers accurate C#/.NET analysis while avoiding project-level SourceGauge package references.
- Output-format and schema details still require a concrete interoperability requirement before being fixed.
- Future language backends may need different parsers or compiler APIs; shared metric concepts and output structure should remain language-neutral where possible.
