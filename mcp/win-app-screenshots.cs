// ---
// id: win-app-screenshots
// name: win-app-screenshots.cs
// description: Capture a PNG of a running Windows application window, and give the picture to the model.
// longDescription: Windows only. It finds a top-level window from a process name, a process id, an executable path, or a window title, and copies its pixels to a PNG. PrintWindow works while another window covers the target, and a screen copy is the fallback. The tool returns the picture to the model, and the path of the saved file.
// tags:
//   - windows
//   - screenshots
//   - images
//   - utilities
// status: beta
// version: 1.0.0
// author: XAKPC Dev Labs
// license: MIT
// ---
#:package Microsoft.Extensions.Hosting@10.0.11
#:package ModelContextProtocol@2.2.0
#:package SixLabors.ImageSharp@3.1.12
#:property PublishAot=false
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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
public record WindowInfo(string Process, int Id, string Handle, string Title);
public record WindowListResult(int Count, WindowInfo[] Windows);

[McpServerToolType]
public static class ScreenshotTools
{
    [McpServerTool, Description("List the visible windows of a running Windows application, so a capture can pick one")]
    public static WindowListResult ListAppWindows(
        [Description("Process name, with or without .exe")] string? name = null,
        [Description("Process id")] int? id = null,
        [Description("Full path of the executable")] string? path = null,
        [Description("Part of the window title. The match is not case sensitive")] string? title = null)
    {
        Capture.RequireWindows();
        Capture.PrepareProcess();

        var windows = Capture.FindTargets(name, id, path, title)
            .Select(w => new WindowInfo(w.Process, w.Id, $"0x{w.Handle.ToInt64():X}", w.Title))
            .ToArray();

        return new WindowListResult(windows.Length, windows);
    }

    [McpServerTool, Description("Capture a window of a running Windows application as a PNG, and return the picture with its file path")]
    public static CallToolResult CaptureAppWindow(
        [Description("Process name, with or without .exe")] string? name = null,
        [Description("Process id")] int? id = null,
        [Description("Full path of the executable")] string? path = null,
        [Description("Part of the window title. The match is not case sensitive")] string? title = null,
        [Description("Output PNG path. The default is a file with the process name and the time, in the temp folder")] string? output = null,
        [Description("Seconds to wait for the window to appear. 0 means that the window must exist now")] int waitSec = 0,
        [Description("Milliseconds to wait after the window comes to the front, before the capture")] int delayMs = 800,
        [Description("How to get the pixels: Auto, PrintWindow, or Screen")] string method = "Auto",
        [Description("Do not bring the window to the front")] bool noActivate = false,
        [Description("Capture the client area only, with no title bar and no border")] bool client = false,
        [Description("Downscale the picture that the model sees to this width. The saved file keeps its full size. 0 removes the limit")] int maxWidth = 1400)
    {
        Capture.RequireWindows();
        Capture.PrepareProcess();

        var found = Capture.WaitForWindows(name, id, path, title, waitSec);

        return Capture.Shoot(found[0], found.Count, output, delayMs, method, noActivate, client, maxWidth);
    }
}

internal static class Capture
{
    internal record Target(string Process, int Id, IntPtr Handle, string Title);

    private static int dpiReady;

