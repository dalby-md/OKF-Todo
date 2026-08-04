# Use OKF-Todo Day to Day

OKF-Todo is a local task application for keeping development, support, investigation, and follow-up work in one place. You can use it entirely through the desktop interface. No AI assistant, online account, or hosted service is required.

This guide follows the way you normally work: choose where the task belongs, capture it quickly, add useful detail, find it again, and move it safely through completion or cancellation.

## Hotkey map

The primary workspace actions use function keys so they remain available without conflicting with editor shortcuts:

| Key | Available from | Action |
| --- | --- | --- |
| **F1** | Main workspace and body editors | Open Help. |
| **F2** | Main workspace and body editors | Open the New task dialog. |
| **F3** | Main workspace and body editors | Focus and select task search. |
| **F8** | Everywhere a Save action is available | Activate the visible enabled **Save** or **Save changes** button. This includes the current task, New task, unsaved-changes confirmation, lookup editor, tag editor, and task-list details. |
| **F9** | Main workspace and body editors | Complete the current task when **Complete** is available. |

Except for **F8 Save**, function-key shortcuts do not run while an application dialog is open. Holding a key down does not repeat an action. **Ctrl+K** remains available to the editors and is not an OKF-Todo shortcut.

## Start with one task

1. Choose a list in the **List** switcher at the top of the window.
2. Select **New task**.
3. Enter a clear title and select **Save** in the New task dialog.
4. Add the details you need: task type, priority, deadline, waiting target, tags, and body.
5. Select the main **Save** button when you have changed those details.

Saving the New task dialog creates the task immediately. The checklist, attachments, comments, relationships, Complete, and Cancel controls become available as soon as the dialog closes. You do not need to press the main Save button a second time to finish creating the task.

Use the main Save button for changes you make after creation. If you try to leave a task with unsaved changes, OKF-Todo asks whether to **Save**, **Discard**, or **Cancel**:

- **Save** keeps the changes and continues.
- **Discard** abandons the changes and continues.
- **Cancel** stays on the current task so you can keep editing.

## Understand the workspace

The workspace has four parts:

| Area | What it controls |
| --- | --- |
| **List switcher** | Chooses which task list you are working in, or selects **All lists**. |
| **Views** | Chooses the kind of work to show, such as Active, Ready, Waiting, Completed, or Trash. |
| **Task queue** | Searches, filters, sorts, selects, opens, and exports tasks in the current list scope. |
| **Task details** | Shows and edits the selected task. |

The status pill beside the application actions reports saves, copied content, completed operations, and errors. Longer messages wrap or use their own header row so the complete feedback remains readable.

The list switcher and the Views rail work together. For example, selecting the **Support** list and the **Waiting** view shows waiting tasks in Support. Selecting **All lists** and **Waiting** shows waiting tasks across every list.

Two labels that sound similar have different purposes:

- **All lists** is a list scope. It combines tasks from every concrete list.
- **All statuses** is a view. It includes active, completed, and cancelled tasks within the selected list scope.

**Trash** is always global. It shows trashed tasks from every list, regardless of the list currently shown in the header.

## Organize work with lists

Every task belongs to one list. A new installation starts with **Default list**.

Use lists for durable areas of responsibility, projects, customers, or contexts—for example **Support**, **Release 2.4**, **Internal improvements**, or **Customer A**. Use views, priorities, deadlines, and tags for characteristics that cut across those areas.

### Switch lists

Choose a concrete list in the header to work only with that list. OKF-Todo remembers the selected scope.

Choose **All lists** when you need a cross-list overview. Task rows then show a subtle list-name pill, and text search can also match list names.

### Manage lists

Select **Manage** beside the list switcher to:

- add a list;
- rename a list;
- drag lists into your preferred order;
- see how many tasks each list contains; or
- delete a list safely.

At least one list must always remain. If you delete a list that contains tasks, OKF-Todo requires another list as the destination and shows the complete number of affected tasks, including tasks in Trash. The tasks are moved; they are not deleted.

### Move tasks between lists

