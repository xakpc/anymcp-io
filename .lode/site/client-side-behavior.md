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

## Where the code comes from

No page holds the code of another server. `src/_includes/copy-functionality.njk` reads the code from
one of two places, in `serverCodeFor(serverId, fileName)`:

```javascript
const block = document.getElementById('server-code');
if (block && block.dataset.serverId === serverId) return block.textContent;

const response = await fetch('/servers/' + serverId + '/' + fileName);
return stripFrontMatter(await response.text());
```

- On a detail page the code is already on the screen, in
  `<code id="server-code" data-server-id="{id}">`. The buttons read that block, so the clipboard and
  the page can never disagree, and nothing is fetched.
- Anywhere else, for example a card on the home page, the published file at `/servers/{id}/{name}` is
  fetched. That file holds the front matter, so `stripFrontMatter()` removes it.

Invariants:
- The code block carries **no** `| safe` filter. The code holds `<` characters, in generics and in
  `/// <summary>` comments. Raw output makes the browser read them as tags: the text leaves the page,
  and `textContent` gives the copy buttons a file that does not compile. `test/site.test.js` decodes
  the block of every server and compares it to `displayCode`.
- Nunjucks escapes a backslash to `&#92;`, as well as the five usual characters. A browser decodes
  all six, so `textContent` gives the source back. A test that reads the built HTML must decode all
  six too.
- `stripFrontMatter()` drops every line through the second `// ---` line, and then trims. This is the
  rule of `extractCodeWithoutFrontMatter` in `src/_data/servers.js`. The two must stay the same, or a
  copy from a card and a copy from a detail page differ.
- The front matter never reaches the clipboard, from either source.

## Copy and download

The same file gives four global functions. Each one takes the button as its last argument, because
the functions are `async` and the implicit `event` global is gone when the promise resolves.

| Function | Called from | Effect |
|---|---|---|
| `copyServerCode(id, file, button)` | card buttons, and the small button on the code block | copies the code, and turns the button green for 2 seconds |
| `copyServerCodeLarge(id, file, button)` | the main button on a detail page | copies the code, and replaces the button classes for 2 seconds |
| `downloadServerFile(id, file, button)` | the download button on a detail page | makes a Blob of the same text, and starts a browser download |
| `copyPrompt(button)` | the agent-prompt button on a detail page | copies the text in `data-prompt` |

A shared helper `restoreButton(button, content, className)` puts the button back to its first look
after 2 seconds. It also clears the inline styles that `copyServerCode()` sets.

A download gives the same text as a copy: the front matter is removed. An agent that wants the
catalog file whole reads `/servers/{id}/{name}`. See [agent endpoints](agent-endpoints.md).

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
