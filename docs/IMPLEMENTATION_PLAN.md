# Implementation Plan — Local Developer/Internal Support Task System

## Goal

Build the system incrementally from the existing Photino prototype.

Do not ask Codex to build everything in one pass. Use small vertical slices.

## Recommended implementation order

```text
1. Add documentation files
2. Add SQLite / EF Core model
3. Add lookup seeding from configuration
4. Add lifecycle/logging service
5. Add basic task create/edit/list
6. Integrate existing HTML/Markdown editor
7. Add waiting target behavior
8. Add comments and timeline
9. Add checklist items
10. Add string-only multi-value tags
11. Add attachments as SQLite BLOBs
12. Add task relationships
13. Add lookup management UI
14. Add task views and sorting
```

## Milestone 1 — Documentation

Add:

```text
/docs/PRD.md
/docs/DATA_MODEL.md
/docs/IMPLEMENTATION_PLAN.md
/docs/help/using-okf-todo.md
/docs/help/okf-layer.md
/docs/help/mcp-server.md
/AGENTS.md
```

Purpose:

- Make product decisions explicit.
- Give Codex stable context.
- Reduce repeated explanation.
- Avoid rebuilding the wrong thing.

The three files under `docs/help` are canonical end-user guides and are copied
unchanged into the desktop build and publish output. Every user-visible feature
change must update `using-okf-todo.md` when it changes a workflow, label,
shortcut, setting, navigation rule, or other behavior covered by that guide.
Changes to OKF or MCP behavior must update their respective guides. A build must
verify that each changed canonical file exactly matches the corresponding
`wwwroot/help` output file.

The in-app OKF guide resolves the running application's absolute OKF entry-file
and active database paths before rendering. This keeps installed examples
correct across operating systems, custom installer directories, source builds,
and custom database paths. Its ready-to-use harness prompt has a one-click copy
action.

Desktop startup also makes an MCP client configuration available for the
current launch mode. Installed copies preserve the Inno Setup configuration;
source checkouts generate an absolute `dotnet run --no-build` command for the
current build configuration; and framework-dependent output points to the
application DLL. The in-app MCP guide resolves the exact configuration path,
launch description, and JSON at runtime and provides a one-click copy action.

## Milestone 2 — Data foundation

Scope:

- Add EF Core entities.
- Add SQLite DbContext.
- Create and upgrade the schema with EF Core migrations.
- Add lookup entities.
- Add initial config seed objects.
- Add startup seeding from config.

`InitialCreate` is the earliest supported database version. Apply pending migrations at startup and add a reviewed migration for every future schema change. A normal build or startup must not delete user data automatically.

Do not build the full UI yet.

Acceptance criteria:

- App starts with SQLite database.
- SQLite foreign-key enforcement is enabled explicitly.
- Orphan rows are rejected, used lookups are restricted, and task-owned rows cascade.
- Empty lookup tables are seeded from config.
- Non-empty lookup tables are not changed.
- Used lookup rows are not hard-deleted.
- Unused non-system lookup rows can be hard-deleted.
- System lookup codes are stable.

Suggested Codex prompt:

```text
Read /docs/PRD.md and /docs/DATA_MODEL.md.

Implement the initial SQLite/EF Core data model for the local task system.

Scope:
- Add lookup tables with common fields: Id, Code, Name, Description, SortOrder, IsActive, IsSystem, CreatedAt, UpdatedAt.
- Add TaskItem, TaskWaitingFor, TaskComment, TaskLogEntry, TaskChecklistItem, TaskAttachment, TaskTag, TaskTaskTag, TaskRelation, TaskRelationType.
- Add startup seeding from configuration: only seed a lookup table if it is empty.
- Add the initial EF Core migration, apply pending migrations at startup, and enforce SQLite foreign keys explicitly.
- Add integration tests for foreign keys, restrict behavior, cascade behavior, unique indexes, and check constraints.
- Do not hard-delete used lookup rows. Use deactivation for values that have existing references.
- Allow hard deletion only for unused non-system lookup rows.
- Do not build UI in this step except what is necessary to compile.

After implementation:
- Show changed files.
- Explain how to create and apply EF Core migrations.
- Add or update tests where appropriate.
```

## Milestone 3 — Lifecycle and logging service

Scope:

- Add service methods for lifecycle operations.
- Add automatic log entries.
- Add timestamp handling.
- Use stable status codes.