For one task, use its **List** field or the task-row menu, choose the destination, and save the change.

For several tasks:

1. Select **Select tasks** in the task-queue header.
2. Select the tasks you want to move.
3. Choose **Move selected to list**.
4. Select the destination list.

After a bulk move, the confirmation message offers **Undo**.

## Use views to answer a question

Views are shortcuts to meaningful subsets of work:

Hover over a view in the navigation rail to see a one-line description of the work it contains.

| View | Use it when you want to… |
| --- | --- |
| **Active** | See all current unfinished work, including tasks that are waiting. |
| **Ready** | See active tasks that have no unresolved waiting target and can be worked on now. |
| **Starred** | Return to tasks you deliberately marked for focus. Finished starred tasks remain available in a collapsed Finished group. |
| **Attention** | Review active tasks that are urgent or overdue. Each task appears once, grouped as Urgent and overdue, Overdue, then Urgent. Hover over its navigation entry or the information icon beside its heading for an explanation. |
| **Act now** | Work the actionable part of Attention: urgent or overdue active tasks that do not have an unresolved waiting target. It uses the same three groups. |
| **Waiting** | See tasks that depend on a person, team, case, response, or other external event. |
| **Completed** | Review completed work. |
| **All statuses** | See active, completed, and cancelled work together. |
| **Trash** | Restore tasks or permanently remove them. |

**Ready** and **Waiting** divide active work by whether it has an unresolved waiting target. Use **Ready** as the focused queue for work you can advance now, and return to **Active** when you need the complete unfinished picture.

**Attention** uses an OR rule: a task belongs there when it has Urgent priority, when its deadline is overdue, or both. Overdue means before today; a task due today is not overdue. A task that is both urgent and overdue appears only in the first group, never twice.

**Act now** keeps that same urgency and overdue rule but removes anything currently waiting. Use Attention when you need the complete risk picture, including blocked work. Use Act now when you want the shorter queue of tasks you can advance immediately. Adding a waiting target from Act now moves the saved task to **Waiting** and keeps it selected.

When you add a waiting target while working in **Ready**, saving moves the task to **Waiting** and keeps it selected. Clearing the waiting target from **Waiting** moves it back to **Ready** in the same way.

When completing, cancelling, reopening, or restoring a task changes the view in which it belongs, OKF-Todo switches to that view and keeps the affected task selected. Only the task queue scrolls to reveal it; the surrounding workspace stays in place.

## Find and order tasks

### Search

Use **Search tasks** to match:

- title;
- task type;
- lifecycle status;
- priority;
- waiting target;
- owner;
- responsible person; and
- tags.

When **All lists** is selected, search also matches list names.

Press **F3** to move directly to task search, including while the HTML or Markdown editor has focus.

### Filter

The default **Compact** layout places these controls in **Filters** beside search:

- **Tags** selects one or more existing tags.
- **Type** selects one task type.
- **Status** selects one lifecycle status, such as Active, Completed, or Cancelled.
- **Priority** selects one priority.

Select **Filters** to open the panel. Its number shows how many tag, type, status, and priority filters are selected.

To keep all four controls visible, open **Settings → Appearance → Task filter layout** and select **Expanded**. Expanded uses the same filter values and behavior, arranged inline below search. Switching layouts does not clear the current filters.

When several tags are selected, a task matches if it has any selected tag. Active filters appear as removable chips below the search and filter controls; that row stays hidden when no filters are selected. Select **Clear** to remove the current search and filter criteria.

The task count beside the view name always shows the number of results after the current search and filters.

To isolate cancelled tasks without adding another view, select **All statuses**, then select **Cancelled** under **Status**. Choose **All lists** first when the result should span every list.

Filters affect only the current on-screen result. They do not change or delete tasks.

### Sort

Use **Sort** to choose the order. Select its direction button to switch between ascending and descending order. Each view remembers its own sort choice and direction. Hover over or focus the information icon beside **Sort** to read an explanation that follows the selected order and direction.

**Triage order** is the default:

