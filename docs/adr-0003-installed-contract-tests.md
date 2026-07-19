# ADR 0003: Installed MCP and OKF Contract Tests

## Status

Proposed

## Context

OKF-Todo ships an OKF context bundle, an application command adapter in the GUI executable, and, when selected in the Windows installer, an MCP server. Automated acceptance tests must prove that an AI harness can understand the installed context, use both supported command paths, and observe the resulting SQLite data without relying on source-code knowledge.

Tests that reference application projects, EF Core entities, internal services, repository documentation, publish output, or a developer database would not verify the installed user experience. Browser automation is also the wrong boundary because these contracts are exposed through files, MCP over standard input/output, and SQLite rather than through the desktop UI.

## Decision

Add a separate Windows installed-contract test project. The test project is a test runner and is not part of the installed product. Its complete product input is limited to files produced by the Windows installer:

- `{install}\okf\todo-database\index.md` and the installed files reachable from it.
- `{install}\Okf-Todo.exe`, invoked as directed by the installed OKF application-command reference with `--okf-command` and `--okf-database-path`.
- `{install}\mcp\Okf-Todo.Mcp.exe` and its installed runtime files.
- Fresh temporary SQLite databases created separately through the installed OKF command adapter and the installed MCP server.

The tests must not reference `Okf-Todo`, `Okf-Todo.Mcp`, or another application project. They must not read the repository's `docs\okf` directory, installer staging directory, publish output, application database, or user preferences.

The installed GUI command adapter and MCP server are hard preconditions. The suite assumes that the Windows installer included the MCP component. A missing `{install}\Okf-Todo.exe` or `{install}\mcp\Okf-Todo.Mcp.exe` fails environment validation and does not skip tests.

Use xUnit, the official .NET MCP client, and `Microsoft.Data.Sqlite`. Do not use Playwright for these tests. The suite is deterministic and offline; no AI model, network service, or additional organizational context participates.

Each test receives an isolated temporary directory and database. It launches and terminates only the installed command process required for that test, uses bounded timeouts, and removes temporary data after completion. Tests must never discover or open the user's normal OKF-Todo database.

Implement every core business case through both supported installed paths:

- **MCP path:** invoke the relevant MCP tools through `{install}\mcp\Okf-Todo.Mcp.exe --database-path <temporary-database>`.
- **OKF/SQLite path:** follow the installed OKF command-interface contract and send command envelopes to `{install}\Okf-Todo.exe --okf-command --okf-database-path <temporary-database>`.

Also implement a separate **OKF-guided direct SQLite capability path**. It reads and validates the installed OKF table descriptions before constructing inserts or updates against a disposable database. This path proves that a harness with OKF and database access can create or change a task and insert an attachment BLOB. It must explicitly assert that raw writes bypass application validation and automatic history; it is not treated as equivalent to the supported command paths.

The supported OKF/SQLite path must not mutate tables using raw SQL. The installed OKF contract directs normal mutations through the application command adapter so validation, lifecycle timestamps, and automatic history are preserved. Direct SQLite mutation is limited to the clearly named capability tests and disposable databases. Other direct SQLite access in the tests is read-only evidence and schema inspection.

The initial suite covers:

1. Installation contract: required installed OKF and MCP entry points exist.
2. OKF structure: the installed entry point and its internal links are valid and remain inside the installed OKF root.
3. MCP protocol: initialize, list tools, call tools, handle errors, and shut down cleanly.
4. MCP business workflows: add, read, list, and change a task, then read its timeline.
5. OKF/SQLite business workflows: perform equivalent add, read, list, change, and timeline operations through the installed OKF command adapter.
6. Cross-path parity: both paths preserve unchanged values, reject invalid input, report missing tasks, and persist data across process restarts.
7. OKF-guided SQLite capability: insert a task and attachment and update a task directly from installed table knowledge, then prove the application observes the stored values and that automatic history was bypassed.
8. SQLite evidence: direct read-only queries confirm that each installed command path wrote the expected isolated data and history.
9. Cross-layer contract: the schema described by installed OKF agrees with the SQLite schemas created through both installed command paths for documented tables, columns, keys, foreign keys, and indexes.

The installation root is supplied through `OKF_TODO_INSTALL_DIR`. When it is absent, the runner uses `%LOCALAPPDATA%\Programs\Okf-Todo`. The resolved absolute path is printed in test diagnostics.

## Consequences

- The tests exercise the same OKF command adapter and MCP artifacts available to an installed user and harness.
- Internal refactoring cannot make the tests pass unless the installed external contracts still work.
- The Windows installer must be built and installed before running the suite.
- Because MCP is assumed installed, the suite is unsuitable for validating a core-only installation.
- The tests run only on Windows and require no administrator rights, network access, AI credentials, or existing OKF-Todo data.
- GUI behavior remains covered separately; adding GUI automation later does not change this contract-test boundary.