Acceptance criteria:

- Creating a task logs `Task created`.
- Starting a task changes status to `ACTIVE` and logs the transition.
- Adding a wait target keeps status `ACTIVE`, sets `WaitingSince`, and logs the waiting target change.
- Clearing a wait target changes status to `ACTIVE`, clears `WaitingSince`, resolves the wait target, and logs both events.
- Completing a task sets `CompletedAt` and logs completion.
- Reopening a task changes status to `ACTIVE` and logs reopening.
- Cancelling a task sets `CancelledAt` and logs cancellation.

Suggested Codex prompt:

```text
Read /docs/PRD.md and /docs/DATA_MODEL.md.

Implement a lifecycle service for TaskItem.

Rules:
- Create task => ACTIVE
- Add wait target => ACTIVE with waiting target
- Clear wait target => ACTIVE
- Complete task => COMPLETED
- Reopen task => ACTIVE
- Cancel task => CANCELLED

For every state-changing operation:
- Update timestamps.
- Add TaskLogEntry rows.
- Use stable lookup Code values, not display Name values.

Add tests for the lifecycle rules.
Do not build unrelated UI.
```

## Milestone 4 — Basic task UI

Scope:

- Task list.
- Create task.
- Edit task.
- Required fields only:
  - Title
  - Task type
- Optional fields:
  - Priority
  - Deadline
  - Source
  - Source reference
  - Source URL
  - Owner
  - Responsible
  - Body

Acceptance criteria:

- User can create a task quickly.
- New task starts as `ACTIVE`.
- Task details present the editable title as the heading with the lifecycle
  pill beside it, without duplicate Task details or Title labels.
- Wide task details arrange the six primary metadata controls in three rows of
  two fields, while smaller layouts continue to reflow responsively.
- Optional Source, Owner, and Responsible fields use fixed task-detail slots
  above the body; preference switches only control visibility.
- Read-only completed, cancelled, and Trash tasks replace disabled metadata and
  editor chrome with readable values and a compact expandable body reader.
- Relationships, checklist, and attachments follow the body, while Timeline
  and Add comment remain the final section.
- User can edit title/body/type/priority/deadline/source.
- User can edit optional owner and responsible values when their independently persisted visibility switches are enabled.
- Overview text search includes owner and responsible values even when their detail fields are hidden.
- Completed and cancelled task editability is controlled by independent persisted preferences that default to disabled.
- A disabled final-state edit preference makes every desktop mutation control read only while preserving review, download, relationship navigation, and Reopen.
- Changes update `UpdatedAt`.
- Meaningful changes create log entries where appropriate.

Suggested Codex prompt:

```text
Add a minimal task list and task create/edit screen using the existing Photino application style.

Required fields:
- Title
- Task type

Optional fields:
- Body
- Priority
- Deadline
- Source
- Source reference
- Source URL

Do not implement attachments, checklist items, tags, or relationships in this step.
Use the existing lifecycle/logging service.
```

## Milestone 5 — Editor integration

Scope:

- Integrate existing HTML/Markdown editor prototype with `TaskItem.Body` and `TaskItem.BodyFormatId`.
- The user should not manually choose Markdown/HTML unless the existing editor already exposes that naturally.
- Store the format chosen by the editor.

Acceptance criteria:

- Task body can be edited.
- Body persists to SQLite.
- Body format persists.
- Existing task body loads correctly when editing.
- Switching tasks or views with unsaved HTML or Markdown changes requires Save, Discard, or Cancel; Save persists before navigation and Cancel retains the current task.

Suggested Codex prompt:

```text
Integrate the existing HTML/Markdown editor prototype into the task edit screen.

Persist:
- TaskItem.Body
- TaskItem.BodyFormatId

The editor should decide whether content is Markdown or HTML.
Do not add attachments or image upload in this step unless already part of the editor and trivial to keep.
```

## Milestone 6 — Waiting target UI

Scope:

- Add one active wait target per task.
- Waiting for is a single text field.
- Direct entry must be possible, for example `INC123456`.

Acceptance criteria:

- Adding wait target keeps task status `ACTIVE` and sets the active waiting target.
- Clearing wait target keeps task status `ACTIVE` and clears the active waiting target.
- Logs are created.
- Only one active wait target is allowed per task.
- Do not add waiting type, URL, follow-up date, or other structured waiting fields.

