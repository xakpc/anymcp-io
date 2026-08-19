# Backlog

Open items. None is a decision yet. Ask the user before you do the work.

## Broken links to a directory that moved

Two templates point to `src/_data/mcp/`. The catalog moved to `mcp/` at the repository root, so both
links give a GitHub 404.

- `src/servers.njk:63` - the "View on GitHub" button:
  `https://github.com/xakpc/anymcp-io/blob/main/src/_data/mcp/{{ serverData.name }}`
  Correct target: `{{ site.github }}/blob/main/mcp/{{ serverData.name }}`
- `src/_includes/page.njk:46` - the "Submit" button:
  `{{ site.github }}/blob/main/src/_data/mcp/CONTRIBUTING.md`
  Correct target: `{{ site.github }}/blob/main/CONTRIBUTING.md`

The `servers.njk` link also writes the repository URL in full, but `site.github` holds the same value.

## Duplicate card markup

`src/_includes/server-card.njk` is a card component, but no template includes it. `src/index.njk` holds
its own copy of the markup. The two copies are not identical: the `index.njk` copy adds
`flex flex-col h-full`. Choose one: use the component in `index.njk`, or delete the component.

## Unused toast system

`src/_includes/toast-system.njk` gives `showToast()`, but no page includes it. The copy buttons give
their feedback with an inline button state instead. Delete the file, or connect it.

## Dark mode is not reachable

`base.njk` sets `darkMode: 'class'` and holds `.dark` CSS rules, but no control adds the `dark` class.
Add a theme switch, or remove the dead rules.

## `.mcp.json` is incomplete

`.mcp.json` lists four servers, and does not list `xquik`. The file is not in git. Decide if the
repository must hold an example configuration for all servers.

## Full catalog in every page

`copy-functionality.njk` writes `{{ servers | dump | safe }}` three times in each page that includes it.
Each page therefore carries the source of every server, three times. The catalog is small now. When it
grows, put the code in a data attribute on the button, or fetch a JSON file.

## Empty directories

`scripts/`, `test/`, and `.github/workflows/` hold no files. The git history shows earlier CI work
(`ci: add wrangler setup`, `ci: fix package lock`). Confirm if CI must return.

Related: [Practices](../practices.md), [Site summary](../site/summary.md).
