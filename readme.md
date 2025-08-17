# AnyMCP - MCP Server Catalog

A static site catalog of single-file MCP (Model Context Protocol) servers for .NET 10 (Preview 4+). These servers written with C# and works with any LLM that supports local MCP connections.

## Contributing

Want to add your MCP server to the catalog? See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed instructions.


## Adding New Servers

Create a `.cs` file in the `mcp/` directory with YAML front matter in comments:

```csharp
// ---
// id: my-server
// name: my-server.cs
// description: What this server does
// tags:
//     - category
//     - integration
// version: 1.0.0
// author: Your Name
// license: MIT
// envVars:
//     - API_KEY
//     - BASE_URL
// ---
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.3

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

// Your MCP server code here...
```

The site automatically discovers C# files and generates individual pages for each server.

## Catalogue Features

- **Static Site Generation**: Built with [Eleventy (11ty)](https://www.11ty.dev/)
- **Modern Design**: Responsive design with Tailwind CSS
- **Server Catalog**: Browse MCP servers with search and filtering
- **Individual Server Pages**: Detailed pages with code, documentation, and setup instructions
- **Setup Guide**: Step-by-step instructions for getting started

## Catalogue Development

```bash
# Install dependencies
npm install

# Start development server
npm run serve

# Build for production
npm run build
```

The site will be available at `http://localhost:8080/`

## Project Structure

```
mcp/                      # MCP server files with YAML front matter
├── server1.cs           # Individual C# MCP servers
├── server2.cs
└── ...
src/
├── _data/               # Data files and parsers
│   ├── servers.js       # Parser for C# files  
│   ├── serversArray.js  # Array converter for pagination
│   └── site.json        # Site metadata
├── _includes/           # Layout templates and components
│   ├── base.njk        # Base HTML layout
│   ├── page.njk        # Page layout with header/footer
│   └── server-card.njk # Server card component
├── index.njk           # Homepage template
├── servers.njk         # Server detail pages (paginated)
└── setup.njk          # Setup guide page
```
