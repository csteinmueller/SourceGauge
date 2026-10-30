# Vision

> Status: **Draft** — owned by the maintainers. Changes via PR with architect review.

## Problem

Teams and tool builders need objective metrics extracted from source code in a form that can be consumed by their own reports, dashboards, quality systems, and research workflows. Existing products often combine metrics with broad analysis, recommendations, or enforcement. SourceGauge focuses on producing well-defined metric data that other tools can use.

## Vision statement

**SourceGauge is a command-line tool that analyzes a codebase and produces trustworthy, standardized software metrics for downstream tools.** C# and .NET are the first target, using Roslyn wherever it provides the most accurate view of the code. Analysis produces data; it does not enforce thresholds or fail a build. The output should be as interoperable as practical, with a stable, documented schema and metric definitions.

## Target users

- **Tool builders** who need dependable source-code metrics as input to their products and services.
- **Engineering teams** who want to analyze their codebase and use metrics in their existing workflows.
- **Researchers and maintainers** who need transparent, reproducible measurements across codebases.

## Goals

| ID | Goal |
|----|------|
| G1 | **Useful output** — produce machine-readable metrics that downstream tools can consume without requiring a SourceGauge package reference in analyzed projects. |
| G2 | **Trustworthy metrics** — every metric has a documented definition, scope, and interpretation, and is verified by tests. |
| G3 | **Interoperable** — use a stable, versioned output contract with consistent metric identities and structure. Results should identify the analysis run and source, the measured scope, metric value and unit, and relevant provenance. Prefer established standards where they represent metric data well. |
| G4 | **C# / .NET first** — use Roslyn where possible to analyze C# accurately in the context of real projects and solutions. |
| G5 | **Extensible and open** — keep the metric model suitable for future language backends, while developing in public under the MIT license. |
| G6 | **Efficient** — analyze codebases with practical resource use and predictable performance. |

## Non-goals (for now)

- Enforcing quality rules, thresholds, or build failures based on metric values.
- Recreating broad static-analysis products such as NDepend; SourceGauge extracts metrics rather than rebuilding a complete analysis suite.
- Dashboards, historical trend storage, or a hosted service.
- Automatic code fixes, refactorings, or recommendations.
- Supporting languages beyond C# in the initial release. Other languages (for example TypeScript, Java, and JavaScript) may be considered later.

## Success indicators

- Metric values match their documented definitions on a reference corpus.
- Third-party tools can consume the output using its documented, versioned contract.
- Analysis performance and output stability meet documented non-functional requirements.
- Adoption by downstream tools, external contributions, and users analyzing real codebases.

## Guiding principles

1. Correctness and clear definitions over number of metrics.
2. Consistent, machine-readable output over a user interface tied to one tool.
3. Keep reported measurements separate from policy decisions and build enforcement.
4. Start with Roslyn and C#; add language-specific analysis without fragmenting the shared output contract.
5. Decisions are documented ([ADRs](adr/README.md)); requirements are traceable ([requirements](requirements/README.md)).
6. Humans own decisions; AI agents assist under explicit [guardrails](../AGENTS.md).
