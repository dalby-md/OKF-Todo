# AGENTS.md Instructions

Respond to the user in English by default.

## Project

This repository contains a personal local developer/internal support task system.

The system is a local-first desktop app, based on an existing Photino prototype that demonstrates an HTML/Markdown editor.

The app is intended for personal developer/support work, not multi-user enterprise task management.

## Important Product Docs

Read these before implementing product changes:

```text
/docs/PRD.md
/docs/DATA_MODEL.md
/docs/IMPLEMENTATION_PLAN.md
```

## Core Product Direction

Build a task handling system with:

- Fast task capture.
- Few required fields.
- Local SQLite database.
- Attachments stored in SQLite as BLOBs.
- HTML/Markdown task body using the existing editor prototype where possible.
- Table-based lookup values.
- Startup seeding of lookup values from configuration only when the table is empty.
- Editable lookup values.
- Deactivation only for lookup values; no hard delete.
- Automatic task log entries for lifecycle and significant changes.
- Separate comments and automatic logs.
- One active waiting target per task.
- Waiting target changes task status to `WAITING`.
- Clearing waiting target changes task status to `ACTIVE`.
- Personal/local ownership. Do not add owner/responsible-user concepts in the first version.

## Technology Assumptions

- Modern .NET / C#.
- Photino desktop application.
- SQLite.
- EF Core unless the existing prototype has made another explicit choice.
- Dependency injection.
- Windows development environment.
- PowerShell or Windows cmd for command examples.

## Coding Style

- Use dependency injection.
- Do not use `Console.WriteLine` in application code.
- Use logging abstractions where logging is needed.
- Keep business rules in services, not scattered through UI event handlers.
- Use stable lookup `Code` values for application logic.
- Use lookup `Name` values only for display.
- Prefer readable, boring code over clever abstractions.

## Frontend

- Keep the UI static under `Okf-Todo/wwwroot`.
- Do not add npm, Vite, Vue, React, or a bundler unless explicitly requested.
- Use local `Okf-Todo/wwwroot/js/jquery.min.js`.
- Put app code in `Okf-Todo/wwwroot/js/app.js`.
- Put styles in `Okf-Todo/wwwroot/css/app.css`.

## Photino Startup

- It is expected that the desktop UI loads from `http://localhost:<port>/index.html`; this is the local Photino static file server, not the internet.
- For blank-window diagnostics, distinguish these cases:
  - Static server not ready: no successful `GET /index.html`.
  - WebView navigation hang: server readiness probe succeeds, `PhotinoWindow.Load(...)` logs, but no `GET /index.html?...` follows.
  - Frontend/bridge issue: HTML/CSS/JS load, but no app bridge log or the UI shows a bridge timeout.
- For this app, keep the startup pattern:
  - start `PhotinoServer.CreateStaticFileServer`
  - probe `/index.html`
  - load `index.html?v=<timestamp>` in Photino to force fresh WebView navigation
  - use `ILogger`, not `Console.WriteLine`

## Entity Naming

Use `TaskItem` as the C# entity name instead of `Task` to avoid confusion with `System.Threading.Tasks.Task`.

Recommended entities:

```text
TaskItem
TaskType
TaskStatus
TaskPriority
TaskSource
WaitingForType
TaskWaitingFor
TaskComment
TaskLogEntry
TaskLogType
TaskChecklistItem
TaskAttachment
AttachmentKind
TaskStakeholder
StakeholderType
StakeholderRole
TaskTag
TaskTaskTag
TaskRelation
TaskRelationType
BodyFormat
```

## Lookup Rules

Most lookup tables should have:

```text
Id
Code
Name
Description nullable
SortOrder
IsActive
IsSystem
CreatedAt
UpdatedAt
```

Rules:

- `Code` is stable.
- `Name` is editable.
- Do not hard-delete lookup rows.
- Allow deactivation only.
- Inactive values remain visible on existing tasks.
- Inactive values are not offered as normal selections for new data.
- Protect system values required by application logic.
- Do not overwrite lookup values from configuration after the table has data.

Startup seeding rule:

```text
If lookup table is empty:
    insert initial values from configuration
Else:
    do nothing
```

## Lifecycle Rules

Use these status codes:

```text
NEW
ACTIVE
WAITING
COMPLETED
CANCELLED
```

Rules:

```text
Create task          => NEW
Start work manually  => ACTIVE
Add wait target      => WAITING
Clear wait target    => ACTIVE
Complete task        => COMPLETED
Reopen task          => ACTIVE
Cancel task          => CANCELLED
```

Each lifecycle operation must:

- Update relevant timestamps.
- Add automatic `TaskLogEntry` rows.
- Use lookup `Code`, not display `Name`.

## Logging Rules

Comments and logs are different.

- `TaskComment` is written by the user.
- `TaskLogEntry` is written by the system.

Automatic logs should include readable messages such as:

```text
Task created
Status changed from New to Active
Priority changed from Normal to Urgent
Waiting for changed to ServiceDesk INC123456
Waiting for ServiceDesk INC123456 was cleared
Task completed
```

Where useful, store old/new values in structured fields.

## Waiting Target Rules

- A task can have at most one active waiting target.
- A wait target can be entered directly, for example `INC123456`.
- The wait target does not have to be registered elsewhere first.
- A wait target can optionally link to a stakeholder.
- Adding a wait target changes status to `WAITING`.
- Clearing a wait target changes status to `ACTIVE`.
- `WaitingSince` should be set while waiting and cleared when no longer waiting.
- `FollowUpAt` should be supported.

## Attachments

Store attachments in SQLite as BLOBs.

Do not store only filesystem paths.

Attachment fields should include:

```text
FileName
ContentType
FileSize
Sha256Hash
AttachmentKindId
ContentBlob
Description
CreatedAt
```

Consider a configurable soft warning for large attachments.

## Source

Task source is optional.

Source is classification/reference only.

Do not implement automatic opening behavior for source URLs.

## Out Of Scope Unless Explicitly Requested

- Multi-user support.
- Authentication.
- Cloud sync.
- ServiceDesk/TFS/Teams/email integrations.
- Automatic URL opening behavior.
- Full project-management workflow.
- Hard deletion of lookup values.
- Making checklist items into full tasks.

## Recommended Development Approach

Use small vertical slices.

Do not implement schema, lifecycle, UI, attachments, tags, stakeholders, and relationships in one change.

Recommended order:

```text
1. Data model
2. Lookup seeding
3. Lifecycle/logging service
4. Basic task list/create/edit
5. Editor integration
6. Waiting target UI
7. Comments/timeline
8. Checklist items
9. Tags
10. Stakeholders
11. Attachments
12. Task relationships
13. Lookup management UI
14. Views/sorting
```

## When Finishing A Task

Report:

- Files changed.
- What was implemented.
- How to run/build/test.
- Any assumptions.
- Any incomplete parts.
