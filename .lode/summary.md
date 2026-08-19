# AnyMCP - Project Summary

AnyMCP is a static web catalog of single-file MCP (Model Context Protocol) servers written in C# for
.NET 10 Preview 4. Each catalog entry is one `.cs` file in the `mcp/` directory. The file holds YAML
front matter in C# comments, and the MCP server code below it. Eleventy (11ty) reads these files at
build time, and makes the home page, one detail page for each server, and a setup guide. Users copy
the code, save it as a `.cs` file, and run it with `dotnet run`. The servers operate with any LLM
client that supports local stdio MCP connections. The site has no database and no CMS. Cloudflare
Pages hosts the built site at https://anymcp.net.

Related lodes:
- [Terminology](terminology.md)
- [Practices](practices.md)
- [Lode map](lode-map.md)
- [Site domain](site/summary.md)
- [Catalog domain](catalog/summary.md)
