// ---
// id: text-format-tools
// description: Everyday text utilities—slugify, UUID/ID generation, hashing, Base64/URL encode-decode, case transforms, dedent.
// tags:
//   - text
//   - utilities
// version: 1.0.0
// author: XAKPC Dev Labs
// license: MIT
// ---
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3
#:package Slugify.Core@4.0.1
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Slugify;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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

// Shared utilities used by all text formatting tools
internal static class TextFormatUtils
{
    internal const int MaxTextSize = 1024 * 1024; // 1MB limit

    internal static void ValidateTextSize(string text)
    {
        if (text?.Length > MaxTextSize)
            throw new ArgumentException($"Text size exceeds maximum limit of {MaxTextSize} characters");
    }

    internal static void ValidateDataSize(byte[] data)
    {
        if (data?.Length > MaxTextSize)
            throw new ArgumentException($"Data size exceeds maximum limit of {MaxTextSize} bytes");
    }

    internal static void ValidateNewlineFormat(string? newline)
    {
        if (newline != null && !string.Equals(newline, "LF", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(newline, "CRLF", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Newline format must be 'LF' or 'CRLF'");
    }

    internal static string NormalizeNewlines(string text, string? targetNewline = null)
    {
        // Normalize all newlines to LF first
        text = Regex.Replace(text, "\r\n|\r|\n", "\n");
        
        // Apply target newline format if specified
        if (string.Equals(targetNewline, "CRLF", StringComparison.OrdinalIgnoreCase))
            text = text.Replace("\n", "\r\n");
            
        return text;
    }

    internal static IEnumerable<string> SplitIntoWords(string text)
    {
        return Regex.Matches(text, @"[A-Z]+(?![a-z])|[A-Z]?[a-z]+|\d+")
                   .Select(m => m.Value)
                   .Where(w => !string.IsNullOrEmpty(w));
    }
}

public record Base64EncodeResult(string Base64);
public record Base64DecodeResult(string Text);
public record UrlEncodeResult(string Encoded);
public record UrlDecodeResult(string Text);
public record HashResult(string Hex, string Base64);
public record SlugifyResult(string Slug);
public record UuidResult(string Id);
public record CaseConvertResult(string Result);
public record DedentTrimResult(string Result);

[McpServerToolType]
public static class EncodingTools
{
    [McpServerTool, Description("Encode text to Base64")]
    public static Base64EncodeResult Base64Encode(
        [Description("Text to encode")] string text,
        [Description("Newline format: LF or CRLF (default: LF)")] string? newline = "LF")
    {
        TextFormatUtils.ValidateTextSize(text);
        TextFormatUtils.ValidateNewlineFormat(newline);

        // Normalize newlines first, then apply target format
        text = TextFormatUtils.NormalizeNewlines(text, newline);

        var bytes = Encoding.UTF8.GetBytes(text);
        var base64 = Convert.ToBase64String(bytes);

        return new Base64EncodeResult(base64);
    }

    [McpServerTool, Description("Decode Base64 to text")]
    public static Base64DecodeResult Base64Decode(
        [Description("Base64 string to decode")] string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        TextFormatUtils.ValidateDataSize(bytes);
        
        var text = Encoding.UTF8.GetString(bytes);

        return new Base64DecodeResult(text);
    }

    [McpServerTool, Description("URL encode text (RFC 3986 percent-encoding)")]
    public static UrlEncodeResult UrlEncode(
        [Description("Text to URL encode")] string text)
    {
        TextFormatUtils.ValidateTextSize(text);
        var encoded = Uri.EscapeDataString(text);
        return new UrlEncodeResult(encoded);
    }

    [McpServerTool, Description("URL decode text (RFC 3986 percent-decoding)")]
    public static UrlDecodeResult UrlDecode(
        [Description("URL encoded text to decode")] string encoded)
    {
        TextFormatUtils.ValidateTextSize(encoded);
        var text = Uri.UnescapeDataString(encoded);
        return new UrlDecodeResult(text);
    }
}

[McpServerToolType]
public static class HashingTools
{
    [McpServerTool, Description("Generate hash of text or base64 bytes")]
    public static HashResult Hash(
        [Description("Text to hash (optional if bytesBase64 provided)")] string? text = null,
        [Description("Base64 encoded bytes to hash (optional if text provided)")] string? bytesBase64 = null,
        [Description("Hash algorithm: SHA256 or SHA512 recommended (MD5/SHA1 are insecure)")] string algorithm = "SHA256")
    {
        bool hasText = !string.IsNullOrEmpty(text);
        bool hasBytes = !string.IsNullOrEmpty(bytesBase64);
        
        if (hasText == hasBytes)
            throw new ArgumentException("Provide exactly one of text or bytesBase64.");

        byte[] data;
        if (hasText)
        {
            TextFormatUtils.ValidateTextSize(text!);
            data = Encoding.UTF8.GetBytes(text!);
        }
        else
        {
            data = Convert.FromBase64String(bytesBase64!);
            TextFormatUtils.ValidateDataSize(data);
        }

        using HashAlgorithm hasher = algorithm.ToUpperInvariant() switch
        {
            "SHA256" => SHA256.Create(),
            "SHA512" => SHA512.Create(),
            "SHA1" => SHA1.Create(),
            "MD5" => MD5.Create(),
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
        };

        var hash = hasher.ComputeHash(data);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        var base64 = Convert.ToBase64String(hash);

        return new HashResult(hex, base64);
    }
}

[McpServerToolType]
public static class IdentifierTools
{
    [McpServerTool, Description("Convert text to URL-friendly slug")]
    public static SlugifyResult Slugify(
        [Description("Text to convert to slug")] string text,
        [Description("Maximum length of the slug (default: 60)")] int? maxLen = 60,
        [Description("Convert to lowercase (default: true)")] bool? lower = true)
    {
        TextFormatUtils.ValidateTextSize(text);

        var config = new SlugHelperConfiguration();
        config.StringReplacements.Add("#", "-sharp");
        config.StringReplacements.Add("+", "-plus");
        config.StringReplacements.Add("&", "-and");
        config.StringReplacements.Add("%", "-percent");
        config.StringReplacements.Add("@", "-at");
        config.ForceLowerCase = lower ?? true;

        var helper = new SlugHelper(config);
        var slug = helper.GenerateSlug(text);

        if (maxLen.HasValue && slug.Length > maxLen.Value)
            slug = slug.Substring(0, maxLen.Value).TrimEnd('-');

        return new SlugifyResult(slug);
    }

    [McpServerTool, Description("Generate UUID v4 (random)")]
    public static UuidResult UuidV4()
    {
        var id = Guid.NewGuid().ToString();
        return new UuidResult(id);
    }

    [McpServerTool, Description("Generate UUID v7 (time-ordered)")]
    public static UuidResult UuidV7()
    {
        var id = Guid.CreateVersion7().ToString();
        return new UuidResult(id);
    }

    [McpServerTool, Description("Generate NanoID")]
    public static UuidResult NanoId(
        [Description("Size of the NanoID (default: 21)")] int? size = 21)
    {
        if (size <= 0 || size > 255)
            throw new ArgumentException("Size must be between 1 and 255");
            
        var id = GenerateNanoId(size ?? 21);
        return new UuidResult(id);
    }

    private static string GenerateNanoId(int size)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_-";
        var chars = new char[size];
        
        for (int i = 0; i < size; i++)
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            
        return new string(chars);
    }
}

[McpServerToolType]
public static class TextProcessingTools
{
    [McpServerTool, Description("Convert text case")]
    public static CaseConvertResult CaseConvert(
        [Description("Text to convert")] string text,
        [Description("Target case: lower, upper, title, camel, pascal, snake, kebab")] string target)
    {
        TextFormatUtils.ValidateTextSize(text);

        var result = target.ToLowerInvariant() switch
        {
            "lower" => text.ToLowerInvariant(),
            "upper" => text.ToUpperInvariant(),
            "title" => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant()),
            "camel" => ToCamelCase(text),
            "pascal" => ToPascalCase(text),
            "snake" => ToSnakeCase(text),
            "kebab" => ToKebabCase(text),
            _ => throw new ArgumentException($"Invalid target case: {target}")
        };

        return new CaseConvertResult(result);
    }

    [McpServerTool, Description("Remove common indentation and trim whitespace")]
    public static DedentTrimResult DedentTrim(
        [Description("Text to dedent and trim")] string text,
        [Description("Newline format: LF or CRLF (default: LF)")] string? newline = "LF")
    {
        TextFormatUtils.ValidateTextSize(text);
        TextFormatUtils.ValidateNewlineFormat(newline);

        // Normalize newlines first for consistent processing
        text = TextFormatUtils.NormalizeNewlines(text);
        var lines = text.Split('\n');

        // Find minimum indentation (ignoring empty lines)
        var minIndent = int.MaxValue;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var indent = 0;
            foreach (var c in line)
            {
                if (c == ' ') indent++;
                else if (c == '\t') indent += 4; // Tab = 4 spaces
                else break;
            }
            minIndent = Math.Min(minIndent, indent);
        }

        if (minIndent == int.MaxValue) minIndent = 0;

        // Remove common indentation
        var dedented = new List<string>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                dedented.Add("");
                continue;
            }