Suggested Codex prompt:

```text
Add waiting target UI to the task edit screen.

Rules:
- A task can have only one active wait target.
- User can enter direct text/reference such as INC123456.
- Adding a wait target keeps the task ACTIVE and sets the active waiting target.
- Clearing a wait target keeps the task ACTIVE and clears the active waiting target.
- Set/clear WaitingSince.
- Create automatic log entries.
- Do not add waiting type, URL, follow-up date, stakeholder link, or other structured waiting fields.

Use the lifecycle service. Do not duplicate lifecycle logic in UI code.
```

## Milestone 7 — Comments and timeline

Scope:

- Add comments.
- Show combined timeline of comments and logs.

Acceptance criteria:

- User can add comments.
- Automatic logs appear in the same timeline.
- Timeline clearly distinguishes comments from automatic logs.
- Logs are append-only in normal UI.

Suggested Codex prompt:

```text
Add a task timeline showing both TaskComment and TaskLogEntry.

Requirements:
- Comments are human-written.
- Logs are automatic.
- Timeline must visually distinguish Comment vs Auto.
- User can add comments.
- Adding a comment creates a COMMENT_ADDED log entry if that log type exists.
```

## Milestone 8 — Checklist items

Status: implemented.

Scope:

- Add checklist item CRUD.
- Add completed/reopened behavior.
- Add progress indicator.

Acceptance criteria:

- User can add/reorder/check/uncheck checklist items.
- Completed items get `CompletedAt`.
- Reopened items clear `CompletedAt`.
- Task list can show `3/5 done`.

Suggested Codex prompt:

```text
Add checklist items to tasks.

Checklist items are lightweight:
- Text
- SortOrder
- IsCompleted
- CompletedAt
- CreatedAt
- UpdatedAt

Do not make checklist items into full tasks.
Add log entries for checklist item added/completed/reopened.
```

## Milestone 9 — Tags

Status: implemented.

Scope:

- Add zero or more string-only tags to tasks.
- Use Select2 with tag creation enabled.

Acceptance criteria:

- User can type a new value to create and attach a tag.
- User can select existing tag values.
- User can remove a tag association using the chip's remove control.
- Tags have no metadata beyond their string value.
- User preferences show tag usage counts and allow renaming tags.
- Unused tags can be hard-deleted.
- Used tags can be merged into another tag, moving all task associations without duplicates and deleting the source tag.

Suggested Codex prompt:

```text
Add string-only multi-value tags using TaskTag and TaskTaskTag.

Use a Select2 multi-select with `tags: true`. Put it on the same row as Waiting for. Creating text adds a tag association; removing a chip removes that association. Do not add colors, sort order, activation state, or other tag metadata.
```

## Milestone 10 — Attachments

Status: implemented.

Scope:

- Store files in SQLite as BLOBs.
- Add hash.
- Add basic attachment list/download/open-save behavior.

Acceptance criteria:

- User can attach a file.
- File content is stored in SQLite.
- Metadata is stored.
- File can be saved/exported again.
- Add log entries for attachment added/removed.
- No filesystem path dependency.

Suggested Codex prompt:

```text
Add task attachments stored in SQLite as BLOBs.

Fields:
- FileName
- ContentType
- FileSize
- Sha256Hash
- ContentBlob
- Description
- CreatedAt

Do not store only a file path.
Add log entries for attachment added/removed.
Consider a configurable soft file size warning.
```

## Milestone 11 — Task relationships

Status: implemented.

Scope:

- Add task relation CRUD.
- Show forward and reverse relation names.

Acceptance criteria:

- User can relate two tasks.
- Source and target task cannot be the same.
- Relation type controls display name and reverse display name.
- Add log entries for relation added/removed.
- Relationships are hidden by default and can be shown through a persisted user preference.

Suggested Codex prompt:

```text
Add task relationships.

Use:
- TaskRelation
- TaskRelationType

Support relation types with Name and ReverseName.
Prevent a task from being related to itself.
Show related tasks on the task detail screen.
Add log entries for relation added/removed.
```

## Milestone 12 — Lookup management UI

Status: implemented.

Scope:

- Add settings screens for lookup tables.
- Allow rename, description edit, sort order edit, activation/deactivation.
- Protect system codes.
- Expose only task types, priorities, and statuses in the preferences UI.
- Keep sources, relationship types, body formats, and log types system-managed in the preferences UI.

