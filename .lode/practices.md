# Practices

These rules apply to all work in this repository.

## Content is file-based

Do not add a database or a CMS. All catalog content stays in `.cs` files in `mcp/`. The site rebuilds
fully on each change. This keeps the site suitable for static hosting.

## Add a server with a file, not with code

To add a catalog entry, add one `.cs` file to `mcp/`. Do not change the parser, the data files, or the
templates. The build finds the new file, and makes its page automatically. See
[server authoring](catalog/mcp-server-authoring.md).

## Keep each server in one file

Do not split a server into more files, and do not add a `.csproj` file. Declare all NuGet packages with
`#:package` directives. The value of the catalog is that a user can copy one file and run it.

## Set `PublishAot=false`

Add this line to each server:

```csharp
#:property PublishAot=false
```

.NET 10 turns AOT compilation on by default for single-file programs. `WithToolsFromAssembly()` uses
reflection to find the tools. AOT removes the metadata that reflection needs, and the server then
exposes no tools.

## Pin one exact version of each package

Write an exact version in each `#:package` directive, for example `@2.2.0`. Do not write a range and do
not omit the version. A user copies the file and runs it months later, and the same versions must
resolve. Keep the version equal in all servers that use the same package. Prefer a stable release to a
preview or a prerelease.

Prefer a package that builds without a warning. A build warning breaks the copy-and-run promise, because
the user sees it on the first run. This is why `image-utility` stays on ImageSharp 3.x. See
[server inventory](catalog/server-inventory.md).

## Send all logs to stderr

Stdio transport uses standard output for protocol messages. A log line on standard output damages the
protocol stream. Each server must configure the console logger like this:

```csharp
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

## Use ES modules in JavaScript

`package.json` sets `"type": "module"`. Write `export default` and `import`, not `require`. This applies
to `.eleventy.js` and to all files in `src/_data/`.

## Style with Tailwind utility classes only

Tailwind CSS loads from a CDN in `src/_includes/base.njk`. The same file holds the Tailwind theme
configuration and the syntax colors. Do not add separate CSS files. See
[styling](site/styling.md).

## Give each front matter field a safe default

The parser must never fail because a field is absent. It supplies a default for every field. Keep this
behavior when you change the parser. See [data pipeline](site/data-pipeline.md).

## Test a change with the local server

Run `npm run serve`, and open http://localhost:8080/. Check the home page, the search box, and the
detail page of the server that you changed.
