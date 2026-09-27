# Presenter review: expected outcome

This is a review aid for the fictional [workbook](work-notes.xlsx), not the observed output of an assistant run. Keep it out of the planning assistant's input. Exact titles and prose may differ; assess facts and organization rather than string matching.

## Proposed grouping

After the presenter confirms the desired grouping, expect two tasks:

| Task | Source rows | Facts and useful actions |
| --- | --- | --- |
| Investigate invoice export timeout (DEMO-17) | SRC-01, SRC-02, SRC-03, SRC-05; worksheet rows 5, 6, 7, 9 | Timeout reported after an upgrade; cause unknown. Check logs. Obtain the outstanding example from Maria. Preserve the repeated report as context without creating a duplicate task. |
| Prepare release checklist | SRC-04, SRC-06; worksheet rows 8, 10 | Independent release-preparation work. Define rollback and verification steps. Release version, date, owner, and procedure remain undecided. |

A first proposal with a separately tracked follow-up can be reasonable if explained. The presenter then chooses the two-task organization in the walkthrough. Do not describe a defensible alternative as an extraction failure.

## Investigation details

The body should retain case reference DEMO-17, the source IDs, and the reported sequence. An upgrade preceding the report is not proof that the upgrade caused the problem.

Possible checklist:

- Check available export logs for timeout details.
- Obtain the failing example already requested from Maria.
- Reproduce and investigate the timeout once sufficient evidence is available. This is a proposed next step, not a source-reported action already performed.

The request to Maria has already been sent. Her reply and the example are outstanding. Do not turn her into the task owner, mark the example received, or propose sending the same initial request as though it had never happened. A later reminder is only a proposal; no reminder date was supplied.

Discuss how to represent waiting in the final proposal. A task-level wait target for Maria/the example can coexist with an unchecked log-inspection action, but may move the task out of Ready/Act now. Keeping the outstanding response in the body/checklist is another deliberate choice. The approved proposal, supported application behavior, and stored representation must agree.

## Release preparation details

Keep this work separate from DEMO-17. There is no evidence that this release fixes the timeout or depends on the investigation.

Possible checklist:

- Agree the release version and deployment procedure.
- Define rollback steps.
- Define post-deployment verification steps.

These are planning actions. No rollback has happened, no release is scheduled, and no verification has passed. Do not invent a date, owner, release number, environment, or successful outcome.

## Lookup and field checks

- Discover the demonstration database's actual lists and active lookup values. Do not invent IDs or assume display names are stable codes.
- A reasonable available task type is acceptable if explained and reviewed.
- If the application requires/defaults a priority or status, identify that as an application default or reviewed choice. It is not a priority stated in the spreadsheet.
- Owner, responsible person, and deadline stay unset unless the presenter explicitly supplies them.
- Keep useful source IDs in task context so every important fact is traceable.
- Do not create custom lookups or extra lists merely to fit the example.

## Common mistakes to look for

| Mistake | Correct treatment |
| --- | --- |
| Six rows become six unrelated tasks | Group by the work and case reference; explain the grouping. |
| SRC-05 becomes a second incident | Same reported problem; preserve context without duplication. |
| “Upgrade caused the timeout” | Cause is unknown; only the reported order is known. |
| Maria becomes the investigation owner | She is the person asked for an example. Ownership is unspecified. |
| An initial request to Maria is marked outstanding | Sending the request is already done; receiving the example is outstanding. |
| Release work is described as a fix for DEMO-17 | The spreadsheet explicitly calls it separate work. |
| Dates or urgency appear without explanation | Leave dates unset; distinguish reviewed defaults from source facts. |
| Tasks are saved while the plan is being discussed | Draft first; save only the explicitly approved final proposal. |
| An uncertain retry creates duplicate tasks | Read existing demo references first and stop if matches exist. |

## Saved-result acceptance checklist

Completed checks below refer to the 2026-09-27 technical rehearsal, with [saved data](evidence/saved-result.json) and limitations in the [run record](README.md#run-record):

- [x] The selected database is the isolated demonstration database.
- [x] The final approved proposal contains two tasks covering all six source notes.
- [x] Both tasks exist once in the reviewed existing list, with recorded IDs.
- [x] Saved titles, bodies, types, priorities, and statuses match the proposal.
- [x] Checklists exist as actual checklist items where approved, not only body text.
- [x] Any approved wait target exists with the expected unresolved state; otherwise the agreed body/checklist representation is present.
- [x] No invented owner/responsible person, deadline, root cause, or completed work was saved.
- [x] Source references are retained and the repeated report caused no duplicate task.
- [x] Read-back confirms the saved data and both tasks appear in the real frontend browser harness.
- [ ] Native released-app display verified, including readable body formatting.
- [x] The presenter-added review comment is visible in Timeline and can be read back.

No application-run item is complete merely because this expected answer has been written.
