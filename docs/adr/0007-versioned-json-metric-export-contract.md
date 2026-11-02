# ADR-0007: Versioned JSON Metric Export Contract

- **Status:** Accepted
- **Date:** 2026-11-01
- **Deciders:** @csteinmueller
- **Related:** REQ-0001, ADR-0006, #18

## Context

REQ-0001 establishes the semantic requirement for documented, machine-readable, versioned metric output backed by a language-neutral model. ADR-0006 accepts that model boundary and defers the concrete encoding, schema, compatibility, CLI, provenance, and ordering policies. Issue #18 requires those choices before export implementation begins; this ADR records the maintainer-approved contract.

The current CLI already routes operational messages to stderr. The export contract should preserve that separation while remaining deterministic and consumable by tools that do not reference SourceGauge assemblies.

## Options considered

1. **JSON with a published JSON Schema** — Pros: broad parser support; represents structured metric, entity, source, and provenance data; supports a separately versioned normative schema. Cons: requires explicit schema and compatibility rules; more verbose than tabular output.
2. **CSV** — Pros: simple and convenient for flat spreadsheet workflows. Cons: nested entities and provenance require ad hoc columns or repeated data; weaker typing and schema evolution.
3. **SARIF** — Pros: established, versioned format with analysis-tool support. Cons: primarily models diagnostics/results; neutral metric records would rely on conventions not directly expressed by its core model.

## Decision

Use JSON as the single MVP1 encoding, with a checked-in JSON Schema using JSON Schema Draft 2020-12. Do not add a runtime schema-validation dependency.

The top-level document contains:

- `schemaVersion`: a string containing the contract major version, initially `"1"`.
- `tool`: producer name and version.
- `run`: analyzed input kind and normalized input path relative to the process working directory, plus analysis configuration when the CLI exposes configuration that affects results.
- `metrics`: an array of metric records.

Each metric record contains:

- `id`: a stable, language-neutral metric identity, such as `sourcegauge.fanin`.
- `scope`: the analysis boundary over which the metric is calculated, such as a project or solution.
- `entity`: the measured entity's kind, project identity, and canonical identifier; a human-readable display name may be included separately and is not part of identity.
- `source`: the source file identity relative to the analyzed input root (the project directory for a project input, or solution directory for a solution input) when the entity originates from a source file.
- `value`: a finite decimal value serialized as a JSON number.
- `unit`: a stable string token, such as `count`.

For C# version 1, `entity.projectId` is the normalized project path relative to the analyzed input root, with the target framework included when multiple target contexts are emitted. The canonical `entity.id` distinguishes declarations using the containing namespace and nested type chain (including generic arity), member kind and name, method generic arity, and ordered parameter type identities and ref-kinds. Constructors use distinct `.ctor` and `.cctor` identities; indexers include their parameter signatures; explicit interface implementations include the interface-qualified member identity. Thus overloaded and generic members cannot share an identifier, and the project identity distinguishes identical symbols in separate projects. Identifiers are opaque to consumers and are guaranteed only within an analyzed input and this identity-format version; identifiers from different language backends need not be comparable.

A document contains at most one metric record for each `(id, scope, entity.projectId, entity.kind, entity.id)` tuple. Metric definitions aggregate their observations into that record. This uniqueness rule makes the proposed ordering a total order.

For example, the shape is:

```json
{
  "schemaVersion": "1",
  "tool": { "name": "SourceGauge", "version": "1.0.0" },
  "run": { "input": { "kind": "project", "path": "src/App/App.csproj" } },
  "metrics": [
    {
      "id": "sourcegauge.fanin",
      "scope": "project",
      "entity": {
        "kind": "method",
        "projectId": "App.csproj",
        "id": "N.Example`1.Run``1(System.Int32)"
      },
      "source": { "path": "Example.cs" },
      "value": 3,
      "unit": "count"
    }
  ]
}
```

Consumers must ignore unknown properties. Producers may add optional properties without changing the major version, provided existing meanings and required fields remain unchanged. Removing or renaming a required property, changing a property's type or meaning, or changing metric identity/unit semantics requires a new major `schemaVersion`.

The CLI emits exactly one JSON document to stdout by default and supports an explicit file destination. All operational messages remain on stderr. Empty analysis results produce a valid document with an empty `metrics` array. Analysis or serialization failures return a nonzero exit code. Serialize fully before writing stdout so serialization failures cannot emit a partial document; a stdout I/O failure can still leave partial bytes, returns nonzero, and must not emit a success message. For file output, write a temporary sibling file and replace the destination only after serialization and writing succeed; replacement failure is reported as an export failure.

For deterministic results, omit timestamps and random run identifiers in version 1. Normalize input and source paths using `/`; paths are relative, and linked source paths outside the analyzed input root retain `..` segments rather than becoming absolute paths. Sort metric records by `id`, `scope`, `entity.projectId`, `entity.kind`, and `entity.id`, comparing each string by its UTF-8 bytes in ascending order, independent of locale and case folding. The uniqueness rule above guarantees no two records tie on the sort key. JSON object property order is not semantically significant; the producer emits fields in schema declaration order for repeatable output.

Paths must contain a non-whitespace character and must not contain control characters. The CLI rejects an input on a different Windows volume from the working directory before analysis because that input cannot be represented as a relative path under this contract. The input is reported as an invalid argument; the CLI does not emit an absolute path.

Version 1 metric values use the .NET `decimal` range and precision, serialized as invariant JSON numbers. NaN and infinity are not permitted. Values outside the supported range fail analysis or export; they must never be rounded silently. Changes to these accepted contract rules require a new or explicitly maintainer-approved ADR update before implementation.

## Consequences

This decision provides consumers with a language-neutral payload and a normative schema while keeping JSON handling in the CLI boundary. It adds a maintained schema and compatibility obligation. Deterministic output excludes wall-clock and random run metadata in version 1; input and tool provenance identify the analysis context instead.

This decision does not define FanIn semantics, metric calculations, additional output encodings, thresholds, or build enforcement. Those remain outside its scope.