            var dedentedLine = line;
            var removed = 0;
            var i = 0;
            while (removed < minIndent && i < line.Length)
            {
                if (line[i] == ' ')
                {
                    removed++;
                    i++;
                }
                else if (line[i] == '\t')
                {
                    removed += 4;
                    i++;
                }
                else break;
            }
            dedented.Add(line.Substring(i));
        }

        var joinedResult = string.Join("\n", dedented);
        var result = TextFormatUtils.NormalizeNewlines(joinedResult.Trim(), newline);

        return new DedentTrimResult(result);
    }

    private static string ToCamelCase(string text)
    {
        var words = TextFormatUtils.SplitIntoWords(text).ToArray();
        
        if (words.Length == 0) return "";

        var result = words[0].ToLowerInvariant();
        for (int i = 1; i < words.Length; i++)
        {
            var word = words[i];
            result += char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
        }
        return result;
    }

    private static string ToPascalCase(string text)
    {
        var words = TextFormatUtils.SplitIntoWords(text);

        var result = "";
        foreach (var word in words)
        {
            result += char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
        }
        return result;
    }

    private static string ToSnakeCase(string text)
    {
        var words = TextFormatUtils.SplitIntoWords(text);
        return string.Join("_", words.Select(w => w.ToLowerInvariant()));
    }

    private static string ToKebabCase(string text)
    {
        var words = TextFormatUtils.SplitIntoWords(text);
        return string.Join("-", words.Select(w => w.ToLowerInvariant()));
    }
}