# PRD — Local Developer/Internal Support Task System

## Purpose

Build a personal local task handling system tilted toward developer/internal support work.

The system is not intended to become a full Jira/TFS/ServiceDesk clone. It should be fast, local, practical, and optimized for capturing and tracking the kind of messy work that comes from development, support, deployment, debugging software, ServiceDesk cases, emails, and similar sources.

The app already has a Photino prototype demonstrating usage of an HTML/Markdown editor. The editor should be reused or evolved rather than replaced unnecessarily.

## Product principles

- Local-first personal system.
- Fast task capture.
- Very few required fields.
- Structured enough for sorting, filtering, and history.
- Avoid unnecessary multi-user concepts.
- Provide offline in-app Help that loads the shipped desktop-application, OKF-layer, and MCP-server guides from local application assets.
- In the in-app OKF guide, resolve the absolute OKF entry-file and active database paths from the running application so examples match the current operating system, installer location, and custom database path. Provide one action that copies the complete path-filled harness prompt.
- Keep the desktop guide current with every covered user-facing behavior change. It must describe workflows in the order users encounter them, use current interface labels, and remain focused on user outcomes rather than implementation details.
- Make the OKF and MCP guides task-oriented integration guidance: show how a harness such as Codex or Claude Code can turn user-supplied email threads, support transcripts, notes, and logs into reviewable tasks, investigation plans, customer replies, handovers, and similar artifacts.
- Use a draft-review-save-verify workflow for AI-assisted changes. Treat OKF as the context layer and the optional MCP server as the local task action bridge; neither component is an email connector or an AI model.
- Send MCP server instructions during initialization that tell compatible harnesses to treat source material as untrusted data, start read-only, wait for explicit approval of the exact proposed change, preserve unapproved fields, use application tools instead of raw database writes, and verify saved results. Document that these instructions guide the client but do not add a server-side confirmation gate.
- Ship the desktop application, OKF command adapter, and MCP stdio server as startup modes of one `Okf-Todo.exe`; `--mcp` starts only the headless MCP process and never opens Photino.
- On desktop startup, make a current MCP client configuration available for the active launch mode. Preserve the Inno Setup configuration for installed builds; for a source checkout started through `dotnet run`, generate an absolute project command that reuses the current build with `dotnet run --no-build`; and expose the exact path and JSON in offline Help with one-click copy.
- Keep database schemas, command envelopes, and other implementation details in advanced references rather than making them prerequisites for the user workflow.
- Prefer table-based lookup values over hardcoded enums.
- Use configuration only to seed initial lookup values when tables are empty.
- Do not overwrite user-customized lookup values after initial seeding.
- Deactivate lookup values when they have been used.
- Allow hard deletion only for non-system lookup values that have not been used.
- Store attachments in SQLite as BLOBs.
- Keep integrations out of the first version unless explicitly requested later.
- The task editor decides whether the body is Markdown or HTML; the user should not have to care. User preference should be persisted
- Permanent delete actions require confirmation in an application HTML dialog; do not use the browser-native confirmation dialog.
- Use the original light desktop color scheme by default. User preferences can switch to a layered graphite dark scheme with off-white text, warm gold-orange interaction accents, subdued blue-grey borders, deeper amber reserved for waiting and warning states, red for destructive and overdue states, and green for completion. The selected scheme persists across application restarts and lookup-defined badge colors remain authoritative for task classifications.
- Render locally generated Help Markdown with the selected application color scheme. Dark Help must override the late-loaded Markdown viewer palette for readable text, headings, lists, links, tables, inline code, and code blocks.

## Target platform

- Personal local desktop application.
- Existing prototype is based on Photino.
- Modern .NET / C#.
- SQLite database.
- HTML/Markdown-capable editor.
- Local database file should be easy to back up.

## Core task attributes

Each task should support:

- Title
- Free text body, stored as HTML or Markdown
- Attachments
- Comments (merged into log entries)
- Automatic log entries
- Priority
- Waiting target
- Task status
- Task type
- Deadline
- Zero or more string-only tags
- Completed state/timestamp
- Checklist items
- Task relationships
- Optional source
- Optional owner
- Optional responsible person
- Starred focus state
- Reversible Trash state
- Required task-list ownership