Acceptance criteria:

- Task type, priority, and status lookup values can be edited in the app.
- Used lookup values are not hard-deleted.
- Unused non-system lookup values can be hard-deleted.
- System lookup codes cannot be changed in normal UI.
- System values required by lifecycle cannot be deactivated.
- Inactive values are not offered for new selections.
- Source, relationship type, body format, and log type values are not exposed as editable preference groups.

Suggested Codex prompt:

```text
Add lookup management UI.

Requirements:
- Lookup rows are editable.
- Do not allow hard delete for used or system lookup rows.
- Allow hard deletion only for unused non-system lookup rows.
- Allow deactivation for used non-system lookup rows.
- Protect Code and IsSystem for system rows.
- Prevent deactivation of system values required by application logic.
- Inactive values remain visible on existing tasks.
```

## Milestone 13 — Views and sorting

Status: implemented.

Scope:

Add first useful views:

```text
Active tasks
Ready tasks
Urgent active tasks
Waiting tasks
Overdue tasks
Completed tasks
All tasks
```

Suggested sort:

```text
1. Overdue tasks
2. Urgent active tasks
3. Active tasks
4. Waiting tasks
5. Can wait
6. Completed hidden by default
```

Acceptance criteria:

- Views use lookup codes, not display names.
- Completed tasks are hidden by default in active views.
- Active includes all unfinished work, Ready contains active tasks without an unresolved waiting target, and Waiting contains active tasks with an unresolved waiting target.
- Attention contains each active task that is urgent or overdue exactly once and presents Urgent and overdue, Overdue, then Urgent groups with explanatory popup help.
- Act now contains the non-waiting subset of Attention, preserves the same three groups, and moves a newly waiting task into Waiting after save.
- Cancelled tasks appear in All, not Completed, and use red struck-through titles with gray pills in the list.
- Active overdue deadlines use a red pill with white text; deadlines due today are not overdue.
- Waiting tasks remain easy to find.
- Every view defaults to visibly explained **Triage order** and offers focus, activity, and organization sort modes suited to developer and support triage. **Status order** states that it follows configured lifecycle-status order and is mainly useful in the All statuses view.
- Lookup-based modes use configured sort order; time-based modes use due, waiting, created, and updated timestamps.
- The compact sort control exposes the selected field explanation through its title and accessible description, provides an icon button for ascending/descending ordering, and persists both selections separately for each view.
- Search, filtering, and sorting share a responsive command area; the filtered result count sits beside the current view name. The persisted Task filter layout preference defaults to Compact, which uses an anchored Filters panel and wraps the command strip only when the task queue becomes narrow. Expanded keeps tags, type, status, and priority visible inline in the original two-column filter layout.
- Text search includes task tags, while explicit multi-tag filtering remains available on demand with OR semantics and removable filter chips.
- The Filters badge counts selected tags and exact task type, lifecycle status, and priority filters. Active field filters participate in the clear action and removable filter summary, which remains absent when no field filters are selected.
- Function-key shortcuts provide editor-safe access to Help (**F1**), New task (**F2**), Search (**F3**), context-aware Save buttons (**F8**), and Complete (**F9**); **Ctrl+K** remains available to the body editors.
- Appearance offers persistent Smallest (12px), Small (14px), Standard (16px), Large (18px), and Largest (20px) font sizes; Standard is the default, and the selected size controls both the application interface and the inherited base text in the HTML and Markdown body editors.
- In-app Help follows the selected color scheme; dark mode explicitly overrides the Markdown viewer's late-loaded foreground, border, inline-code, table, and code-block colors.

## Milestone 14 — Database management

Status: implemented.

Scope:

- Add a dedicated **Data & maintenance** page under Preferences while preserving database backup.
- Select the destination with the native save-file dialog.
- Use SQLite's online backup API.
- Validate the temporary backup before replacing the selected destination.
- Restore a selected valid SQLite database from any source filename without changing that source.
- Stage, migrate, and validate restore/reset databases before replacing the managed `okf-todo.db` at next startup.
- Create a dated safety backup before restore or reset.
- Require the `RESET DATABASE` typed confirmation before a complete database reset, accepting uppercase or lowercase.
- Offer built-in sample data when the database has no tasks and make selective removal easy.
- Mark seeded tasks with an internal `IsSampleData` database field so removal never relies on editable tags.

