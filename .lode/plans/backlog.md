# Backlog

Open items. None is a decision yet. Ask the user before you do the work.

## `npm test` fails at home for a connected server

The build layer cannot rebuild a server that the local LLM client holds open; see
[automated testing](../catalog/automated-testing.md). Today the fix is to close the client. The
harness could instead see the MSB3026 lock warnings and say what to do. Decide if that is worth the
code.

## The lode still describes the inside of a guest server

[Practices](../practices.md) says the lode covers XAKPC Dev Labs servers only. Two lines in
[server inventory](../catalog/server-inventory.md) break that rule: they call `xquik` "the model to
follow for a new API-backed server", and they name its `XquikApiResult` record. An API-backed server
of this project must carry that pattern instead. Decide whether to cut the two lines now, or to keep
them until such a server exists.

## No server carries a date

The parser invents no date any more, and no `.cs` file sets `lastUpdated` or `createdDate`, so the
cards show no date and the sitemap holds no `<lastmod>`. Decide if the front matter of each server
must carry a real date, or if the field goes away.

## A dark theme does not exist

The site has one light theme. `darkMode`, the `.dark` rules, and the last `dark:` utility class are
all gone. A dark theme now needs a switch, a palette, and the classes to go with it. It is a
feature, and not a repair. See [styling](../site/styling.md).

Related: [Practices](../practices.md), [Site summary](../site/summary.md).
