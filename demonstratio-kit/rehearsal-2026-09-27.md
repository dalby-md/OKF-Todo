# Technical rehearsal — 2026-09-27

Status: **approved plan saved and verified**. Two tasks and six unchecked checklist items exist in the isolated database. The review comment was added through the real frontend and read back. No public outreach has occurred. Native desktop verification and a presentation issue described below remain open.

## Completed checks

- Read the six actual records from `work-notes.xlsx` and prepared [the two-task proposal](proposed-plan.md).
- Initialized a separate, empty database through the installed application's command interface. Confirmed an empty task list, the existing Default list, and available task types and priorities.
- Opened the current checkout's real frontend and application services through a temporary local browser harness with separate preferences. Dismissed the first-run sample-data offer with **Skip**.
- Opened **Help → OKF layer**, checked its displayed database path, selected **Copy prompt**, and read the browser clipboard. The copied prompt contained the selected demonstration database and actual checkout OKF entry path, with no unresolved template placeholders.
- Built the temporary frontend harness and its application dependency in Release: zero warnings and zero errors.
- Following Søren's explicit approval, saved task **1**, Investigate invoice export timeout (DEMO-17), and task **2**, Prepare release checklist, in Default list. Both are Active with Normal priority; owner, responsible person, and deadline are unset.
- Read back and compared both complete bodies and all approved fields, all six actual unchecked checklist items, and the exact unresolved Maria waiting label. Confirmed two tasks with no duplicates; task 2 has no wait.
- Opened both tasks in the real frontend. Added `Demo review: source notes checked against the saved plan.` to task 1 through the interface; verified exactly one matching comment through the installed application command interface, and no comments on task 2.

The user's already-running native desktop app was left running. This rehearsal did not read or write its normal database.

## Versions and method

- Installed application: file version `1.0.0.0`; product version `1.0.0+128431ed76ed0e7434bf2b805bf808a0477bb264`.
- Frontend/services checkout: `50bfe252b2e49a864e08c8f489a6b12d36b4b8d7`.
- Assistant: Codex in this existing conversation, using local file access, the installed application command interface, and browser automation.
- Command destination: `artifacts/promotion-rehearsal/OkfTodoPromotionDemo-20260927/promotion-demo.db`, always explicitly selected using `--okf-database-path`.
- Intended save route: the application's command adapter, following its installed OKF instructions. Direct SQLite task writes and the normal connected MCP destination are not used.

The installed command executable and checkout frontend have different revisions. Browser verification establishes the frontend/service behavior exercised here; it does not establish a complete native Photino run against the released version. The conversation already contains the expected outcome, so this is not a blind interpretation or newcomer test.

## Local evidence

Generated evidence is retained under the ignored `artifacts/promotion-rehearsal/` directory, relative to the repository root:

- `source-notes.json`: the six extracted source records.
- `01-lists.json`, `02-lookups.json`, `03-empty-tasks.json`: successful command responses before any task writes.
- `help-demo.png`: genuine frontend screenshot showing Help and the resolved demonstration paths after copying the prompt. This is a browser-harness capture, not a native desktop capture.
- `commands.py` and `host/`: local rehearsal tooling; not public demonstration inputs.

Machine-specific paths appear in local evidence. Public demonstration material should use a clearly labeled fictional example and a reviewed capture from the intended release.

Portable evidence retained in the kit:

- [saved-result.json](evidence/saved-result.json): final task, checklist, and Timeline read-back; contains only fictional work data.
- [review-comment.png](evidence/review-comment.png): task 1 checklist and verified review comment.
- [release-task.png](evidence/release-task.png): task 2 and the two-task queue.

These screenshots are genuine browser-harness captures, not native desktop captures or a finished promotional recording.

## Findings before recording

The frontend displayed the saved Markdown bodies as literal `##` headings and inline text in its rich-text editor. The stored bodies retain their exact approved Markdown and line breaks, as independently verified. Resolve or explain this presentation behavior in the target release before recording; the current screenshots are evidence, not polished promotion assets.

The temporary host initially failed while logging an EF query warning to Windows Event Log under restricted permissions. Switching that host to console logging resolved the failure; subsequent task-detail and comment checks passed. No application product code was changed.

## Next steps

- [x] Obtain explicit approval for the exact proposal and review comment. Søren: "I approve", 2026-09-27.
- [x] Save only that proposal, then read back every task, checklist, and waiting field. See saved-result.json.
- [x] Inspect both tasks in the interface, add the approved review comment, and verify it through the command interface. Browser-harness frontend check passed.
- [ ] Resolve or explain Markdown presentation in the intended release before recording.
- [ ] Reproduce the complete workflow in the native released application.
- [ ] Record the real workflow and obtain an independent newcomer reproduction.

Campaign task P05 remains open. No source-work checklist item is complete merely because its task plan has been drafted.
