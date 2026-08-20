# Build and Deploy

## Commands

```bash
npm install          # install dependencies
npm run serve        # development server at http://localhost:8080/
npm start            # alias for serve
npm run build        # production build into _site/
npm run deploy       # build, and then publish to Cloudflare Pages
```

There is no debug script. `DEBUG=Eleventy*` is POSIX syntax, and it fails in PowerShell, the shell
of the main developer. Set the variable first:

```powershell
$env:DEBUG = "Eleventy*"; npx eleventy
```

## Eleventy configuration

`.eleventy.js` uses ES module syntax, because `package.json` sets `"type": "module"`.

```javascript
export default function(eleventyConfig) {
  eleventyConfig.addPassthroughCopy("src/_headers");
  // filters: date, localeString, wireName, lineCount, agentPrompt, serversManifest
  return {
    templateFormats: ["md", "njk", "html", "liquid"],
    dir: { input: "src", includes: "_includes", data: "_data", output: "_site" },
    htmlTemplateEngine: "njk",
    markdownTemplateEngine: "njk"
  };
};
```

Filters:
- `date(value, format)` - returns `M/D/YYYY` for that exact format string. If not, it returns the
  locale date. It returns an empty string for a false value.
- `localeString(number)` - adds thousands separators. It returns an empty string for `0`.
- `wireName(name)` - returns the snake case form of a C# method name, which is the name that a
  `tools/list` response holds. The module level `toWireName()` holds the rule, and `serversManifest`
  calls that same function, so the manifest and the pages can never disagree.
- `lineCount(text)` - returns the number of lines of a string. A server page shows the first 20
  lines of the code, and the label of the button that opens the rest needs the total. See
  [client-side behavior](client-side-behavior.md).
- `agentPrompt(id, siteUrl)` - returns the one line that the **Copy Prompt** button puts on the
  clipboard. A card and a detail page both write that button, so the text lives here and the two
  cannot drift. See [client-side behavior](client-side-behavior.md).
- `serversManifest(serversArray, siteUrl)` - returns the full JSON body of `/servers.json`. It drops
  `code` and `displayCode`, makes each URL absolute, and adds a `run` block with the correct argv.

`wireName` and `serversManifest` serve the machine-readable endpoints. See
[agent endpoints](agent-endpoints.md).

The site has no `src/assets/` directory, and no passthrough rule for one. Add both together when a
static file arrives. A rule for a path that does not exist copies nothing and raises no error, so
such a rule is dead weight, and not a build failure.

`src/_headers` goes to `_site/_headers`. Eleventy removes the input directory from the path, and
Cloudflare Pages reads the file from the root of the build output. The file gives
`text/plain; charset=utf-8` to the `.md` and `.cs` outputs. Without a rule, a `.cs` file has no
entry in the mime table and becomes `application/octet-stream`.

## Dependencies

| Package | Use |
|---|---|
| `@11ty/eleventy` ^3.1.6 | static site generator |
| `yaml` ^2.9.0 | parses the front matter in `servers.js` |
| `wrangler` ^4.124.0 (dev) | Cloudflare Pages deployment |

## Hosting

Cloudflare Pages serves the site. `wrangler.toml` names the project:

```toml
name = "anymcp-io"
pages_build_output_dir = "_site"
```

The public domain is `https://anymcp.net` (`src/_data/site.json`). The Plausible analytics script in
`base.njk` identifies the site with a script file name that Plausible supplies
(`https://plausible.io/js/pa-bNgyo1A6RpizKemf0zkKa.js`), and not with a `data-domain` attribute. Keep
the file name as Plausible gives it, or the site records no data. The Cloudflare project name `anymcp-io` and
the GitHub repository name `anymcp-io` keep the old spelling. They are identifiers, and not the domain.

The output is fully static, so any static host can serve `_site/`.

Related: [Site summary](summary.md).
