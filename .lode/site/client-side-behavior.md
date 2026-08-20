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
- Anywhere else, the published file at `/servers/{id}/{name}` is fetched. That file holds the front
  matter, so `stripFrontMatter()` removes it.

**Only a detail page copies code today, so the fetch branch has no caller.** A catalog card gives
**Copy Prompt** and **View**, and the three code functions all live on a detail page, where the
first branch answers. Keep the branch: it is the only thing that makes `copyServerCode()` correct
on a page that does not show the code, and a new such page is one card away.

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

The same file gives five global functions. Each one takes the button as its last argument, because
the functions are `async` and the implicit `event` global is gone when the promise resolves.

| Function | Called from | Effect |
|---|---|---|
| `copyServerCode(id, file, button)` | the small button on the code block | copies the code, and turns the button green for 2 seconds |
| `copyServerCodeLarge(id, file, button)` | the main button on a detail page | copies the code, and replaces the button classes for 2 seconds |
| `downloadServerFile(id, file, button)` | the download button on a detail page | makes a Blob of the same text, and starts a browser download |
| `copyPrompt(button)` | the **Copy Prompt** button on a card, and **Copy Agent Prompt** on a detail page | copies the text in `data-prompt` |
| `toggleServerCode(button)` | the **Show all N lines** button on a detail page | opens and closes the code block |

A shared helper `restoreButton(button, content, className)` puts the button back to its first look
after 2 seconds. It also clears the inline styles that `copyServerCode()` sets.

A download gives the same text as a copy: the front matter is removed. An agent that wants the
catalog file whole reads `/servers/{id}/{name}`. See [agent endpoints](agent-endpoints.md).

## The agent prompt

The prompt is **the main action of the site**. It is the primary button of a catalog card, labelled
**Copy Prompt**, and the first button of a detail page, labelled **Copy Agent Prompt**. A person
pastes the line into a coding agent, and the agent reads the `.md` page and installs the server.
Copying the source code is the second choice, and it is on the detail page only.

```html
<button onclick="copyPrompt(this)" data-prompt="{{ server.id | agentPrompt(site.url) }}">

<!-- expands to: -->
<button onclick="copyPrompt(this)" data-prompt="Read https://anymcp.net/servers/xquik/index.md and install the xquik MCP server into this project.">
```

**The text comes from the `agentPrompt` filter in `.eleventy.js`, and not from a template.** Two
templates write the button, `src/index.njk` and `src/servers.njk`. A sentence in two templates
becomes two different sentences, so the filter holds it and each template calls
`{{ id | agentPrompt(site.url) }}`. `test/site.test.js` compares the two pages against the same
expected text.

**The prompt is a pointer, and not a procedure.** It gives the task and the URL, and nothing more.
Each rule for the agent is in `/servers/{id}/index.md`, in the `## Rules` list that
`src/server-md.njk` writes: obey the page, install into the current project, ask which client the
user has, name each file before you change it, and stay in the root directory. The agent reads that
page before it writes a file, so a rule in two places is only a rule to change two times. See
[agent endpoints](agent-endpoints.md).

The text is in a `data-prompt` attribute, and not in the `onclick` attribute. `copyPrompt()` reads
`button.dataset.prompt`. Invariant: the template writes `data-prompt` with no `| safe` filter.
Nunjucks escapes the value, so a quote in the text cannot break the markup, and `dataset` gives
the original text back.

## The code block opens and closes

A detail page shows the first 20 lines of the server file. `win-app-screenshots.cs` is 755 lines
without its front matter, and the whole file pushes "How to Run" off the screen.

```html
<div id="server-code-wrap" data-open="false" style="max-height: 28rem; overflow: hidden;">
  <pre><code id="server-code" data-server-id="xquik">...</code></pre>
  <div id="server-code-fade" ...></div>
</div>
<button id="server-code-toggle" onclick="toggleServerCode(this)"
        data-lines="131" data-collapsed="28rem">Show all 131 lines</button>
```

**The clip is a `max-height`, and never `display: none`.** The whole file stays in the DOM in both
states, so `serverCodeFor()` reads `#server-code.textContent` and gives the complete source while
the block is closed. A `<details>` element or a `hidden` attribute would empty the clipboard, the
download, and the test that compares the block with `displayCode`.

Three more rules hold this block together:

- **The fade comes after `</pre>`, and never inside `<code>`.** `test/site.test.js` takes the block
  text from `id="server-code"` to the first `</code>`. An element inside the code element enters
  that text and the comparison fails.
- **28rem is 20 lines.** `base.njk` sets `font-size: 0.875rem`, `line-height: 1.5`, and
  `padding: 1.5rem` on `pre[class*="language-"]`. A change to any of the three moves the clip.
- **The `lineCount` filter in `.eleventy.js` gives the number in the label.** The template hides the
  button and the fade when the file is 28 lines or shorter, because a clip that cuts 2 lines is
  only a second click.

The fade is a `linear-gradient` to `#011627`, which is the Night Owl background that `base.njk`
sets. A change to that background needs the same change here.

## Small helpers

`copy-text.njk` gives `copyBlock(button)`. The setup page uses it for its command examples. The
function finds the `pre code` element in the container of the button, and copies its `textContent`.
The snippet therefore exists one time, in the block that the reader sees. An earlier version took the
text as an argument, which put a second, escaped copy of each snippet in the `onclick` attribute.

Related: [Templates and layouts](templates-and-layouts.md), [Data pipeline](data-pipeline.md).
