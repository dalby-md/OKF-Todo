# Excel notes to tasks: video transcript

This captioned recording uses fictional data and has no audio. It combines a workbook preview, a replay of the earlier planning conversation, and recordings of the real OKF Todo frontend in a local browser test host. The source is [work-notes.xlsx](../work-notes.xlsx).

## 1. Start with the work you already have

The Work notes worksheet has six records. Four concern invoice export case DEMO-17: a timeout reported after an upgrade, unchecked logs, an example already requested from Maria, and a repeated report. Two concern separate release preparation, including rollback and verification. Owners and deadlines are missing.

The on-screen table contains the actual workbook values; it is a preview, not a recording of Microsoft Excel.

## 2. Use the instructions for this installation

The real Help page displays the OKF entry file and the active demonstration database path. Selecting Copy prompt copies these values. The capture checks that the copied prompt selects the isolated recording database and contains no unresolved placeholders.

The database is separate from the user's normal work.

## 3. Ask for a plan before saving

The displayed request asks the assistant to read the workbook, group related notes, identify repeated reports, preserve source IDs, and propose tasks and checklists. Unknown owners and deadlines should remain unset. No save should occur before explicit approval.

This is an excerpt from the demonstration guide. Codex performed the earlier rehearsal; no new AI inference is portrayed in this recording.

## 4. Review the proposed tasks

The reviewed plan groups SRC-01, SRC-02, SRC-03, and SRC-05 into one invoice-export investigation. Its checklist covers logs, obtaining Maria's example, and reproducing/investigating the timeout. The upgrade is not a confirmed cause.

SRC-04 and SRC-06 become a separate release checklist covering agreement on the version and procedure, rollback, and post-deployment verification.

Maria is the person asked for an example, not an assigned task owner. Record the unresolved wait. Leave owners and deadlines unset.

## 5. The user decides what gets saved

The video quotes Søren's prior response, “I approve”, to the complete two-task proposal and review comment. The agreed tasks use Active status, Normal priority, and Default list, with three unchecked checklist items each.

This is a replay of the earlier human approval. The requested recording repeats the same approved plan in a fresh isolated demonstration database.

## 6. Save and read back

The installed application command interface created the two tasks and six checklist items. The saved tasks were read back. Execution waits are omitted; the video resumes with the resulting application views. See [recording-result.json](recording-result.json).

## 7. Continue the work in OKF Todo

The recorded interface shows both tasks, their checklists, and the unresolved Maria waiting label. All checklist items remain unchecked. Planning the work did not complete it.

The captured frontend displays the Markdown body as literal text. The stored Markdown is unchanged. This remains a presentation issue to check in the native released application.

OKF Todo is free and open source. It requires a separate AI assistant for this planning workflow; that assistant's account, costs, and data handling are separate.

## Capture details

- Recorded on 27 September 2026 at 1440 × 1000 pixels.
- Workbook cells were read from the actual XLSX file. Proposal and approval scenes are explicitly labeled replays.
- Help and result scenes use real frontend components and application services through a temporary browser host, not fabricated task screens.
- Task creation uses the installed application command adapter. Version differences and native-display limitations from the [rehearsal report](../rehearsal-2026-09-27.md) still apply.
- A new empty database was initialized at `artifacts/promotion-video/OkfTodoPromotionDemo-recording/promotion-demo.db`. The normal user database was not used.
- Loading and execution waits were removed; scene reading times are editorial choices, not performance measurements.
- This is a first viewing copy. It is not a continuous recording of native Excel, Codex, and the installed desktop application.

## Verification

The final MP4 is 83.12 seconds long, 1440 × 1000 pixels, approximately 8 MB, and has no audio track. Full-file decoding completed without errors. Browser playback and seeking passed, including the review and saved-task chapters. Local player and documentation links were checked. The fresh saved task bodies, fields, waiting label, and six unchecked checklist items match the previously approved plan.

No application capability changed; MCP changes are not applicable to these recording and documentation artifacts. Native desktop recording and the body-formatting check remain incomplete. Nothing was published externally.
