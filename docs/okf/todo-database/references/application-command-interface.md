---
type: Application Command Interface
title: Task Application Command Interface
description: Defines the supported command path for agents that consume the OKF bundle and need to read or mutate tasks.
resource: Okf-Todo/Services/ApplicationCommandService.cs
tags:
  - okf
  - todo
  - commands
timestamp: 2026-07-25T00:00:00Z
---

# Task Application Command Interface

## Purpose

OKF is a descriptive Markdown knowledge format and does not execute commands itself. An agent that consumes this bundle must invoke the application's `--okf-command` adapter when it needs to read or mutate task data.

The adapter and the Photino JavaScript bridge both dispatch through `ApplicationCommandService`. Task mutations therefore use the same validation, lifecycle, timestamp, relationship, and automatic history-log behavior as the desktop UI.

Do not write directly to [TaskItems](../tables/task-items.md), [TaskLogEntries](../tables/task-log-entries.md), or related tables.

## Invocation

Pass one JSON command envelope on standard input:

```powershell
$request = @'
{"messageId":"okf-1","type":"task.get","payload":{"id":42}}
'@

$request | dotnet run -c Release --no-build --project .\Okf-Todo\Okf-Todo.csproj -- --okf-command
```

The adapter writes one JSON response envelope to standard output and diagnostic logs to standard error.

By default, commands use the same personal database as the desktop application. For isolated automation or testing, pass an absolute database file path with `--okf-database-path`:

```powershell
$request | dotnet run -c Release --no-build --project .\Okf-Todo\Okf-Todo.csproj -- `
  --okf-command `
  --okf-database-path C:\Temp\okf-command-test.db
```

`--okf-database-path` is accepted only together with `--okf-command`. The application creates the parent directory when necessary, applies pending migrations, and seeds empty lookup tables before executing the command.

Exit codes:

- `0`: the command succeeded and the response has `"ok": true`.
- `1`: validation or application command processing failed and the response has `"ok": false`.
- `2`: standard input did not contain a command.

## Command Envelope

```json
{
  "messageId": "caller-defined-correlation-id",
  "type": "task.update",
  "payload": {}
}
```

Use a unique `messageId` per call. Responses preserve the identifier and use `<command-type>.result` as their response type.

## Task Mutation Workflow

For updates, read the task first with `task.get`, preserve fields that are not changing, and then submit the complete `task.update` payload. The update path records every changed field in [TaskLogEntries](../tables/task-log-entries.md).

Supported task mutation commands include:

- `task.create`
- `task.update`
- `taskList.moveTasks`
- `taskList.undoMove`
- `task.start`
- `task.undoStart`
- `task.complete`
- `task.reopen`
- `task.cancel`
- `task.waiting.add`
- `task.waiting.clear`
- `task.star.set`
- `task.star.setMany`
- `task.trash`
- `task.trash.restore`
- `task.trash.delete`
- `task.comment.create`
- `task.comment.delete`
- `task.checklist.create`, `task.checklist.update`, `task.checklist.complete`, `task.checklist.reorder`, and `task.checklist.delete`
- `task.relation.create` and `task.relation.delete`
- `task.attachment.create` and `task.attachment.delete`

Use `task.timeline.get` to verify the resulting automatic history entries.

List discovery and desktop list administration commands are:

- `taskList.list`
- `taskList.create`
- `taskList.rename`
- `taskList.reorder`
- `taskList.delete`
- `taskList.moveTasks`
- `taskList.undoMove`

MCP exposes discovery and task assignment/moves, but deliberately does not expose add, rename, reorder, or delete for the master list in this version.

## Required list ownership and resolution

Every task has a required `taskListId`. Discover concrete lists with:

```json
{"messageId":"lists-1","type":"taskList.list","payload":{}}
```

Task creation accepts optional `taskListId` and optional `contextTaskId`. Task update accepts optional `taskListId`; when omitted, the existing task supplies its own context and remains in its current list. All application, MCP, and OKF-guided database writers must use this exact resolution precedence:

1. Use an explicit list reference when supplied.
2. Otherwise infer the list from available task context, such as the existing, source, related, or parent task.
3. Otherwise use the list currently named `Default list`, when present.
4. Otherwise use the first list ordered by `SortOrder`, then `Id`.
5. If no list exists, transactionally insert `Default list` with `SortOrder` 10 and current UTC `CreatedAt` and `UpdatedAt` values, then use its generated ID.

For direct SQLite work, resolve and store the concrete `TaskLists.Id` in `TaskItems.TaskListId` inside the same transaction as the task write. `All lists` is a synthetic desktop scope and must never be inserted into `TaskLists`.

## Core task command contracts

The following payloads are sufficient for a harness that has only this installed OKF bundle and access to the installed command adapter. JSON property names use camel case.

Create a task with `task.create`:

```json
{
  "messageId": "create-1",
  "type": "task.create",
  "payload": {
    "title": "Investigate failed deployment",
    "taskListId": 1,
    "taskTypeCode": "INVESTIGATION",
    "body": "Evidence and proposed next steps.",
    "bodyFormatCode": "MARKDOWN",
    "taskPriorityCode": "NORMAL",
    "taskSourceCode": "DEPLOYMENT",
    "sourceReference": "Release 1842",
    "sourceUrl": null,
    "owner": "Platform team",
    "responsible": "Anna Jensen",
    "deadline": null,
    "activeWaitingForLabel": null,
    "tags": ["deployment", "investigation"]
  }
}
```

