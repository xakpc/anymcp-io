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

## Copy and download

`src/_includes/copy-functionality.njk` gives three global functions:

| Function | Called from | Effect |
|---|---|---|
| `copyServerCode(id, button)` | card buttons, and the small button on the code block | copies the code, and turns the button green for 2 seconds |
| `copyServerCodeLarge(id, button)` | the main button on a detail page | copies the code, and replaces the button classes for 2 seconds |
| `downloadServerFile(filename)` | the download button on a detail page | makes a Blob, and starts a browser download |

Each function reads the full catalog from an inline data dump:

```javascript
const serverData = {{ servers | dump | safe }};
const server = serverData[serverId];
navigator.clipboard.writeText(server.displayCode);
```

Consequences to keep in mind:
- The dump puts the full catalog, code included, in every page that includes this file. The page size
  grows with the catalog.
- The functions always copy `displayCode`, so the front matter never reaches the clipboard.
- `downloadServerFile()` finds the server with `filename.replace('.cs', '')`. This works only when the
  `name` field equals the server id plus `.cs`. See [backlog](../plans/backlog.md).

## Small helpers

- `copy-text.njk` gives `copyText(text)`. The setup page uses it for command examples.
- `toast-system.njk` gives `showToast(message, type)`. No page includes this file at present.

Related: [Templates and layouts](templates-and-layouts.md), [Data pipeline](data-pipeline.md).
