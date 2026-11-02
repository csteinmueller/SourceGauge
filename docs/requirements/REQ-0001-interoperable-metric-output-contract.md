# REQ-0001: Interoperable, language-neutral metric output contract

- **Kind:** FR
- **Priority:** Must
- **Status:** Accepted
- **Vision goal:** G1, G3, G5
- **Issue:** #21
- **Related ADRs:** ADR-0006

## Statement

SourceGauge shall expose analysis results through a documented, machine-readable, versioned metric output contract backed by a language-neutral shared model that represents metric identity, measured entity and scope, value and unit, analyzed-source identity, and relevant tool, schema, and run provenance.

## Rationale

Downstream tools need dependable metric data they can consume without depending on SourceGauge internals or Roslyn. A shared, versioned contract supports interoperability while allowing future language backends to provide consistent semantic data.

## Acceptance criteria

- **Given** a representative analysis result, **when** it is represented by the shared model and interpreted according to the documented contract, **then** a consumer can identify the contract version and interpret the metric identity, measured entity and scope, value and unit, analyzed-source identity, and relevant provenance.
- **Given** the shared model is used by the C# backend, **when** the model is compiled and contract-tested, **then** it contains no Roslyn-specific types and its semantic representation does not depend on a particular language backend.
- **Given** the contract is implemented, **when** representative results are validated against its documented semantics, **then** the tests verify those semantics without relying on an unapproved encoding or schema decision.

## Notes

This requirement does not select JSON, CSV, SARIF, or another encoding; define concrete schema fields or structure beyond the semantic information in the statement; select a version representation or compatibility policy; or define CLI destinations and failure behavior. Those architecture decisions are governed by ADR-0006 and its follow-up for the concrete export contract.