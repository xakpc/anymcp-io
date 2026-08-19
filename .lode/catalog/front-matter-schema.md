# Front Matter Schema

The front matter is a YAML block inside C# line comments. It starts with a `// ---` line, and ends with
the next `// ---` line. The parser removes the first two characters of each line, and keeps the
indentation.

```csharp
// ---
// id: my-server
// name: my-server.cs
// description: One short line about the server
// longDescription: A longer paragraph for the detail page
// tags:
//   - category
//   - integration
// status: stable
// version: 1.0.0
// author: Your Name
// license: MIT
// envVars:
//   - API_KEY
// ---
```

## Fields

| Field | Type | Default | Use |
|---|---|---|---|
| `id` | string | file name without `.cs` | page URL `/servers/{id}/`, and the data key |
| `name` | string | file name plus `.cs` | page title, and the suggested file name |
| `description` | string | `"MCP Server"` | card text, and the page meta description |
| `longDescription` | string | the `description` value | first paragraph of the detail page |
| `tags` | list of strings | `[]` | card tags, and search text. A single string becomes a list of one. |
| `status` | `stable`, `beta`, or `alpha` | `stable` | a badge shows only when the status is not `stable` |
| `version` | string | `"1.0.0"` | sidebar |
| `author` | string | `"Unknown"` | sidebar |
| `license` | string | `"MIT"` | sidebar |
| `downloads` | number | `0` | sidebar. It shows only when the value is more than 0. The number is manual. |
| `lastUpdated` | date `YYYY-MM-DD` | today | sidebar |
| `createdDate` | date `YYYY-MM-DD` | `lastUpdated`, or today | sidebar |
| `envVars` | list of strings | `[]` | adds an `env` block to the `.mcp.json` snippet on the page |

The parser adds two more fields that the front matter does not supply:
- `tools` - the list that `extractToolsFromCSharp()` finds. See [data pipeline](../site/data-pipeline.md).
- `code` and `displayCode` - the full text, and the text without the front matter.

## Rules

- Keep `id` unique. A repeated `id` makes one entry overwrite the other, because `id` is the data key.
- Set `name` to `{id}.cs`. The download button finds the server with `filename.replace('.cs', '')`, so
  a different name breaks the download.
- Do not add a field that the table does not list. The parser ignores it.
- Every field is optional. A file with no front matter still becomes a catalog entry.

Related: [Server authoring](mcp-server-authoring.md), [Data pipeline](../site/data-pipeline.md).
