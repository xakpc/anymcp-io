# MCP Server Authoring

Follow these steps to add a server to the catalog.

1. Make a file `mcp/my-server.cs`.
2. Add the front matter. See [front matter schema](front-matter-schema.md).
3. Write the server code below the front matter.
4. Run `npm test -- --server my-server`. The harness finds the new file by itself, so no
   test file, workflow, or configuration needs a change. See
   [automated testing](automated-testing.md).
5. Run `npm run serve`, and open http://localhost:8080/servers/my-server/.
6. Test the server with a real LLM client through `.mcp.json`.
7. Send a pull request. The same three test layers run in GitHub Actions.

## The standard skeleton

```csharp
// ---
// id: my-server
// name: my-server.cs
// description: One short line about the server
// tags:
//   - utilities
// version: 1.0.0
// author: Your Name
// license: MIT
// ---
#:package Microsoft.Extensions.Hosting@10.0.11
#:package ModelContextProtocol@2.2.0
#:property PublishAot=false
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Send all logs to stderr. Stdout carries the protocol.
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class MyTools
{
    [McpServerTool, Description("Say what the tool does. The LLM reads this text.")]
    public static string DoSomething(
        [Description("Say what this parameter is.")] string input)
    {
        return input.ToUpperInvariant();
    }
}
```

## Rules that the build enforces

- Put the attribute and its `Description` on one line. The parser reads the tool list from that line.
- Make each tool method `public static`. The parser skips other methods.
- Keep the method signature within 10 lines below the attribute.
- Put the tool class after the top-level statements. C# requires this order.

## Rules that the protocol enforces

- Set `#:property PublishAot=false`. `WithToolsFromAssembly()` uses reflection, and AOT removes the
  metadata that reflection needs. Without this line the server starts, but shows no tools.
- Send every log line to stderr. A log line on stdout breaks the protocol stream.
- Read secrets from environment variables. Do not put a key in the file. List each variable in
  `envVars`, so the page shows the correct `.mcp.json` snippet.

## Local test configuration

`.mcp.json` in the repository root tells the LLM client how to start each server:

```json
{
  "mcpServers": {
    "my-server": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", ".\\mcp\\my-server.cs", "-v", "q"]
    }
  }
}
```

The `-v q` argument makes `dotnet run` quiet. Without it, the build output can reach stdout and disturb
the client. It is two array elements, `"-v"` and `"q"`, and not one element `"-v q"`. In JSON each
backslash of a Windows path is doubled.

`.mcp.json` is not in git, and it does not list every server. Add your entry by hand.

The site publishes the same instructions for a user, at `/install.md` and `/servers/{id}/index.md`.
Keep the two the same. Those pages install a server **into a project**: the file goes in
`.mcp-servers/` in the project of the user, and `args` holds the relative path
`./.mcp-servers/{id}.cs`. This repository is such a project, so its own `.mcp.json` has the same
shape. See [agent endpoints](../site/agent-endpoints.md).

`dotnet build` and `dotnet run` add no `bin` or `obj` directory next to a `.cs` file. A file-based
app is not a project, so the SDK keeps the output in a cache of the user, under
`%TEMP%/dotnet/runfile/`. This is why `.gitignore` needs no entry for build output.

## Package versions in the catalog now

```csharp
#:package Microsoft.Extensions.Hosting@10.0.11
#:package ModelContextProtocol@2.2.0
```

Use these same versions for a new server, or the catalog becomes inconsistent.

## Test a server without an LLM client

`npm run test:protocol -- --server my-server` does this automatically, and is the normal way. See
[automated testing](automated-testing.md).

The manual recipe below stays useful to debug one server by hand, for example to see the raw bytes, or
to call a tool with your own arguments. Send three JSON-RPC lines to the server on standard input.
Each line is one message:

```bash
printf '%s\n%s\n%s\n' \
 '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}' \
 '{"jsonrpc":"2.0","method":"notifications/initialized"}' \
 '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' > in.jsonl

{ cat in.jsonl; sleep 6; } | dotnet run mcp/my-server.cs 2>/dev/null
```

To call one tool, add a fourth line:

```json
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"my_tool","arguments":{"a":1}}}
```

The `sleep` is necessary. When standard input reaches end of file, the host shuts down at once, and the
replies can be lost before the process writes them. Without the `sleep` the output is empty, and the
server looks broken when it is correct. Read stderr to see the true cause of a failed tool call: the
reply on stdout only says `An error occurred invoking '<tool>'`, but stderr holds the full exception.

The tool name on the wire is not the C# method name. The SDK converts `GeneratePassword` to
`generate_password`. The catalog page shows the C# name.

Related: [Server inventory](server-inventory.md), [Automated testing](automated-testing.md),
[Practices](../practices.md).