1. overdue work;
2. urgent active work;
3. other active work;
4. waiting work;
5. work that can wait; and
6. completed or cancelled work.

Within those groups, earlier deadlines rise first. Other sort choices help you focus by configured priority, due date, or waiting time; review activity by updated or created time; or organize by title, task type, or **Status order**.

**Status order** groups tasks by the status order configured under **Settings → Data & values**: Active, Completed, then Cancelled by default. It is mainly useful in **All statuses**, because most other views already contain only one status.

Use **Asc** or **Desc** to reverse the selected order. For Triage order, ascending uses the sequence shown above.

## Add the right amount of task detail

Only the title, task type, and list are required. Add other information when it helps you decide, act, or hand work over.

### Core fields

- **List** says where the task belongs.
- **Task type** says what kind of work it is.
- **Priority** expresses relative urgency: Urgent, Normal, or Can wait.
- **Deadline** records when the task is due.
- **Waiting for** records the person, team, case, answer, approval, or event blocking progress. Free text such as `ServiceDesk INC123456` is allowed.
- **Tags** add lightweight searchable labels. Type a new value and press Enter, or choose an existing tag.

Task type and lifecycle status are different. A task can be an Investigation or Request while its status is Active, Completed, or Cancelled.

### Owner and Responsible

These optional free-text fields have different meanings:

- **Owner** is the person or team accountable for the outcome.
- **Responsible** is the person currently expected to perform or coordinate the work.

They are hidden independently by default. Enable either field under **Settings → Task details**. Enabled fields appear with the other task details above the body. Search matches both fields even when they are hidden.

### Source fields

Enable **Show source fields** under **Settings → Task details** when you want to record where work came from, such as ServiceDesk, email, deployment, or monitoring. The enabled fields appear above the body. Source, source reference, and source URL are descriptive information; OKF-Todo does not open or synchronize external systems automatically.

### Body editor

Use the body for context that does not fit in fields: the problem statement, evidence, links, diagnostic notes, decisions, draft replies, or next steps.

OKF-Todo supports HTML and Markdown editing. Choose your preferred default under **Settings → General → Editor mode**. The mode selector below the editor lets you work with the active task in Markdown or WYSIWYG form.

Drag the horizontal resize bar below the editor to change its height. The minimum is 200 pixels, and OKF-Todo remembers the chosen height.

### Relationships

Enable **Show relationships** under **Settings → Task details** to connect tasks that block, depend on, duplicate, follow, or otherwise relate to each other.

Following a relationship opens the related task. If it belongs to another list, OKF-Todo changes the concrete list scope as needed. **All lists** remains global.

### Checklist

Use checklist items for small steps inside one task. A checklist item is not a separate task and does not have its own priority or deadline.

The task queue shows checklist progress, such as `2/3`. Adding, completing, or reopening checklist items is recorded in the Timeline.

### Attachments

Use **Add file** to keep supporting material with the task. Attachments are stored inside the local OKF-Todo database, so they are included in a database backup and do not depend on the original file path.

The current attachment limit is 25 MB per file. You can download or remove an attachment from the task.

### Timeline and comments

The Timeline combines:

- automatic history, such as creation, field changes, lifecycle changes, list moves, checklist activity, and attachments; and
- comments you add manually.

Use comments for dated progress notes that should not replace the task body. The Timeline remains the final section of task details.

## Star work that deserves attention

Select the star on a task row to add or remove it from your focus set. Star state is independent of priority and lifecycle status, and starring does not add Timeline noise.

Completed and cancelled starred tasks remain available in the **Starred** view under Finished. A trashed task keeps its star state but cannot be starred or unstarred until restored.

On compact or stacked layouts, task-level Star and Trash actions are available from the task-details menu because the selected row may be outside the visible area.

## Work with several tasks at once

Select **Select tasks** beside the result count to enter selection mode. You can then:

- select individual visible tasks or **Select all**;
- star or unstar the selection;
- move the selection to another list; or
- move the selection to Trash.

Selection applies only to tasks currently shown by the active list, view, search, filters, and expanded groups. On a smaller window, the available actions move to a bottom action bar.

