# OKF Todo promotion plan and working checklist

Last updated: 2026-09-27.
Status: working plan under discussion. Positioning and objective are agreed; site order, demonstration, and schedule remain proposed. This file does not authorize external posting or contacting anyone.

## Start here in every promotion session

Read this file before continuing. Preserve completed actions, decisions, evidence, and previous submissions. Ask about missing facts that affect the current step; continue independent preparation where possible. Update this file as decisions and actions happen.

Work on one new site at a time. Prepare its specific material, let Søren review it, and obtain an explicit instruction to submit or send. Existing authorization for an exact submission remains valid; do not ask again without a material change. A request to research or draft is not an instruction to contact people.

Never mark outreach complete because text was drafted, a form was opened, or a submission was merely attempted. Record successful submission separately from public visibility, editorial acceptance, and feedback review. Do not create a recurring automation or application database tasks just because this checklist exists.

## Agreed direction

- Objective: regular users and useful feedback.
- OKF Todo is free and open source.
- **Lead with turning existing source material into an initial, usable task plan in every context.** Excel spreadsheets, screenshots, emails, and notes are examples, not an exhaustive list of supported inputs.
- The primary message is the transformation from material to planned work. Local storage, open source, and the desktop workspace support that message. Do not demote AI-assisted planning to a secondary selling point on general sites.
- Adapt examples, detail, and tone to each audience while keeping this central promise consistent.
- This is a gradual process: approach one site, learn, and improve the next approach.
- Previous promotion: Søren reports Reddit only. Subreddit, URL, date, and results are still unknown. No previous promotion on the other proposed sites was reported.
- Confirmed experience: Søren has successfully used Codex with an Excel file and screenshots of Microsoft To Do tasks. This is user-reported experience; the campaign demonstration has not yet been independently reproduced. The save/access route still needs recording.
- Make the built-in Help part of the story: it supplies ready-to-copy instructions populated with the actual files and configuration for the running installation. Søren highlighted this in an installed-app screenshot on 2026-09-27.

## Positioning and accurate claims

Working headline for channels that permit assisted writing:

> Turn the material you already have into a task plan you can act on.

Working explanation, to refine after verifying the demonstration:

> Use your AI assistant to turn spreadsheets, screenshots, emails, and notes into a proposed plan. Review the tasks, approve what should be saved, and manage the resulting work in OKF Todo, a free, open-source desktop application.

Always make the complete workflow understandable:

1. The user supplies material to an AI assistant with the necessary file, image, and local-tool capabilities.
2. The assistant interprets the material and proposes a plan, identifying uncertainty and missing facts.
3. The user reviews and corrects the proposed tasks and explicitly approves the save.
4. The assistant saves the approved work through an available OKF Todo access route and reads it back to verify it.
5. The user can see, organize, update, and complete the work in OKF Todo, and continue using the assistant where useful.

The question our demonstration must answer is: **What do I gain beyond asking a chatbot to write a list?** Show the approved plan becoming persistent tasks with usable context and progress, visible in the desktop application and accessible in later work.

Boundaries to keep accurate:

- The assistant interprets files and images. OKF Todo is not being presented as a built-in Excel importer, OCR service, mail connector, or bundled AI model.
- Input support depends on the chosen assistant and its tools. Verify each advertised example before publishing a claim about it. Do not claim arbitrary file support or error-free extraction.
- OKF supplies structured context; it does not itself read files or execute changes. MCP provides an optional local action bridge. Use the actual demonstrated route in instructions; do not imply MCP is the only route.
- Review and approval are part of the workflow. Do not claim the MCP server enforces an interactive approval dialog; its usage instructions guide the client.
- OKF Todo is free; third-party AI services may have their own accounts, costs, and data handling. Local task storage does not imply that a chosen cloud assistant processes source material locally.
- Describe the currently verified Windows release and installation routes. Do not equate source-build targets with tested macOS/Linux installers.
- Do not promise automatic inbox monitoring, cloud sync, multi-user collaboration, automatic scheduling, or native import features not demonstrated in the released product.

Product references: [README](README.md), [OKF guide](docs/help/okf-layer.md), [MCP guide](docs/help/mcp-server.md), and [desktop guide](docs/help/using-okf-todo.md). Check release behavior as well as documentation before claiming a feature publicly.

