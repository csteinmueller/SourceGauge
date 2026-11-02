---
description: "Manually request an architecture review of the current branch; the developer agent normally invokes review automatically after quality gates pass."
agent: architect
---

Review the changes on the current branch compared to `origin/main`.

Follow the review procedure and output format defined in your agent instructions. Context provided by the developer:

${input:context:Issue number and a short summary of what changed}