Only `title` and `taskTypeCode` must be provided by the caller; required list ownership is resolved by the precedence above when `taskListId` is omitted. The command returns the saved task detail in `payload`, including its numeric `id`, `taskListId`, and `taskListName`. New tasks start with status `ACTIVE` and receive a `TASK_CREATED` history entry.

Read a task with `task.get`:

```json
{
  "messageId": "get-1",
  "type": "task.get",
  "payload": {
    "id": 42
  }
}
```

Replace editable task fields with `task.update`:

```json
{
  "messageId": "update-1",
  "type": "task.update",
  "payload": {
    "id": 42,
    "title": "Investigate failed deployment and release variables",
    "taskListId": 1,
    "taskTypeCode": "INVESTIGATION",
    "body": "Evidence and proposed next steps.",
    "bodyFormatCode": "MARKDOWN",
    "taskPriorityCode": "NORMAL",
    "taskSourceCode": "DEPLOYMENT",
    "sourceReference": "Release 1842",
    "sourceUrl": null,
    "owner": "Platform team",
    "responsible": "Anna Jensen",
    "deadline": null,
    "activeWaitingForLabel": null,
    "tags": ["deployment", "investigation"]
  }
}
```

This is replacement semantics: call `task.get` first and preserve every field that must remain. A null or omitted optional value is cleared, and a null or empty tag collection removes all tags. The command returns the complete saved task detail and creates history for changed fields.

Move one or more non-Trash tasks to a concrete list:

```json
{
  "messageId": "move-list-1",
  "type": "taskList.moveTasks",
  "payload": {
    "taskIds": [42, 43],
    "destinationListId": 2
  }
}
```

The result contains each task's original and destination list IDs and names. Pass its `items` unchanged to `taskList.undoMove` for an immediate reverse move. Every changed task receives a `TASK_UPDATED` Timeline entry naming its source and destination lists.

## Star and Trash command contracts

Set the star state for one task:

```json
{"messageId":"star-1","type":"task.star.set","payload":{"id":42,"isStarred":true}}
```

Set the same star state for multiple tasks:

```json
{"messageId":"star-many-1","type":"task.star.setMany","payload":{"taskIds":[42,43],"isStarred":true}}
```

Star changes update `IsStarred`, `StarredAt`, and `UpdatedAt` but deliberately do not create timeline entries.

Move one or more tasks to reversible Trash:

```json
{"messageId":"trash-1","type":"task.trash","payload":{"taskIds":[42,43]}}
```

Restore tasks from Trash:

```json
{"messageId":"restore-1","type":"task.trash.restore","payload":{"taskIds":[42,43]}}
```

Moving to Trash sets `DeletedAt`; restoring clears it. Both operations preserve lifecycle status, star state, and task-owned data and create a `TASK_UPDATED` history entry. A trashed task remains readable but rejects normal mutations until restored.

Permanently delete tasks that are already in Trash:

```json
{"messageId":"delete-1","type":"task.trash.delete","payload":{"taskIds":[42,43]}}
```

Permanent deletion rejects tasks outside Trash and removes task-owned rows plus relationships where a deleted task is either source or target. This cannot be undone.

## Attachment command contract

Add an attachment with `task.attachment.create` after `task.create` returns the task ID:

```json
{
  "messageId": "attachment-1",
  "type": "task.attachment.create",
  "payload": {
    "taskId": 42,
    "fileName": "error.log",
    "contentType": "text/plain",
    "base64Data": "VGltZW91dCBhZnRlciAzMCBzZWNvbmRzLg==",
    "description": "Customer diagnostic output"
  }
}
```

`base64Data` contains the complete file content. The application normalizes the file name, rejects invalid Base64 and files larger than 25 MB, calculates the byte length and SHA-256 hash, stores the content as a SQLite BLOB, updates the task timestamp, and creates an `ATTACHMENT_ADDED` history entry. The response payload contains the updated attachment list.

Related commands use these payloads:

```json
{"messageId":"attachment-list-1","type":"task.attachment.list","payload":{"taskId":42}}
{"messageId":"attachment-get-1","type":"task.attachment.get","payload":{"attachmentId":7}}
{"messageId":"attachment-delete-1","type":"task.attachment.delete","payload":{"taskId":42,"attachmentId":7}}
```

The get response includes `fileName`, `contentType`, and `base64Data`.

## Direct SQLite capability and boundary

A harness that is separately granted write access to the SQLite database can use the linked table documents to construct direct inserts and updates, including BLOB rows in `TaskAttachments`. OKF is documentation and cannot prohibit those writes.

Direct SQL is not equivalent to an application command. It bypasses filename normalization, file-size validation, automatic hashes unless the harness calculates them, task timestamp coordination, lifecycle services, and automatic history. Use a transaction, enable SQLite foreign keys, and restrict direct writes to disposable or explicitly approved databases. Prefer the application command adapter for normal task and attachment mutations.

## Sources

- `Okf-Todo/Program.cs`
- `Okf-Todo/Services/ApplicationCommandService.cs`
- `Okf-Todo/Services/OkfCommandRunner.cs`
- `Okf-Todo/Bridge/BridgeMessageHandler.cs`
