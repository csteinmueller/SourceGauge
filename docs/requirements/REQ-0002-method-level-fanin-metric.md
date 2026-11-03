# REQ-0002: Method-level FanIn metric

- **Kind:** FR
- **Priority:** Must
- **Status:** Accepted
- **Vision goal:** G1, G2, G3, G4
- **Issue:** #19
- **Related ADRs:** ADR-0008, ADR-0007

## Statement

SourceGauge shall report method-level FanIn as the number of distinct eligible source caller methods with at least one statically resolved direct call to an eligible target method within the selected project or solution and target-framework context.

## Rationale

Downstream consumers need a reproducible measure of incoming source-method dependencies. Counting distinct callers rather than call sites makes the value independent of repeated calls by one method. The accepted definition scopes the metric to relationships Roslyn can resolve from the selected source analysis boundary.

## Acceptance criteria

- **Given** an eligible target method with no eligible callers in the analysis boundary, **when** FanIn is collected, **then** the method is reported with value `0` and unit `count`.
- **Given** one caller method contains multiple eligible call sites to the same target, **when** FanIn is collected, **then** that caller contributes exactly one to the target's value.
- **Given** multiple eligible caller methods resolve calls to a target, **when** FanIn is collected, **then** each distinct caller contributes one, self-recursion contributes zero, and mutual recursion between distinct methods is counted.
- **Given** an eligible call in a project or solution, **when** FanIn is collected, **then** only relationships within the selected boundary count; solution analysis includes cross-project calls only when both caller and target projects are in that solution.
- **Given** a project with multiple target-framework contexts, **when** FanIn is collected, **then** each context is analyzed independently and results are identified with the target framework as specified by ADR-0007.
- **Given** calls involving overloads, constructors, interface/virtual dispatch, generated code, dynamic or unresolved calls, **when** FanIn is collected, **then** results follow the accepted rules and exclusions in ADR-0008.
- **Given** collected FanIn values, **when** they are emitted, **then** they use the language-neutral result contract in REQ-0001 and ADR-0007 and do not create diagnostics, thresholds, or build failures.

## Notes

The normative eligibility, call-resolution, dispatch, generated-code, and boundary rules are defined in accepted ADR-0008. The approved representative expected-value fixture matrix is maintained in issue #19.