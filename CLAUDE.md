# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is AnyMCP, a static site catalog of single-file MCP (Model Context Protocol) servers for .NET 10 Preview 4. The site is built with Eleventy (11ty) and serves as a directory where users can browse, search, and copy MCP servers that work with any LLM supporting local connections.

## Essential Commands

```bash
# Development server (serves at http://localhost:8080/)
npm run serve
npm start  # alias for serve

# Production build (outputs to _site/)
npm run build

# Debug build with verbose logging
npm run debug
```

## Dependencies

- **@11ty/eleventy**: Static site generator
- **yaml**: YAML parser for C# front matter extraction

## Architecture & Data Flow

### Core Architecture
The site uses 11ty's static generation with a C# file-driven approach:

1. **Data Layer**: Server definitions live as `.cs` files in `src/_data/mcp/` directory
2. **Parse Layer**: `src/_data/servers.js` discovers and parses C# files with YAML front matter
3. **Transform Layer**: `src/_data/serversArray.js` converts the servers object into an array for pagination
4. **Template Layer**: Nunjucks templates generate static HTML from the parsed data
5. **Pagination**: Individual server pages are auto-generated via 11ty pagination

### Key Patterns

**Data Structure**: Each C# server file follows this front matter schema:
```yaml
# In C# comments between // --- markers
id: server-id
name: server-name.cs
description: Brief description
longDescription: Optional detailed description
tags:
  - category
  - integration
status: stable|beta|alpha
downloads: 1250
lastUpdated: 2024-01-15
version: 1.0.0
author: Author Name
license: MIT
envVars:
  - ENV_VAR_NAME
  - ANOTHER_VAR
```

**Pagination System**: The `servers.njk` template uses 11ty pagination to create individual pages at `/servers/{server-id}/` for each server. The pagination pulls from `serversArray` (transformed data) rather than the raw `servers` object.

**Layout Hierarchy**:
- `base.njk` - HTML document shell with Tailwind CSS via CDN
- `page.njk` - Common page layout with header/navigation 
- Individual page templates (`index.njk`, `servers.njk`, `setup.njk`) extend `page.njk`

### Styling Strategy
- Tailwind CSS loaded via CDN in `base.njk` with custom color variables
- Inline Tailwind config in the template defines the design system colors
- No separate CSS files - all styling is utility-first in templates

### Search & Interaction
- Client-side search implemented in vanilla JS on homepage
- Server cards include searchable metadata via data attributes
- Copy-to-clipboard functionality for server code

## Adding New Servers

1. Create a `.cs` file in `src/_data/mcp/`
2. Add YAML front matter in C# comments at the top:
   ```csharp
   // ---
   // id: server-id
   // name: server-name.cs
   // description: Brief description
   // tags:
   //     - tag1
   //     - tag2
   // version: 1.0.0
   // author: Author Name
   // license: MIT
   // envVars:
   //     - ENV_VAR_NAME
   // ---
   ```
3. Write your MCP server code below the front matter
4. Build automatically generates the server page and includes it in the catalog

## C# Server Parser

The custom parser (`parseCSharpMcpServer`) extracts metadata from C# comment-based YAML front matter:

- **Front Matter**: YAML between `// ---` markers in C# comments
- **Auto-discovery**: Any `.cs` file in `src/_data/mcp/` becomes a server
- **Fallbacks**: Missing metadata defaults to filename, "stable" status, etc.
- **Data Flow**: `servers.js` scans mcp directory → `serversArray.js` converts to array for pagination

## Modifying Templates

- **Homepage catalog**: Edit `src/index.njk`
- **Server detail pages**: Edit `src/servers.njk` (affects all server pages)
- **Server cards**: Edit `src/_includes/server-card.njk`
- **Site-wide data**: Edit `src/_data/site.json`

## 11ty Configuration Notes

The `.eleventy.js` config uses ES modules (`export default`) due to `"type": "module"` in package.json. Key customizations:

- Custom `date` and `localeString` filters for template rendering
- Input directory: `src/`, output: `_site/`
- Template formats: Nunjucks (`.njk`), Markdown, HTML, Liquid
- Static assets copied from `src/assets/` and `public/`

## Content Management

All content is file-based - no CMS or database. The site rebuilds entirely on each change, making it suitable for static hosting (Netlify, Vercel, GitHub Pages, etc.).