Acceptance criteria:

- Backup includes the complete SQLite database, including BLOB content.
- The active database is not modified.
- Cancelling the native dialog creates no file.
- A failed backup does not replace an existing valid backup.
- Success and failure are reported in the application status.
- The directory from the last successful backup is used as the next dialog's starting directory.
- A restore source remains unchanged, regardless of its filename.
- Restore and reset cannot silently replace an open database; the prepared operation is applied at the next start.
- Reset warnings enumerate the affected data and require `RESET DATABASE`.
- With an empty database, a centered blocking first-run dialog keeps the workspace unavailable until the user chooses **Create first task**, **Explore with sample data**, or **Skip**, and points to the detailed Help guides for using the OKF data layer and MCP server after the user begins. Skip changes no data, suppresses the prompt for the current session, and allows it to return on the next empty-database start.
- Present the three first-run actions as a polished guided decision sheet: explanatory clean-start and sample-data choice rows, sample-removal reassurance grouped with the sample choice, and a separated Help-and-Skip footer. Preserve the same hierarchy and affordances in light and dark modes.
- Sample-data creation disables both entry points and shows a spinner with an explicit adding label until refresh completes.
- The 50-task set contains ten coherent worked cases plus moderate and simple tasks. It demonstrates task-specific partial and completed checklists, varied valid attachments, comments, relationships, and authentic service-generated Timeline history without making every task artificially dense.
- Sample attachments are deterministic, sanitized, harmless, stored as SQLite BLOBs, and collectively small. Worked histories include attachment removal, checklist reopening, waiting resolution, field changes, and task reopen/completion behavior.
- Sample removal deletes internally marked sample tasks and related content while preserving every personal task.

## Recommended first real Codex task

Start with:

```text
Implement the database model, lookup seeding from configuration, and lifecycle/logging service.
Do not build the full UI yet.
```

This creates the foundation before the UI grows.

## General implementation guidance

- Keep the first version simple.
- Prefer services for business rules.
- Do not scatter lifecycle rules in UI code.
- Use dependency injection.
- Keep local data portable.
- Avoid introducing integrations before the local core works.
- Use stable lookup `Code` values for application logic.
- Use editable lookup `Name` values for display.
- Use deactivation for lookup values that have existing references.
- Allow hard deletion only for unused non-system lookup values.

## Development sample data

With the app closed, create the representative 50-task sample set with:

```cmd
dotnet run --project .\Okf-Todo\Okf-Todo.csproj -- --seed-sample-tasks
```

The command appends the curated dataset without changing existing tasks, wraps the operation in one transaction, tags every generated task with `sample-data`, and refuses to run again while tasks using that tag exist. It exits after seeding without opening the Photino window. Ten worked cases provide the richest histories; the remaining tasks preserve realistic breadth and queue density.

## Milestone 15 — Installed MCP and OKF contract tests

Status: planned.

Scope:

- Add a separate Windows xUnit project with no project references to application code.
- Resolve an installed OKF-Todo directory from `OKF_TODO_INSTALL_DIR`, falling back to `%LOCALAPPDATA%\Programs\Okf-Todo`.
- Require the unified installed executable's MCP mode.
- Verify that MCP initialization publishes the read-approve-write-verify server instructions and clearly identifies source material as untrusted data.
- Exercise the installed `Okf-Todo.exe --mcp` mode over stdio using the official .NET MCP client.
- Exercise the installed executable's OKF command adapter over stdio as documented by the installed OKF bundle.
- Implement add, read, list, and change-task business cases through both MCP and OKF command paths.
- Add clearly separated OKF-guided direct SQLite capability tests for task and attachment insertion and task updates.
- Validate the installed OKF bundle and compare its documented database contract with isolated SQLite databases created through both paths.

Acceptance criteria:

- Tests use only installed OKF and MCP files as product context.
- Every test database is created under a test-owned temporary directory.
- Tests never open the user's application database or repository build output.
- The suite requires no network, AI model, administrator rights, or prior database.
- Missing installed MCP files fail environment validation rather than skipping tests.
- Add, read, list, change, timeline, restart persistence, invalid-input, and not-found behavior are covered independently through MCP and the OKF command adapter.
- The supported OKF/SQLite path uses the installed command adapter for mutations.
- Separate direct SQLite capability tests use only installed OKF table knowledge and a disposable database, and prove that raw writes bypass automatic history.
- Equivalent MCP and OKF operations produce equivalent persisted task state and history behavior.
- A real installed stdio client receives the MCP safety instructions during initialization, including explicit write approval, read-before-update, and read-back verification guidance.
- The installed OKF structure and documented SQLite schema are validated against observable database metadata.