### Show how the assistant gets the right context and access

The built-in Help is a practical starting point for the source-to-plan workflow. Show it before supplying the example material:

1. Open **Help → OKF layer**. It displays the actual absolute path to the installed OKF entry file and the active task database, including a custom database path when used.
2. Select **Copy prompt**. The prompt already contains those paths and instructions to inspect, propose, wait for approval, and verify the saved result. Paste it into the assistant and grant the necessary local access.
3. For the MCP route, **Help → MCP server** provides the actual configuration file path and **Copy configuration**. The configuration matches the installed executable or current source/framework-dependent launch mode, so the instructions point to the relevant program as well as the files.
4. Supply the spreadsheet or screenshots, request the proposed task plan, review it, approve the save, and inspect the resulting tasks in OKF Todo.

Promotional emphasis: users can start from instructions prepared by their own copy of OKF Todo, with concrete paths already filled in. This connects the assistant to the application's documented task context and an available access route. Explain OKF/MCP terminology only as far as the audience needs it.

Evidence: Søren's screenshot shows the installed OKF guide with resolved entry-file and database paths and **Copy prompt**. The executable/configuration behavior is documented in the current [MCP guide](docs/help/mcp-server.md); it is not shown in that screenshot. Neither observation proves which route was used in the earlier Excel/screenshot session.

For the public demonstration, use Help from the running demonstration instance and verify that it identifies the isolated demonstration database. Do not reuse Søren's personal paths as universal setup instructions. Copying instructions supplies context; the assistant still needs the appropriate tools and file permissions.

## Proposed sites and sequence

These are editorial recommendations, not predictions of reach or acceptance. Reassess after each meaningful result. No launch dates are booked.