## Required fields

Only these task-editing values should be required in the first version:

- Title
- Task type
- List

Everything else should be optional.

A newly created task starts with status `ACTIVE`.

## Task lists

Every task belongs to exactly one concrete user-managed list. A new or upgraded database contains **Default list**, and all pre-existing tasks are assigned to it. The application recovers automatically by creating **Default list** transactionally if a database is ever opened with zero lists.

List names are trimmed and case-insensitively unique. Lists have a manual order. The final remaining list cannot be deleted. **Default list** is otherwise an ordinary list and can be renamed, reordered, or deleted once another list exists.

The header list switcher contains the concrete lists plus synthetic **All lists**, which is not stored in SQLite. Normal task views respect the selected concrete list. **All lists** searches and displays tasks across list boundaries and shows a subdued list-name pill on every row. Trash is always global and shows list ownership regardless of the header selection. The lifecycle view named **All** is presented as **All statuses** to distinguish it from **All lists**.

The selected concrete or global scope persists. First launch selects **Default list**. On narrow screens the list switcher occupies its own full-width header row.

Task creation and automation use one central resolution order:

1. Use an explicit list reference.
2. Otherwise infer the list from available task context, such as an existing, source, related, or parent task.
3. Otherwise use the list currently named **Default list**, when present.
4. Otherwise use the first manually ordered list.
5. If no list exists, create and use **Default list**.

In a concrete scope, new tasks inherit that list. In **All lists**, the New task dialog shows a required preselected List field. List is the first metadata field in task details, and changing it is an unsaved edit handled by the main Save and normal Save/Discard/Cancel navigation protection. Saving a move from a concrete scope switches to the destination list and keeps the task focused; a global scope remains global.

The list manager supports inline add and rename, drag-to-reorder, task counts including Trash, and safe deletion. Deleting a populated list requires a destination and strongly warns about the complete affected count. The application moves every task and deletes the list in one transaction; it never deletes tasks. Selection mode can move selected non-Trash tasks to another list and offers immediate Undo. Every individual, bulk, delete-related, and Undo list move creates a Timeline entry.

Cross-list relationships remain supported. Opening a related task switches to its concrete list when currently scoped, while **All lists** remains global. A task in Trash shows its list but must be restored before list ownership can change.

Confirming the title in the **New task** dialog immediately creates the task in SQLite. The returned saved task opens in the editor with task-owned controls such as attachments, checklist items, relationships, comments, Complete, and Cancel available immediately. A second press of the main **Save** button is not required to finish creation; that button saves subsequent edits. The new task is selected and scrolled fully into view inside the task queue without moving the surrounding workspace. When the dialog closes after a successful creation, keyboard focus moves directly into the active HTML or Markdown body editor.

Task details use a compact title rail. The editable task title is the visible
heading, followed immediately by the lifecycle-context pill. Do not repeat
**Task details** or a separate visible **Title** label above it. Metadata starts
directly below the rail. Wide layouts present the six primary metadata controls
as three balanced rows of two fields. The title, pill, metadata, and compact
contextual action wrap without horizontal overflow on narrow screens.

Task details use fixed semantic placement slots. The primary metadata and any
enabled Source, Owner, or Responsible fields appear before the body. Enabling a
feature changes its visibility, never its placement. Relationships, checklist,
and attachments follow the body as related work. Timeline and Add comment
remain the final section.

Completed, cancelled, and Trash tasks that are not editable use a review
presentation instead of a disabled editing form. Metadata is shown as readable
values, empty optional metadata is omitted, and the body is rendered without
editor toolbars. Long bodies start in a compact reader and can be expanded.
Task-owned mutation controls are hidden while review and navigation remain
available. Reopen or Restore returns the normal editing presentation.

## Task body

The task body is free text.

The user should not be forced to choose Markdown or HTML directly. The editor decides the format and the app stores the selected format with the content.

