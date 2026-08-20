// The MCP SDK converts a C# method name to snake case, so GeneratePassword is
// generate_password on the wire. The catalog page shows the C# name. An agent that
// checks a tools/list response needs the wire name, so the machine-readable pages
// show both. The filter and the manifest share this one definition.
function toWireName(name) {
  return String(name || '')
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
    .toLowerCase();
}

export default function(eleventyConfig) {
  // Note: Using client-side Prism.js instead of server-side highlighting

  // Cloudflare Pages reads _headers from the root of the build output (_site).
  // Eleventy strips the input-directory prefix, so "src/_headers" lands at "_site/_headers".
  eleventyConfig.addPassthroughCopy("src/_headers");

  // Add date filter
  eleventyConfig.addFilter("date", function(date, format) {
    if (!date) return '';
    const d = new Date(date);
    if (format === 'M/D/YYYY') {
      return `${d.getMonth() + 1}/${d.getDate()}/${d.getFullYear()}`;
    }
    return d.toLocaleDateString();
  });

  // Add locale string filter for numbers
  eleventyConfig.addFilter("localeString", function(num) {
    if (!num) return '';
    return num.toLocaleString();
  });

  eleventyConfig.addFilter("wireName", toWireName);

  // The number of lines of a code block. A server page shows the first 20 lines and gives
  // a button for the rest, and the label of that button needs the total.
  eleventyConfig.addFilter("lineCount", (text) => String(text || "").split("\n").length);

  // The one line that the Copy Agent Prompt button puts on the clipboard. A catalog card
  // and a detail page both write it, so the text lives here and not in a template, and the
  // two can never drift. The line is a pointer and not a procedure: /servers/{id}/index.md
  // opens with the rules that an agent obeys.
  eleventyConfig.addFilter("agentPrompt", function(id, siteUrl) {
    const base = String(siteUrl || "").replace(/[/]+$/, "");
    return `Read ${base}/servers/${id}/index.md and install the ${id} MCP server into this project.`;
  });

  // The catalog manifest for machine consumers, at /servers.json. It drops `code` and
  // `displayCode`, which are larger than all other fields together, and it makes every
  // URL absolute, because an agent reads this file with no base URL.
  eleventyConfig.addFilter("serversManifest", function(serversArray, siteUrl) {
    const base = String(siteUrl || '').replace(/\/+$/, '');

    return JSON.stringify({
      version: 1,
      site: base,
      install: `${base}/install.md`,
      index: `${base}/llms.txt`,
      runtime: {
        sdk: '.NET 10 or later',
        check: 'dotnet --list-sdks',
        note: 'The dotnet run <file>.cs command with #:package directives is a feature of the .NET 10 SDK. Each SDK from version 10 operates, and each earlier SDK fails. Check with --list-sdks, and not with --version: a global.json file can select an earlier SDK on a computer that also has version 10. A file-based app writes no bin directory and no obj directory near the file. The SDK keeps the build output in a cache in the temporary directory of the user.',
        scope: 'project',
        serverDirectory: '.mcp-servers/',
        // The server file is shared; the configuration file is not. A consumer that picks the
        // wrong entry here writes a file its client never reads, and gets no error to go on,
        // so the manifest names the file, the format, and the page for each client.
        // postInstall is the end state an installing agent should expect, so it does not read
        // the last human step as a failure and retry the registration.
        clients: [
          {
            id: 'claude-code',
            label: 'Claude Code, Claude Desktop, Cursor, Windsurf',
            configFile: '.mcp.json in the project root',
            format: 'json',
            docs: `${base}/install/claude-code.md`,
            postInstall: 'The claude mcp add command reports "Pending approval" for a project server until a person approves the server. For an agent, this is the correct end state: stop there, and tell the user to start the client again and approve the server.'
          },
          {
            id: 'codex',
            label: 'Codex',
            configFile: '.codex/config.toml in the project root',
            format: 'toml',
            docs: `${base}/install/codex.md`,
            postInstall: 'Codex reads a project configuration file only from a trusted project. The trust entry goes in the personal ~/.codex/config.toml of the user, and an agent must not write it. For an agent, the correct end state is this: .codex/config.toml holds the server, and the user has the two steps that remain, the trust entry and a restart with a new chat.'
          }
        ]
      },
      servers: (serversArray || []).map(s => ({
        id: s.id,
        file: s.name,
        description: s.description,
        longDescription: s.longDescription,
        status: s.status,
        version: s.version,
        license: s.license,
        author: s.author,
        tags: s.tags || [],
        // envVars holds names only. lint.test.js compares this list against the
        // GetEnvironmentVariable("NAME") strings in the source, so strings are the contract.
        env: (s.envVars || []).map(v => (typeof v === 'string' ? v : v.name)),
        tools: (s.tools || []).map(t => ({
          name: toWireName(t.name),
          methodName: t.name,
          description: t.description
        })),
        urls: {
          page: `${base}/servers/${s.id}/`,
          markdown: `${base}/servers/${s.id}/index.md`,
          source: `${base}/servers/${s.id}/${s.name}`
        },
        // Ready to paste into an mcpServers entry. The path is relative to the project, which
        // is where the default install puts the file, so nothing needs substituting.
        // "-v" "q" keeps the MSBuild output off stdout, which carries the JSON-RPC stream.
        run: {
          command: 'dotnet',
          args: ['run', `./.mcp-servers/${s.name}`, '-v', 'q'],
          cwd: 'the project directory'
        }
      }))
    }, null, 2);
  });

  // Set up directory structure
  return {
    templateFormats: [
      "md",
      "njk",
      "html",
      "liquid"
    ],

    // Input directory
    dir: {
      input: "src",
      includes: "_includes",
      data: "_data",
      output: "_site"
    },

    // Use nunjucks for .html files
    htmlTemplateEngine: "njk",
    markdownTemplateEngine: "njk"
  };
};