| ID | Site | Purpose and proposed priority | Current state |
| --- | --- | --- | --- |
| CON | [Console.dev](https://console.dev/selection-criteria) | First editorial pitch once the demonstration works. Show how source material becomes planned development/support work. | Candidate; not contacted |
| HN | [Hacker News / Show HN](https://news.ycombinator.com/showhn.html) | Seek substantive discussion and early users after the pitch and setup are clear. | Candidate; not submitted |
| ALT | [AlternativeTo](https://alternativeto.net/faq/) | Build a lasting discovery listing, with AI-assisted task planning described accurately. | Candidate; not submitted |
| DEV | [DEV Community](https://dev.to/terms) | Publish a useful, reproducible source-to-plan walkthrough. | Candidate; not published |
| PH | [Product Hunt](https://www.producthunt.com/launch/how-product-hunt-works) | Broader launch after initial feedback improves the demonstration and explanation. | Candidate; not submitted |
| RD | [Reddit r/dotnet](https://www.reddit.com/r/dotnet/comments/1rgx9j4/rule_change/) | Conditional technical follow-up about turning source material into durable .NET-backed tasks. Check previous Reddit activity first. | Hold: history and eligibility |
| CP | [CodePen](https://codepen.io/charge) | Optional supporting interactive demo; weaker fit for the complete desktop/assistant workflow. | Deferred: value versus preparation effort |
| CS | [Console on Substack](https://console.substack.com/archive) | Historically relevant open-source audience. | Hold: visible archive ends at 2024-02-11; current activity unconfirmed |

Console.dev and Console on Substack are separate publications. Confirm which one Søren originally intended before prioritizing either on that basis. The Console.dev recommendation stands on its published developer-tool criteria.

Existing Reddit activity is a separate historical record, not evidence of a post in r/dotnet. Do not assume where it appeared. Broader Reddit outreach stays on hold until the earlier post and each community's current rules are understood.

Default effort assumption, pending Søren's preference: unpaid outreach, one active preparation/submission at a time, with time reserved for responses. A pending directory review or editorial decision need not prevent preparing the next site. Avoid stacking live discussions beyond the time available to answer them.

## Shared demonstration and preparation

First prepared scenario: use Codex with the fictional [Excel work notes](demonstratio-kit/work-notes.xlsx). The [demonstration guide](demonstratio-kit/README.md) covers isolated setup, Help's Copy prompt, analysis, human review, approval, save, read-back, and recording. The separate [expected outcome](demonstratio-kit/expected-outcome.md) is for the presenter and must stay out of the planning assistant's source input. Screenshots of fictional task lists, Microsoft To Do, and email can follow as separate examples after verification. The original material is unavailable on this machine and is not required for this new example.

Make the planning step visible: explain how the proposed work is organized and where human judgment changes it. If sources overlap or omit dates, show the assistant flagging those questions for review. Do not imply automatic, lossless Microsoft To Do synchronization or migration from screenshots.

Show **Help → OKF layer → Copy prompt**, then the source, the proposed plan, a meaningful human correction, explicit approval, and the actual saved tasks in OKF Todo. If demonstrating MCP, also show where its ready-to-copy configuration comes from. End by updating or completing part of the work. A list displayed only in chat does not demonstrate the full value.

Prepare a short recording, provisionally 60–90 seconds, plus a written walkthrough that explains setup. Do not hide the need to connect the assistant. Use fictional source material and a separate demonstration database. Do not expose real customer or employer information.

- [x] P01 — Confirm objective and central positioning. Evidence: Søren's answers in the promotion discussion, 2026-09-27.
- [ ] P02 — Identify the existing Reddit post(s), dates, and feedback.
- [x] P03a — Confirm the assistant and previously successful input types. Evidence: Søren reports Codex with an Excel file and Microsoft To Do task screenshots, 2026-09-27.
- [x] P03b — Record the actual save/access route and choose the demonstration's input material. Evidence: fictional work-notes.xlsx, installed application CLI saves and read-back, and frontend review comment in the 2026-09-27 rehearsal report. This records the new demonstration route, not the unavailable original materials.
- [x] P03c — Record the built-in Help as the concrete setup starting point. Evidence: Søren's installed-app screenshot and current OKF/MCP guides, 2026-09-27. This completes documentation of the feature, not an end-to-end setup test.
- [x] P04 — Create fictional source material and an expected plan for checking the result. Evidence: six-row work-notes.xlsx, demonstration guide, and separate expected-outcome checklist under demonstratio-kit, 2026-09-27. Workbook rendered, visually checked, and exported records read back; application run still pending.
- [ ] P05 — Run the complete workflow against the released app; inspect saved titles, details, checklists, and any proposed dates against the sources. Record version, assistant, tools, and limitations.
  - Partial evidence: [2026-09-27 technical rehearsal](demonstratio-kit/rehearsal-2026-09-27.md) passed Help Copy prompt, approved saving, complete read-back, and browser-harness frontend/comment verification. Tasks 1 and 2 and six unchecked checklist items match the proposal. P05 stays open for a native released-app run and a Markdown presentation issue found before recording.
- [ ] P06 — Record the demonstration and produce a readable before/after example.
  - [x] Prepare the written before/after example: [From Excel notes to a task plan](docs/source-to-plan.md), with all six sources mapped to the two verified tasks and explicit rehearsal limitations.
  - [x] Produce a captioned viewing copy: [YouTube walkthrough](https://youtu.be/cXIRZgOagrA), [player](demonstratio-kit/video/watch.html), and [transcript](demonstratio-kit/video/transcript.md). Workbook data, proposal/approval replay, real Help and saved-task captures; fresh isolated save/read-back verified. This does not complete native recording or an independent AI trial.
  - [ ] Record and review the native desktop demonstration after checking body formatting.
- [ ] P07 — Verify a newcomer can install, use Help's resolved paths and copy actions to give the assistant context/access, and reproduce the example using the linked instructions.
- [ ] P08 — Check public destination pages for consistent positioning, platform claims, working download links, and a clear feedback route. Record necessary edits before undertaking a separate website change.
  - [x] Prepare the repository presentation: README introduction, linked 83-second video preview, visible replay disclosure, and download/sample/feedback links. The local player uses the same presentation. The main video links open YouTube; the local player remains available for offline viewing.
  - [x] Publish the video on YouTube. User confirmed publication on 2026-09-27: [Excel notes to an OKF Todo plan](https://youtu.be/cXIRZgOagrA).
  - [ ] Publish the updated repository documentation, then verify the YouTube, app download, and sample links on the public repository.
- [ ] P09 — Choose the first site and reserve time to answer responses.

Use the existing [screenshot gallery](docs/screenshots.md) and [captions](docs/images/screenshots/README.md) as supporting material. A workspace screenshot alone does not establish the source-to-plan workflow. The source icon is `Okf-Todo/Resources/Okf-Todo-icon.png`; the canonical workspace screenshot is `docs/images/okf-todo-task-workspace.png`. Check that artwork matches the promoted release.

## Content and tone for each site

### CON — Console.dev

Angle: a developer starts with scattered evidence and ends with a reviewed investigation plan in a local task workspace. Use one concrete example, a short demonstration link, install/repository links, and a clear explanation of the assistant setup. Be concise, specific, and candid about limitations. Proposed pitch length: 120–180 words, not a site-imposed limit.

Its [selection criteria](https://console.dev/selection-criteria) emphasize usefulness in developer workflows and accept tool suggestions at `hello@console.dev`. Pitch the stable application for general tool consideration; the separate beta section excludes stable releases. Inclusion is an editorial decision.

- [ ] CON-1 — Verify current route; prepare and review the pitch and demonstration.
- [ ] CON-2 — Send the authorized pitch; record sent-message evidence and date.
- [ ] CON-3 — Record editorial response or an explicit no-response review; decide whether one courteous follow-up is appropriate.
- [ ] CON-4 — Verify any published coverage separately and record lessons.

### HN — Show HN

Angle: the material-to-plan workflow, why Søren built it, and how a reviewed plan becomes persistent local work. Demonstrate something people can actually try. Explain setup and relevant design trade-offs plainly; stay available for questions.

The [Show HN rules](https://news.ycombinator.com/showhn.html) require a personally built, usable project. The [general guidelines](https://news.ycombinator.com/newsguidelines.html) prohibit generated or AI-edited text and soliciting votes or comments. Søren must author the submission and replies; assistants may research product facts and verify the demo, but must not supply or polish HN-ready prose. Do not paste this document's sample copy there.

Søren confirmed he will write the HN submission himself. Assistant work covers the linked README, documentation, and demonstration materials. This distinction follows the [moderator clarification](https://news.ycombinator.com/item?id=49557024) about external articles versus text appearing on HN itself. Earlier conversation drafts are not submission copy.

The prepared visitor route is **repository README → [worked example](docs/source-to-plan.md) → installation and [reproduction guide](demonstratio-kit/README.md)**. Use the repository as the Show HN destination so readers can reach the actual product and source. These changes are prepared locally, not published. Check the public links after publication, and keep HN-1 open until account access, history, and the try-it route have been checked.

- [ ] HN-1 — Check current rules, account access, duplicate submissions, and try-it route.
- [ ] HN-2 — Søren writes his own title and introduction; verify supporting facts and links without rewriting his prose.
- [ ] HN-3 — Publish when Søren can respond; record the submission URL and date.
- [ ] HN-4 — Answer questions personally; record feedback and a later outcome review.

### ALT — AlternativeTo

Angle: AI-assisted conversion of existing material into tasks, followed by ongoing local task management. Use neutral descriptive language, accurate categories, Windows availability, free/open-source status, and setup requirements. Choose alternatives by genuine overlapping use cases; do not claim feature parity with team platforms.

The [FAQ](https://alternativeto.net/faq/) describes an account-based suggestion process and moderation. Use designated link fields rather than links in the description. Free review may take months; submission is not publication. A paid priority route exists, but paid promotion is not part of this proposed campaign.

- [ ] ALT-1 — Check for an existing entry; prepare accurate listing fields and source-to-plan images.
- [ ] ALT-2 — Review and submit; record confirmation and date.
- [ ] ALT-3 — Record moderation outcome and independently verify public visibility if accepted.
- [ ] ALT-4 — Review resulting questions or referrals when evidence is available.

### DEV — DEV Community

Angle: teach a repeatable method for turning messy input into a reviewed plan and saved tasks. Include fictional input, prompts, a correction, the actual result, and setup details. A working article concept is “From a support spreadsheet and email to a task plan with OKF Todo.” Do not present that title as final copy.

Use the reported Codex/Excel/Microsoft To Do screenshot workflow for the first article once reproduced; the support-email concept can be a later tutorial after verification. Tone: practical and explanatory. Readers should learn a useful method even if they do not install. [DEV's content policy](https://dev.to/terms) requires substantive, relevant content and disallows posts designed primarily for promotion/backlinks. Check current assisted-writing rules before drafting or publishing and follow any disclosure requirements.

- [ ] DEV-1 — Verify current policies and write a complete, reproducible tutorial.
- [ ] DEV-2 — Review facts, example results, and any required disclosure.
- [ ] DEV-3 — Publish when authorized; record the public URL and date.
- [ ] DEV-4 — Respond to questions and record reproduction problems and lessons.

### PH — Product Hunt

Angle: start with existing material and finish with an actionable plan in OKF Todo. Use a short benefit-led tagline, a source-to-result gallery, and a demonstration. Explain the external assistant requirement near the promise. The maker introduction should describe the problem, who benefits, and one specific request for feedback.

The [launch guide](https://www.producthunt.com/launch/how-product-hunt-works) permits free self-submission through a personal account and prohibits buying hunts or artificial traffic. Check [posting access](https://help.producthunt.com/en/articles/481909-how-can-i-get-access-to-post) before scheduling. Featured placement is not guaranteed. Final field lengths and media requirements must be checked in the current form.

- [ ] PH-1 — Confirm account eligibility, prior listings, current fields, and response availability.
- [ ] PH-2 — Prepare and review tagline, description, maker introduction, gallery, and demonstration.
- [ ] PH-3 — Schedule or publish the authorized launch; record its actual state and URL.
- [ ] PH-4 — Verify it went live; answer questions and record outcomes.

### RD — Reddit / possible r/dotnet follow-up

Start by understanding the earlier Reddit post. Preserve its feedback and avoid reposting the same announcement. For r/dotnet, connect the source-to-plan demonstration to the implementation: how the resulting tasks remain usable through desktop and assistant workflows. Technical detail supports the user outcome.

The [published r/dotnet rule change](https://www.reddit.com/r/dotnet/comments/1rgx9j4/rule_change/) restricts product promotion to a specified Saturday window, requires Promotion flair, limits release announcements, and prohibits AI-written posts. Recheck the current rule and its timezone before posting. Søren writes the post himself. Other subreddits have separate rules; do not transfer permission between communities.

- [ ] RD-1 — Recover the historical post and outcomes; assess whether further Reddit outreach adds value.
- [ ] RD-2 — Verify the selected community's current rules and eligibility; record the decision.
- [ ] RD-3 — If proceeding, Søren writes and publishes an eligible post; record its URL/date.
- [ ] RD-4 — Record replies, resulting learning, and review outcome.

### CP — CodePen

[CodePen](https://codepen.io/charge) focuses on frontend code demonstrations. A possible supporting piece could show supplied example material beside its reviewed task plan. It must clearly distinguish a simulated illustration from a working assistant-to-desktop integration. Do not imply a Pen runs the full application or proves file interpretation.

Tone: educational, interactive, and useful to frontend developers. Given the agreed selling point, defer unless the demo has enough independent value to justify building and maintaining it. Check current public-code licensing terms before copying application code into a Pen.

- [ ] CP-1 — Decide whether a CodePen example supports the main story well enough to build.
- [ ] CP-2 — If selected, build and verify the clearly labelled example; check rights and terms.
- [ ] CP-3 — Review and publish when authorized; record the URL and date.
- [ ] CP-4 — Review feedback and whether it helped people understand or try the real workflow.

### CS — Console on Substack

The [visible archive](https://console.substack.com/archive) checked on 2026-09-27 ends with a February 2024 issue. This does not prove permanent closure. Establish current activity and a submission route before preparing outreach.

If active, pitch the open-source source-to-plan workflow, with a repository link, example, and concise maintainer story. Tone: personal and concrete. Do not substitute the Console.dev contact address.

- [ ] CS-1 — Establish current editorial activity and submission route; otherwise record a deferred decision.
- [ ] CS-2 — If viable, prepare and review the tailored pitch.
- [ ] CS-3 — Send only when authorized; record evidence and date.
- [ ] CS-4 — Record response, any verified coverage, and lessons.

## Completion rules and evidence log

Change `[ ]` to `[x]` only when that exact action happened. Add its task ID, date, and evidence below. Direct user confirmation is acceptable: label it as user-reported rather than independently verified. Record dates in `YYYY-MM-DD`; include Europe/Copenhagen timezone for scheduled times.

Use these states in the site table or log: Candidate, Preparing, Ready for review, Approved, Submitted, Published, Awaiting response, Reviewed, Deferred, Declined, or Closed without response. A checkbox records an action; a state records where the overall approach stands.

Scheduled is not published. Sent is not accepted. Published is not evidence of users. A failed attempt stays unchecked. If a site is declined or skipped, record the reason and leave unperformed actions unchecked. Close a cycle after reviewing its outcome, even when no coverage resulted; never manufacture successful publication to close it.

| Date | Task/site | What actually happened | Evidence | Next action |
| --- | --- | --- | --- | --- |
| 2026-09-27 | P01 | Objective and source-material-to-plan positioning confirmed | User statements in this discussion | Choose and verify demonstration |
| 2026-09-27 | P03a | Codex used successfully with an Excel file and screenshots of Microsoft To Do tasks | User-reported in this discussion; not independently reproduced | Record access route and prepare reproducible example |
| 2026-09-27 | P03c | Added installation-specific Help and copy actions to the promotion story and demonstration plan | User-supplied screenshot shows resolved OKF/database paths; current guides document the prompt and MCP launch configuration | Demonstrate this setup against the isolated example database |
| 2026-09-27 | P04 | Prepared a fictional Excel-only demonstration with source notes, presenter checks, prompts, isolated-database launch instructions, and a run record | [Demo guide](demonstratio-kit/README.md); workbook rendering and exported six-record comparison passed | Run P05 together; leave save, recording, and outreach unchecked |
| 2026-09-27 | Historical Reddit | Søren reports previous promotion only on Reddit; original posting date unknown | User-reported; subreddit and URL pending | Complete P02 |

Keep private correspondence, account details, and customer material out of this potentially public repository. Record a safe summary or reference instead. Preserve approved copy or its version reference so later edits do not obscure what was actually sent.

## Learning and follow-up

For each approach, record the time spent, substantive questions, people who report reproducing the workflow, blockers, and people who voluntarily report continuing to use the app. Downloads, page visits, and stars are supporting indicators; they cannot establish retention. Record unavailable measurements as unknown, not zero.

Proposed outcome reviews: about a week after a public post, and again later if useful feedback continues. These are checklist intentions, not scheduled reminders. Editorial and directory timelines may be much longer. Do not chase every silent submission or wait indefinitely before moving on.

Decide the next step from the evidence:

- If people cannot understand what the assistant does versus what OKF Todo does, improve the explanation and demonstration.
- If they understand the promise but cannot connect the assistant or reproduce the save, improve that path before wider promotion.
- If they create plans but do not return, learn what prevents ongoing use before seeking more traffic.
- If a channel produces relevant users and useful feedback, prioritize comparable audiences; avoid repeating the same post without a substantive reason.

## Open decisions

- Which access route will the first demonstration use? The Excel example is prepared, starting from Help's OKF instructions. Søren's earlier use of Codex with Excel and Microsoft To Do screenshots is confirmed; the original session's save route remains unknown and need not be reconstructed.
- Where was the previous Reddit post, and what response did it receive? Question pending.
- Which Console publication was originally intended? Both are documented separately until clarified.
- What preparation and response pace is comfortable? No weekly schedule agreed.
- Which site goes first after the demonstration is verified? Console.dev is the current proposal.

## Scope of this change

This file is the campaign instruction document and Markdown task tracker. The first fictional workbook and demonstration instructions are prepared under demonstratio-kit. Application behavior, database design, and canonical Help are unchanged. No application build is required for these artifacts. MCP applicability: no new application capability or MCP contract is introduced; demonstration work will use existing approved access routes. The real application run, recording, actual outreach, and final site copy remain future checklist actions.
