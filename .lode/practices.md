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

## A server for one platform must still start on every platform

The test harness builds and probes each server on Linux. A server that speaks to one operating system
only must therefore compile, start, and list its tools everywhere. Two rules make this true:

- Run no platform call while the host starts. Put the call in the tool method, and not in the
  top-level statements. A `DllImport` resolves at the first call, so a start on another platform
  never loads the library.
- Start each tool with a guard that throws:

  ```csharp
  if (!OperatingSystem.IsWindows())
  {
      throw new PlatformNotSupportedException("...");
  }
  ```

Say the platform in the `description`, and give the server a tag for it. The front matter has no
field for a platform, and the lint layer refuses an unknown field.

Prefer a portable package to a platform library. `System.Drawing.Common` runs on Windows only, and a
reference to it from a plain `net10.0` target makes CA1416 warnings that fail the build layer.
`win-app-screenshots` uses P/Invoke and ImageSharp instead. See
[Windows window capture](catalog/windows-window-capture.md).

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

## Write the agent pages in Simplified Technical English

The machine-readable outputs are procedures that a coding agent obeys without a person. Write them in
ASD-STE100 Simplified Technical English. This applies to `src/llms.njk`, `src/agent-install.njk`,
`src/server-md.njk`, the `runtime` strings in the `serversManifest` filter in `.eleventy.js`, and the
`data-prompt` text in `src/servers.njk`.

The rules that change the text most:

- One instruction in one sentence. Use the imperative form for each instruction.
- Keep a procedural sentence to 20 words, and a descriptive sentence to 25 words.
- Use the active voice and the simple present tense.
- Use one word for one meaning. Examples of the words to keep out: verify (use check), symptom (use
  problem), equivalent (use the same), invent (use make), expand a variable (use replace), correct as
  a verb (use change), smoke-test (use test manually).
- Start a warning with the command, and give the cause after it.

The prose of a page is free to change, but the commands are not. `test/site.test.js` checks the
command text, the order of the scopes, and the `-v q` option. Run `npm test` after a rewrite.

The front matter `description` and `longDescription` fields also reach an agent, through
`/llms.txt`, `/servers.json`, and the top of each `.md` page. They are catalog metadata, and this
rule does not apply to them today. See [front-matter schema](catalog/front-matter-schema.md).

The pages for a person, `src/index.njk`, `src/setup.njk`, and the HTML part of `src/servers.njk`,
keep their usual English.

## Test a change with the local server

Run `npm run serve`, and open http://localhost:8080/. Check the home page, the search box, and the
detail page of the server that you changed.

## A server enters the catalog only after `npm test` passes

The harness lints the metadata, compiles the file, and starts the server to read its tool list. It
finds a new file by itself, so adding a server needs no change to a test file. See
[automated testing](catalog/automated-testing.md).

## The lode covers XAKPC Dev Labs servers only

A catalog entry from another author is a guest in `mcp/`. The lode documents the servers of XAKPC Dev
Labs, and the machinery that carries every entry: the parser, the templates, the harness, and the
front matter contract. It does not document how a server of another author works inside, and it does
not hold that server up as the model to follow.

Read the `author` field of the front matter to tell the two apart. `xquik` is the one guest entry
today.

A guest entry still appears where a fact about the catalog needs it, for example a row in the
[server inventory](catalog/server-inventory.md), or a name in a command line. What must not appear is
its internal design, its API, or a lesson learned from its code. Keep such knowledge in the
repository of its author.

## A server must list its tools with no secret set

The test harness removes every declared `envVars` name from the environment before it starts a server.
A user pastes the file and runs it before they set any key, and the client must still show the tools. A
key is needed only when the LLM calls the tool. This rule also lets a pull request from a fork run the
full harness, because CI gives it no secret.