Select **Done selecting** when finished.

## Export or copy a work inventory

Select **Export** beside the task count when you need a task overview for a handover, incident review, planning note, customer-status preparation, or another document.

The export contains exactly the tasks in the current results: the selected list or **All lists**, the current lifecycle view, search text, tag, type, status, and priority filters. The dialog shows the scope, ordering, and resulting task count before you continue.

Collapsing a group does not remove its matching tasks from the export. For example, completed and cancelled Starred tasks remain included when the Finished group is collapsed.

When **All lists** is selected, the export includes a List column so readers can see where each task belongs. A concrete-list export omits that redundant column. Export is not available in the Trash view, and trashed tasks are never included.

Use the **Fields** library to add ID, Title, List, Type, Status, Priority, Deadline, Waiting for, Owner, Responsible, Source, Tags, Checklist progress, Checklist items, or Updated to the **Export recipe**. The recipe order is the exported table's left-to-right column order. Drag a recipe field to reorder it, or use its move buttons from the keyboard. Remove a field to return it to the library. Select at least one field available in the current list scope. **List** applies only when **All lists** is selected, but its selection and position are retained when you return to a concrete list. OKF-Todo saves the ordered recipe in your user preferences and restores it the next time you open Export.

**Checklist progress** is the compact completed/total value such as `2/6`. Add the separate **Checklist items** field when readers need the actual steps. It preserves checklist order and labels every item **Open** or **Done**. Detailed items are opt-in, so existing saved recipes remain compact. The preview initially shows three items per task and offers **Show all N** when more exist; the exported Markdown or copied HTML always includes every item.

Under **Row order**, choose **Keep task queue order** to export tasks in the current view order. Choose **Sort by recipe** to sort by the first recipe field, then use each following field to break ties. Use the direction control on a recipe field to reverse that part of the sort. Empty values remain last. OKF-Todo remembers this mode and the field directions with the recipe.

The larger **Preview** area shows up to the first 50 matching tasks. Scroll the preview vertically to inspect more rows or horizontally when the recipe contains more columns; the field library and ordered recipe remain in place.

Choose **Export Markdown** to create a `.md` file. If the open task has unsaved changes, OKF-Todo saves them first. Then choose the destination in the Windows save dialog. The application remembers the directory used by the last successful task export.

Choose **Copy as HTML** to place the same selected tasks and columns on the clipboard as a formatted table. OKF-Todo also places a plain-text Markdown fallback on the clipboard. Paste into an application that accepts rich text, such as Outlook, Word, Teams, or a rich-text editor, to retain the HTML table. Copying does not open a file dialog or create a file.

The table includes only the columns you selected. It does not include the task body, attachment contents, comments, relationships, or Timeline. Checklist item text and current Open/Done state appear only when **Checklist items** is selected.

Treat the Markdown file as a readable snapshot for communication and analysis. It is not a backup and cannot restore your tasks. Use **Settings → Data & maintenance** when you need a complete portable copy of the database.

## Complete, cancel, reopen, and delete safely

### Complete or cancel

Use **Complete** when the intended work is finished. Use **Cancel** when the task should not be completed.

Completed and cancelled tasks are read only by default. Their details are shown as readable values rather than disabled fields, and their body is shown without editing toolbars. Select **Show full body** when a long body is collapsed. Empty optional fields and unavailable mutation controls stay out of the way, while attachments, relationships, checklist results, and the Timeline remain available for review.

Select **Reopen to edit** to return a finished task to Active and restore the normal fields and HTML or Markdown editor.

If your workflow requires direct editing of finished work, enable **Allow editing completed tasks** or **Allow editing cancelled tasks** independently under **Settings → Task details**.

### Move to Trash

Moving a task to Trash is reversible. It removes the task from normal views but preserves its lifecycle state, star, checklist, attachments, comments, relationships, and Timeline.

Use **Undo** in the confirmation message when you moved something by mistake.

Tasks in Trash use the same compact read-only review presentation. Restore a task before changing its fields or related content.