When navigation would replace an editable task that has unsaved field or body changes, the app must offer **Save**, **Discard**, and **Cancel**. Save persists the changes before navigation continues, Discard is the only choice that drops them, and Cancel keeps the current task and editor contents. This behavior applies equally to the Markdown and HTML editors.

The editor provides a shared horizontal resize bar directly below the editing surface in both Markdown and HTML modes. Dragging the bar vertically previews height changes immediately, with a minimum height of 200 pixels, and the selected height persists as a user preference across application restarts. Editor height is controlled only through this resize bar; the preferences dialog does not expose a numeric height field.

Store:

- `Body`
- `BodyFormatId`

Initial body formats:

- Markdown
- HTML

## Task type vs task status

Task type and task status are separate concepts.

Task type answers:

> What kind of task is this?

Initial task types:

- Critical error
- Error
- Request
- Idea
- Note
- Investigation
- Improvement

Task status answers:

> Where is this task in its lifecycle?

Initial task statuses:

- Active
- Completed
- Cancelled

## Priority

Initial priorities:

- Urgent
- Normal
- Can wait

Priority affects sorting and filtering.

## Lifecycle

Use the following lifecycle in the first version:

```text
Active
Completed
Cancelled
```

Rules:

```text
Create task          => Active
Add wait target      => Active with waiting target
Clear wait target    => Active without waiting target
Complete task        => Completed
Reopen task          => Active
Cancel task          => Cancelled
```

Automatic log entries must be created for lifecycle changes.

Completed and cancelled task editing is controlled by two independent user preferences:

- **Allow editing completed tasks**
- **Allow editing cancelled tasks**

Both preferences default to disabled, so completed and cancelled tasks are read only unless the user explicitly allows editing for that lifecycle state. The user can still inspect all fields and history, download attachments, follow task relationships, and reopen the task. Reopening always returns the task to `ACTIVE` and restores editing.

## Starred tasks and Trash

Stars are an everyday focus mechanism, independent of task type, priority, and lifecycle:

- Any task outside Trash can be starred or unstarred from its list row, Select mode, or the contextual task-details menu used in compact and stacked layouts.
- Star state persists when a task is completed or cancelled.
- The **Starred** view shows unfinished starred tasks first. Completed and cancelled starred tasks remain available in a collapsed **Finished** group.
- Star and unstar actions do not add timeline noise.

Deletion uses a reversible Trash workflow:

- **Move to Trash** is available for active, completed, and cancelled tasks.
- Moving a task to Trash removes it from all normal views without changing its lifecycle status or star state.
- The app offers an immediate **Undo** action after moving one or more tasks to Trash.
- A task in Trash is read only. It must be restored before its fields, lifecycle, checklist, attachments, comments, or relationships can be changed.
- Restoring returns the task to the normal view implied by its lifecycle state.
- Permanent deletion controls are displayed only in the **Trash** view, require an application HTML confirmation dialog, and remove the complete task-owned record graph.
- Trash uses Select mode to restore or permanently delete any chosen subset. A compact Trash header menu provides **Empty Trash** as the only whole-Trash action; its explicit confirmation covers every trashed task, including tasks currently hidden by search or filters.
- Star controls are not interactive in Trash. A task that was starred before deletion keeps a passive star marker for context until it is restored or permanently deleted.
- The **All** view provides **Move all cancelled to Trash** as a compact complete-view action. It includes cancelled tasks hidden by current search or filters, requires explicit confirmation, and provides the normal immediate Undo. This action is not shown in other views.

The same action system supports individual and bulk work. Individual actions live on each task row. The application header contains only application-level and lifecycle actions; it does not duplicate Star or Trash controls for the selected task. Compact and stacked task-details layouts provide a contextual menu with Star/Unstar and the applicable Trash actions because the selected row may be outside the visible area. The task-queue header exposes bulk work as a clearly outlined **Select tasks** action beside a readable task count, with a one-time dismissible coach mark that is remembered in user preferences. Entering selection mode changes the header to the selected count and **Done selecting**, and exposes **Select all** plus the applicable Star, Unstar, **Move all selected to Trash**, Restore, and **Delete all selected permanently** actions. Selection operates only on tasks currently rendered by the active view and filters and uses a bottom action bar on smaller screens. Complete-view actions use a labeled **More** menu only when an action is currently available; unavailable Select and More controls are hidden instead of appearing disabled.

