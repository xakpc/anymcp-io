# Server Inventory

Five servers are in `mcp/` now. All use `Microsoft.Extensions.Hosting@9.0.8` and
`ModelContextProtocol@0.3.0-preview.3`, and all set `PublishAot=false`.

| id | Purpose | Tags | Author | Env vars |
|---|---|---|---|---|
| `date-times-mcp` | date and time values, date arithmetic, and formatting | datetime, utilities, formatting, calculations | XAKPC Dev Labs | none |
| `generate-password` | makes secure passwords. The length and the complexity are options. | password, security, utilities | XAKPC Dev Labs | none |
| `image-utility` | resizes, converts, and compresses images. It also removes metadata, and makes thumbnails. | images, media, utilities | XAKPC Dev Labs | none |
| `text-format-tools` | slugify, UUID, hashing, Base64 and URL codecs, case transforms, and dedent | text, utilities | XAKPC Dev Labs | none |
| `xquik` | searches and inspects public X posts through the Xquik REST API | api, social, web | Xquik | `XQUIK_API_KEY` |

Notes:
- `xquik` is the only server that calls an external API, and the only server with an env var. It is the
  model to follow for a new API-backed server. It defines a `record XquikApiResult(int StatusCode,
  bool IsSuccess, JsonNode? Body)` to carry an HTTP result to the tool methods.
- Only `date-times-mcp` and `xquik` declare `status: stable` in the front matter. The other three get
  `stable` from the parser default.
- Only `xquik` declares a `name` field. The other four get the name from the file name.
- `.mcp.json` lists four servers, and does not list `xquik`.

Related: [Server authoring](mcp-server-authoring.md), [Front matter schema](front-matter-schema.md).
