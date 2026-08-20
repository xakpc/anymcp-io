# Build and Deploy

## Commands

```bash
npm install          # install dependencies
npm run serve        # development server at http://localhost:8080/
npm start            # alias for serve
npm run build        # production build into _site/
npm run debug        # build with verbose Eleventy logs
npm run deploy       # build, and then publish to Cloudflare Pages
```

`npm run debug` sets `DEBUG=Eleventy*`. This syntax works in a POSIX shell. In PowerShell, set the
variable first:

```powershell
$env:DEBUG = "Eleventy*"; npx eleventy
```

## Eleventy configuration

`.eleventy.js` uses ES module syntax, because `package.json` sets `"type": "module"`.

```javascript
export default function(eleventyConfig) {
  eleventyConfig.addPassthroughCopy("src/assets");
  eleventyConfig.addPassthroughCopy("src/_headers");
  // filters: date, localeString, displayCodeMap, wireName, serversManifest
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
- `displayCodeMap(servers)` - returns a JSON string that maps each server id to its `displayCode`.
  The copy scripts use it. See [client-side behavior](client-side-behavior.md).
- `wireName(name)` - returns the snake case form of a C# method name, which is the name that a
  `tools/list` response holds.
- `serversManifest(serversArray, siteUrl)` - returns the full JSON body of `/servers.json`. It drops
  `code` and `displayCode`, makes each URL absolute, and adds a `run` block with the correct argv.

Both new filters serve the machine-readable endpoints. See [agent endpoints](agent-endpoints.md).

Note: `src/assets/` is empty at present. The passthrough rule is ready for future static files.

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
`base.njk` must use the same domain, or it records no data. The Cloudflare project name `anymcp-io` and
the GitHub repository name `anymcp-io` keep the old spelling. They are identifiers, and not the domain.

The output is fully static, so any static host can serve `_site/`.

Related: [Site summary](summary.md).