### Restore or permanently delete

Open **Trash**, enter selection mode, and select the tasks you want to restore or permanently delete.

Permanent deletion is available only in Trash and always requires confirmation. It removes the complete task and its owned content. **Empty Trash** deletes every trashed task, including tasks hidden by the current search or filters.

Use permanent deletion only when you are certain the information is no longer needed.

## Personalize the application

Open **Settings** to adjust and maintain OKF-Todo:

| Page | What you can change |
| --- | --- |
| **General** | Default HTML or Markdown editor mode. |
| **Appearance** | Light or layered graphite dark color scheme; Smallest, Small, Standard, Large, or Largest interface font size; Auto, Side by side, or Stacked task layout; and Compact or Expanded task filter layout. In dark mode, warm gold-orange identifies primary interaction, while deeper amber is reserved for waiting and warning states. |
| **Task details** | Visibility of source, owner, responsible, and relationship sections; editability of completed and cancelled tasks. |
| **Data & values** | Task types, priorities, statuses, and tag administration. |
| **Data & maintenance** | Back up or restore the database, add or remove sample data, or deliberately reset all data. |

Preference changes apply immediately and persist between application restarts.

The selected color scheme also applies to the complete in-app **Help** content, including headings, paragraphs, lists, links, tables, inline code, and copyable code blocks.

The **Auto** layout adapts to the available window size. Use **Side by side** when you want the task queue and details visible together on a large screen. Use **Stacked** when you prefer the queue above the selected task.

**Font size** defaults to **Standard** at 16px. Choose **Smallest** (12px) or **Small** (14px) for a denser interface, or **Large** (18px) or **Largest** (20px) for easier reading. The change applies immediately to the interface and the base text in both body editors, and it is remembered after restart. Explicit font sizes already contained in rich HTML remain unchanged.

Use the **Compact** task filter layout when you want more room for the task list. Use **Expanded** when you want Tags, Type, Status, and Priority to remain visible without opening **Filters**.

## Protect and maintain your data

Open **Settings → Data & maintenance** to protect or replace the local database and to manage sample data.

### Back up your work

Select **Create backup**, then choose a destination in the Windows save dialog.

The backup contains the complete SQLite database, including:

- lists and tasks;
- body content and embedded images;
- tags and lookup values;
- checklist items;
- attachments;
- relationships;
- comments; and
- automatic history.

The application validates the backup before replacing the selected destination. It remembers the directory from the last successful backup. Database-backed preferences are included.

Create a backup before large reorganizations, bulk automation, or direct database work.

### Restore from a database file

Select **Restore from a file** and choose a valid OKF-Todo SQLite database. The selected file can have any name. OKF-Todo:

- leaves your selected file unchanged;
- validates and updates a private staged copy;
- creates a dated safety backup of the current database; and
- installs the staged copy as the standard managed `okf-todo.db` when the application next starts.

When preparation succeeds, OKF-Todo shows the managed database and safety-backup paths and asks you to close the application. Start it again to use the restored database. If validation fails or you cancel the file dialog, the active database is not replaced.

### Start with or remove sample data

When the database has no tasks, a centered first-run dialog offers **Create first task**, **Explore with sample data**, or **Skip**. The rest of OKF-Todo remains unavailable until you choose one of these actions. **Skip** closes the dialog without changing the database; if the database is still empty, the dialog is offered again the next time OKF-Todo starts. The dialog also points you to the detailed **Help** guides for using the OKF data layer and MCP server with Codex or Claude Code after you begin. Sample data adds 50 clearly marked tasks that demonstrate priorities, waiting work, checklists, attachments, relationships, comments, Timeline history, completed work, and cancelled work.

Ten tasks are detailed worked cases covering incidents, deployments, security, database recovery, diagnostics, and performance investigations. Open them to see task-specific multi-step checklists, small downloadable evidence files, realistic progress comments, and automatic history such as priority or deadline changes, waiting being cleared, checklist items being reopened, attachments being removed, and completed work being reopened. Other sample tasks are intentionally simpler so the task queue still resembles everyday use.

