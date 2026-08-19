# MCP Server Authoring

Follow these steps to add a server to the catalog.

1. Make a file `mcp/my-server.cs`.
2. Add the front matter. See [front matter schema](front-matter-schema.md).
3. Write the server code below the front matter.
4. Run `npm run serve`, and open http://localhost:8080/servers/my-server/.
5. Test the server with a real LLM client through `.mcp.json`.
6. Send a pull request.

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
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3
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
      "args": ["run", ".\mcp\my-server.cs", "-v q"]
    }
  }
}
```

The `-v q` argument makes `dotnet run` quiet. Without it, the build output can reach stdout and disturb
the client. `.mcp.json` is not in git, and it does not list every server. Add your entry by hand.

## Package versions in the catalog now

```csharp
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3
```

Use these same versions for a new server, or the catalog becomes inconsistent.

Related: [Server inventory](server-inventory.md), [Practices](../practices.md).
