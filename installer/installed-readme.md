# OKF-Todo installed integration

The desktop application, command adapter, MCP server, and OKF context use the same personal SQLite database:

```text
%LOCALAPPDATA%\Okf-Todo\okf-todo.db
```

Installing, upgrading, repairing, or uninstalling OKF-Todo never overwrites or
removes this database. Only an explicit restore or reset performed inside the
application can replace it.

## OKF context

The installed Open Knowledge Format context graph starts at:

```text
..\okf\todo-database\index.md
```

The graph describes the SQLite schema, relationships, integrity rules, lifecycle rules, and supported command interface. SQLite remains the source of task data.

## Application command adapter

Send one JSON command on standard input to:

```text
..\Okf-Todo.exe --okf-command
```

Application logs are written to standard error and the JSON response is written to standard output.

## MCP server

`mcp-config.json` in this directory contains a ready-to-copy MCP client configuration using `..\Okf-Todo.exe --mcp`. The MCP server is a headless stdio mode started on demand by the MCP client; it does not open the desktop window and is not a Windows service.

Open **Help** in the desktop application for step-by-step OKF and MCP setup, usage, safety, and troubleshooting guidance.