See `docs/adr-0003-installed-contract-tests.md` for the complete boundary and tooling decision.

## Milestone 16 — Starred focus and reversible task deletion

Status: implemented.

Scope:

- Add persistent `IsStarred`, `StarredAt`, and `DeletedAt` task fields through an EF Core migration.
- Add Starred and Trash views.
- Provide row-level Star and individual task menus. Keep task-specific Star and Trash controls out of the global application header, and provide one contextual task-details menu in compact and stacked layouts where the selected row may be outside the visible area.
- Add filtered bulk selection with Star, Unstar, Move all selected to Trash, Restore, and Delete all selected permanently.
- Make bulk selection discoverable through a labeled Select tasks control, readable task/selection counts, an explicit Done selecting state, and a one-time dismissible coach mark persisted in user preferences.
- Keep permanent-delete controls out of normal views. In Trash, use Select mode for arbitrary subsets and a compact header overflow action for Empty Trash across the complete Trash view.
- Add Move all cancelled to Trash to All as a confirmed complete-view shortcut that includes matches hidden by client-side filters or collapsed groups.
- Keep Trash reversible by default with an immediate Undo action.
- Make trashed tasks read only throughout the desktop UI and task mutation services.
- Preserve star and lifecycle state across Trash and restore.

Acceptance criteria:

- Star state persists and is independent of lifecycle status.
- Starred shows active focus first and keeps completed/cancelled stars in a collapsed Finished group.
- Moving a task to Trash removes it from all normal views but preserves its complete record graph.
- Bulk selection never reaches tasks hidden by the current view, filters, or collapsed group.
- Small windows use a fixed bottom selection action bar without horizontal page overflow.
- Select and complete-view More actions remain labeled at practical widths, collapse cleanly when the task queue is narrow, and are hidden when no applicable tasks or actions exist.
- Permanent deletion is possible only from Trash and requires the application confirmation dialog.
- Empty Trash confirms the complete Trash count and includes tasks hidden by current search or filters.
- Star controls remain unavailable in Trash; previously starred tasks retain a passive marker as context.
- Selection actions operate only on selected rendered tasks. The All-only cancelled Trash action confirms its complete matching count, includes hidden or collapsed matches, remains reversible through Undo, and never appears in other views.
- Wide layouts use row controls without duplicating Star or Trash in the application header. Compact and stacked task details expose one contextual menu with Star/Unstar and the applicable Trash actions.
- Restore returns an individually opened task to its appropriate lifecycle view and editable state.
- Playwright coverage verifies selection discoverability and coach-mark persistence, individual star/trash/undo, bulk Trash, permanent deletion, restore, read-only behavior, and the responsive action bar.

## Milestone 17 — User-managed task lists

Status: implemented.

Scope:

- Add `TaskList` with case-insensitively unique names, manual order, timestamps, counts, CRUD, reorder, and safe transactional deletion.
- Add required restrictive `TaskListId` ownership to every task and backfill upgraded databases into `Default list`.
- Guarantee at least one list at startup and before task creation.
- Centralize explicit, task-context, named-default, manual-order, and zero-list resolution for desktop commands, MCP, and documented OKF-guided writes.
- Add the responsive header list switcher, synthetic **All lists**, list manager, first task metadata field, global list pills/search, scoped queries, list-aware creation, bulk move with Undo, Trash restrictions, and cross-list relationship navigation.
- Add MCP list discovery, explicit/inferred task creation assignment, and task moves without exposing master-list administration.
- Record every list move in the task Timeline.

Acceptance criteria:

- Migration creates `Default list`, backfills existing tasks, and establishes required restrictive ownership.
- The final list cannot be deleted; deleting a used list moves every normal and Trash task transactionally.
- Concrete and global scopes persist; Trash remains global.
- The same list-resolution precedence is used and documented across application commands, MCP, and OKF/SQLite writes.
- Focused service, migration, MCP, installed-contract, and Playwright coverage exercise creation, resolution, scoping, list management, moves, Undo, and responsive behavior.

