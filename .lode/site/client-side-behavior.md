# Client-Side Behavior

All interaction uses plain JavaScript. There is no framework and no bundler.

## Search

The script is at the end of `src/index.njk`. It filters the cards that are already in the HTML.

```javascript
const serverData = card.dataset.server.toLowerCase();
const shouldShow = searchTerm === '' || serverData.includes(searchTerm);
card.style.display = shouldShow ? '' : 'none';
```

The `data-server` attribute holds the name, the description, and the tags of the server. The match is a
simple substring test. When no card matches, the script hides `#servers-grid`, and shows `#no-results`.

## The shared code map

`src/_includes/copy-functionality.njk` writes one global object at the top of its script:

```javascript
window.serverCode = {{ servers | displayCodeMap | safe }};
```

The `displayCodeMap` filter is in `.eleventy.js`. It maps each server id to its `displayCode`, and
drops all other fields. See [build and deploy](build-and-deploy.md).

Invariants:
- The map holds `displayCode` only. The front matter never reaches the browser or the clipboard.
- The map is written one time for each page. All functions below read it.
- The map is inline JSON in a `<script>` block. A server file that holds the text `</script>` breaks
  the page. No catalog file holds this text now.

The page size grows with the catalog, because each page holds the full map. The catalog is small. When
it grows, put the code in a data attribute on the button, or fetch a JSON file.

## Copy and download

The same file gives three global functions:

| Function | Called from | Effect |
|---|---|---|
| `copyServerCode(id, button)` | card buttons, and the small button on the code block | copies the code, and turns the button green for 2 seconds |
| `copyServerCodeLarge(id, button)` | the main button on a detail page | copies the code, and replaces the button classes for 2 seconds |
| `downloadServerFile(id, filename)` | the download button on a detail page | makes a Blob, and starts a browser download |
| `copyPrompt(button)` | the agent-prompt button on a detail page | copies the text in `data-prompt` |

Each function reads `window.serverCode`, and stops when the id gives no code:

```javascript
const code = window.serverCode[serverId];
if (!code) return;
navigator.clipboard.writeText(code);
```

A shared helper `restoreButton(button, content, className)` puts the button back to its first look
after 2 seconds. It also clears the inline styles that `copyServerCode()` sets.

`downloadServerFile()` takes the server id, so it does not depend on the value of the `name` field.
The same file is also at `/servers/{id}/{id}.cs`. See [agent endpoints](agent-endpoints.md).

## The agent prompt

The **Copy Agent Prompt** button gives a person one line to paste into a coding agent. The line names
the `.md` page of the server, and the agent reads it and installs the server.

The text is in a `data-prompt` attribute, and not in the `onclick` attribute:

```html
<button onclick="copyPrompt(this)" data-prompt="Install the xquik MCP server ...">
```

`copyPrompt()` reads `button.dataset.prompt`. Invariant: the template writes `data-prompt` with no
`| safe` filter. Nunjucks escapes the value, so a quote in the text cannot break the markup, and
`dataset` gives the original text back.

## Small helpers

`copy-text.njk` gives `copyBlock(button)`. The setup page uses it for its command examples. The
function finds the `pre code` element in the container of the button, and copies its `textContent`.
The snippet therefore exists one time, in the block that the reader sees. An earlier version took the
text as an argument, which put a second, escaped copy of each snippet in the `onclick` attribute.

Related: [Templates and layouts](templates-and-layouts.md), [Data pipeline](data-pipeline.md).
