// ---
// id: date-times-mcp
// description: Provides date and time utilities including current date/time, date calculations, and formatting
// tags:
//   - datetime
//   - utilities
//   - formatting
//   - calculations
// status: stable
// version: 1.0.0
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
public static class DateTimeTools
{
    [McpServerTool, Description("Gets current date and time in various formats")]
    public static string GetCurrentDateTime(string format = "yyyy-MM-dd HH:mm:ss")
    {
        return DateTime.Now.ToString(format);
    }

    [McpServerTool, Description("Calculates the difference between two dates")]
    public static (int days, int hours, int minutes) DateDifference(DateTime startDate, DateTime endDate)
    {
        var diff = endDate - startDate;
        return (diff.Days, diff.Hours, diff.Minutes);
    }

    [McpServerTool, Description("Adds or subtracts time from a date")]
    public static DateTime AddTime(DateTime date, int days = 0, int hours = 0, int minutes = 0)
    {
        return date.AddDays(days).AddHours(hours).AddMinutes(minutes);
    }
}
