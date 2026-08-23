# AI Assistants and OKF-Todo

An AI assistant is an AI chat application that can also use tools on your
computer. Instead of only answering a question, it may be able to open files,
search folders, run programs, and save results after you approve them.

Codex, Claude Code, Gemini CLI, GitHub Copilot CLI, and OpenCode are examples.
You do not need to use one of these specific products. Any AI assistant can help
if it has the access needed for the work you want it to do.

## What can an AI assistant do for you?

Imagine that a customer sends you a long email about a production problem. You
can give the email to an AI assistant and ask it to:

- summarize what happened;
- separate confirmed facts from assumptions;
- prepare an OKF-Todo task;
- suggest an investigation plan;
- draft a reply to the customer;
- show everything to you for review;
- save the approved task in OKF-Todo;
- read the saved task back and confirm what was stored.

The same approach works with support transcripts, meeting notes, diagnostic
logs, deployment output, ideas, and handover notes. The assistant does the
reading and drafting. You decide what is correct and what should be saved.

## What does the assistant need?

An assistant that can only chat can still write a useful summary or task draft.
You must then copy the result into OKF-Todo yourself.

An assistant becomes much more useful when it can use tools on your computer.
For OKF-Todo, useful tools include:

- a file-reading tool for the installed OKF instructions;
- a SQLite tool that can read and write the OKF-Todo database;
- an MCP client that can connect to the OKF-Todo MCP server;
- a terminal tool that can run OKF-Todo commands.

The assistant only needs one way to save work. It does not need all of these
tools.

## Direct SQLite access can be a major advantage

OKF-Todo keeps its data in one local SQLite database. If your AI assistant can
work with SQLite, it can read your real task data instead of relying on text that
you copy into the conversation.

With approved SQLite access, the assistant can:

- search existing tasks before proposing a duplicate;
- use the actual task types, priorities, lists, and other values in your
  database;
- create or update tasks without manual copying;
- make several related changes together;
- read the records back immediately and show you exactly what was saved.

This is especially useful when turning a large amount of source material into
several related tasks or when the assistant needs to understand existing work
before suggesting a change.

SQLite access must come from a real SQLite-capable tool. The database is not a
text document and must not be edited with an ordinary file editor. The assistant
should first read the installed OKF files, which explain the database structure
and OKF-Todo's rules.

Direct SQLite writes do not pass through the OKF-Todo application services. The
assistant must therefore follow the documented database rules, use a
transaction, preserve relationships, and add required Timeline history. If you
prefer a more controlled interface, use the MCP server instead.

## SQLite, MCP, CLI, or chat only?

There are four practical ways to use an AI assistant with OKF-Todo:

### Direct SQLite

Choose this when the assistant has a trustworthy SQLite tool and you want the
most direct and flexible access to your local data. Give it access to both the
installed OKF files and the database. Always require a proposal before a write.

### MCP server

Choose this when the assistant supports MCP. MCP gives the assistant named
OKF-Todo tools for finding, reading, creating, and updating tasks. These tools
use the application's rules and automatically handle supported Timeline
history. The assistant does not need direct database access.

### OKF-Todo commands

Choose this when the assistant can run terminal commands. The assistant can use
OKF-Todo's command interface and read the structured result returned by the
application.

### Chat only

Choose this when the assistant cannot use local tools. It can still analyze your
material and prepare drafts, but you copy the approved result into OKF-Todo
yourself.

## Does the AI have to run locally?

No. The application containing the assistant may run on your computer while the
AI model runs on the provider's servers. In that case, the assistant can use
local tools even though the model itself is not local.

Your OKF-Todo database remains on your computer. However, text or files you give
to the assistant may be sent to its model provider. Check the provider's privacy
and data controls before sharing customer emails, logs, or other sensitive
material.

## Examples of AI assistants

These are examples, not endorsements or a complete list. Products change, so
follow their current official documentation when enabling file, terminal, or
MCP access.

- [Codex](https://developers.openai.com/codex) can work with files, run approved
  tools and commands, and use MCP servers.
- [Claude Code](https://code.claude.com/docs/en/cli-usage) provides file and
  terminal tools and can connect to MCP servers.
- [Gemini CLI](https://github.com/google-gemini/gemini-cli/blob/main/docs/tools/mcp-server.md)
  works with local files and tools and supports MCP servers.
- [GitHub Copilot CLI](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference)
  provides permission-controlled file and command access and supports
  [local MCP servers](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-mcp-servers).
- [OpenCode](https://opencode.ai/docs/tools) provides file and terminal tools
  with configurable permissions and supports
  [local MCP servers](https://opencode.ai/v2/docs/mcp-servers).

When considering another product, ask three simple questions:

1. Can it read files from locations I approve?
2. Can it use SQLite, MCP, or local commands?
3. Can it ask before making changes and show me the saved result afterward?

If the answer is yes, it can probably work with OKF-Todo.

## A safe first example

Start by asking the assistant to prepare a draft without saving anything:

```text
Read the customer email below and prepare an OKF-Todo task.
Separate confirmed facts from assumptions and include an investigation plan.
Do not save anything yet. Show me the complete proposed task first.

[paste the customer email here]
```

Review the title, task body, priority, tags, and proposed actions. Correct
anything that is wrong or unclear. When the proposal is ready, approve that
specific change:

```text
I approve this proposed task. Save exactly this version in OKF-Todo, then read
the saved task back and show me what was stored.
```

This creates a simple four-step habit:

1. **Draft:** the assistant prepares a proposal without changing OKF-Todo.
2. **Review:** you correct facts, assumptions, and task details.
3. **Save:** you explicitly approve the exact change.
4. **Verify:** the assistant reads the saved result back.

Giving an assistant access to a file, database, command, or MCP server does not
mean that every change is approved. Access makes the tool available. Your
approval decides when and how it may be used.

## Next steps

- Follow the [OKF layer guide](help/okf-layer.md) when you want the assistant to
  read the installed OKF instructions and work directly with SQLite.
- Follow the [MCP server guide](help/mcp-server.md) when you want the assistant
  to use structured OKF-Todo tools.
- Read the [application command interface](okf/todo-database/references/application-command-interface.md)
  when you want the assistant to run OKF-Todo commands.
- Read [Using OKF-Todo day to day](help/using-okf-todo.md) when you want to use
  the desktop application without an AI assistant.
