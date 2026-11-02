# ADR-0008: FanIn Metric Definition

- **Status:** Proposed
- **Date:** 2026-11-01
- **Deciders:** Maintainer acceptance pending
- **Related:** #19, REQ-0001, ADR-0007

## Context

SourceGauge needs a precisely defined first metric whose values are reproducible and interpretable by downstream tools. FanIn can mean a count of call sites, distinct caller methods, or callers aggregated at type level. Roslyn can resolve many C# call relationships semantically, but virtual dispatch, generated code, external references, and dynamic calls require explicit scope rules.

REQ-0001 and ADR-0007 establish the language-neutral result contract and JSON export behavior. This ADR proposes FanIn semantics only; it does not change the result model, schema, serializer, or CLI contract. Issue #19 requires maintainer approval of the definition and expected fixtures before implementation.

## Options considered

1. **Method-level, distinct caller methods (recommended)** — Pros: measures incoming dependencies on an individual method; repeated call sites in one caller do not inflate the value; Roslyn symbol binding resolves overloads. Cons: requires explicit rules for constructors, generated code, and dispatch; one caller contributes at most one regardless of call frequency.
2. **Method-level, call-site count** — Pros: reflects the number of syntactic call relationships. Cons: repeated calls from one method inflate coupling and make the value sensitive to refactoring call frequency without changing the set of dependent methods.
3. **Type-level, distinct caller types** — Pros: summarizes incoming dependencies at a broader architectural boundary. Cons: aggregates methods and can hide which operation is depended upon; requires separate rules for aggregation across members and partial types.

## Decision

**Proposed definition for maintainer approval; no option is accepted until this ADR is accepted and fixtures are approved.** Define FanIn as the number of distinct eligible source caller methods with at least one statically resolved direct call to an eligible target method.

The recommended v1 rules are:

- **Measurement level:** method. Eligible targets are source-declared ordinary methods and instance constructors. Eligible callers are source-declared ordinary methods and instance or static constructors. Static constructors are excluded as targets because C# has no explicit source call to a `.cctor`; calls written inside a static constructor still contribute as calls from that caller. Each method symbol is identified by its canonical declaration identity; partial declarations represent one method.
- **Counted relationships:** an `IInvocationOperation` target, an object-creation constructor, or an explicit `this`/`base` constructor initializer. Generic constructed methods map to their original declaration; reduced extension calls map to the declared extension method. Overloads remain distinct by resolved symbol identity.
- **Distinct callers:** a caller method contributes at most one to a target, regardless of how many call sites it contains. A method does not count as its own caller; recursive self-calls contribute zero, while a different method in a mutual-recursion cycle contributes normally.
- **Dispatch:** count the symbol selected by compile-time binding. An interface call contributes to the interface member; it does not fan out to possible implementations. A call statically bound to an override or base member contributes to that resolved member. Do not perform whole-program virtual dispatch analysis.
- **Analysis boundary:** for a project input, include source declarations in the selected project; for a solution input, include declarations in projects that belong to that solution. Only caller and target methods inside the same selected boundary participate. Solution analysis counts cross-project calls when both projects are in the solution; references outside the boundary do not contribute.
- **Target frameworks:** analyze each target-framework context independently; do not union call relationships across frameworks. When a project has multiple target contexts, include the target framework in `entity.projectId` as specified by ADR-0007 so records from different framework contexts remain distinct. A project with one target context uses its relative project path without a framework suffix.
- **Excluded entities and calls:** exclude generated source, local functions, lambdas, static constructors as targets, property/indexer/event accessors, operators, implicit conversions, delegate invocations, method-group conversions, reflection, dynamic calls, and unresolved bindings. They do not create target records in v1; static constructors may still be callers as described above.
- **Zero values:** emit a FanIn record with value `0` for every eligible target method with no distinct caller in the boundary.
- **Output:** use metric identity `sourcegauge.fanin`, scope `project` or `solution` as appropriate, unit `count`, and the entity identity from ADR-0007. Values remain data and do not create diagnostics or build failures.

Before implementation, maintainers must accept or revise the measurement level, caller-vs-call-site rule, included call forms and entities, dispatch behavior, generated-code policy, analysis boundary, and per-target-framework behavior. They must approve representative fixtures and expected values covering zero FanIn, repeated call sites, overloads, constructors (including static constructors as callers), recursion, interface/virtual calls, generated and unresolved code, multi-target projects, and cross-project references within a solution.

## Consequences

If accepted, the definition is implementable with Roslyn semantic operations and yields a stable count of distinct incoming source-method dependencies. It deliberately does not measure call frequency, runtime dispatch, external callers, or type-level aggregation. Those would require a separately defined metric or a future ADR.

Until maintainers accept this ADR and approve the fixture set in #19, the recommendation is not an implementation contract and no FanIn collection code should be added.