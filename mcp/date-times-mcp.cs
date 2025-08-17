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

public record CurrentDateTimeResult(string FormattedDateTime);
public record DateDifferenceResult(int Days, int Hours, int Minutes);
public record AddTimeResult(DateTime ResultDate);

[McpServerToolType] 
public static class DateTimeTools
{
    [McpServerTool, Description("Gets current date and time in various formats")]
    public static CurrentDateTimeResult GetCurrentDateTime(
        [Description("Date format string (default: yyyy-MM-dd HH:mm:ss)")] string format = "yyyy-MM-dd HH:mm:ss")
    {
        return new CurrentDateTimeResult(DateTime.Now.ToString(format));
    }

    [McpServerTool, Description("Calculates the difference between two dates")]
    public static DateDifferenceResult DateDifference(
        [Description("Start date")] DateTime startDate, 
        [Description("End date")] DateTime endDate)
    {
        var diff = endDate - startDate;
        return new DateDifferenceResult(diff.Days, diff.Hours, diff.Minutes);
    }

    [McpServerTool, Description("Adds or subtracts time from a date")]
    public static AddTimeResult AddTime(
        [Description("Base date")] DateTime date, 
        [Description("Days to add")] int days = 0, 
        [Description("Hours to add")] int hours = 0, 
        [Description("Minutes to add")] int minutes = 0)
    {
        return new AddTimeResult(date.AddDays(days).AddHours(hours).AddMinutes(minutes));
    }
}
