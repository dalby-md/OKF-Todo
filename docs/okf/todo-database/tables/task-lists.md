---
type: SQLite Table
title: TaskLists
description: Stores user-managed task lists and their manual display order.
resource: Okf-Todo/Data/AppDbContext.cs
tags:
  - sqlite
  - todo
timestamp: 2026-07-25T00:00:00Z
---


# TaskLists

## Purpose

Stores user-managed task lists and their manual display order.

## Schema

| Column | SQLite type | Null | Default | Role |
| --- | --- | --- | --- | --- |
| `Id` | `INTEGER` | No | `-` | primary key position 1 |
| `Name` | `TEXT` | No | `-` | value |
| `SortOrder` | `INTEGER` | No | `-` | value |
| `CreatedAt` | `TEXT` | No | `-` | value |
| `UpdatedAt` | `TEXT` | No | `-` | value |

## Relationships

No foreign keys originate from this table.

## Indexes

- `IX_TaskLists_Name` on `Name`: unique.
- `IX_TaskLists_SortOrder` on `SortOrder`: non-unique.

## Integrity Rules

See [Database Integrity Rules](../references/integrity-rules.md) for cross-table policy.

## Application Semantics

Structural facts are generated from the inspected SQLite database. Application behavior is governed by the product data model and services.
- Every task belongs to exactly one concrete task list; the UI's `All lists` scope is synthetic and is not stored here.
- Names are trimmed and case-insensitively unique, and at least one list must exist.
- The list currently named `Default list` is an ordinary list after creation and may be renamed, reordered, or deleted when another list remains.
- Zero-list recovery inserts `Default list` with sort order 10 and current UTC creation and update timestamps.
- Deleting a list never deletes tasks: affected tasks, including trashed tasks, are moved to a required destination in the same transaction.

## Sources

- [Data model](../../../DATA_MODEL.md)
- `Okf-Todo/Data/AppDbContext.cs`
