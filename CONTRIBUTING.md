# Contributing to AnyMCP

Thank you for contributing to the AnyMCP catalog! This guide will help you add your MCP server to our collection.

## Quick Start

1. **Fork the repository** and clone it locally
2. **Create a new `.cs` file** in the `mcp/` directory
3. **Add YAML front matter** in C# comments at the top
4. **Write your MCP server code** below the front matter
5. **Test locally** by running `npm run serve`
6. **Submit a pull request**

## Adding Your MCP Server

### 1. File Location

Place your MCP server file in the `mcp/` directory at the root of the repository:

```
mcp/
├── your-server-name.cs
└── ...
```

### 2. File Structure

Your `.cs` file must include YAML front matter in C# comments, followed by your server code:

```csharp
// ---
// id: your-server-id
// name: your-server-name.cs
// description: Brief description of what your server does
// tags:
//     - category
//     - integration-type
//     - technology
// status: stable
// version: 1.0.0
// author: Your Name
// license: MIT
// envVars:
//     - API_KEY
//     - BASE_URL
// ---
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register the MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Build and run the MCP Server Application
await builder.Build().RunAsync();

//====== TOOLS ======

[McpServerToolType]
public static class Tools
{
    [McpServerTool, Description("Tool Description")]
    public static string Tool(...)
    {
       ...
    }
}
```

### 3. Front Matter Fields

#### Required Fields

- **`id`**: Unique identifier for your server (kebab-case, used in URLs)
- **`name`**: Display name, typically the filename
- **`description`**: Brief description (1-2 sentences)
- **`author`**: Your name or organization

#### Optional Fields

- **`tags`**: Array of categories/tags for filtering (see common tags below)
- **`status`**: `stable`, `beta`, or `alpha` (defaults to `stable`)
- **`license`**: License type (defaults to `MIT`)
- **`envVars`**: Array of required environment variables

### 4. Common Tags

Use these standardized tags for consistency:

**Categories:**
- `ai` - AI/ML integrations
- `api` - REST API interactions
- `database` - Database connections
- `file-system` - File operations
- `productivity` - Productivity tools
- `security` - Security-related tools
- `utility` - General utilities
- `web` - Web scraping/browsing

**Technologies:**
- `github` - GitHub integration
- `jira` - Jira integration
- `openai` - OpenAI API
- `azure` - Microsoft Azure
- `aws` - Amazon Web Services
- `docker` - Docker-related
- `kubernetes` - Kubernetes tools

### 5. Code Requirements

#### Dependencies
- Target **.NET 10 Preview 4** or compatible
- Use **ModelContextProtocol** package version 0.3.0-preview.3 or later
- Include package references with `#:package` directives

#### Best Practices
- **Single file**: Keep everything in one `.cs` file
- **Clear documentation**: Add XML comments for public methods
- **Error handling**: Include proper error handling and logging
- **Environment variables**: Use environment variables for configuration
- **Resource disposal**: Properly dispose of resources

#### Example Implementation Structure

```csharp
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register the MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Build and run the MCP Server Application
await builder.Build().RunAsync();

//====== TOOLS ======

[McpServerToolType]
public static class SomeTools
{
    [McpServerTool, Description("The tool that do someting")]
    public static string SomeTool(...)
    {
       ...
    }
}
```

## Submission Guidelines

### Pull Request Process

1. **Fork** the repository to your GitHub account
2. **Create a branch** for your MCP server: `git checkout -b add-your-server-name`
3. **Add your server file** to the `mcp/` directory
4. **Test locally** to ensure it works properly
5. **Commit your changes**: `git commit -m "Add [Your Server Name] MCP server"`
6. **Push to your fork**: `git push origin add-your-server-name`
7. **Create a Pull Request** with a clear description

### Pull Request Description

Include in your PR description:

- **Server name and purpose**
- **Key features and capabilities**
- **Requirements**

### Example PR Title and Description

**Title:** Add Weather API MCP Server

**Description:**
```
## Summary
Adds a new MCP server for fetching weather data from OpenWeatherMap API.

## Features
- Current weather conditions
- 5-day forecast
- Location search by city name or coordinates
- Temperature unit conversion

## Requirements
- OpenWeatherMap API key (OPENWEATHER_API_KEY)
- .NET 10 Preview 4
```

## Code of Conduct

- **Be respectful** in all interactions
- **Provide useful, working code** that adds value to the catalog
- **Follow the established patterns** and conventions
- **Test your contributions** before submitting
- **Respond to feedback** constructively

## Getting Help

- **Issues**: Open an issue for questions or problems
- **Discussions**: Use GitHub Discussions for general questions
- **Examples**: Look at existing servers in the `mcp/` directory for reference

## License

By contributing to AnyMCP, you agree that your contributions will be licensed under the same license as the project (MIT).