## Milestone 18 — Task inventory export and HTML clipboard

Status: implemented.

Scope:

- Add a visible task-queue **Export** action and a focused current-results preview dialog with a two-pane ordered **Export recipe**.
- Export the task IDs produced by the selected concrete list or synthetic **All lists**, active lifecycle view, search, and tag/type/status/priority filters. Preserve current queue order by default, with an explicit **Sort by recipe** mode for composite ordering by the ordered export fields.
- Keep collapsed groups presentation-only, so matching finished Starred tasks remain included, and exclude Trash entirely.
- Provide a picker for the existing operational columns and persist the selected column set in the current user's application preferences.
- Generate a compact operational Markdown table with the selected IDs, planning fields, ownership, source, tags, checklist progress, optional detailed checklist items with Open/Done status, and updated timestamps.
- Generate an encoded, compact HTML version of the same table and copy it with a plain-text Markdown fallback through the WebView clipboard API.
- Use the native save-file dialog, remember the last successful export directory, and write UTF-8 files atomically.
- Require the current task's unsaved edits to be saved before exporting.

Acceptance criteria:

- A concrete-list export contains exactly the current filtered results in view sort order and omits the redundant List column.
- A global export contains exactly the current filtered results across concrete lists and includes the List column.
- A Starred-view export includes matching completed and cancelled tasks even when the Finished group is collapsed.
- Trash is excluded and the Export action is unavailable in the Trash view.
- The export dialog restores the current user's ordered field recipe, row-order mode, and per-field directions; requires at least one applicable field; and applies List only to global exports while retaining its saved recipe position.
- Markdown-reserved characters and multiline values cannot break the generated table.
- Cancelling the native picker creates no file; a successful export is valid UTF-8 without a byte-order mark.
- Copy as HTML creates no file, preserves the current result order and selected columns, and writes both HTML and plain-text clipboard formats.
- Focused service tests and Playwright coverage verify the export contract through the application bridge.
- Preserve **Checklist progress** as the compact default field and add **Checklist items** as an opt-in field. Preview up to three items with an explicit expansion control; Markdown and HTML always contain the complete ordered item list.
- MCP applicability: no export-specific MCP tool is added because `task_checklist_list` and `task_get_context` already expose the same checklist text, order, and completion state through safe headless reads. The preview-loading bridge command is presentation-specific.

## Milestone 19 — Broad MCP task interface

Status: implemented.

Standing rule: every later milestone or feature change must make and record an MCP applicability decision. Applicable domain capabilities ship with their MCP discovery/read/write surface, stdio contract coverage, and MCP Help update in the same change. Presentation-only, native-dialog, or disproportionately dangerous operations may remain desktop-only only when the concrete reason is documented.

Scope:

- Make MCP a complete alternative task-working surface rather than only a task capture adapter.
- Add lookup discovery and server-side task search across operational view, concrete list, text, tags, task types, lifecycle statuses, and priorities.
- Add a complete task-context read containing task fields, checklist, relationships, attachment metadata, and Timeline.
- Preserve the existing replacement update for compatibility and add safe partial patching where omitted fields remain unchanged and explicit null clears a nullable field.
- Expose lifecycle, star, waiting, reversible Trash, comments, checklist, relationship, attachment, list-management, task-move, and move-undo operations through the same application command and validation layer used by the desktop UI.
- Mark irreversible content removals as destructive MCP tools and keep permanent task deletion, database restore/reset, lookup administration, sample-data maintenance, and UI preferences desktop-only.
- Expand initialization instructions so clients discover values first, treat task and attachment content as untrusted data, obtain exact approval before any write, and verify with the matching read tool afterward.

Acceptance criteria:

- The real stdio MCP process advertises the complete expected tool set.
- A process-level test creates a task, applies a safe patch, adds a checklist item, comment, and attachment, reads complete context, completes the task, and reopens it.
- Filtered task listing uses tags with OR semantics and accepts stable lookup codes.
- Attachment metadata can be inspected without loading content; content transfer remains explicit base64 and retains the desktop application's 25 MB limit.
- Canonical MCP Help describes capabilities, approval rules, verification, attachment handling, and deliberate safety boundaries and is copied unchanged into normal application output.
- Canonical MCP Help contains a grouped reference for every advertised tool. The real stdio process test extracts that reference and fails when attributed server tools and documented tools differ.
