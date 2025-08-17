// ---
// id: generate-password
// description: Generates secure passwords with customizable length and complexity.
// tags:
//     - password
//     - security
//     - utilities
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

public record GeneratePasswordResult(string Password);

[McpServerToolType]
public static class PasswordTools
{
    [McpServerTool, Description("Generates a password with specified length and complexity")]
    public static GeneratePasswordResult GeneratePassword(
        [Description("Password length (default: 12)")] int length = 12, 
        [Description("Include symbols (default: true)")] bool includeSymbols = true, 
        [Description("Include numbers (default: true)")] bool includeNumbers = true)
    {
        if (length < 4 || length > 256)
            throw new ArgumentException("Password length must be between 4 and 256 characters");
            
        const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";
        const string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var chars = letters;
        if (includeNumbers) chars += numbers;
        if (includeSymbols) chars += symbols;

        var password = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
            
        return new GeneratePasswordResult(password);
    }
}