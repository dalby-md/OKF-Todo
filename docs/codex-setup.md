# Codex setup for OKF Todo

How to set up Codex, with its skills and MCP servers, for work on this repository
on Windows. This guide is for maintainers. To connect an AI assistant to OKF Todo
itself, see [AI assistants](ai-assistants.md) and the
[MCP server](help/mcp-server.md) guide.

## 1. Where things live

| What | Location |
| --- | --- |
| Repository instructions | `AGENTS.md` at the repository root |
| Repository skills | `.agents/skills/<skill-name>/SKILL.md` |
| Personal instructions | `%USERPROFILE%\.codex\AGENTS.md` |
| Personal skills | `%USERPROFILE%\.agents\skills` (current location) or `%USERPROFILE%\.codex\skills` (older location, still read) |
| MCP servers, plugins and trusted projects | `%USERPROFILE%\.codex\config.toml` |

## 2. Repository instructions and skills

Codex reads `AGENTS.md` and `.agents/skills` from the repository without any
setup. The repository skills are:

| Skill | Use it for |
| --- | --- |
| `compile-okf-context` | Regenerating the OKF database bundle after a database design change |
| `photino-diagnostics` | Build, startup, asset or messaging failures in the Photino app |
| `photino-feature` | Adding or changing a desktop feature |
| `photino-packaging` | Publishing and packaging release output |

Mark the repository as trusted, so that Codex loads its project settings. Add
this to `config.toml`, or accept the trust prompt the first time you open the
folder:

```toml
[projects.'C:\git\OKF-Todo']
trust_level = "trusted"
```

## 3. Personal skills

These skills live in the maintainer's profile, not in the repository. To move
them to another machine, copy both skill folders:

```powershell
Copy-Item -Recurse <backup>\.codex\skills "$env:USERPROFILE\.codex\skills"
```

```powershell
Copy-Item -Recurse <backup>\.agents\skills "$env:USERPROFILE\.agents\skills"
```

Codex calls `.codex\skills` the deprecated location. It still reads skills
from it, but put new skills in `.agents\skills`.

In `%USERPROFILE%\.codex\skills`:

| Skill | What it does | Needs |
| --- | --- | --- |
| `dalby-writing` | Writes and proofreads Help, README files and release notes in the house style | Nothing |
| `domain-modeling` | Keeps the glossary and ADRs in `docs` up to date | Nothing |
| `grilling` | Stress-tests a plan or design | Nothing |
| `grill-with-docs` | Grills you and writes ADRs and glossary entries as it goes. It only runs when you ask for it by name | Nothing |
| `excalidraw-skill` | Draws diagrams on a live Excalidraw canvas | Node.js and the `excalidraw` MCP server (section 4) |
| `civica-referat` | Board minutes PDF. Not related to this repository | Python 3 with `reportlab` |

In `%USERPROFILE%\.agents\skills`:

| Skill | What it does |
| --- | --- |
| `drawio-skill` | draw.io diagrams. It only runs when you ask for it by name |
| `find-skills` | Finds and installs more skills |

## 4. MCP servers

All three servers start through `npx`, so Node.js must be installed. Add them
with `codex mcp add`:

```powershell
codex mcp add excalidraw -- npx -y mcp-excalidraw-server
```

```powershell
codex mcp add context7 -- npx -y @upstash/context7-mcp
```

```powershell
codex mcp add playwright -- npx -y "@playwright/mcp@latest"
```

`codex mcp add` can't set a startup timeout. If `npx` is slow the first time
it runs, you can either add `startup_timeout_sec = 60` to each server in
`config.toml`, or write the servers there yourself. This is the configuration
in use today, which calls Node by its full path so that it works under the
Codex sandbox:

```toml
[mcp_servers.excalidraw]
command = "C:/Program Files/nodejs/node.exe"
args = ["C:/Program Files/nodejs/node_modules/npm/bin/npx-cli.js", "-y", "mcp-excalidraw-server"]
startup_timeout_sec = 60

[mcp_servers.context7]
command = "C:/Program Files/nodejs/node.exe"
args = ["C:/Program Files/nodejs/node_modules/npm/bin/npx-cli.js", "-y", "@upstash/context7-mcp"]
startup_timeout_sec = 60

[mcp_servers.playwright]
command = "C:/Program Files/nodejs/node.exe"
args = ["C:/Program Files/nodejs/node_modules/npm/bin/npx-cli.js", "-y", "@playwright/mcp@latest"]
startup_timeout_sec = 60
```

| Server | Use it for |
| --- | --- |
| `excalidraw` | Diagrams through `excalidraw-skill` |
| `context7` | Current documentation for libraries such as Photino.NET, EF Core and SQLite |
| `playwright` | Driving a browser for [UI tests](testing/ui-tests.md) and checking the UI |

### Excalidraw canvas server

The `excalidraw` MCP server only talks to a canvas server, which you start
yourself before drawing. It listens on `http://127.0.0.1:3000`, which is the
address the server expects by default.

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

### Built into the Codex desktop app

The Codex desktop app adds its own plugins and a `node_repl` MCP server
automatically: browser, Chrome, computer use, visualize, documents, PDF,
spreadsheets and presentations. Don't copy these entries from one machine's
`config.toml` to another, because their paths include the app's version
number. Installing the app sets them up, and you can turn them on or off under
`[plugins."<name>"]`.

## 5. Check the setup

```powershell
codex mcp list
```

Then start a Codex session in the repository and ask it to list its skills and
MCP servers. You should see the four repository skills in that list.
