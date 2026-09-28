# Documentation

Start with the guide for the work you want to do. Each procedure has one owner;
other pages summarize it and link there instead of keeping another walkthrough.

| Need | Authoritative guide |
| --- | --- |
| Use the desktop application, including backup and restore | [Using OKF Todo](help/using-okf-todo.md) |
| Understand what an AI assistant needs | [AI assistants](ai-assistants.md) |
| Understand the context layer | [What is OKF?](what-is-okf.md) |
| Set up and approve direct SQLite work | [OKF layer](help/okf-layer.md) |
| Set up MCP and discover its complete tool surface | [MCP server](help/mcp-server.md) |
| Use the application command adapter | [Command reference](okf/todo-database/references/application-command-interface.md) |
| Build, sign, and publish an Inno installer | [Windows installer](installer-build-and-signing.md) |
| Build a Store package | [Store package](../packaging/msix/STORE.md) |
| Certify and publish a Store release | [Store release runbook](../packaging/msix/RELEASE-RUNBOOK.md) |
| Test a local MSIX installation | [MSIX prototype](../packaging/msix/README.md) |
| Run browser tests | [UI tests](testing/ui-tests.md) |
| Test an installed product | [Installed contract tests](../Okf-Todo.InstalledContractTests/README.md) |
| Check native desktop behavior | [Interactive checks](testing/interactive-repository-check.md) |

## Product and implementation references

- [PRD](PRD.md): current intended product behavior.
- [Data model](DATA_MODEL.md): database design and integrity rules.
- [Implementation plan](IMPLEMENTATION_PLAN.md): milestones, superseded scope,
  remaining work, and validation boundaries.
- [OKF database bundle](okf/todo-database/index.md): navigable schema and application semantics.
- [Screenshot gallery](screenshots.md): current product illustrations.
- [Worked example](source-to-plan.md): source material to a reviewed plan.

## Keeping the guides consistent

The three guides under `help` are canonical sources for offline Help. Build and
publish output receives unchanged copies; never edit an output copy separately.
After a Help edit, build the desktop project and compare all three normal Release
output files with their sources. The MCP process tests also check that the tool
reference matches the advertised tools.

Keep detailed UI instructions and the complete MCP tool list out of the root
README. Update their owning guides and keep the README links useful. Distinguish
requirements from historical design options and release-specific validation
records. Old screenshots and past test results are not evidence of current behavior.

Documentation corrections do not change the MCP contract. New application
capabilities still require the MCP applicability decision described in [AGENTS.md](../AGENTS.md).
