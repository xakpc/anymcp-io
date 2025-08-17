// ---
// id: image-utility
// description: Fast image resize/convert/compress, metadata stripping, and thumbnail generation for everyday assets.
// tags:
//   - images
//   - media
//   - utilities
// version: 1.0.0
// author: XAKPC Dev Labs
// license: MIT
// ---
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3
#:package SixLabors.ImageSharp@3.1.11
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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
public record ImageProcessResult(string Output, int Width, int Height);
public record ImageConvertResult(string Output);
public record ImageStripMetadataResult(string[] RemovedTags);

[McpServerToolType]
public static class ImageTools
{
         [McpServerTool, Description("Resize an image with various fit modes")]
        public static ImageProcessResult ImageResize(
            [Description("Input image path")] string input,
            [Description("Output image path")] string output,
            [Description("Target width in pixels")] int? width = null,
            [Description("Target height in pixels")] int? height = null,
            [Description("Fit mode: contain, cover, fill, fitWidth, fitHeight")] string fit = "contain",
            [Description("Keep EXIF metadata")] bool? keepExif = false,
            [Description("JPEG quality 1-100")] int? quality = 80)
        {
            ImageUtils.ValidateInputFile(input);
            ImageUtils.ValidateOutputPath(output, true);

            var inputPath = ImageUtils.NormalizePath(input);
            var outputPath = ImageUtils.NormalizePath(output);

            using var image = Image.Load(inputPath);

            // Validate source dimensions
            ImageUtils.ValidateImageDimensions(image.Width, image.Height);

            // Auto-rotate based on EXIF
            image.Mutate(x => x.AutoOrient());

            // Calculate target size
            var originalSize = new Size(image.Width, image.Height);
            var targetSize = ImageUtils.CalculateResizeSize(originalSize, width, height, fit);

            // Validate target dimensions
            ImageUtils.ValidateImageDimensions(targetSize.Width, targetSize.Height);

            // Resize image
            if (targetSize != originalSize)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = targetSize,
                    Mode = fit.ToLowerInvariant() switch
                    {
                        "contain" => ResizeMode.Max,
                        "cover" => ResizeMode.Crop,
                        "fill" => ResizeMode.Stretch,
                        "fitwidth" or "fitheight" => ResizeMode.Max,
                        _ => ResizeMode.Max
                    }
                }));
            }

            // Remove metadata if requested
            if (!keepExif.GetValueOrDefault())
            {
                image.Metadata.ExifProfile = null;
                image.Metadata.XmpProfile = null;
                image.Metadata.IptcProfile = null;
            }

            // Determine output format from extension
            var extension = Path.GetExtension(output).TrimStart('.').ToLowerInvariant();
            var encoder = ImageUtils.GetEncoder(extension, quality);

            image.Save(outputPath, encoder);

            return new ImageProcessResult(output, image.Width, image.Height);
        }

    [McpServerTool, Description("Convert image to different format")]
    public static ImageConvertResult ImageConvert(
        [Description("Input image path")] string input,
        [Description("Output image path")] string output,
        [Description("Target format: webp, jpeg, png")] string format,
        [Description("Quality for lossy formats (1-100)")] int? quality = 85)
    {
        ImageUtils.ValidateInputFile(input);
        ImageUtils.ValidateOutputPath(output, true);

        var inputPath = ImageUtils.NormalizePath(input);
        var outputPath = ImageUtils.NormalizePath(output);

        using var image = Image.Load(inputPath);

        // Validate dimensions
        ImageUtils.ValidateImageDimensions(image.Width, image.Height);

        // Auto-rotate based on EXIF
        image.Mutate(x => x.AutoOrient());

        var encoder = ImageUtils.GetEncoder(format, quality);
        image.Save(outputPath, encoder);

        return new ImageConvertResult(output);
    }
}

internal static class ImageUtils
{
    internal static string? ConfiguredRoot { get; set; } = Directory.GetCurrentDirectory();
    internal const int MaxDimensionLimit = 12000; // 12k pixels max to prevent OOM

    internal static string NormalizePath(string path)
    {
        // Convert to absolute path and normalize
        var fullPath = Path.GetFullPath(path, ConfiguredRoot ?? Directory.GetCurrentDirectory());

        // Security check: ensure path is within configured root
        var rootPath = Path.GetFullPath(ConfiguredRoot ?? Directory.GetCurrentDirectory());
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path outside configured root: {path}");

        return fullPath;
    }

    internal static void ValidateInputFile(string path)
    {
        var normalizedPath = NormalizePath(path);
        if (!File.Exists(normalizedPath))
            throw new FileNotFoundException($"Input file not found: {path}");
    }

    internal static void ValidateOutputPath(string path, bool overwrite = false)
    {
        var normalizedPath = NormalizePath(path);
        if (!overwrite && File.Exists(normalizedPath))
            throw new InvalidOperationException($"Output file already exists: {path}. Enable overwrite if needed.");

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);
    }

    internal static void ValidateImageDimensions(int width, int height)
    {
        if (width > MaxDimensionLimit || height > MaxDimensionLimit)
            throw new ArgumentException($"Image dimension exceeds maximum limit of {MaxDimensionLimit} pixels (got {width}x{height}). This prevents out-of-memory errors.");
    }

    internal static IImageEncoder GetEncoder(string format, int? quality = null)
    {
        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => new JpegEncoder { Quality = quality ?? 80 },
            "png" => new PngEncoder(),
            "webp" => new WebpEncoder { Quality = quality ?? 85 },
            _ => throw new ArgumentException($"Unsupported format: {format}. Supported: jpeg, png, webp")
        };
    }

    internal static Size CalculateResizeSize(Size original, int? targetWidth, int? targetHeight, string fitMode)
    {
        if (!targetWidth.HasValue && !targetHeight.HasValue)
            return original;

        return fitMode.ToLowerInvariant() switch
        {
            "contain" => CalculateContain(original, targetWidth, targetHeight),
            "cover" => CalculateCover(original, targetWidth, targetHeight),
            "fill" => new SixLabors.ImageSharp.Size(targetWidth ?? original.Width, targetHeight ?? original.Height),
            "fitwidth" => targetWidth.HasValue ? new Size(targetWidth.Value, (int)(original.Height * (double)targetWidth.Value / original.Width)) : original,
            "fitheight" => targetHeight.HasValue ? new Size((int)(original.Width * (double)targetHeight.Value / original.Height), targetHeight.Value) : original,
            _ => throw new ArgumentException($"Invalid fit mode: {fitMode}. Supported: contain, cover, fill, fitWidth, fitHeight")
        };
    }

    private static Size CalculateContain(Size original, int? maxWidth, int? maxHeight)
    {
        var scaleX = maxWidth.HasValue ? (double)maxWidth.Value / original.Width : double.MaxValue;
        var scaleY = maxHeight.HasValue ? (double)maxHeight.Value / original.Height : double.MaxValue;
        var scale = Math.Min(scaleX, scaleY);

        if (scale >= 1) return original;

        return new Size((int)(original.Width * scale), (int)(original.Height * scale));
    }

    private static Size CalculateCover(Size original, int? targetWidth, int? targetHeight)
    {
        if (!targetWidth.HasValue || !targetHeight.HasValue)
            throw new ArgumentException("Both width and height required for cover mode");

        var scaleX = (double)targetWidth.Value / original.Width;
        var scaleY = (double)targetHeight.Value / original.Height;
        var scale = Math.Max(scaleX, scaleY);

        return new Size((int)(original.Width * scale), (int)(original.Height * scale));
    }
}