    internal static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "win-app-screenshots reads the pixels with the Windows GDI, so it runs on Windows only.");
        }
    }

    /// <summary>
    /// Tells Windows that this process works in physical pixels. Without this
    /// call the coordinates are wrong on a display with a scale above 100
    /// percent, and the picture is cut. -4 is
    /// DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2.
    ///
    /// The call belongs to the first tool call, and not to the start of the
    /// host. A DllImport resolves at the first call, so the server starts and
    /// lists its tools on every platform, and only a capture fails away from
    /// Windows. The test harness builds and probes each server on Linux.
    /// </summary>
    internal static void PrepareProcess()
    {
        if (Interlocked.Exchange(ref dpiReady, 1) == 1)
        {
            return;
        }

        // An old Windows has no such entry point, and the process then keeps
        // the awareness that it has.
        try { Native.SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
    }

    internal static List<Target> FindTargets(string? name, int? id, string? path, string? title)
    {
        if (name is null && id is null && path is null && title is null)
        {
            throw new ArgumentException("Give a name, an id, a path, or a title to choose the application.");
        }

        // A null map means that no process filter is set, so every window is a
        // candidate and the title alone chooses.
        var wanted = WantedProcesses(name, id, path);
        var found = new List<Target>();

        foreach (var handle in Native.TopLevelWindows())
        {
            var processId = Native.ProcessIdOf(handle);

            if (wanted is not null && !wanted.ContainsKey(processId))
            {
                continue;
            }

            var windowTitle = Native.TitleOf(handle);

            if (title is not null && !windowTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            found.Add(new Target(ProcessNameOf(processId, wanted), (int)processId, handle, windowTitle));
        }

        return found;
    }

    internal static List<Target> WaitForWindows(string? name, int? id, string? path, string? title, int waitSec)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(waitSec, 0, 600));
        var found = FindTargets(name, id, path, title);

        while (found.Count == 0 && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(500);
            found = FindTargets(name, id, path, title);
        }

        if (found.Count == 0)
        {
            throw new InvalidOperationException(
                "No window matched. Is the application running? Raise waitSec to wait for the start, " +
                "or call list_app_windows to see the windows that exist now.");
        }

        return found;
    }

    internal static CallToolResult Shoot(Target target, int matchCount, string? output, int delayMs,
        string method, bool noActivate, bool client, int maxWidth)
    {
        var mode = method.Trim().ToLowerInvariant() switch
        {
            "" or "auto" => "auto",
            "printwindow" => "printwindow",
            "screen" => "screen",
            _ => throw new ArgumentException($"Unknown method '{method}'. Use Auto, PrintWindow, or Screen.")
        };

        var handle = target.Handle;

        if (Native.IsIconic(handle))
        {
            // A minimized window holds no pixels. Restore it for every method.
            Native.ShowWindow(handle, Native.ShowRestore);
            Thread.Sleep(400);
        }

        var isForeground = false;

        if (!noActivate)
        {
            isForeground = Native.ForceForeground(handle);
        }

        Thread.Sleep(Math.Clamp(delayMs, 0, 30000));

        // The measurements. GetWindowRect adds the invisible resize border of
        // the compositor, which gives a grey edge, so the true bounds come from
        // DWM and the PrintWindow picture needs a cut.
        Native.GetWindowRect(handle, out var windowRect);

        var hasFrame = Native.DwmGetWindowAttribute(
            handle, Native.ExtendedFrameBounds, out var frame, Marshal.SizeOf<Native.RECT>()) == 0;

        if (!hasFrame)
        {
            // Windows 7 and some remote sessions run with no compositor.
            frame = windowRect;
        }

        Native.GetClientRect(handle, out var clientRect);
        var clientOrigin = default(Native.POINT);
        Native.ClientToScreen(handle, ref clientOrigin);

        var left = client ? clientOrigin.X : frame.Left;
        var top = client ? clientOrigin.Y : frame.Top;
        var width = client ? clientRect.Right - clientRect.Left : frame.Right - frame.Left;
        var height = client ? clientRect.Bottom - clientRect.Top : frame.Bottom - frame.Top;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("The window has no size. It is not drawn yet.");
        }

        var used = "PrintWindow";
        var image = mode == "screen"
            ? null
            : WithPrintWindow(handle, client, windowRect, frame, clientRect, width, height);

        if (image is null)
        {
            if (mode == "printwindow")
            {
                throw new InvalidOperationException(
                    "PrintWindow gave an empty picture for this window. Call again with method Screen.");
            }

            image = FromScreen(left, top, width, height);
            used = "Screen";
        }

        using (image)
        {
            var file = ResolveOutput(output, target.Process);
            image.SaveAsPng(file);

            var note = new StringBuilder();
            note.Append($"{target.Process} ({target.Id}) '{target.Title}' ");
            note.Append($"{image.Width}x{image.Height} by {used}");
            note.Append($"\nSaved: {file}");

            if (matchCount > 1)
            {
                note.Append($"\n{matchCount} windows matched, and the first one is in the picture. ");
                note.Append("Call list_app_windows to see them all.");
            }

            if (!noActivate && !isForeground && used == "Screen")
            {
                note.Append("\nThe window did not come to the front, so another window can be in the picture.");
            }

            using var inline = Downscale(image, maxWidth);
            using var buffer = new MemoryStream();
            inline.SaveAsPng(buffer);

            return new CallToolResult
            {
                Content =
                [
                    // FromBytes and not the Data property. Data holds the bytes
                    // of the base64 text, so a raw PNG there reaches the client
                    // as broken characters.
                    ImageContentBlock.FromBytes(buffer.ToArray(), "image/png"),
                    new TextContentBlock { Text = note.ToString() }
                ]
            };
        }
    }

    /// <summary>
    /// Asks the window to draw itself. This works while another window covers
    /// it, and it needs no focus. A null answer means that the window gave
    /// nothing, and the caller must copy the display instead.
    /// </summary>
    private static Image<Bgra32>? WithPrintWindow(IntPtr handle, bool client,
        Native.RECT windowRect, Native.RECT frame, Native.RECT clientRect, int width, int height)
    {
        var flags = Native.PrintFullContent;
        int fullWidth;
        int fullHeight;

        if (client)
        {
            flags |= Native.PrintClientOnly;
            fullWidth = clientRect.Right - clientRect.Left;
            fullHeight = clientRect.Bottom - clientRect.Top;
        }
        else
        {
            fullWidth = windowRect.Right - windowRect.Left;
            fullHeight = windowRect.Bottom - windowRect.Top;
        }

        if (fullWidth <= 0 || fullHeight <= 0)
        {
            return null;
        }

        var pixels = Native.Render(fullWidth, fullHeight, hdc => Native.PrintWindow(handle, hdc, flags));

        if (pixels is null || IsBlank(pixels, fullWidth, fullHeight))
        {
            return null;
        }

        var image = Image.LoadPixelData<Bgra32>(pixels, fullWidth, fullHeight);

        if (client)
        {
            return image;
        }

        // Cut the invisible border away, so the result holds the same area that
        // the screen method gives.
        var cut = Rectangle.Intersect(
            new Rectangle(frame.Left - windowRect.Left, frame.Top - windowRect.Top, width, height),
            image.Bounds);

        if (cut.Width <= 0 || cut.Height <= 0)
        {
            return image;
        }

        image.Mutate(x => x.Crop(cut));

        return image;
    }

    /// <summary>
    /// Copies the pixels of the display. It is exact for what the eye sees, but
    /// it gives the window above the target when the target is behind another.
    /// </summary>
    private static Image<Bgra32> FromScreen(int left, int top, int width, int height)
    {
        var screen = Native.GetDC(IntPtr.Zero);

        try
        {
            var pixels = Native.Render(width, height, hdc =>
                Native.BitBlt(hdc, 0, 0, width, height, screen, left, top, Native.SrcCopy | Native.CaptureBlt));

            if (pixels is null)
            {
                throw new InvalidOperationException("The screen copy gave no pixels.");
            }

            return Image.LoadPixelData<Bgra32>(pixels, width, height);
        }
        finally
        {
            Native.ReleaseDC(IntPtr.Zero, screen);
        }
    }

    /// <summary>
    /// PrintWindow can answer true and still give an empty picture. Some
    /// windows that draw with the graphics card behave like this. A picture
    /// with one colour only is empty.
    /// </summary>
    private static bool IsBlank(byte[] pixels, int width, int height)
    {
        var first = BitConverter.ToUInt32(pixels, 0);
        var stepX = Math.Max(1, width / 24);
        var stepY = Math.Max(1, height / 24);

        for (var y = 0; y < height; y += stepY)
        {
            for (var x = 0; x < width; x += stepX)
            {
                if (BitConverter.ToUInt32(pixels, ((y * width) + x) * 4) != first)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Always gives a new picture, so the caller can dispose it while the full
    /// size original stays alive.
    /// </summary>
    private static Image<Bgra32> Downscale(Image<Bgra32> source, int maxWidth)
    {
        var copy = source.Clone();

        if (maxWidth > 0 && copy.Width > maxWidth)
        {
            // A height of 0 keeps the ratio of the sides.
            copy.Mutate(x => x.Resize(maxWidth, 0));
        }

        return copy;
    }

    private static string ResolveOutput(string? output, string process)
    {
        var file = output;

        if (string.IsNullOrWhiteSpace(file))
        {
            // Path.GetTempPath and not the TEMP variable: the lint layer asks
            // that each variable that the code reads is in the envVars list,
            // and the page would then show an env block that nobody must set.
            var folder = Path.Combine(Path.GetTempPath(), "win-app-screenshots");
            file = Path.Combine(folder, $"{process}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        }

        var full = Path.GetFullPath(file);
        var parent = Path.GetDirectoryName(full);

        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        return full;
    }

    private static Dictionary<uint, string>? WantedProcesses(string? name, int? id, string? path)
    {
        if (name is null && id is null && path is null)
        {
            return null;
        }

        var wanted = new Dictionary<uint, string>();

        if (id is not null)
        {
            try
            {
                using var one = Process.GetProcessById(id.Value);
                wanted[(uint)one.Id] = one.ProcessName;
            }
            catch (ArgumentException)
            {
                // The process is gone. An empty map matches no window, and the
                // caller reports that nothing matched.
            }

            return wanted;
        }

        if (name is not null)
        {
            var clean = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;

            foreach (var process in Process.GetProcessesByName(clean))
            {
                using (process)
                {
                    wanted[(uint)process.Id] = process.ProcessName;
                }
            }

            return wanted;
        }

        var full = Path.GetFullPath(path!);

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (string.Equals(ExecutablePath(process), full, StringComparison.OrdinalIgnoreCase))
                {
                    wanted[(uint)process.Id] = process.ProcessName;
                }
            }
        }

        return wanted;
    }

    private static string ProcessNameOf(uint processId, Dictionary<uint, string>? known)
    {
        if (known is not null && known.TryGetValue(processId, out var value))
        {
            return value;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);

            return process.ProcessName;
        }
        catch
        {
            return "unknown";
        }
    }

    private static string? ExecutablePath(Process process)
    {
        // A protected process refuses this read, and throws.
        try { return process.MainModule?.FileName; } catch { return null; }
    }
}

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    internal delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

    // DWMWA_EXTENDED_FRAME_BOUNDS, SW_RESTORE, and GW_OWNER.
    internal const int ExtendedFrameBounds = 9;
    internal const int ShowRestore = 9;
    internal const uint GwOwner = 4;

    // PW_CLIENTONLY and PW_RENDERFULLCONTENT. The second flag makes a window
    // that draws with the graphics card give its true pixels.
    internal const uint PrintClientOnly = 1;
    internal const uint PrintFullContent = 2;

    // SRCCOPY, and CAPTUREBLT for the layered windows above the target.
    internal const uint SrcCopy = 0x00CC0020;
    internal const uint CaptureBlt = 0x40000000;

    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr window, uint command);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] internal static extern int GetWindowTextLength(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr window, StringBuilder text, int max);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window, out RECT rectangle);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr window, out RECT rectangle);
    [DllImport("user32.dll")] internal static extern bool ClientToScreen(IntPtr window, ref POINT point);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool AttachThreadInput(uint from, uint to, bool attach);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);
    [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out RECT rectangle, int size);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateDIBSection(IntPtr deviceContext, ref BITMAPINFOHEADER header, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr handle);
    [DllImport("gdi32.dll")] internal static extern bool BitBlt(IntPtr target, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr handle);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr deviceContext);
    [DllImport("gdi32.dll")] internal static extern bool GdiFlush();

    /// <summary>
    /// Makes a 32 bit top-down DIB, runs the draw action against its device
    /// context, and gives the pixels back as BGRA bytes. CreateDIBSection hands
    /// the pixel pointer back, so no GetDIBits call is necessary.
    /// </summary>
    internal static byte[]? Render(int width, int height, Func<IntPtr, bool> draw)
    {
        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = width,
            // A negative height gives the rows from the top. A positive height
            // gives them in the other order, and the picture is upside down.
            biHeight = -height,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0
        };

        var memoryDc = CreateCompatibleDC(IntPtr.Zero);

        if (memoryDc == IntPtr.Zero)
        {
            return null;
        }

        var section = IntPtr.Zero;
        var previous = IntPtr.Zero;

        try
        {
            section = CreateDIBSection(memoryDc, ref header, 0, out var bits, IntPtr.Zero, 0);

            if (section == IntPtr.Zero || bits == IntPtr.Zero)
            {
                return null;
            }

            previous = SelectObject(memoryDc, section);

            if (!draw(memoryDc))
            {
                return null;
            }

            // GDI holds the drawing in a batch. Read the memory only after the
            // batch reaches the device context.
            GdiFlush();

            var pixels = new byte[width * height * 4];
            Marshal.Copy(bits, pixels, 0, pixels.Length);

            // GDI leaves the alpha byte at zero, and every pixel of the PNG
            // would then be fully transparent. Make each one opaque.
            for (var i = 3; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
            }

            return pixels;
        }
        finally
        {
            if (previous != IntPtr.Zero)
            {
                SelectObject(memoryDc, previous);
            }

            if (section != IntPtr.Zero)
            {
                DeleteObject(section);
            }

            DeleteDC(memoryDc);
        }
    }

    /// <summary>
    /// Brings a window to the front. Windows stops a background process from
    /// taking the focus, so the input queue of this thread joins the queue of
    /// the window that has the focus now. The call is then legal.
    /// </summary>
    internal static bool ForceForeground(IntPtr window)
    {
        var current = GetForegroundWindow();

        if (current == window)
        {
            return true;
        }

        var currentThread = current == IntPtr.Zero ? 0 : GetWindowThreadProcessId(current, out _);
        var thisThread = GetCurrentThreadId();

        var attached = currentThread != 0
            && currentThread != thisThread
            && AttachThreadInput(thisThread, currentThread, true);

        try
        {
            BringWindowToTop(window);
            SetForegroundWindow(window);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(thisThread, currentThread, false);
            }
        }

        return GetForegroundWindow() == window;
    }

    internal static List<IntPtr> TopLevelWindows()
    {
        var found = new List<IntPtr>();

        EnumWindows((window, parameter) =>
        {
            // A visible window with a title and no owner is a real window. This
            // drops the hidden helper windows of the framework.
            if (IsWindowVisible(window)
                && GetWindow(window, GwOwner) == IntPtr.Zero
                && GetWindowTextLength(window) > 0)
            {
                found.Add(window);
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }

    internal static uint ProcessIdOf(IntPtr window)
    {
        GetWindowThreadProcessId(window, out var processId);

        return processId;
    }

    internal static string TitleOf(IntPtr window)
    {
        var length = GetWindowTextLength(window);

        if (length <= 0)
        {
            return string.Empty;
        }

        var text = new StringBuilder(length + 1);
        GetWindowText(window, text, text.Capacity);

        return text.ToString();
    }
}
