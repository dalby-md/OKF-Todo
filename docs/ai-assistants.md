# AI Assistants and OKF-Todo

OKF-Todo can work with an AI assistant that has access to suitable tools on your
computer. The assistant does not have to be designed specifically for coding,
and the AI model itself does not have to run locally.

What matters is whether the assistant can receive your source material, use the
OKF-Todo context, reach an approved action interface, and let you review its
work before anything is saved.

## Model, assistant, and tools

These terms describe different parts of the system:

- An **AI model** interprets instructions and generates a response. On its own,
  it cannot see a path on your computer, run a command, or update OKF-Todo.
- An **AI assistant** combines a model with an application that manages the
  conversation, context, permissions, and available tools.
- A **tool** performs a concrete action, such as reading a Markdown file,
  running an application command, querying SQLite, or calling an MCP operation.

The assistant may use a model hosted by a provider while its approved tools run
on your computer. In that arrangement, the tools are local even though model
inference is not. Check the assistant provider's data and privacy controls before
supplying customer mail, support transcripts, logs, or other sensitive material.

OKF-Todo remains a local-first application: its database and application
services stay on your computer. Connecting an AI assistant does not by itself
make the model local or prevent selected conversation content from being sent to
the assistant's model provider.

## What access the assistant needs

The required access depends on the result you want and the interface you choose.

| Approach | Required capability | What the assistant can do |
| --- | --- | --- |
| Artifact only | Accept source material and produce reviewable text or files | Prepare summaries, plans, replies, handovers, and proposed tasks without opening the OKF-Todo database |
| OKF with direct SQLite | Read local Markdown files and use a SQLite-capable tool against the approved database path | Inspect current values and perform explicitly approved database changes using the installed OKF rules |
| MCP server | Act as an MCP client and start or connect to the local OKF-Todo MCP server | Discover, read, create, and update supported task data through structured application tools |
| CLI commands | Run approved local commands and read structured command results | Use the OKF-Todo application command interface from a terminal or script |

Generic file access is not sufficient for direct database work. A SQLite database
must not be edited as an ordinary text or binary file. Direct database access
requires a SQLite-capable tool and compliance with the schema, foreign-key,
transaction, and Timeline rules described by the installed OKF context.

For broad task work, prefer the MCP server because it exposes structured
operations backed by the application's validation and lifecycle services. The
CLI is useful for explicit application commands and automation. Direct SQLite
access is an advanced route: it can perform operations outside the application
services, so the assistant is responsible for preserving integrity and recording
required history.

## Compatibility checklist

An assistant is a practical fit for OKF-Todo when it supports the capabilities
needed by your chosen approach:

- It can receive the source material you want analyzed without treating that
  material as instructions to follow.
- It can read the installed OKF Markdown entry point and follow relevant links.
- It can access paths outside its initial workspace when you explicitly approve
  the OKF directory or database location.
- It can use at least one safe action route: OKF with a SQLite-capable tool, the
  local MCP server, or OKF-Todo CLI commands.
- It has understandable permission controls for file access, commands, and tools.
- It can begin with read-only inspection and show the complete proposed change.
- It can wait for explicit approval before writing.
- It can read the affected task data back after saving and show the final stored
  result.

An assistant that can only generate chat responses can still prepare useful
artifacts. It is not able to update OKF-Todo until you provide a compatible,
approved action route.

## Example implementations

The products below are representative examples, not endorsements or a complete
compatibility list. Their features and configuration formats can change, so use
their current official documentation when connecting them.

### Codex

[Codex](https://developers.openai.com/codex) can work with project files and
controlled tools, run commands in an approved environment, and use MCP servers.
That makes it suitable for the OKF and CLI routes as well as the built-in
OKF-Todo MCP server. A Codex task working in this repository is an example of an
AI assistant using a model together with workspace access, permissions, and
local tools; Codex is more than the model alone.

### Claude Code

[Claude Code](https://code.claude.com/docs/en/cli-usage) provides terminal and
file tools and can configure MCP servers. It can use the OKF context with an
approved database tool, run OKF-Todo CLI commands, or connect to the OKF-Todo
MCP server.

### Gemini CLI

[Gemini CLI](https://github.com/google-gemini/gemini-cli/blob/main/docs/tools/mcp-server.md)
works with local files and tools and supports configurable MCP servers. It can
use the OKF context and local commands or connect to the OKF-Todo MCP server when
the required paths, folder trust, and tool calls are approved.

### GitHub Copilot CLI

[GitHub Copilot CLI](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference)
supports permission-controlled file and command access and
[configurable local MCP servers](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-mcp-servers).
It can use OKF-Todo when the required directories and operations are allowed for
the session or repository.

### OpenCode

[OpenCode](https://opencode.ai/docs/tools) provides file editing and terminal
tools with configurable permissions and supports
[local MCP servers](https://opencode.ai/v2/docs/mcp-servers). It can use the
OKF, CLI, or MCP routes when configured with access to the necessary local paths
and commands.

Other desktop applications, command-line agents, IDE assistants, and custom
agent systems can also work with OKF-Todo. Product category is less important
than satisfying the compatibility checklist and preserving the approval
workflow.

## Draft, review, save, verify

Use the same four-stage workflow with every assistant and every action route:

1. **Draft:** provide the source material and request a proposed artifact or
   task change without allowing a write.
2. **Review:** check facts, assumptions, target tasks, fields, attachments, and
   lifecycle actions. Correct the proposal in the conversation.
3. **Save:** explicitly approve the exact proposal and the interface the
   assistant should use. Approval for one change is not open-ended permission
   for later changes.
4. **Verify:** have the assistant read the saved result through the database,
   MCP, or application command interface and show what is now stored.

Granting access to a directory, database, command, or MCP server only makes the
capability available. It is not approval to make every possible change. Keep
tool permissions as narrow as practical and approve writes only after reviewing
the complete proposal.

## Continue with an action guide

- Use the [OKF layer guide](help/okf-layer.md) for installed paths, direct
  SQLite workflows, reusable prompts, and the detailed approval process.
- Use the [MCP server guide](help/mcp-server.md) to configure a compatible MCP
  client and work through structured OKF-Todo tools.
- Use the [application command interface](okf/todo-database/references/application-command-interface.md)
  for the supported CLI command contract.
- Use [OKF-Todo day to day](help/using-okf-todo.md) when you want to work only
  through the desktop application.
