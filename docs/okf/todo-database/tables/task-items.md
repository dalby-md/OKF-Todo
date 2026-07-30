---
type: SQLite Table
title: TaskItems
description: Stores the primary task records and lifecycle state.
resource: Okf-Todo/Data/AppDbContext.cs
tags:
  - sqlite
  - todo
timestamp: 2026-07-30T00:00:00Z
---


# TaskItems

## Purpose

Stores the primary task records and lifecycle state.

## Schema

| Column | SQLite type | Null | Default | Role |
| --- | --- | --- | --- | --- |
| `Id` | `INTEGER` | No | `-` | primary key position 1 |
| `ActivatedAt` | `TEXT` | Yes | `-` | value |
| `Body` | `TEXT` | Yes | `-` | value |
| `BodyFormatId` | `INTEGER` | Yes | `-` | foreign key to BodyFormats.Id |
| `CancelledAt` | `TEXT` | Yes | `-` | value |
| `CompletedAt` | `TEXT` | Yes | `-` | value |
| `CreatedAt` | `TEXT` | No | `-` | value |
| `Deadline` | `TEXT` | Yes | `-` | value |
| `DeletedAt` | `TEXT` | Yes | `-` | value |
| `IsStarred` | `INTEGER` | No | `0` | value |
| `Owner` | `TEXT` | Yes | `-` | value |
| `Responsible` | `TEXT` | Yes | `-` | value |
| `SourceReference` | `TEXT` | Yes | `-` | value |
| `SourceUrl` | `TEXT` | Yes | `-` | value |
| `StarredAt` | `TEXT` | Yes | `-` | value |
| `TaskListId` | `INTEGER` | No | `-` | foreign key to TaskLists.Id |
| `TaskPriorityId` | `INTEGER` | Yes | `-` | foreign key to TaskPriorities.Id |
| `TaskSourceId` | `INTEGER` | Yes | `-` | foreign key to TaskSources.Id |
| `TaskStatusId` | `INTEGER` | No | `-` | foreign key to TaskStatuses.Id |
| `TaskTypeId` | `INTEGER` | No | `-` | foreign key to TaskTypes.Id |
| `Title` | `TEXT` | No | `-` | value |
| `UpdatedAt` | `TEXT` | No | `-` | value |
| `WaitingSince` | `TEXT` | Yes | `-` | value |
| `IsSampleData` | `INTEGER` | No | `0` | value |

## Relationships

- `BodyFormatId` references [BodyFormats](body-formats.md).`Id`; delete `RESTRICT`, update `NO ACTION`.
- `TaskListId` references [TaskLists](task-lists.md).`Id`; delete `RESTRICT`, update `NO ACTION`.
- `TaskPriorityId` references [TaskPriorities](task-priorities.md).`Id`; delete `RESTRICT`, update `NO ACTION`.
- `TaskSourceId` references [TaskSources](task-sources.md).`Id`; delete `RESTRICT`, update `NO ACTION`.
- `TaskStatusId` references [TaskStatuses](task-statuses.md).`Id`; delete `RESTRICT`, update `NO ACTION`.
- `TaskTypeId` references [TaskTypes](task-types.md).`Id`; delete `RESTRICT`, update `NO ACTION`.

## Indexes

- `IX_TaskItems_BodyFormatId` on `BodyFormatId`: non-unique.
- `IX_TaskItems_DeletedAt_IsStarred` on `DeletedAt`, `IsStarred`: non-unique.
- `IX_TaskItems_IsSampleData` on `IsSampleData`: non-unique.
- `IX_TaskItems_StarredAt` on `StarredAt`: non-unique.
- `IX_TaskItems_TaskListId` on `TaskListId`: non-unique.
- `IX_TaskItems_TaskPriorityId` on `TaskPriorityId`: non-unique.
- `IX_TaskItems_TaskSourceId` on `TaskSourceId`: non-unique.
- `IX_TaskItems_TaskStatusId` on `TaskStatusId`: non-unique.
- `IX_TaskItems_TaskTypeId` on `TaskTypeId`: non-unique.

## Integrity Rules

See [Database Integrity Rules](../references/integrity-rules.md) for cross-table policy.

## Application Semantics

Structural facts are generated from the inspected SQLite database. Application behavior is governed by the product data model and services.
- `TaskListId` is required and identifies the task's concrete owning list.
- Missing list assignment resolves in this order: explicit list, contextual task list, the list named `Default list`, first manually ordered list, then creation of `Default list` when no lists exist.
- `Owner` is optional free text identifying the person or team accountable for the task.
- `Responsible` is optional free text identifying the person currently expected to perform or coordinate the work.
- `IsSampleData` is an internal ownership marker set by the built-in sample-data seeder; it is authoritative for selective sample removal, while the editable `sample-data` tag is not.
- Removing sample data deletes only tasks where `IsSampleData = 1`, their owned rows, and relationships involving those tasks; personal tasks remain.
- The overview text search includes both values even when their independently controlled task-detail fields are hidden.

## Sources

- [Data model](../../../DATA_MODEL.md)
- `Okf-Todo/Data/AppDbContext.cs`
