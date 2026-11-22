# Claude Code setup for OKF Todo

How to set up Claude Code, with its skills and MCP servers, for work on this
repository on Windows. This guide is for maintainers. To connect an AI assistant
to OKF Todo itself, see [AI assistants](ai-assistants.md) and the
[MCP server](help/mcp-server.md) guide.

## 1. Where things live

| What | Location |
| --- | --- |
| Repository instructions | `AGENTS.md` at the repository root |
| Repository skills | `.agents/skills/<skill-name>/SKILL.md` |
| Personal skills | `%USERPROFILE%\.claude\skills\<skill-name>\SKILL.md` |
| User-scope MCP servers | `mcpServers` in `%USERPROFILE%\.claude.json` |
| Project-scope MCP servers | `.mcp.json` at the repository root (none yet) |

## 2. Repository instructions and skills

The repository keeps its agent instructions in `AGENTS.md` and its skills in
`.agents/skills`, which is where Codex reads them. Claude Code looks for
`CLAUDE.md` and `.claude/skills` instead. Both are committed, so a fresh clone
works with no setup:

- `CLAUDE.md` contains the single line `@AGENTS.md`, which makes Claude Code
  load `AGENTS.md`.
- `.claude/skills` is a committed copy of `.agents/skills`. It uses no
  symbolic links, so it works on every clone, zip download and operating
  system.

`.agents/skills` is the source of truth. Never edit `.claude/skills` directly.
After every change under `.agents/skills`, update the copy from the repository
root:

```powershell
.\tools\sync-claude-skills.ps1
```

To check the copy without changing anything:

```powershell
.\tools\sync-claude-skills.ps1 -Check
```

Two checks catch a forgotten sync:

- The GitHub Actions workflow `Claude skills sync`
  (`.github/workflows/claude-skills-sync.yml`) runs the `-Check` on every push
  and pull request that touches either skills folder or the script.
- The test `ClaudeSkillsCopyTests` in `Okf-Todo.Tests` fails when the copy is
  out of date, so local test runs catch it too.

The repository skills are:

| Skill | Use it for |
| --- | --- |
| `compile-okf-context` | Regenerating the OKF database bundle after a database design change |
| `photino-diagnostics` | Build, startup, asset or messaging failures in the Photino app |
| `photino-feature` | Adding or changing a desktop feature |
| `photino-packaging` | Publishing and packaging release output |

## 3. Personal skills used with this repository

These skills live in the maintainer's `%USERPROFILE%\.claude\skills` folder, not
in the repository. To move them to another machine, copy the whole folder:

```powershell
Copy-Item -Recurse <backup>\skills "$env:USERPROFILE\.claude\skills"
```

| Skill | What it does | Needs |
| --- | --- | --- |
| `dalby-writing` | Writes and proofreads Help, README files and release notes in the house style | Nothing |
| `domain-modeling` | Keeps the glossary and ADRs in `docs` up to date | Nothing |
| `grilling` | Stress-tests a plan or design | Nothing |
| `grill-with-docs` | Grills you and writes ADRs and glossary entries as it goes. It only runs when you type `/grill-with-docs` | Nothing |
| `excalidraw-skill` | Draws diagrams on a live Excalidraw canvas | Node.js and the `excalidraw` MCP server (section 4) |
| `civica-referat` | Board minutes PDF. Not related to this repository | Python 3 with `reportlab` |

## 4. MCP servers

### excalidraw (user scope)

Add the server:

```powershell
claude mcp add excalidraw -s user -e EXPRESS_SERVER_URL=http://127.0.0.1:3000 -- cmd /c npx -y mcp-excalidraw-server
```

The server only talks to a canvas server, which you start yourself before
drawing:

```powershell
git clone https://github.com/yctimlin/mcp_excalidraw; cd mcp_excalidraw; npm ci; npm run build
```

```powershell
$env:PORT=3000; npm run canvas
```

Then open http://127.0.0.1:3000 in a browser.

### OKF Todo's own MCP server

OKF Todo ships its own MCP server for reading and saving tasks. Setup and the
full tool list are in [MCP server](help/mcp-server.md).

### Connectors from the claude.ai account

These come from claude.ai through your login, not from a local config file.
Turn them on or off in claude.ai → Settings → Connectors.

- **Claude Docs**: creates and edits shared docs.
- **Library docs lookup** (`resolve-library-id`, `query-docs`): current
  documentation for libraries and frameworks such as Photino.NET and EF Core.

### Built into the Claude desktop app

You don't need to install any of these. Installing the desktop app is enough.

- **Claude_Browser**: the browser pane, which is useful for checking the UI.
- **terminal**: the Terminal panel.
- **visualize**: inline diagrams and widgets.
- **scheduled-tasks**: local scheduled tasks.

### Needs a separate install or switch

- **claude-in-chrome**: install the Claude in Chrome extension and sign in
  with the same account.
- **computer-use**: turn it on in the desktop app's settings. You need it for
  [interactive checks](testing/interactive-repository-check.md) of the native
  window.

## 5. Check the setup

```powershell
claude mcp list
```

Then start a session in the repository and ask Claude to list its active skills
and MCP servers. You should see the four repository skills in that list.
