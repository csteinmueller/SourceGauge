---
description: "Record an accepted requirement (FR or NFR) in docs/requirements with a stable REQ ID."
agent: developer
tools: [read, search, edit]
---

Record this requirement: ${input:requirement:Requirement statement or issue number}

1. Read [docs/requirements/README.md](../../docs/requirements/README.md) and [the template](../../docs/requirements/_template.md).
2. Assign the next free `REQ-xxxx` ID and create `docs/requirements/REQ-xxxx-<slug>.md`.
3. Link it to the vision goal it supports and to the GitHub issue.
4. Acceptance criteria must be testable (Given/When/Then).
5. Add the requirement to the index table in `docs/requirements/README.md`. Do not modify other files.
