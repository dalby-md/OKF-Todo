# From Excel notes to a task plan

OKF Todo is a free, open-source desktop task manager for development and support work. Use an AI assistant to read existing material, propose a plan, and save the tasks you approve. Continue the work in OKF Todo with checklists, waiting information, and a Timeline of comments and changes.

**[Install on Windows](../README.md#install-on-windows)** · **[Download the example workbook](../demonstratio-kit/work-notes.xlsx)** · **[Follow the demonstration guide](../demonstratio-kit/README.md)**

**[Watch the captioned walkthrough](../demonstratio-kit/video/excel-to-tasks.mp4)**: actual workbook data, a labeled replay of the reviewed proposal and approval, and real browser-hosted application captures. This viewing copy is not a continuous native Excel/Codex recording. [Transcript](../demonstratio-kit/video/transcript.md).

## Before: six notes about two pieces of work

This example uses a fictional spreadsheet. It contains related actions, a repeated report, an unanswered request, and separate release work. No real customer data is involved.

| Source | Note | Context supplied in the spreadsheet |
| --- | --- | --- |
| SRC-01 | Invoice export times out | Reported after an upgrade; cause unknown. Reference DEMO-17. |
| SRC-02 | Check export logs | Logs for DEMO-17 have not been checked. |
| SRC-03 | Get a failing example from Maria | Already requested; no reply or promised date. |
| SRC-04 | Prepare release checklist | Separate work; release version, date, and owner undecided. |
| SRC-05 | Export timeout — follow up | Another note about the same DEMO-17 problem; no new failure confirmed. |
| SRC-06 | Rollback and verification steps | Include these in release preparation; deployment procedure undecided. |

The planning decision is how to turn these notes into useful work. Six rows do not necessarily mean six tasks. Maria has been asked for an example, but that does not make her the investigation owner. A timeout reported after an upgrade does not establish its cause.

## After: two reviewed tasks

In our technical rehearsal, Codex prepared a two-task proposal. After explicit approval, it saved the following tasks through OKF Todo's command interface and read them back.

### Investigate invoice export timeout (DEMO-17)

The task brings together SRC-01, SRC-02, SRC-03, and SRC-05. Its body separates facts, proposed next steps, open questions, and source references.

Checklist—all still to do:

- [ ] Check available export logs for timeout details.
- [ ] Obtain the failing example already requested from Maria.
- [ ] Reproduce and investigate the timeout when enough evidence is available.

The task records an outstanding wait for **Maria — failing example for DEMO-17**. Log inspection can proceed while the response is outstanding. The cause, owner, and deadline remain unknown.

### Prepare release checklist

This separate task covers SRC-04 and SRC-06.

Checklist—all still to do:

- [ ] Agree the release version and deployment procedure.
- [ ] Define rollback steps.
- [ ] Define post-deployment verification steps.

Both tasks were saved in **Default list**, with **Active** status and **Normal** priority. These were reviewed application choices, not urgency inferred from the source. Neither task has an owner, responsible person, or deadline assigned.

A review comment was then added through the interface and read back: “Demo review: source notes checked against the saved plan.” Creating the plan did not complete the investigation or release work.

See the [complete approved proposal](../demonstratio-kit/proposed-plan.md) and [saved-result evidence](../demonstratio-kit/evidence/saved-result.json).

## How to try the workflow

You need OKF Todo and a separate AI assistant with access to local tools. The assistant must be able to read your chosen source format. The prepared example uses Codex and an Excel workbook; it does not establish equivalent results for every assistant or input format.

1. Follow the [demo setup](../demonstratio-kit/README.md#1-open-a-separate-demonstration-database) to open a separate database. Select **Skip** if offered sample tasks.
2. Open **Help → OKF layer → Copy prompt**. The running app supplies its actual instruction-file and database paths. Confirm that the selected database is the demo database, then give the assistant the copied prompt and access to the indicated locations.
3. Give it the workbook and ask for a proposal without saving. Review grouping, checklist items, outstanding responses, and any assumptions.
4. Explicitly approve the final proposal. Ask the assistant to save it, read back the result, and compare it with what you approved.
5. Open the tasks in OKF Todo and continue working with them. Mark a checklist item complete when that work is actually done.

The [full demonstration guide](../demonstratio-kit/README.md) contains setup instructions and prompts. For an independent trial, give the assistant only the workbook and setup instructions; keep this worked answer out of its input.

## What the application and assistant each do

The assistant reads and interprets your source material. OKF Todo supplies task context and ways to read and save work. Its built-in Help gives you a concrete starting point, with paths filled in for the running installation. [Read about assistant setup](ai-assistants.md).

OKF Todo has no built-in AI model and does not automatically watch your inbox or import spreadsheets. Available input formats depend on your assistant and its tools. The review-before-save instructions describe the workflow you should follow; they are not a universal software-enforced approval barrier.

The task database stays on your computer. An external assistant may send source material or task context to its provider, depending on its settings. Use fictional material for this example. OKF Todo is free; a separate assistant may require an account or payment.

## What has been verified

On 27 September 2026, the rehearsal saved two tasks and six actual checklist items to an isolated database using the installed Windows application's command interface. Read-back matched the approved bodies, fields, checklist items, and waiting information. The real application frontend, served through a local browser test host, displayed both tasks and saved the review comment.

This was a technical rehearsal in a conversation that already contained the expected outcome. It was not an independent newcomer trial or a complete native desktop recording. The installed command executable and the frontend checkout also used different revisions.

The test frontend displayed the Markdown body as literal text rather than formatted headings and lists. Stored text and line breaks matched the proposal. Native released-app display and this formatting issue still need checking before a promotional recording. The [rehearsal report](../demonstratio-kit/rehearsal-2026-09-27.md) records versions, screenshots, and remaining checks.

## Feedback that would help

If you try it, [open a GitHub issue](https://github.com/dalby-md/Okf-Todo/issues) with the assistant you used, the type of source material, and the step that helped or caused difficulty. In particular: were related notes grouped usefully, were missing facts left open, and did the saved tasks match your approved plan?

Use fictional or redacted examples when sharing a problem. Never attach your personal task database or customer material.
