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

## Dark mode is not reachable

`base.njk` sets `darkMode: 'class'` and holds `.dark` CSS rules, but no control adds the `dark` class.
Add a theme switch, or remove the dead rules.

## `.mcp.json` is incomplete

`.mcp.json` lists four servers, and does not list `xquik`. Decide if the repository must hold an
example configuration for all servers.

## `downloadServerFile()` depends on the name field

The function finds the server with `filename.replace('.cs', '')`. This works only when the `name`
field equals the server id plus `.cs`. A server with a different `name` gives no download. Pass the
server id to the function instead.

## `npm run debug` needs a POSIX shell

The script sets `DEBUG=Eleventy*` with POSIX syntax. This fails in PowerShell, the shell of the main
developer. Use `cross-env`, or delete the script.

## Empty directories

`scripts/`, `test/`, `src/assets/`, and `.github/workflows/` hold no files. Git does not track an empty
directory, so they exist only on the local disk. The git history shows earlier CI work
(`ci: add wrangler setup`, `ci: fix package lock`). Confirm if CI must return.

`.gitignore` also lists `/test-results/`. No test tool writes this directory now.

Related: [Practices](../practices.md), [Site summary](../site/summary.md).