## Task inventory export and HTML clipboard

The task-queue header provides a visible **Export** action for creating a portable Markdown work inventory or copying a formatted HTML table. Both actions contain exactly the current results: the selected concrete list or synthetic **All lists** scope, active lifecycle view, search text, tag, type, status, and priority filters, in the current sort field and direction. The dialog previews the resulting count before continuing.

Collapsed groups are presentation only and do not remove matching tasks from the export. In particular, completed and cancelled tasks in the Starred view remain included when its Finished group is collapsed.

When the synthetic **All lists** scope is selected, the Markdown table includes a List column. A concrete-list export omits the redundant List column.

The export dialog provides a column picker limited to the existing task-inventory fields: task ID, title, list when global, type, status, priority, deadline, waiting target, owner, responsible person, source, tags, checklist progress, and last-updated timestamp. At least one column available in the current list scope is required. The selected column set is stored in the current user's application preferences and restored the next time the dialog opens. The List selection is retained but only applies when **All lists** is selected.

Multiline text and Markdown table delimiters are escaped so the generated table remains valid. Bodies, comments, Timeline entries, relationships, and attachment content are not included; this is an operational work inventory, not an archival export.

If the selected task has unsaved changes, either share action requires those changes to be saved first. **Export Markdown** uses the native save-file dialog. Files use UTF-8 without a byte-order mark, are written through a temporary file before replacement, receive a useful timestamped `.md` default name, and start in the directory of the last successful task export. Cancelling or failing an export does not change that preference.

**Copy as HTML** writes both `text/html` and a `text/plain` Markdown fallback to the system clipboard. The HTML uses encoded cell content and a compact table with inline styles so it pastes usefully into common rich-text destinations. It does not open a file dialog, create a file, or change the remembered Markdown export directory. If HTML clipboard support is unavailable, the dialog reports the failure and remains open.

Trash is never included in Markdown task export, and the Export action is not available in the Trash view. A database backup remains the only supported complete portable copy of task bodies, attachments, relationships, comments, checklists, history, and trashed tasks.

## Lifecycle timestamps

Store timestamps directly on the task for fast querying:

- CreatedAt
- UpdatedAt
- ActivatedAt
- WaitingSince
- CompletedAt
- CancelledAt

`WaitingSince` is only set while the task currently has an active wait target.

## Waiting target

Each task can have at most one active wait target.

The wait target is important enough to affect task visibility and emphasis, while the task remains active.

When a wait target is added:

- Task status remains `ACTIVE`.
- `WaitingSince` is set.
- Automatic log entries are created.

When a wait target is cleared:

- The wait target gets `ResolvedAt`.
- Task status changes to `ACTIVE`.
- `WaitingSince` is cleared.
- Automatic log entries are created.

Waiting for is a simple text field. The user must be able to enter direct text, for example:

```text
INC123456
```

It should not be necessary to register the wait target elsewhere before it can be used.

Do not add waiting type, URL, follow-up date, or other structured waiting fields in the first version.

## Source

A task can optionally have a source.

Source answers:

> Where did this task come from?

Source is classification/reference only. It should not trigger automatic opening behavior or integrations.

Store:

- SourceId nullable
- SourceReference nullable
- SourceUrl nullable

Initial sources:

- Manual
- ServiceDesk
- Email
- Teams
- Deployment
- Monitoring/logs
- User report

Examples:

```text
Source: ServiceDesk
SourceReference: INC123456
```

```text
Source: TFS / Azure DevOps
SourceReference: Release #1842
```

Source is not the same as waiting target.

Source fields are hidden in task details by default. User preferences can show them, and the choice persists across application restarts.

Example:

```text
Source: Email from Anna
Waiting for: ServiceDesk INC123456
```

## Comments and automatic logs

Comments and logs are separate concepts.

