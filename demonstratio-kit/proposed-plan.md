# Proposed fictional demonstration plan

Prepared from the six records in `work-notes.xlsx` on 2026-09-27. Explicitly approved by Søren ("I approve"), saved as tasks **1** and **2**, and read back successfully on the same date. The approved review comment was added through the frontend and verified. See [evidence](evidence/saved-result.json) and [the rehearsal report](rehearsal-2026-09-27.md). This is a technical rehearsal in the existing conversation, which already contains the expected outcome; it is not a blind extraction test.

Destination: the newly initialized `artifacts/promotion-rehearsal/OkfTodoPromotionDemo-20260927/promotion-demo.db`, confirmed empty through the installed application's command interface. List: **Default list**, ID 1. The normal user database is not the destination.

## Task 1: Investigate invoice export timeout (DEMO-17)

- Type: Investigation (`INVESTIGATION`), discovered from the demo database.
- Status: Active (`ACTIVE`, application creation rule).
- Priority: Normal (`NORMAL`, application default, not stated urgency).
- Body format: Markdown (`MARKDOWN`).
- Source reference: `DEMO-17`; source category and URL unset.
- Owner, responsible person, and deadline: unset.
- Tags: none.
- Waiting target: **Maria — failing example for DEMO-17**, unresolved. This places the task in Waiting even though log inspection can proceed.

Body to save:

```markdown
## Facts
- Invoice export was reported to time out after an upgrade. The cause is unknown.
- Export logs have not been checked yet.
- Maria has already been asked for a failing example. No reply or promised date is recorded.
- A second note repeats the same DEMO-17 report; it does not establish a separate failure.

## Proposed next steps
- Check available export logs for timeout details.
- Obtain the example already requested from Maria.
- Reproduce and investigate the timeout when enough evidence is available.

## Open questions
- What do the logs show, and which input reproduces the timeout?
- Who owns the investigation, and is there a deadline?

## Source
Fictional example: work-notes.xlsx, Work notes, SRC-01, SRC-02, SRC-03, SRC-05.
The investigation remains Active with an unresolved wait for Maria's example.
Log inspection can proceed while that response is outstanding.
```

Actual checklist items to create, all unchecked:

1. Check available export logs for timeout details.
2. Obtain the failing example already requested from Maria.
3. Reproduce and investigate the timeout when enough evidence is available.

## Task 2: Prepare release checklist

- Type: Request (`REQUEST`), available default type; a categorization choice.
- Status: Active (`ACTIVE`, application creation rule).
- Priority: Normal (`NORMAL`, application default, not stated urgency).
- Body format: Markdown (`MARKDOWN`).
- Source reference: `DEMO-RELEASE`; a demonstration identifier, not a source-provided release number.
- Source category, URL, owner, responsible person, and deadline: unset.
- Tags: none. No waiting target.

Body to save:

```markdown
## Facts
- Release preparation is separate from the DEMO-17 investigation.
- The checklist must include rollback and verification steps.
- Release version, date, owner, and deployment procedure have not been decided.

## Proposed next steps
- Agree the release version and deployment procedure.
- Define rollback steps.
- Define post-deployment verification steps.

## Open questions
- Which release and procedure should the checklist cover?
- Who is responsible, and when is it needed?

## Source
Fictional example: work-notes.xlsx, Work notes, SRC-04 and SRC-06.
DEMO-RELEASE is an identifier for this demonstration, not a real release number.
```

Actual checklist items to create, all unchecked:

1. Agree the release version and deployment procedure.
2. Define rollback steps.
3. Define post-deployment verification steps.

## Verification action

After saving and reading both tasks back, add this review comment to task 1 through the isolated real frontend: `Demo review: source notes checked against the saved plan.` Then verify the comment through the command interface. No investigation work will be marked completed.

No extra tasks, lists, lookups, attachments, relationships, or edits to existing work are proposed. Recheck demo references before creation to avoid duplicate retries.
