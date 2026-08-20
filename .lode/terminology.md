# Terminology

- MCP - Model Context Protocol. The protocol that lets an LLM client call external tools.
- MCP server - A program that gives tools to an LLM client. In this project it is one C# file.
- Single-file server - An MCP server that is fully contained in one `.cs` file. It needs no project file.
- Front matter - The YAML block at the top of a `.cs` file. Each line starts with `//`. The block
  starts and ends with a `// ---` line. See [front-matter schema](catalog/front-matter-schema.md).
- Server id - The unique key of a catalog entry. It sets the page URL `/servers/{id}/`.
- `servers` - The 11ty global data object. Keys are server ids. Values are parsed server objects.
- `serversArray` - The same data as an array. 11ty pagination needs an array.
- `code` - The full text of a `.cs` file, front matter included.
- `displayCode` - The text of a `.cs` file after the parser removes the front matter. The site shows
  and copies this text.
- Tool - One C# method with the `[McpServerTool]` attribute. The parser finds tools automatically.
- 11ty (Eleventy) - The static site generator that builds the site.
- Nunjucks (`.njk`) - The template language of all site pages and components.
- Stdio transport - The MCP transport that this project uses. The client starts the server process,
  and speaks to it through standard input and standard output.
- `#:package` - A .NET 10 file-level directive. It declares a NuGet package for a single-file program.
- `#:property` - A .NET 10 file-level directive. It sets an MSBuild property, for example `PublishAot`.
- `.mcp.json` - The project configuration file of Claude Code, Claude Desktop, Cursor, and Windsurf.
  It uses the key `mcpServers`. Codex does not read it.
- `.codex/config.toml` - The project configuration file of Codex. It uses the key `mcp_servers`, with
  an underscore. Claude Code does not read it. See [MCP clients](site/mcp-clients.md).
- Trust entry - The lines in the personal `~/.codex/config.toml` of a user that mark one directory as
  trusted. Codex loads a project `.codex/config.toml` only from a trusted project, and gives no error
  message when the entry is absent. The entry is not committed.
- `env_vars` - The Codex key that names an environment variable to send to a server. Codex replaces
  no `${...}`, so this is the way a committed Codex configuration holds a name and no secret.
- Client page - One of `/install/claude-code.md` or `/install/codex.md`. It holds the last install
  step for one client. `/install.md` holds the steps that are the same for every client.
- Harness - The test code in `test/`, driven by `scripts/run-tests.js`. See
  [automated testing](catalog/automated-testing.md).
- Layer - One stage of the harness: `lint`, `build`, or `protocol`. They run in that order.
- Stray stdout - A line that a server writes to standard output that is not a JSON-RPC message. One
  such line breaks the protocol stream, so the protocol layer fails on it.
- `llms.txt` - The file at the site root that gives a coding agent the index of the catalog. The
  llmstxt.org convention defines the shape: one H1, a blockquote, and H2 sections of links.
- Agent endpoint - One of the machine-readable outputs of the site: `/llms.txt`, `/install.md`,
  `/install/claude-code.md`, `/install/codex.md`, `/servers.json`, `/servers/{id}/index.md`, or
  `/servers/{id}/{id}.cs`. See [agent endpoints](site/agent-endpoints.md).
- Raw source URL - `/servers/{id}/{id}.cs`. It holds the catalog file, byte for byte, so an agent
  downloads it and does not read the HTML page.
- Agent prompt - The one line that the **Copy Agent Prompt** button puts on the clipboard. It names
  the `.md` page of a server, and a coding agent reads that page and installs the server.
- Project install - The default. The server file goes in `.mcp-servers/` in the project of the user,
  and the entry goes in the configuration file of the client in that same project. The path is
  relative, and both files go into version control.
- User install - The alternative, with `claude mcp add --scope user` or with `codex mcp add`. The
  server is available in every project of one person, and the path must be absolute. Use it only when
  the user asks for it.
- Runfile cache - The directory where the SDK keeps the build output of a file-based app, under
  `%TEMP%/dotnet/runfile/`. A `.cs` file therefore makes no `bin` or `obj` directory beside it.
- `_headers` - The Cloudflare Pages file that sets the content type of the `.md` and `.cs` outputs.
- Wire name - The tool name that a client sees. The SDK makes it from the C# method name in snake
  case, so `GeneratePassword` becomes `generate_password`. The catalog page shows the C# name.