While the sample tasks are being created, the selected action is disabled and shows a spinner with **Adding sample data...** until the task list is ready.

Sample data is easy to remove:

1. Open **Settings → Data & maintenance**.
2. Select **Remove sample data**.
3. Review the confirmation and select **Remove sample data** again.

OKF-Todo removes only tasks that it marked internally as sample data, together with their related attachments, comments, checklists, relationships, and Timeline entries. Tasks you create yourself are preserved, even if you manually give one the `sample-data` tag. Changes made directly to a sample task are removed with that task.

Sample data can be added only when the database has no tasks. If you want examples in a database that already contains work, create a backup first and use a separate database rather than resetting your current work.

### Reset the entire database

The **Danger zone** contains **Reset to empty** and **Reset with sample data**.

> **Warning:** Reset replaces the complete database. Every task, list, attachment, embedded image, comment, checklist item, relationship, tag, lookup value, Timeline entry, Trash item, and database-backed preference is removed. This is not the normal way to remove sample tasks.

Use **Remove sample data** when you only want to remove the examples and preserve your own work.

A full reset displays the current data counts, requires you to type `RESET DATABASE` (uppercase or lowercase is accepted), and creates a dated safety backup before it prepares the replacement database. You must close and start OKF-Todo again to finish. Do not continue unless you intend to replace all current data.

## Solve common problems

| What you see | What to check |
| --- | --- |
| A task seems to be missing | Check the selected list, view, search text, tag/type/priority filters, and collapsed Finished group. Try **All lists → All statuses** with filters cleared. |
| A completed or cancelled task cannot be edited | Select **Reopen to edit**, or change the applicable setting under **Settings → Task details**. |
| A task in Trash cannot be edited or starred | Restore it first. Trash tasks are deliberately read only. |
| A field or section is missing | Check **Settings → Task details** for Source, Owner, Responsible, and Relationships visibility. |
| Search returns no tasks | Remove filter chips or select **Clear**. Remember that list scope and view still apply. |
| The wrong tasks moved in a bulk action | Selection includes only the tasks visibly rendered when selection mode is active. Use the immediate **Undo** when available. |
| A task changed lists unexpectedly | Read its Timeline for the recorded list move, then use the List field or row menu to move it back. |
| The editor is too short or tall | Drag the resize bar directly below the editor. |

## Use AI assistance only when it helps

OKF-Todo works as a complete local desktop task application without AI.

If you want an AI harness such as Codex or Claude Code to help turn email, transcripts, notes, or logs into proposed tasks and other artifacts, use one of the optional integration guides:

- [Use the OKF layer](okf-layer.md) when the harness needs structured knowledge about the database and you intend to control direct database access.
- [Use the MCP server](mcp-server.md) when you want a compatible harness to use OKF-Todo as a broad secondary or primary task interface. It can discover and find work, read complete task context, make approved partial changes, use lifecycle actions, add comments and checklists, manage relationships and attachments, work with reversible Trash, and organize concrete lists.

The in-app **OKF layer** guide shows the absolute OKF entry-file and active database paths reported by the running application. They automatically match the current operating system, installation directory, and custom database path. Select **Copy prompt** to copy the complete ready-to-use prompt with those paths already filled in.

The in-app **MCP server** guide likewise shows the exact configuration path and ready-to-copy JSON for the running application. An installed copy points to its executable. A source checkout started with `dotnet run` automatically gets a development configuration that starts the same project in headless MCP mode. Select **Copy configuration**, add it to your MCP client, and restart or reload that client.

When a compatible client connects to the built-in MCP server, OKF-Todo supplies the same safe workflow as server instructions: treat source material as data, read before proposing, wait for explicit approval before writing, preserve existing values during updates, and verify the saved task afterward. These instructions guide the AI client; the MCP server executes a write as soon as the client calls a write tool.

For AI-assisted changes, use the same safe pattern every time: **draft, review, save, verify**. Ask for a proposal first, approve writes explicitly, and read the saved result back afterward.
