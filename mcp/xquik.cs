// ---
// id: xquik
// name: xquik.cs
// description: Search and inspect public X posts through the Xquik REST API.
// tags:
//   - api
//   - social
//   - web
// status: stable
// version: 1.0.0
// author: Xquik
// license: MIT
// envVars:
//   - XQUIK_API_KEY
// ---
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3
#:property PublishAot=false
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

public record XquikApiResult(int StatusCode, bool IsSuccess, JsonNode? Body);

[McpServerToolType]
public static class XquikTools
{
    private const string ApiBaseUrl = "https://xquik.com";
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    [McpServerTool, Description("Search public X posts with the Xquik API")]
    public static XquikApiResult SearchTweets(
        [Description("Search query, X status URL, tweet ID, or account date window")] string query,
        [Description("Sort order: Latest or Top")] string queryType = "Latest",
        [Description("Maximum tweets to return, from 1 to 200")] int limit = 20,
        [Description("Pagination cursor from a previous response")] string? cursor = null,
        [Description("Only return posts after this ISO 8601 timestamp")] string? sinceTime = null,
        [Description("Only return posts before this ISO 8601 timestamp")] string? untilTime = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("query is required");
        }

        var safeLimit = Math.Clamp(limit, 1, 200);
        var safeQueryType = string.Equals(queryType, "Top", StringComparison.OrdinalIgnoreCase)
            ? "Top"
            : "Latest";

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("q", query),
            new("queryType", safeQueryType),
            new("limit", safeLimit.ToString())
        };

        AddOptional(parameters, "cursor", cursor);
        AddOptional(parameters, "sinceTime", sinceTime);
        AddOptional(parameters, "untilTime", untilTime);

        return SendGet("/api/v1/x/tweets/search", parameters);
    }

    [McpServerTool, Description("Get one public X post by ID with the Xquik API")]
    public static XquikApiResult GetTweet(
        [Description("X post ID")] string tweetId)
    {
        if (string.IsNullOrWhiteSpace(tweetId))
        {
            throw new ArgumentException("tweetId is required");
        }

        return SendGet($"/api/v1/x/tweets/{Uri.EscapeDataString(tweetId)}", []);
    }

    private static void AddOptional(List<KeyValuePair<string, string>> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters.Add(new KeyValuePair<string, string>(name, value));
        }
    }

    private static XquikApiResult SendGet(string path, IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var apiKey = Environment.GetEnvironmentVariable("XQUIK_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Set XQUIK_API_KEY before calling Xquik tools.");
        }

        var queryString = string.Join("&", parameters.Select(
            parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        var uri = string.IsNullOrEmpty(queryString)
            ? new Uri($"{ApiBaseUrl}{path}")
            : new Uri($"{ApiBaseUrl}{path}?{queryString}");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = Http.Send(request);
        var bodyText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var body = TryParseJson(bodyText);

        return new XquikApiResult((int)response.StatusCode, response.IsSuccessStatusCode, body);
    }

    private static JsonNode? TryParseJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return JsonValue.Create(body);
        }
    }
}
