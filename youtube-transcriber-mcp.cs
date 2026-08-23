// ---
// id: youtube-transcriber-mcp
// name: youtube-transcriber-mcp.cs
// description: Pulls YouTube transcripts for a given URL using YoutubeExplode and exposes it as an MCP tool.
// tags:
//     - productivity
// status: beta
// version: 1.0.0
// author: robalexclark
// license: MIT
// envVars:
// ---

#:package YoutubeExplode@6.5.4
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.4

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure console logging with Trace level
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register MCP server and tools (instance-based)
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Build the host
IHost host = builder.Build();

// Setup cancellation token for graceful shutdown (Ctrl+C or SIGTERM)
using CancellationTokenSource cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true; // Prevent the process from terminating immediately
    cts.Cancel();
};

try
{
    // Run the host with cancellation support
    await host.RunAsync(cts.Token);
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Unhandled exception: {ex}");
    Environment.ExitCode = 1;
}


[McpServerToolType]
public class YouTubeTranscriberTool(ILogger<YouTubeTranscriberTool> logger)
{
    private readonly ILogger<YouTubeTranscriberTool> _logger = logger;

    [McpServerTool, Description(@"Retrieves YouTube transcripts for a provided YouTube URL.")]
    public async Task<string> RetrieveYoutubeTranscript(
        [Description(@"The YouTube URL to retrieve a transcript for.")]
        string url, CancellationToken cancellationToken = default)
    {
        // Log the incoming query for debugging
        _logger.LogInformation("Received YouTube transcript retrieval request. {url}", url);

        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("Empty or null url received");
            return "Error: Url cannot be empty";
        }

        string lang = "en";
        string? output = "-o";

        try
        {
            var result = await FetchTranscriptAsync(url, lang, outputFormat: output?.EndsWith(".srt", StringComparison.OrdinalIgnoreCase) == true ? "srt" : "text");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcript retrieval failed with general error: {ErrorMessage}", ex.Message);
            return $"Error: {ex.Message}";
        }
    }

    private static async Task<string> FetchTranscriptAsync(string video, string lang, string outputFormat)
    {
        var yt = new YoutubeClient();

        var manifest = await yt.Videos.ClosedCaptions.GetManifestAsync(video);

        ClosedCaptionTrackInfo? trackInfo = null;
        try { trackInfo = manifest.GetByLanguage(lang); } catch { /* fall back below */ }
        trackInfo ??= manifest.Tracks.FirstOrDefault();

        if (trackInfo == null)
            throw new InvalidOperationException("No caption tracks found for this video.");

        var track = await yt.Videos.ClosedCaptions.GetAsync(trackInfo);

        if (string.Equals(outputFormat, "srt", StringComparison.OrdinalIgnoreCase))
        {
            return ToSrt(track);
        }
        else
        {
            return string.Join("\n",
                track.Captions
                    .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                    .Select(c => c.Text.Replace("\r", " ").Replace("\n", " ").Trim()));
        }
    }

    private static string ToSrt(ClosedCaptionTrack track)
    {
        var sb = new StringBuilder();
        int i = 1;
        foreach (var c in track.Captions)
        {
            string start = ToSrtTime(c.Offset);
            string end = ToSrtTime(c.Offset + c.Duration);
            sb.Append(i.ToString()).Append('\n');
            sb.Append(start).Append(" --> ").Append(end).Append('\n');
            sb.Append(c.Text.Replace("\r", " ").Replace("\n", " ").Trim()).Append('\n');
            sb.Append('\n');
            i++;
        }
        return sb.ToString();
    }

    private static string ToSrtTime(TimeSpan t) =>
        $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}";
}