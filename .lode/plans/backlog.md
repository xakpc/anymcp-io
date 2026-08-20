# Backlog

Open items. None is a decision yet. Ask the user before you do the work.

## Dark mode is not reachable

`base.njk` sets `darkMode: 'class'` and holds `.dark` CSS rules, but no control adds the `dark` class.
Add a theme switch, or remove the dead rules.

## `npm run debug` needs a POSIX shell

The script sets `DEBUG=Eleventy*` with POSIX syntax. This fails in PowerShell, the shell of the main
developer. Use `cross-env`, or delete the script.

## Every page carries the code of every server

`copy-functionality.njk` writes `window.serverCode` with the `displayCode` of all servers into each
page. The page size grows with the catalog. `/servers/{id}/{id}.cs` now holds the same text, so the
copy and download functions can fetch that URL, and the map can go away. See
[agent endpoints](../site/agent-endpoints.md).

## The site and the client keep separate server names

The local `.mcp.json` names servers `date-time` and `image-tools`, but the catalog ids are
`date-times-mcp` and `image-utility`. The published instructions use the catalog id. The file is not
in git, so this costs nothing now. Decide if the two must agree.

## Empty directories

`src/assets/` holds no files. Git does not track an empty directory, so it exists only on the local
disk. Decide if the site needs it.

`.gitignore` lists `/test-results/`. The test harness writes no file there, and `dotnet build` keeps
its artifacts in a user level cache, so the line has no use now.

`scripts/`, `test/`, and `.github/workflows/` now hold the test harness. See
[automated testing](../catalog/automated-testing.md).

Related: [Practices](../practices.md), [Site summary](../site/summary.md).
