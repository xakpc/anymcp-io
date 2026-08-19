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
