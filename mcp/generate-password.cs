// ---
// id: generate-password
// description: Generates secure passwords with customizable length and complexity.
// tags:
//     - password
//     - security
//     - generator
// version: 1.1.0
// author: XAKPC Dev Labs
// license: MIT
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
public static class TextTools
{
    [McpServerTool, Description("Generates a password with specified length and complexity")]
    public static string GeneratePassword(int length = 12, bool includeSymbols = true, bool includeNumbers = true)
    {
        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var chars = letters;
        if (includeNumbers) chars += numbers;
        if (includeSymbols) chars += symbols;

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}