# Templates and Layouts

All templates use Nunjucks. Layouts are in `src/_includes/`. Pages are in `src/`.

```mermaid
flowchart TD
    A["base.njk<br/>HTML shell, head, Tailwind config, styles"] --> B["page.njk<br/>header, logo, badges, nav"]
    B --> C["index.njk<br/>/"]
    B --> D["servers.njk<br/>/servers/{id}/"]
    B --> E["setup.njk<br/>/setup/"]
    C --> F["footer.njk"]
    C --> G["copy-functionality.njk"]
    D --> F
    D --> G
    E --> F
    E --> H["copy-text.njk"]
```

Seven more templates make the machine-readable output. They use no layout, because their output is
not HTML: `llms.njk`, `agent-install.njk`, `server-md.njk`, `server-raw.njk`, `servers-json.njk`,
`robots.njk`, and `sitemap.njk`. See [agent endpoints](agent-endpoints.md).

## base.njk

`base.njk` holds the full HTML document. It sets the title as `{{ title }} - {{ site.title }}`. It
loads the fonts, Tailwind, Plausible analytics, and Prism.js from CDNs. It holds the Tailwind theme
configuration and all custom CSS. See [styling](styling.md).

## page.njk

`page.njk` adds the sticky header. Two page variables control the header:

- `showBadges` - shows the version badges from `site.badges`, and the GitHub star button.
- `showNavigation` - shows the "Setup" link and the "Submit" button.

Only the home page sets both variables to `true`.

## index.njk

`index.njk` shows the hero section, the search box, and the card grid. The template writes the card
markup inline in a `{% for server in serversArray %}` loop. Each card carries the search index in a
data attribute:

```html
<div ... data-server="{{ server.name }} {{ server.description }} {{ server.tags | join(' ') }}">
```

## servers.njk

`servers.njk` makes one page for each server through 11ty pagination:

```yaml
pagination:
  data: serversArray
  size: 1
  alias: serverData
permalink: "/servers/{{ serverData.id }}/"
eleventyComputed:
  title: "{{ serverData.name }}"
  description: "{{ serverData.description }}"
```

`servers.njk` also sets `agentMarkdown` in `eleventyComputed`. `base.njk` reads it, and writes a
`link rel="alternate"` that points at the markdown copy of the page.

The page shows the description, a status badge when the status is not `stable`, the tags, the tool
list, the source code with copy, download, and agent-prompt buttons, a ready `.mcp.json` snippet, and
a metadata sidebar. The `.mcp.json` snippet adds an `env` block when `serverData.envVars` is not
empty.

The snippet uses the `mcpServers` key, which Claude Code, Claude Desktop, Cursor, and Windsurf read.
A note says that Visual Studio and VS Code use `servers`. The `args` array ends with `"-v"` and
`"q"`, and each snippet on the site must keep them.

Invariant: the page prints `serverData.displayCode`, and not `serverData.code`. The front matter must
not appear on the page or in the clipboard. The raw endpoint `/servers/{id}/{id}.cs` is different: it
prints `serverData.code`, byte for byte, and `test/site.test.js` compares a hash.

## setup.njk

`setup.njk` is a static guide. It explains how to install .NET 10, how to save a server file,
and how to configure the LLM client. It uses `copy-text.njk` for its command examples.

## Shared components

- `footer.njk` - the site footer. All three pages include it.
- `copy-functionality.njk` - copy and download scripts. See [client-side behavior](client-side-behavior.md).
- `copy-text.njk` - a `copyBlock(button)` helper for the setup page. It copies the code block that
  the button belongs to, so each snippet exists one time.

Related: [Data pipeline](data-pipeline.md), [Styling](styling.md).