Comments are human-written notes.

Logs are automatic factual history.

Example timeline:

```text
2026-07-03 12:15  Auto     Task created
2026-07-03 12:22  Comment  Looks related to the release variable replacement script.
2026-07-03 12:40  Auto     Priority changed from Normal to Urgent
2026-07-03 12:45  Auto     Waiting for changed to ServiceDesk INC123456
2026-07-05 09:10  Auto     Waiting for ServiceDesk INC123456 was cleared
2026-07-05 10:30  Auto     Task completed
```

Logs should store both:

- Readable message
- Structured old/new values where useful

New tasks log only `Task created`. For an existing task, every changed field must create a log entry. Fields with dedicated lifecycle logs keep those messages. Other fields use `Field: Changed 'old value' to 'new value'`. Editor body or format changes use only `Editor changed`. Tag changes log the old and new tag lists.

## Checklist items

Tasks can have lightweight checklist items.

Checklist items are not full tasks.

Checklist items should not have their own priority, status, deadline, attachments, or comments in the first version.

A checklist item should support:

- Text
- Sort order
- IsCompleted
- CompletedAt
- CreatedAt

Example:

```text
Task: Fix failed deployment

Checklist:
[ ] Check build artifact
[ ] Compare appsettings.json
[ ] Verify release variables
[ ] Run console app manually
[ ] Update deployment note
```

The task list shows progress when a task has checklist items:

```text
Fix failed deployment    3/5 done
```

The task editor supports adding, editing, deleting, reordering, completing, and reopening checklist items. Added, completed, and reopened items create automatic timeline logs. The checklist appears above attachments and the timeline.

## Attachments

Attachments are stored in SQLite as BLOBs.

Reason:

- Single portable database file.
- Easy backup.
- No broken file paths.
- Simpler local deployment.
- Simpler export/import later.

Recommended attachment fields:

- FileName
- ContentType
- FileSize
- Sha256Hash
- ContentBlob
- Description
- CreatedAt

A soft size limit should be considered, for example 25–50 MB per attachment.

The initial UI supports adding, downloading, and removing attachments. Attachments are limited to 25 MB and appear above the task timeline.

## Database management

Preferences provide a **Data & maintenance** page for backup, restore, sample data, and an explicitly separated danger zone.

Backup uses the native save-file dialog and SQLite's online backup API. The generated database is validated before it replaces the selected destination. The directory from the last successful backup is remembered; cancelling or failing does not change that preference.

Restore uses the native open-file dialog. A selected SQLite database may have any filename: OKF-Todo validates and migrates a private staged copy, leaves the selected source unchanged, creates a dated safety backup, and installs the staged copy as the managed `okf-todo.db` on the next application start. The UI requires the application to close after preparation so later edits cannot be lost.

The Data & maintenance danger zone can replace the complete database with either a fresh empty database or a fresh database containing sample data. It must:

- state that every task, list, attachment, comment, checklist, relationship, preference, and history entry will be removed;
- explain that **Remove sample data** is the safe action when the user only wants to remove samples;
- require the typed confirmation `RESET DATABASE`, matched case-insensitively after trimming;
- create a dated safety backup before replacement; and
- finish through the same restart boundary as restore.

The backup contains the complete SQLite database, including tasks, body images, attachments, lookups, tags, relationships, comments, checklists, history, and database-backed preferences.

## First-run and sample data

When the database contains no tasks, the Active task list offers two equal, non-blocking choices: create the first task or add the standard 50-task sample set. The sample option must emphasize that it is easy to remove later from **Settings → Data & maintenance** and that tasks created by the user are preserved.

While sample tasks are being generated, both sample-data entry points are disabled and show a spinner plus an explicit adding label until the refreshed task list is ready.

Each generated sample task is labelled internally with `TaskItem.IsSampleData = true`. This database marker is authoritative; the visible `sample-data` tag remains ordinary editable tag text. **Remove sample data** deletes only internally marked sample tasks and their owned content and relationships. A personal task is never removed merely because it has a `sample-data` tag.

