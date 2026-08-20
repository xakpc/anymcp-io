export default function(eleventyConfig) {
  // Note: Using client-side Prism.js instead of server-side highlighting

  // Copy static assets
  eleventyConfig.addPassthroughCopy("src/assets");

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

  // Map of server id -> displayCode, for the client-side copy and download scripts.
  // Only displayCode is needed in the browser, so the rest of each server is dropped.
  eleventyConfig.addFilter("displayCodeMap", function(servers) {
    const map = {};
    for (const [id, server] of Object.entries(servers || {})) {
      map[id] = server.displayCode;
    }
    return JSON.stringify(map);
  });

  // The MCP SDK converts a C# method name to snake case, so GeneratePassword is
  // generate_password on the wire. The catalog page shows the C# name. An agent that
  // checks a tools/list response needs the wire name, so the machine-readable pages
  // show both.
  eleventyConfig.addFilter("wireName", function(name) {
    return String(name || '')
      .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
      .toLowerCase();
  });

  // The catalog manifest for machine consumers, at /servers.json. It drops `code` and
  // `displayCode`, which are larger than all other fields together, and it makes every
  // URL absolute, because an agent reads this file with no base URL.
  eleventyConfig.addFilter("serversManifest", function(serversArray, siteUrl) {
    const base = String(siteUrl || '').replace(/\/+$/, '');
    const wire = (name) => String(name || '')
      .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
      .toLowerCase();

    return JSON.stringify({
      version: 1,
      site: base,
      install: `${base}/install.md`,
      index: `${base}/llms.txt`,
      runtime: {
        sdk: '.NET 10',
        check: 'dotnet --version',
        note: 'dotnet run <file>.cs with #:package directives is a .NET 10 SDK feature. A file-based app writes no bin or obj directory next to the file; the SDK caches the build under the temp directory of the user.',
        scope: 'project',
        configFile: '.mcp.json in the project root',
        serverDirectory: '.mcp-servers/'
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
          name: wire(t.name),
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
