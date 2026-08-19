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
| `downloadServerFile(filename)` | the download button on a detail page | makes a Blob, and starts a browser download |

Each function reads `window.serverCode`, and stops when the id gives no code:

```javascript
const code = window.serverCode[serverId];
if (!code) return;
navigator.clipboard.writeText(code);
```

A shared helper `restoreButton(button, content, className)` puts the button back to its first look
after 2 seconds. It also clears the inline styles that `copyServerCode()` sets.

`downloadServerFile()` finds the server with `filename.replace('.cs', '')`. This works only when the
`name` field equals the server id plus `.cs`. See [backlog](../plans/backlog.md).

## Small helpers

`copy-text.njk` gives `copyText(text)`. The setup page uses it for command examples.

Related: [Templates and layouts](templates-and-layouts.md), [Data pipeline](data-pipeline.md).
