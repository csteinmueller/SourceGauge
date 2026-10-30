# Requirements

Accepted requirements live here as one Markdown file each, with a stable ID that never gets reused.

- **Functional requirements (FR)** — what analysis does and what metric data it produces (e.g. "reports cyclomatic complexity per method in the output").
- **Non-functional requirements (NFR)** — quality attributes (performance, compatibility, usability, security, …).

## Process

1. Propose via a GitHub issue using the **Requirement** template (label `requirement`).
2. Discuss and refine until the statement is testable and prioritized (MoSCoW).
3. Once accepted, record it here with `/new-requirement` (or by copying [_template.md](_template.md)) as `REQ-xxxx-<slug>.md` and add it to the index below.
4. Implementation PRs reference the `REQ-xxxx` ID; tests should name or reference it where practical.
5. Changing an accepted requirement → PR updating the file; superseding → set status `Superseded by REQ-yyyy`.

## Statuses

`Proposed` → `Accepted` → `Implemented` (or `Rejected` / `Superseded`)

## Index

| ID | Title | Kind | Priority | Status |
|----|-------|------|----------|--------|
| — | _No requirements recorded yet._ | | | |
