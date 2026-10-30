---
applyTo: "**/*.cs"
---

# C# / Roslyn analysis conventions

- Use Roslyn project and compilation context where it improves C# metric correctness; do not require a SourceGauge reference in the analyzed project.
- Keep metric definitions and results independent of Roslyn-specific syntax and symbol types at the shared output boundary.
- Keep analysis stateless where practical, avoid unnecessary work and allocations in large codebases, and account for partial types, local functions, lambdas, expression bodies, top-level statements, and generated code as appropriate to each metric.
- Do not translate metric values into analyzer diagnostics, threshold violations, or build failures.
- Tests cover documented metric semantics, representative C# edge cases, and the structured output contract.
