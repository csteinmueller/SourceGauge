---
description: "Create a technical-debt issue draft from a finding or shortcut."
tools: [read, search]
---

Draft a GitHub issue using the **Technical debt** template (`.github/ISSUE_TEMPLATE/tech_debt.yml`) for:

${input:debt:Describe the shortcut, flaw or cleanup item and where it is}

Output the title (prefix `debt: `) and all template fields (description, origin, impact, remediation, severity, type) as Markdown the user can paste into a new issue. Do not create the issue yourself.
