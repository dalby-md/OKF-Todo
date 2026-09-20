# Waiting follow-up design — issue #5

Status: proposed implementation design. No follow-up field or action is implemented by the September 2026 UI repair pass. The current product rule deliberately has no structured follow-up date; this proposal defines the separate vertical slice that would change it.

## User workflow

For an active task waiting for customer logs, keep the task Deadline as the delivery commitment. Set **Follow up on** to tomorrow to record the next contact date. The Waiting row shows the target, waiting age, and follow-up date. **Followed up** records a short contact note and optionally schedules the next follow-up. It never changes Deadline.

Use date-only values in the user's local calendar. A follow-up is due on or before today. A deadline is overdue only before today. Neither is a timestamp or an automatic notification promise.

## Views and lifecycle

- Add a distinct **Follow-ups** view containing active, non-Trash tasks with an unresolved waiting target and a follow-up date on or before today. It respects the selected list, search, and other filters. Do not alter Active, Ready, Waiting, Attention, or Act now semantics.
- Show waiting age from the unresolved target's WaitingSince, using local calendar dates. A follow-up does not restart that age.
- Setting, clearing, and rescheduling follow-up dates requires the task to be active and waiting. A missing date means no scheduled follow-up.
- Clearing waiting or resolving it during Complete closes the waiting episode; its old date remains historical but stops appearing in due work.
- Cancel closes the waiting episode as well. Reopen starts active without reviving an old waiting target or contact date. This lifecycle adjustment must be reviewed and documented with the implementation.
- Replacing a waiting target starts a new episode with no inferred follow-up date. Preserve the old date only in the resolved episode.
- Trash suppresses due work and forbids writes. Restore can make an unchanged active waiting episode due again.
- **Followed up** requires a nonempty note. The user explicitly chooses a next date or clears scheduling; there is no automatic recurrence. Record a readable Timeline entry including the note and old/new dates in the same transaction.

## Implementation slice

1. Update PRD, DATA_MODEL, and the relevant lifecycle rules with the accepted behavior before implementation.
2. Add nullable DateOnly `FollowUpOn` to each waiting episode through a reviewed EF Core migration and snapshot. Existing rows receive null; never infer dates from Deadline. Preserve the one-active-wait-target constraint. Decide indexing from the actual due-work query plan rather than adding speculative indexes.
3. Extend the existing task/lifecycle service and command validation layer. Read results expose the active waiting ID, follow-up date, waiting age, and due state. Safe partial writes preserve omitted fields; explicit null clears only the follow-up date. Reject stale waiting episode IDs so a delayed client cannot update a replaced target.
4. Implement the date field, Follow-ups view, due badges, and Followed up dialog. Route unsaved task edits through the existing Save/Discard/Cancel protection. Keep Timeline last.
5. Expose discoverable MCP reads and tools for setting/clearing/rescheduling the active wait's date and recording a follow-up note. Reuse shared commands. Date-setting is idempotent; recording contact is a non-idempotent write. Both are closed-world, non-destructive operations that require client approval and read-back verification. Extend tool discovery and real stdio contract tests, with current values available before writes.
6. Refresh canonical desktop and MCP Help, normal application output, and the generated/validated OKF bundle using the repo-local compile-okf-context skill.

## Acceptance checks

- Upgrade a populated database: all existing fields, attachments, and history survive; no dates are invented.
- Compare due yesterday, today, tomorrow, and null, including local midnight and daylight-saving boundaries.
- Test every lifecycle row above, active-wait replacement, repeated date-setting, invalid dates, missing waiting episodes, and stale IDs.
- Verify Deadline never changes and unrelated partial-update fields survive.
- Run the same read/schedule/record/reschedule/clear flow through desktop commands and real MCP stdio; inspect persisted state and history.
- Verify Follow-ups scope, filter/export behavior, keyboard access, and responsive layouts.

This design does not add email delivery, reminders, background jobs, or autonomous contact.