The 50-task sample set is curated rather than uniformly dense. Ten worked cases tell coherent support and development stories through task-specific bodies, checklists, small valid attachments, comments, relationships, and automatic Timeline history. Other tasks provide moderate or simple examples so the queue still resembles normal use. The set covers partially and fully completed checklists, a reopened checklist item, attachment addition and removal, waiting added and cleared, priority and deadline changes, and a completed task that is reopened and completed again. Generated files are sanitized, deterministic, harmless, and collectively remain small enough for fast first-run creation.

Sample data can be added through the UI only when the database has no tasks. With an empty database, a centered blocking first-run dialog keeps the workspace unavailable until the user chooses **Create first task** or **Explore with sample data**. A downloadable or URL-restored sample database is not used: the built-in seeder keeps sample generation aligned with the current migration and application version.

## Tags

A task can have zero or more tags. Each tag is only a string expression with no color, order, activation state, or other metadata.

Task-list text search includes tag values. The task list also provides an existing-tag multi-select filter and exact single-select filters for task type, lifecycle status, and priority. Status filtering uses the stable lifecycle code so renamed display values do not change the result. When multiple tags are selected, a task matches when it has any selected tag; it does not need to have all selected tags.

Entering a new value creates the tag and attaches it to the task. Removing a tag chip detaches it from the task.

User preferences provide tag administration. A tag value can be renamed. An unused tag can be permanently deleted. A used tag can be merged into another tag; every task association moves to the target tag without duplicates, and the source tag is deleted.

## Task relationships

Tasks can be related to other tasks.

Use a flexible relation table instead of hardcoding columns on `Task`.

Initial relation types:

- Blocks / Blocked by
- Depends on / Required by
- Duplicate of / Has duplicate
- Related to / Related to
- Created from / Created task
- Follow-up to / Has follow-up

Relationships are mainly for navigation and overview in the first version.

The task editor supports adding and removing relationships, shows the correct forward or reverse name, and navigates directly to the related task. Duplicate and self relationships are rejected. Relationship removal uses the application HTML confirmation dialog.

The relationships section is hidden in task details by default. User preferences can show it, and the choice persists across application restarts.

Only `Blocks` / `Depends on` may affect sorting later.

## Owner and responsible

Tasks can optionally record two separate free-text values:

- `Owner`: the person or team accountable for the task.
- `Responsible`: the person currently expected to perform or coordinate the work.

The fields do not require a user directory or lookup table. They appear side by
side with the additional task details above the body when both are enabled.

Owner and Responsible are hidden independently by default. User preferences provide separate **Show owner** and **Show responsible** switches, and each choice persists across application restarts.

The overview text search matches both fields even when either field is hidden in task details.

## Lookup values

All controlled values should be table-based, not hardcoded as enums.

The initial values should come from configuration files.

Startup rule:

```text
If lookup table is empty:
    insert initial values from configuration
Else:
    leave table unchanged
```

This gives good initial defaults while allowing local customization.

Lookup values should be editable in the app UI.

Lookup values should normally be deactivated instead of deleted.

Hard deletion is allowed only when all of these are true:

- The lookup value is not a system value.
- The lookup value is not referenced by any task or history row.
- Deleting it will not break application logic.

Used lookup values must remain in the database and can only be deactivated.

System-critical lookup rows should be protected:

- Code should not be editable in the normal UI.
- IsSystem should not be editable in the normal UI.
- System rows should not be deactivated if application logic depends on them.

The display name can be edited.

Example:

```text
Code: ACTIVE
Name: Active
```

The user may rename `Active` to `Open`, but the code remains `ACTIVE`.

## Lookup management UI

Add a settings/admin area for editable lookup values:

- Task types
- Statuses
- Priorities

