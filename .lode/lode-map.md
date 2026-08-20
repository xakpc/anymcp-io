# Lode Map

The index of all lode files. Read this file first.

```
.lode/
├── summary.md                        one-paragraph snapshot of the project
├── terminology.md                    domain words and their meanings
├── practices.md                      rules and patterns for all work here
├── lode-map.md                       this index
├── plans/
│   └── backlog.md                    open items and known defects
├── site/                             the Eleventy static site
│   ├── summary.md                    domain overview, and the build flow diagram
│   ├── data-pipeline.md              how mcp/*.cs becomes page data
│   ├── templates-and-layouts.md      the Nunjucks layout tree and the pages
│   ├── styling.md                    Tailwind theme, fonts, and code colors
│   ├── client-side-behavior.md       search, copy, download, and toast scripts
│   ├── agent-endpoints.md            the machine-readable copy of the catalog
│   └── build-and-deploy.md           commands, 11ty config, and Cloudflare Pages
├── catalog/                          the C# MCP servers
│   ├── summary.md                    domain overview
│   ├── front-matter-schema.md        metadata fields, defaults, and rules
│   ├── mcp-server-authoring.md       how to write and test a new server
│   ├── automated-testing.md          the test harness and the CI workflow
│   ├── server-inventory.md           the servers that exist now
│   └── windows-window-capture.md     how win-app-screenshots copies window pixels
└── tmp/                              session scraps. Git ignores this directory.
```

## Where to look

| Question | File |
|---|---|
| What is this project? | [summary.md](summary.md) |
| What does a word mean? | [terminology.md](terminology.md) |
| What rules must I follow? | [practices.md](practices.md) |
| How do I add a server to the catalog? | [catalog/mcp-server-authoring.md](catalog/mcp-server-authoring.md) |
| Which metadata fields exist? | [catalog/front-matter-schema.md](catalog/front-matter-schema.md) |
| Which servers exist now? | [catalog/server-inventory.md](catalog/server-inventory.md) |
| How does the screenshot server work? | [catalog/windows-window-capture.md](catalog/windows-window-capture.md) |
| How do the tests work? | [catalog/automated-testing.md](catalog/automated-testing.md) |
| How does a `.cs` file become a web page? | [site/data-pipeline.md](site/data-pipeline.md) |
| Which template must I change? | [site/templates-and-layouts.md](site/templates-and-layouts.md) |
| How do I change a color or a font? | [site/styling.md](site/styling.md) |
| Why does the copy button behave like this? | [site/client-side-behavior.md](site/client-side-behavior.md) |
| How does an agent install a server? | [site/agent-endpoints.md](site/agent-endpoints.md) |
| How do I build and publish? | [site/build-and-deploy.md](site/build-and-deploy.md) |
| What is broken or unfinished? | [plans/backlog.md](plans/backlog.md) |

## Code to lode map

| Code | Lode |
|---|---|
| `mcp/*.cs` | [catalog/](catalog/summary.md) |
| `mcp/win-app-screenshots.cs`, `scripts/Get-AppScreenshot.ps1` | [catalog/windows-window-capture.md](catalog/windows-window-capture.md) |
| `src/_data/servers.js`, `src/_data/serversArray.js` | [site/data-pipeline.md](site/data-pipeline.md) |
| `src/*.njk`, `src/_includes/*.njk` | [site/templates-and-layouts.md](site/templates-and-layouts.md) |
| `src/llms.njk`, `server-md.njk`, `server-raw.njk`, `servers-json.njk`, `agent-install.njk`, `robots.njk`, `sitemap.njk`, `src/_headers` | [site/agent-endpoints.md](site/agent-endpoints.md) |
| `src/_includes/base.njk` (theme part) | [site/styling.md](site/styling.md) |
| `src/_includes/copy-functionality.njk`, `copy-text.njk` | [site/client-side-behavior.md](site/client-side-behavior.md) |
| `.eleventy.js`, `package.json`, `wrangler.toml` | [site/build-and-deploy.md](site/build-and-deploy.md) |
| `.mcp.json` | [catalog/mcp-server-authoring.md](catalog/mcp-server-authoring.md) |
| `test/*`, `scripts/*`, `.github/workflows/mcp-tests.yml` | [catalog/automated-testing.md](catalog/automated-testing.md) |
