# AnyMCP - MCP Server Catalog

A static site catalog of single-file MCP (Model Context Protocol) servers for .NET 10. These servers written with C# and works with any LLM that supports local MCP connections.

## Using a server

Every server is one file. It installs into a project, not into your machine. From the root of the
project:

```bash
mkdir -p .mcp-servers
curl -fsSL -o .mcp-servers/date-times-mcp.cs https://anymcp.net/servers/date-times-mcp/date-times-mcp.cs
dotnet build .mcp-servers/date-times-mcp.cs -v q
```

The server file is shared by every MCP client. The configuration file is not — pick yours:

**Claude Code** (also Claude Desktop, Cursor, Windsurf) reads `.mcp.json`:

```bash
claude mcp add date-times-mcp --scope project -- dotnet run ./.mcp-servers/date-times-mcp.cs -v q
```

**Codex** reads `.codex/config.toml`:

```toml
[mcp_servers.date-times-mcp]
command = "dotnet"
args = [
    "run",
    "./.mcp-servers/date-times-mcp.cs",
    "-v",
    "q",
]
startup_timeout_sec = 60
```

Codex loads a project config only from a trusted project, so your personal `~/.codex/config.toml`
also needs `[projects.'<absolute path>']` with `trust_level = "trusted"`. On Windows use the plain
`C:\Users\...` path, not the extended `\\?\C:\...` form — Codex silently ignores the project config
when the two don't match. Don't commit that trust entry, and don't run `codex mcp add` for a project
install: it registers the server globally instead. Full steps at <https://anymcp.net/install/codex.md>.

Commit `.mcp-servers/date-times-mcp.cs` and your client's config, and everyone who clones the project
gets the server. The path is relative, so it works on every operating system.

The build adds no `bin` or `obj` directory to the project: a file-based app is not a project, so the
SDK caches the compiled output under your temp directory. `.gitignore` needs no new line.

`-v q` is required: standard output carries the JSON-RPC stream, and without it the build
output can reach that stream and break the connection.

Use `--scope user` with an absolute path instead to install a server for every project.

The full procedure, including the .NET 10 SDK prerequisite and the configuration shape for
each client, is at <https://anymcp.net/install.md>. Every server also has its own page:

| Resource | URL |
|---|---|
| Catalog index for agents | <https://anymcp.net/llms.txt> |
| Catalog as JSON | <https://anymcp.net/servers.json> |
| Claude Code setup | <https://anymcp.net/install/claude-code.md> |
| Codex setup | <https://anymcp.net/install/codex.md> |
| One server, for an agent | `https://anymcp.net/servers/<id>/index.md` |
| One server, raw C# | `https://anymcp.net/servers/<id>/<id>.cs` |

## Contributing

Want to add your MCP server to the catalog? See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed instructions.


## Adding New Servers

Create a `.cs` file in the `mcp/` directory with YAML front matter in comments:

```csharp
// ---
// id: my-server
// name: my-server.cs
// description: What this server does
// tags:
//     - category
//     - integration
// version: 1.0.0
// author: Your Name
// license: MIT
// envVars:
//     - API_KEY
//     - BASE_URL
// ---
#:package Microsoft.Extensions.Hosting@10.0.11
#:package ModelContextProtocol@2.2.0
#:property PublishAot=false

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

// Your MCP server code here...
```

The site automatically discovers C# files and generates individual pages for each server.

## Catalogue Features

- **Static Site Generation**: Built with [Eleventy (11ty)](https://www.11ty.dev/)
- **Modern Design**: Responsive design with Tailwind CSS
- **Server Catalog**: Browse MCP servers with search and filtering
- **Individual Server Pages**: Detailed pages with code, documentation, and setup instructions
- **Setup Guide**: Step-by-step instructions for getting started

## Catalogue Development

```bash
# Install dependencies
npm install

# Start development server
npm run serve

# Build for production
npm run build
```

The site will be available at `http://localhost:8080/`

## Project Structure

```
mcp/                      # MCP servers. One .cs file is one catalog entry.
src/
├── _data/                # Data files and parsers
│   ├── servers.js        # Parses the front matter and the tools out of mcp/*.cs
│   ├── serversArray.js   # The same data as an array, for pagination
│   └── site.json         # Site metadata
├── _includes/            # Layouts and components
│   ├── base.njk          # HTML shell, Tailwind theme, alternate links
│   ├── page.njk          # Header and navigation
│   ├── footer.njk
│   ├── copy-functionality.njk  # Copy, download, and agent-prompt buttons
│   └── copy-text.njk     # Copies the code block a button belongs to
├── index.njk             # Home page
├── servers.njk           # Server detail pages (paginated)
├── setup.njk             # Setup guide
├── server-md.njk         # /servers/{id}/index.md, for agents
├── server-raw.njk        # /servers/{id}/{id}.cs, the raw source
├── servers-json.njk      # /servers.json
├── agent-install.njk     # /install.md, the steps every client shares
├── install-claude-code.njk  # /install/claude-code.md
├── install-codex.njk     # /install/codex.md
├── llms.njk              # /llms.txt
├── robots.njk            # /robots.txt
├── sitemap.njk           # /sitemap.xml
└── _headers              # Cloudflare Pages content types
test/                     # lint, site, build, and protocol layers
scripts/run-tests.js      # The only test entry point
```
