# Backlog

Open items. None is a decision yet. Ask the user before you do the work.

## The site and the client keep separate server names

The local `.mcp.json` names servers `date-time` and `image-tools`, but the catalog ids are
`date-times-mcp` and `image-utility`. The published instructions use the catalog id. The file is not
in git, so this costs nothing now. Decide if the two must agree.

## `scripts/Get-AppScreenshot.ps1` is not in git

The PowerShell script that `win-app-screenshots.cs` comes from is on the local disk only. Two lode
files point at it: [lode map](../lode-map.md) and
[windows window capture](../catalog/windows-window-capture.md). Commit it, or delete it and remove
both pointers.

## `npm test` fails at home for a connected server

The build layer cannot rebuild a server that the local LLM client holds open; see
[automated testing](../catalog/automated-testing.md). Today the fix is to close the client. The
harness could instead see the MSB3026 lock warnings and say what to do. Decide if that is worth the
code.

## No server carries a date

The parser invents no date any more, and no `.cs` file sets `lastUpdated` or `createdDate`, so the
cards show no date and the sitemap holds no `<lastmod>`. Decide if the front matter of each server
must carry a real date, or if the field goes away.

## A dark theme does not exist

The site has one light theme. `darkMode`, the `.dark` rules, and the last `dark:` utility class are
all gone. A dark theme now needs a switch, a palette, and the classes to go with it. It is a
feature, and not a repair. See [styling](../site/styling.md).

Related: [Practices](../practices.md), [Site summary](../site/summary.md).