The **Settings** button opens a Preferences dialog with isolated pages for General, Appearance, Task details, Data & values, and Data & maintenance. Selecting a navigation item shows only that page's settings. Preference changes apply immediately. Appearance contains the color scheme, interface font size, task layout, and task filter layout controls. Font size offers Smallest at 12px, Small at 14px, Standard at 16px, Large at 18px, and Largest at 20px; Standard is the default, and the selection persists in the application preferences file. The selected size also controls the inherited base text size in both HTML and Markdown body editors without replacing explicit font sizes stored in rich content. The task filter layout defaults to Compact and can switch between the anchored Filters panel and an Expanded presentation that keeps tags, type, status, and priority visible inline. Task details contains visibility controls for optional task-detail fields and sections plus independent editability switches for completed and cancelled tasks. Data & values contains lookup and tag management; backup, restore, sample data, and reset are available only on Data & maintenance.

Task sources, relationship types, body formats, and log types are system-managed in the first version and are not editable in the preferences UI.

## First useful views

Suggested first views:

```text
Active tasks
Ready tasks
Starred tasks
Attention tasks
Waiting tasks
Completed tasks
All tasks
Trash
```

`Active tasks` is the complete unfinished-work umbrella and includes tasks that are waiting. `Ready tasks` contains active tasks with no unresolved wait target. `Waiting tasks` contains active tasks with an unresolved wait target. Ready and Waiting are therefore complementary operational queues within Active.

`Attention tasks` combines active tasks that have urgent priority or a deadline before the current local date. It uses OR semantics, shows each task once, and groups the result as Urgent and overdue, Overdue, then Urgent. The view navigation provides popup help explaining the inclusion and grouping rules.

Cancelled tasks appear only in `All tasks`, where their titles use red struck-through text and all pills are gray. They do not appear in `Completed tasks`.

Tasks in Trash appear only in `Trash`. Starred tasks in Trash do not appear in `Starred` until restored.

An active task whose deadline is before the current local date uses a red deadline pill with white text. A task due today is not overdue.

## Task-list sorting

Every view defaults to **Triage order**. The UI explains that this is a work-triage sequence rather than a database priority field:

```text
1. Overdue tasks
2. Urgent active tasks
3. Active tasks
4. Waiting tasks
5. Can wait
6. Completed and cancelled work
```

Within each group, tasks with the earliest deadline come first, undated tasks remain visible, and the most recently updated task breaks remaining ties. Descending order reverses the complete triage sequence.

Waiting tasks should not disappear. They should be easy to review.

The task list also offers purpose-driven alternatives for developer and support work:

- Focus: configured priority, due date, and waiting since.
- Activity: updated and created timestamps.
- Organize: title, task type, and **Status order**. Status order follows the configured lifecycle-status order and is mainly useful in the `All statuses` view.

The compact control exposes the selected sort explanation through a visible, focusable information icon and accessible description. The popup text follows both the selected order and Asc/Desc direction. The control works after text and tag filtering and provides an icon button for ascending or descending direction. The selected field and direction persist separately for each task view. Lookup-based ordering follows the configured lookup sort order rather than display names, and tasks without a value remain last in either direction.

Task browsing uses a compact responsive command strip. Search, a **Filters** button, and sorting share one row and wrap only when the task queue becomes narrow. The visible result count sits beside the current view name. **Filters** opens an anchored panel for tags, task type, lifecycle status, and priority, and its badge reports the number of selected field filters. Tags remain a multi-select with OR semantics. Active field filters appear as removable chips in a slim contextual row that is absent when no field filters are selected.

Primary workspace actions use function keys that remain available from the main page and while either the HTML or Markdown body editor has focus: **F1** opens Help, **F2** opens New task, **F3** focuses task search, **F8** activates the visible enabled Save action, and **F9** completes the current task. F8 applies to the main task Save button and Save or Save changes buttons in dialogs. Disabled actions do nothing. Other shortcuts are suppressed while an application dialog is open, and all shortcuts ignore held-key repeat events. **Ctrl+K** is reserved for editor behavior and is not an application shortcut.

## Out of scope for first version

- Multi-user support.
- Authentication.
- Cloud sync.
- Deep integrations with ServiceDesk, TFS/Azure DevOps, Teams, or email.
- Automatic opening behavior for source URLs.
- Advanced workflow states like Resolved, Verified, Closed.
- Hard deletion of lookup values that are system values or already used.
- Checklist items as full tasks.
