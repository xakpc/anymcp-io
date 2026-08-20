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
- `.mcp.json` - The local client configuration file. It tells the LLM client how to start each server.
- Harness - The test code in `test/`, driven by `scripts/run-tests.js`. See
  [automated testing](catalog/automated-testing.md).
- Layer - One stage of the harness: `lint`, `build`, or `protocol`. They run in that order.
- Stray stdout - A line that a server writes to standard output that is not a JSON-RPC message. One
  such line breaks the protocol stream, so the protocol layer fails on it.
- `llms.txt` - The file at the site root that gives a coding agent the index of the catalog. The
  llmstxt.org convention defines the shape: one H1, a blockquote, and H2 sections of links.
- Agent endpoint - One of the machine-readable outputs of the site: `/llms.txt`, `/install.md`,
  `/servers.json`, `/servers/{id}/index.md`, or `/servers/{id}/{id}.cs`. See
  [agent endpoints](site/agent-endpoints.md).
- Raw source URL - `/servers/{id}/{id}.cs`. It holds the catalog file, byte for byte, so an agent
  downloads it and does not read the HTML page.
- Agent prompt - The one line that the **Copy Agent Prompt** button puts on the clipboard. It names
  the `.md` page of a server, and a coding agent reads that page and installs the server.
- Project install - The default. The server file goes in `.mcp-servers/` in the project of the user,
  and the entry goes in the `.mcp.json` of that project. The path is relative, and both files go into
  version control.
- User install - The alternative, with `--scope user`. The server is available in every project of
  one person, and the path must be absolute. Use it only when the user asks for it.
- Runfile cache - The directory where the SDK keeps the build output of a file-based app, under
  `%TEMP%/dotnet/runfile/`. A `.cs` file therefore makes no `bin` or `obj` directory beside it.
- `_headers` - The Cloudflare Pages file that sets the content type of the `.md` and `.cs` outputs.
- Wire name - The tool name that a client sees. The SDK makes it from the C# method name in snake
  case, so `GeneratePassword` becomes `generate_password`. The catalog page shows the C# name.
