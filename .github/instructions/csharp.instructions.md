---
applyTo: "**/*.cs"
---

# C# / Roslyn analyzer conventions

- `src/SourceGauge.Analyzers` targets `netstandard2.0`: no APIs beyond .NET Standard 2.0, no runtime package dependencies.
- Every `DiagnosticAnalyzer` must:
  - be `sealed`, decorated with `[DiagnosticAnalyzer(LanguageNames.CSharp)]`;
  - call `context.EnableConcurrentExecution()` and `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` in `Initialize`;
  - hold no mutable state;
  - define descriptors as `static readonly` fields with ID `SGxxxx`, category `DiagnosticConventions.MetricsCategory`, and a `helpLinkUri`.
- Read thresholds from `AnalyzerConfigOptions` (`.editorconfig` keys `sourcegauge.<metric>.<option>`), with documented defaults.
- Avoid LINQ, closures, and boxing in callbacks that run per syntax node.
- Tests cover: no diagnostic below threshold, diagnostic at/above threshold, configuration override, and edge cases (partial types, local functions, lambdas, generated code).
