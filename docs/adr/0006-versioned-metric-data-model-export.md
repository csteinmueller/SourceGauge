# ADR-0006: Versioned Metric Data Model and Export

- **Status:** Accepted
- **Date:** 2026-11-01
- **Deciders:** @csteinmueller
- **Related:** REQ-0001, #18, #21

## Context

Downstream tools need a stable, documented contract for consuming SourceGauge results. The contract must be language-neutral, independent of Roslyn, and identify each metric, its measured entity and scope, value and unit, analyzed source, and relevant tool, schema, and run provenance.

Issue #18 recommends JSON for MVP1 but explicitly leaves the encoding, schema, compatibility policy, fields, CLI behavior, failure behavior, provenance variability, and ordering for maintainer decision. Acceptance of this ADR records the direction and sequencing below; it does not accept those concrete contract choices.

## Options considered

1. **JSON (recommended for maintainer review)** — Pros: broad parser and tooling support; represents nested envelopes and provenance naturally; preserves typed values. Cons: schema and compatibility rules must be defined separately; more verbose than tabular formats.
2. **CSV** — Pros: simple, compact for flat records, widely usable in spreadsheets. Cons: awkward for nested provenance and entity data; weaker representation of types and schema evolution.
3. **SARIF** — Pros: established, versioned format with broad tooling support in static analysis. Cons: oriented toward analysis results and diagnostics; metric records may require conventions that are less direct for general-purpose consumers.

MVP1 should select one encoding. Multiple export formats are out of scope for #18.

## Decision

Accept a language-neutral result envelope with a versioned schema, independent of Roslyn, as required by REQ-0001. Defer selection of the encoding and concrete schema and export policies to a follow-up ADR under #18. JSON remains a recommendation for that decision, not an accepted encoding.

The envelope should identify the analyzed source and include tool and schema provenance plus run metadata. Each metric record should carry a stable metric identity, measured entity identity and type, scope, value, and unit. These are semantic requirements for the proposal; exact field names, types, requiredness, and definitions remain subject to maintainer approval.

For CLI behavior, the recommendation is to emit the machine-readable payload to stdout by default, support an explicitly selected file destination, and send operational messages to stderr. Metric values must not determine process success. Serialization or destination failures should produce a nonzero exit status and must not be reported as successful export; partial-output behavior, including whether output is buffered or files are written atomically, requires approval.

For reproducibility, the recommendation is deterministic ordering by metric identity, entity identity, and scope, with a stable tie-break rule. Source identity and tool/schema versions should identify the analysis inputs and producer. Timestamps, run IDs, absolute paths, and other values that may vary between identical analyses need an explicit policy; they must not undermine the approved determinism guarantee.

The following choices remain unresolved and require maintainer acceptance before implementation fixes the contract:

- Initial encoding.
- Schema-version representation and compatibility policy.
- Exact envelope and metric fields, types, and meanings.
- Source identity and which run/provenance values may vary.
- Default and optional output destinations, operational-message separation, and export/partial-output failure behavior.
- Deterministic ordering and tie-breaking rules.

## Consequences

This decision establishes the language-neutral model boundary without introducing Roslyn-specific types into the shared metric model. The concrete export contract will require a consumer-facing schema or equivalent normative contract, a representative sample, and tests for empty results, escaping, ordering, metadata variability, and output failures.

This accepted ADR establishes the model boundary, the semantic requirements in REQ-0001, and the decision sequence; it is not the implementation contract for #18. Implementation of the export contract must wait until the unresolved choices above are recorded in an accepted follow-up ADR.